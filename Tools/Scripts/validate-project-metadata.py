"""Read-only project metadata checks. Never execute or repair entrypoints."""
import argparse
import json
import os
from pathlib import Path
import re
import stat
import sys

CONFIG_PATH = "Tools/Configuration/metadata-validation.json"

class ValidationError(Exception):
    def __init__(self, code, detail=""):
        self.code, self.detail = code, str(detail)[:512]
        super().__init__(code)

def fail(code, detail=""):
    raise ValidationError(code, detail)

def text(value, code):
    if not isinstance(value, str) or not value.strip():
        fail(code, "Nonempty text required")
    return value

def no_links(path):
    for item in (path, *path.parents):
        try:
            info = item.lstat()
        except FileNotFoundError:
            fail("MISSING_PATH", str(item))
        if stat.S_ISLNK(info.st_mode) or getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
            fail("LINK_REJECTED", str(item))

def project_root(value):
    if not isinstance(value, str) or not value or value.startswith(("\\\\", "//")):
        fail("INVALID_ROOT")
    root = Path(value)
    if not root.is_absolute() or root.parent == root:
        fail("INVALID_ROOT")
    try:
        no_links(root)
    except ValidationError as error:
        if error.code == "MISSING_PATH":
            fail("INVALID_ROOT", value)
        raise
    if not root.is_dir():
        fail("INVALID_ROOT", value)
    return root.resolve()

def child(root, relative):
    text(relative, "INVALID_PATH")
    canonical = relative.replace("\\", "/")
    parts = canonical.split("/")
    if ":" in canonical or "\x00" in canonical or any(p in ("", ".", "..") or p.endswith((".", " ")) for p in parts) or Path(relative).is_absolute():
        fail("INVALID_PATH", relative)
    result = root.joinpath(*parts)
    no_links(result)
    return result

def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            fail("DUPLICATE_KEY", key)
        result[key] = value
    return result

def check_depth(value, maximum):
    stack = [(value, 1)]
    while stack:
        node, depth = stack.pop()
        if depth > maximum:
            fail("JSON_DEPTH")
        if isinstance(node, dict):
            stack.extend((v, depth+1) for v in node.values())
        elif isinstance(node, list):
            stack.extend((v, depth+1) for v in node)

class Reader:
    def __init__(self, root):
        self.root, self.read_files = root, set()

    def bytes(self, relative, maximum):
        p = child(self.root, relative)
        if not p.is_file():
            fail("MISSING_PATH", relative)
        if p.stat().st_size > maximum:
            fail("FILE_TOO_LARGE", relative)
        with p.open("rb") as stream:
            data = stream.read(maximum+1)
        if len(data) > maximum:
            fail("FILE_TOO_LARGE", relative)
        self.read_files.add(relative)
        return data

    def json(self, relative, maximum, depth):
        try:
            value = json.loads(self.bytes(relative, maximum).decode("utf-8-sig"),
                               object_pairs_hook=unique_object,
                               parse_constant=lambda v: fail("INVALID_JSON", v))
        except (ValueError, UnicodeError, RecursionError) as error:
            fail("INVALID_JSON", str(error))
        check_depth(value, depth)
        return value

def fields(value, names, code):
    if not isinstance(value, dict) or set(value) != set(names):
        fail(code, "Unexpected fields")
    return value

def configuration(reader):
    c = reader.json(CONFIG_PATH, 65536, 16)
    fields(c, ("schema_version","dataset_id","description","policy_path","limits","catalogs"), "INVALID_CONFIG")
    if type(c["schema_version"]) is not int or c["schema_version"] != 1 or c["dataset_id"] != "workflow.metadata_validation":
        fail("INVALID_CONFIG", "Unsupported profile")
    text(c["description"],"INVALID_CONFIG"); text(c["policy_path"],"INVALID_CONFIG")
    caps = {"max_json_bytes":16777216,"max_source_bytes":16777216,"max_depth":128,"max_descriptors":4096,"max_catalog_entries":4096}
    limits = fields(c["limits"], caps, "INVALID_CONFIG")
    for key, value in limits.items():
        if type(value) is not int or not 1 <= value <= caps[key]:
            fail("INVALID_CONFIG", key)
    groups = c["catalogs"]
    if not isinstance(groups,list) or len(groups) != 3:
        fail("INVALID_CONFIG", "Three groups required")
    for g in groups:
        fields(g, ("path","directory","kind","id_field","status_mode"), "INVALID_CONFIG")
        for value in g.values(): text(value,"INVALID_CONFIG")
        if g["kind"] not in ("module","data","script") or g["id_field"] != ("dataset_id" if g["kind"]=="data" else "id"):
            fail("INVALID_CONFIG", "Unsupported identity")
        if g["status_mode"] != ("independent" if g["kind"]=="data" else "match"):
            fail("INVALID_CONFIG", "Unsupported status semantics")
    if any(len({g[key] for g in groups}) != 3 for key in ("kind","path","directory")):
        fail("INVALID_CONFIG", "Duplicate group")
    return c

def validate(root_value):
    root = project_root(root_value)
    reader = Reader(root)
    config = configuration(reader)
    limits = config["limits"]
    load = lambda path: reader.json(path,limits["max_json_bytes"],limits["max_depth"])
    policy = load(config["policy_path"])
    if not isinstance(policy,dict) or not isinstance(policy.get("descriptor_policy"),dict):
        fail("INVALID_POLICY")
    required = policy["descriptor_policy"].get("required_fields")
    if not isinstance(required,list) or not required or any(not isinstance(v,str) or not v.strip() for v in required) or len(set(required))!=len(required):
        fail("INVALID_POLICY", "Required fields must be unique text")
    descriptors = []
    catalog_ids, descriptor_ids, data_ids = set(), set(), set()
    total_entries = symbol_count = dependency_count = 0
    for group in config["catalogs"]:
        catalog = load(group["path"])
        if not isinstance(catalog,dict) or type(catalog.get("schema_version")) is not int or catalog["schema_version"]!=1 or not isinstance(catalog.get("entries"),list):
            fail("INVALID_CATALOG")
        entries = catalog["entries"]
        if len(entries)>limits["max_catalog_entries"]: fail("CATALOG_LIMIT",group["path"])
        folder = child(root,group["directory"])
        if not folder.is_dir(): fail("MISSING_PATH",group["directory"])
        actual = set()
        catalog_path = child(root,group["path"])
        for file in folder.iterdir():
            if file.suffix.lower()==".json" and file!=catalog_path:
                no_links(file)
                if not file.is_file(): fail("INVALID_DESCRIPTOR",str(file))
                actual.add(file)
                if len(actual)>limits["max_descriptors"]: fail("DESCRIPTOR_LIMIT")
        declared = set()
        for entry in entries:
            if not isinstance(entry,dict) or any(k not in entry for k in ("id","description","status","descriptor")):
                fail("INVALID_CATALOG")
            for k in ("id","description","status","descriptor"): text(entry[k],"INVALID_CATALOG")
            if entry["id"] in catalog_ids: fail("DUPLICATE_ID",entry["id"])
            catalog_ids.add(entry["id"])
            file = child(root,entry["descriptor"])
            if file.parent!=folder or file in declared: fail("INVALID_CATALOG","Descriptor must have one entry in its configured directory")
            declared.add(file)
            d = load(entry["descriptor"])
            if not isinstance(d,dict) or any(k not in d for k in required): fail("INVALID_DESCRIPTOR",entry["descriptor"])
            for k in ("id","kind","description","status"): text(d.get(k),"INVALID_DESCRIPTOR")
            if type(d.get("schema_version")) is not int or d["schema_version"]!=1 or d["kind"]!=group["kind"] or not isinstance(d.get("entrypoints"),list):
                fail("INVALID_DESCRIPTOR",entry["descriptor"])
            if d["id"] in descriptor_ids: fail("DUPLICATE_ID",d["id"])
            descriptor_ids.add(d["id"])
            identity = text(d.get(group["id_field"]),"INVALID_DESCRIPTOR")
            if entry["id"]!=identity or entry["description"]!=d["description"] or (group["status_mode"]=="match" and entry["status"]!=d["status"]):
                fail("CATALOG_MISMATCH",entry["descriptor"])
            if group["kind"]=="data": data_ids.add(identity)
            for point in d["entrypoints"]:
                if not isinstance(point,dict) or "path" not in point or "symbol" not in point:
                    fail("INVALID_DESCRIPTOR","Entrypoint requires path and symbol")
                path = text(point["path"],"INVALID_DESCRIPTOR")
                symbol = point["symbol"]
                if symbol is not None: text(symbol,"INVALID_DESCRIPTOR")
                source = child(root,path)
                if not source.is_file(): fail("MISSING_PATH",path)
                if source.stat().st_size>limits["max_source_bytes"]: fail("FILE_TOO_LARGE",path)
                if source.suffix.lower()==".cs" and symbol is not None:
                    code = reader.bytes(path,limits["max_source_bytes"]).decode("utf-8-sig")
                    name = symbol.split(".")[-1].split("(")[0]
                    if not name or re.search(r"\b"+re.escape(name)+r"\b",code) is None: fail("MISSING_SYMBOL",path+"::"+symbol)
                    symbol_count += 1
            dependencies = d.get("data_dependencies",[])
            if not isinstance(dependencies,list) or any(not isinstance(v,str) or not v.strip() for v in dependencies):
                fail("INVALID_DESCRIPTOR","Data dependencies must be IDs")
            descriptors.append(d)
            if len(descriptors)>limits["max_descriptors"]: fail("DESCRIPTOR_LIMIT")
        if actual!=declared: fail("DESCRIPTOR_INVENTORY_MISMATCH",group["directory"])
        total_entries += len(entries)
    for d in descriptors:
        for identity in d.get("data_dependencies",[]):
            if identity not in data_ids: fail("UNKNOWN_DATA_DEPENDENCY",identity)
            dependency_count += 1
    return {"status":"passed","descriptors":len(descriptors),"catalog_entries":total_entries,"datasets":len(data_ids),
            "csharp_symbols_checked":symbol_count,"data_dependencies_checked":dependency_count,"files_read":len(reader.read_files),
            "limitations":["C# symbols are lexical matches, not compilation proof.","Data catalog status has independent business meaning.","Paths and inputs must remain stable during this read-only check."]}

def main():
    parser = argparse.ArgumentParser(description=__doc__,exit_on_error=False)
    parser.add_argument("--project-root")
    try:
        args, unknown = parser.parse_known_args()
        if unknown or args.project_root is None: fail("INVALID_ARGUMENTS")
        result, code = validate(args.project_root), 0
    except ValidationError as error:
        result, code = {"status":"failed","error_code":error.code,"detail":error.detail}, 1
    except (OSError,UnicodeError,ValueError,argparse.ArgumentError) as error:
        result, code = {"status":"failed","error_code":"IO_OR_ARGUMENT_ERROR","detail":str(error)[:512]}, 1
    except Exception as error:
        result, code = {"status":"failed","error_code":"INTERNAL_ERROR","detail":type(error).__name__}, 1
    print(json.dumps(result,ensure_ascii=False,indent=2))
    return code

if __name__=="__main__":
    sys.exit(main())
