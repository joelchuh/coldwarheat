"""Real CLI fixtures. Creates a fresh temporary tree and intentionally keeps all artifacts."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile

REPO = Path(__file__).absolute().parents[2]
ENTRY = REPO / "Tools/Scripts/validate-project-metadata.py"
CONFIG = json.loads((REPO / "Tools/Configuration/metadata-validation.json").read_text(encoding="utf-8-sig"))
POLICY = json.loads((REPO / "Docs/Workflow/workflow-policy.json").read_text(encoding="utf-8-sig"))
ROOT = Path(tempfile.mkdtemp(prefix="coldwarheat-metadata-tests-"))
checks = []
serial = 0

def write(root, path, obj):
    p = root / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(obj, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

def read(root, path):
    return json.loads((root / path).read_text(encoding="utf-8"))

def fixture():
    global serial
    serial += 1
    root = ROOT / ("case %03d" % serial)
    root.mkdir()
    write(root, "Tools/Configuration/metadata-validation.json", CONFIG)
    write(root, CONFIG["policy_path"], POLICY)
    for group in CONFIG["catalogs"]:
        kind = group["kind"]
        d = {field: [] for field in POLICY["descriptor_policy"]["required_fields"]}
        d.update(schema_version=1, id=kind+".fixture", kind=kind, description="fixture "+kind,
                 status="ready", entrypoints=[])
        if kind == "data":
            d["dataset_id"] = "fixture.data"
            write(root, "Data/value.json", {"value": 1})
            d["entrypoints"] = [{"path":"Data/value.json","symbol":None}]
        elif kind == "module":
            p=root/"Code/Fixture.cs";p.parent.mkdir()
            p.write_text("public class Fixture { public void DoWork() {} }\n",encoding="utf-8")
            d["entrypoints"]=[{"path":"Code/Fixture.cs","symbol":"DoWork"}]
            d["data_dependencies"]=["fixture.data"]
        else:
            p=root/"Code/never-execute.py"
            p.write_text("raise RuntimeError('THIS ENTRY MUST NEVER EXECUTE')\n",encoding="utf-8")
            d["entrypoints"]=[{"path":"Code/never-execute.py","symbol":None}]
        descriptor=group["directory"]+"/fixture."+kind+".json"
        write(root, descriptor, d)
        write(root, group["path"], {"schema_version":1,"description":"fixture catalog","entries":[{
            "id":d[group["id_field"]],"description":d["description"],"status":d["status"],"descriptor":descriptor}]})
    return root

def invoke(root, extra=()):
    process = subprocess.run([sys.executable,str(ENTRY),"--project-root",str(root),*extra],
                             capture_output=True,text=True,encoding="utf-8",timeout=15)
    if process.stderr.strip():
        raise AssertionError("Unexpected stderr: "+process.stderr[:500])
    return process.returncode,json.loads(process.stdout)

def expect(root, code):
    exit_code,result=invoke(root)
    assert exit_code==1 and result.get("error_code")==code,(exit_code,result,code)

def check(name, body):
    try:
        body()
        checks.append({"name":name,"passed":True})
    except Exception as error:
        checks.append({"name":name,"passed":False,"error":str(error)[:700]})

def snapshot(root):
    return {str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in root.rglob("*") if p.is_file()}

def positive():
    root=fixture();before=snapshot(root);exit_code,result=invoke(root)
    assert exit_code==0 and result["status"]=="passed",(exit_code,result)
    assert result["descriptors"]==3 and result["catalog_entries"]==3,result
    assert snapshot(root)==before,"Inputs changed or output files added"
check("success-is-readonly-and-does-not-execute-entrypoints-with-space-path",positive)

M="Docs/Descriptors/fixture.module.json"
MC="Docs/Descriptors/catalog.json"
DC="Data/catalog.json"
C="Tools/Configuration/metadata-validation.json"

def mutate_case(name, code, edit):
    def body():
        root=fixture();edit(root);expect(root,code)
    check(name,body)

def change(root,path,fn):
    value=read(root,path);fn(value);write(root,path,value)

mutations=[
 ("missing-required-field","INVALID_DESCRIPTOR",lambda r:change(r,M,lambda j:j.pop("tests"))),
 ("wrong-description-type","INVALID_DESCRIPTOR",lambda r:change(r,M,lambda j:j.update(description=[]))),
 ("unknown-descriptor-kind","INVALID_DESCRIPTOR",lambda r:change(r,M,lambda j:j.update(kind="unknown"))),
 ("wrong-entrypoint-type","INVALID_DESCRIPTOR",lambda r:change(r,M,lambda j:j.update(entrypoints="Code/Fixture.cs"))),
 ("duplicate-catalog-id","DUPLICATE_ID",lambda r:change(r,MC,lambda j:j["entries"].append(j["entries"][0].copy()))),
 ("catalog-description-mismatch","CATALOG_MISMATCH",lambda r:change(r,MC,lambda j:j["entries"][0].update(description="changed"))),
 ("catalog-status-mismatch","CATALOG_MISMATCH",lambda r:change(r,MC,lambda j:j["entries"][0].update(status="planned"))),
 ("catalog-id-mismatch","CATALOG_MISMATCH",lambda r:change(r,MC,lambda j:j["entries"][0].update(id="wrong.id"))),
 ("data-id-mismatch","CATALOG_MISMATCH",lambda r:change(r,DC,lambda j:j["entries"][0].update(id="data.fixture"))),
 ("undeclared-descriptor","DESCRIPTOR_INVENTORY_MISMATCH",lambda r:write(r,"Docs/Descriptors/orphan.module.json",read(r,M))),
 ("missing-descriptor-file","MISSING_PATH",lambda r:change(r,MC,lambda j:j["entries"][0].update(descriptor="Docs/Descriptors/missing.json"))),
 ("missing-entrypoint-file","MISSING_PATH",lambda r:change(r,M,lambda j:j["entrypoints"][0].update(path="Code/missing.cs"))),
 ("missing-csharp-symbol","MISSING_SYMBOL",lambda r:change(r,M,lambda j:j["entrypoints"][0].update(symbol="Missing"))),
 ("uppercase-csharp-missing-symbol","MISSING_SYMBOL",lambda r:change(r,M,lambda j:j["entrypoints"][0].update(path="Code/Fixture.CS",symbol="Missing"))),
 ("entrypoint-trailing-dot","INVALID_PATH",lambda r:change(r,M,lambda j:j["entrypoints"][0].update(path="Code/Fixture.cs."))),
 ("entrypoint-trailing-space","INVALID_PATH",lambda r:change(r,M,lambda j:j["entrypoints"][0].update(path="Code/Fixture.cs "))),
 ("directory-trailing-dot","INVALID_PATH",lambda r:change(r,M,lambda j:j["entrypoints"][0].update(path="Code./Fixture.cs"))),
 ("directory-trailing-space","INVALID_PATH",lambda r:change(r,M,lambda j:j["entrypoints"][0].update(path="Code /Fixture.cs"))),
 ("descriptor-trailing-dot","INVALID_PATH",lambda r:change(r,MC,lambda j:j["entries"][0].update(descriptor=M+"."))),
 ("policy-trailing-space","INVALID_PATH",lambda r:change(r,C,lambda j:j.update(policy_path=CONFIG["policy_path"]+" "))),
 ("uppercase-orphan-descriptor","DESCRIPTOR_INVENTORY_MISMATCH",lambda r:write(r,"Docs/Descriptors/orphan.module.JSON",read(r,M))),
 ("unknown-data-dependency","UNKNOWN_DATA_DEPENDENCY",lambda r:change(r,M,lambda j:j.update(data_dependencies=["missing.data"]))),
 ("wrong-dependency-type","INVALID_DESCRIPTOR",lambda r:change(r,M,lambda j:j.update(data_dependencies=[{"id":"fixture.data"}]))),
 ("descriptor-path-escape","INVALID_PATH",lambda r:change(r,MC,lambda j:j["entries"][0].update(descriptor="../outside.json"))),
 ("entrypoint-path-escape","INVALID_PATH",lambda r:change(r,M,lambda j:j["entrypoints"][0].update(path="../outside.cs"))),
 ("absolute-entrypoint","INVALID_PATH",lambda r:change(r,M,lambda j:j["entrypoints"][0].update(path=str(r/"Code/Fixture.cs")))),
 ("wrong-catalog-entries-type","INVALID_CATALOG",lambda r:change(r,MC,lambda j:j.update(entries={}))),
 ("non-object-catalog-entry","INVALID_CATALOG",lambda r:change(r,MC,lambda j:j.update(entries=[False]))),
 ("catalog-oversize","CATALOG_LIMIT",lambda r:change(r,C,lambda j:j["limits"].update(max_catalog_entries=1)) or change(r,MC,lambda j:j["entries"].append(j["entries"][0].copy()))),
 ("descriptor-count-limit","DESCRIPTOR_LIMIT",lambda r:change(r,C,lambda j:j["limits"].update(max_descriptors=2))),
 ("source-size-limit","FILE_TOO_LARGE",lambda r:change(r,C,lambda j:j["limits"].update(max_source_bytes=5))),
 ("duplicate-json-key","DUPLICATE_KEY",lambda r:(r/M).write_text('{"id":"one","id":"two"}',encoding="utf-8")),
 ("malformed-json","INVALID_JSON",lambda r:(r/M).write_text("{broken",encoding="utf-8")),
 ("nonstandard-json-number","INVALID_JSON",lambda r:(r/M).write_text('{"x":NaN}',encoding="utf-8")),
 ("json-byte-limit","FILE_TOO_LARGE",lambda r:(r/M).write_text(" "*(CONFIG["limits"]["max_json_bytes"]+1),encoding="utf-8")),
 ("json-depth-limit","JSON_DEPTH",lambda r:(r/M).write_text("["*(CONFIG["limits"]["max_depth"]+1)+"0"+"]"*(CONFIG["limits"]["max_depth"]+1),encoding="utf-8")),
 ("config-unknown-field","INVALID_CONFIG",lambda r:change(r,C,lambda j:j.update(unexpected=True))),
 ("config-bool-limit","INVALID_CONFIG",lambda r:change(r,C,lambda j:j["limits"].update(max_depth=True))),
 ("config-root-escape","INVALID_PATH",lambda r:change(r,C,lambda j:j.update(policy_path="../policy.json"))),
 ("policy-no-fields","INVALID_POLICY",lambda r:change(r,CONFIG["policy_path"],lambda j:j["descriptor_policy"].update(required_fields=[]))),
]
for name,code,edit in mutations:mutate_case(name,code,edit)

def planned():
    root=fixture()
    change(root,M,lambda j:j.update(planned_entrypoints=[{"path":"Code/future.cs","symbol":"Future"}]))
    assert invoke(root)[0]==0
check("planned-entrypoints-do-not-pretend-to-be-callable",planned)

def uppercase_source():
    root=fixture()
    change(root,M,lambda j:j["entrypoints"][0].update(path="Code/Fixture.CS"))
    exit_code,result=invoke(root)
    assert exit_code==0 and result["csharp_symbols_checked"]==1,(exit_code,result)
check("uppercase-csharp-still-checks-symbol",uppercase_source)

def uppercase_descriptor():
    root=fixture()
    (root/M).rename(root/(M+".tmp"))
    (root/(M+".tmp")).rename(root/(M[:-4]+"JSON"))
    change(root,MC,lambda j:j["entries"][0].update(descriptor=M[:-4]+"JSON"))
    assert invoke(root)[0]==0
check("declared-uppercase-json-is-in-inventory",uppercase_descriptor)

def separate_data_status():
    root=fixture();change(root,DC,lambda j:j["entries"][0].update(status="verified-subset"))
    assert invoke(root)[0]==0
check("data-catalog-business-status-is-independent",separate_data_status)

def dynamic_policy():
    root=fixture()
    change(root,CONFIG["policy_path"],lambda j:j["descriptor_policy"]["required_fields"].append("new_required"))
    expect(root,"INVALID_DESCRIPTOR")
check("required-fields-come-from-shared-policy",dynamic_policy)
check("relative-root-rejected",lambda:expect(Path("relative-project"),"INVALID_ROOT"))
check("missing-root-rejected",lambda:expect(ROOT/"does-not-exist","INVALID_ROOT"))

passed=sum(c["passed"] for c in checks)
report={"status":"passed" if passed==len(checks) else "failed","total":len(checks),"passed":passed,
        "failed":len(checks)-passed,"fixture_root":str(ROOT),"checks":checks,
        "link_boundary":"Actual Windows junction is verified separately; these fixtures do not create links."}
(ROOT/"summary.json").write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8",newline="\n")
print(json.dumps(report,ensure_ascii=False,indent=2))
sys.exit(0 if passed==len(checks) else 1)
