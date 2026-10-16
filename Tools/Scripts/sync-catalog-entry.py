"""Preview or synchronize one existing metadata catalog entry; never execute described code."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import sys
import tempfile

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location("catalog_metadata", Path(__file__).with_name("validate-project-metadata.py"))
metadata = importlib.util.module_from_spec(spec)
spec.loader.exec_module(metadata)


class TrackingReader(metadata.Reader):
    def __init__(self, root):
        super().__init__(root)
        self.originals = {}

    def bytes(self, relative, maximum):
        data = super().bytes(relative, maximum)
        if relative in self.originals and self.originals[relative] != data:
            metadata.fail("INPUT_CHANGED", relative)
        self.originals[relative] = data
        return data


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def plan_sync(project_root_value, descriptor_relative):
    root = metadata.project_root(project_root_value)
    reader = TrackingReader(root)
    config = metadata.configuration(reader)
    limits = config["limits"]
    load = lambda path: reader.json(path, limits["max_json_bytes"], limits["max_depth"])
    selected = metadata.child(root, descriptor_relative)
    groups = [g for g in config["catalogs"] if metadata.child(root, g["directory"]) == selected.parent]
    if len(groups) != 1:
        metadata.fail("UNCONFIGURED_DESCRIPTOR", descriptor_relative)
    group = groups[0]
    d = load(descriptor_relative)
    if not isinstance(d, dict) or type(d.get("schema_version")) is not int or d["schema_version"] != 1 or d.get("kind") != group["kind"]:
        metadata.fail("INVALID_DESCRIPTOR", descriptor_relative)
    for field in ("id", "description", "status", group["id_field"]):
        metadata.text(d.get(field), "INVALID_DESCRIPTOR")
    catalog_relative = group["path"]
    catalog = load(catalog_relative)
    if not isinstance(catalog, dict) or type(catalog.get("schema_version")) is not int or catalog["schema_version"] != 1 or not isinstance(catalog.get("entries"), list):
        metadata.fail("INVALID_CATALOG", catalog_relative)
    if len(catalog["entries"]) > limits["max_catalog_entries"]:
        metadata.fail("CATALOG_LIMIT", catalog_relative)
    matches = []
    for entry in catalog["entries"]:
        if not isinstance(entry, dict):
            metadata.fail("INVALID_CATALOG", catalog_relative)
        for field in ("id", "description", "status", "descriptor"):
            metadata.text(entry.get(field), "INVALID_CATALOG")
        if metadata.child(root, entry["descriptor"]) == selected:
            matches.append(entry)
    if not matches:
        metadata.fail("ENTRY_NOT_REGISTERED", descriptor_relative)
    if len(matches) != 1:
        metadata.fail("AMBIGUOUS_ENTRY", descriptor_relative)
    entry = matches[0]
    if entry["id"] != d[group["id_field"]]:
        metadata.fail("IDENTITY_MISMATCH", descriptor_relative)
    changes = {}
    for field in (("description", "status") if group["status_mode"] == "match" else ("description",)):
        if entry[field] != d[field]:
            changes[field] = {"before": entry[field], "after": d[field]}
            entry[field] = d[field]
    original = reader.originals[catalog_relative]
    candidate = (json.dumps(catalog, ensure_ascii=False, indent=2) + "\n").encode("utf-8") if changes else original
    # Capture the policy before the full candidate validation, then recheck before writing.
    reader.bytes(config["policy_path"], limits["max_json_bytes"])
    validation = metadata.validate(str(root), byte_overrides={catalog_relative: candidate})
    return {"root": root, "descriptor": descriptor_relative, "catalog": catalog_relative, "id": entry["id"],
            "original": original, "candidate": candidate, "protected": dict(reader.originals),
            "maximum": limits["max_json_bytes"], "changes": changes, "validation": validation}


def verify_unchanged(plan):
    reader = metadata.Reader(plan["root"])
    for relative, original in plan["protected"].items():
        if reader.bytes(relative, plan["maximum"]) != original:
            metadata.fail("INPUT_CHANGED", relative)


def replace_catalog(plan):
    verify_unchanged(plan)
    target = metadata.child(plan["root"], plan["catalog"])
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(mode="wb", prefix=".catalog-sync-", suffix=".tmp", dir=target.parent, delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(plan["candidate"])
            stream.flush()
            os.fsync(stream.fileno())
        verify_unchanged(plan)
        metadata.no_links(temporary)
        os.replace(temporary, target)
        temporary = None
    finally:
        if temporary is not None and temporary.exists():
            metadata.no_links(temporary)
            temporary.unlink()


def synchronize(project_root_value, descriptor_relative, write=False, expected_catalog_sha=None, expected_candidate_sha=None):
    hashes = (expected_catalog_sha, expected_candidate_sha)
    if type(write) is not bool or (not write and any(h is not None for h in hashes)) or (write and any(not isinstance(h, str) or re.fullmatch(r"[0-9a-fA-F]{64}", h) is None for h in hashes)):
        metadata.fail("INVALID_ARGUMENTS")
    plan = plan_sync(project_root_value, descriptor_relative)
    base_sha = sha256(plan["original"])
    if write and expected_catalog_sha.lower() != base_sha:
        metadata.fail("CATALOG_CHANGED", plan["catalog"])
    if write and expected_candidate_sha.lower() != sha256(plan["candidate"]):
        metadata.fail("CANDIDATE_CHANGED", plan["catalog"])
    if write:
        verify_unchanged(plan)
        if plan["changes"]:
            replace_catalog(plan)
    return {"status": ("updated" if write else "preview") if plan["changes"] else "unchanged",
            "catalog": plan["catalog"], "descriptor": plan["descriptor"], "entry_id": plan["id"],
            "catalog_sha256": base_sha, "candidate_sha256": sha256(plan["candidate"]),
            "changes": plan["changes"], "validation": plan["validation"],
            "formatting": "UTF-8, LF, two-space indent when changed",
            "limitations": ["Inputs must remain stable; not a hostile concurrent-path or hard-link sandbox.",
                            "Metadata checks do not prove compilation, game rules, or Git synchronization."]}


def main():
    # The JSON protocol must not depend on the Windows console code page.
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__, exit_on_error=False)
    parser.add_argument("--project-root")
    parser.add_argument("--descriptor")
    parser.add_argument("--write", action="store_true")
    parser.add_argument("--expected-catalog-sha")
    parser.add_argument("--expected-candidate-sha")
    try:
        args, unknown = parser.parse_known_args()
        if unknown or args.project_root is None or args.descriptor is None:
            metadata.fail("INVALID_ARGUMENTS")
        result = synchronize(args.project_root, args.descriptor, args.write, args.expected_catalog_sha, args.expected_candidate_sha)
        code = 0
    except metadata.ValidationError as error:
        result, code = {"status": "failed", "error_code": error.code, "detail": error.detail}, 1
    except (OSError, UnicodeError, ValueError, argparse.ArgumentError) as error:
        result, code = {"status": "failed", "error_code": "IO_OR_ARGUMENT_ERROR", "detail": str(error)[:512]}, 1
    except Exception as error:
        result, code = {"status": "failed", "error_code": "INTERNAL_ERROR", "detail": type(error).__name__}, 1
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return code


if __name__ == "__main__":
    sys.exit(main())
