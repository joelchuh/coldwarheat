"""Real CLI/temporary-file regressions. Keep every fixture and summary for review."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
from unittest import mock

REPO = Path(__file__).absolute().parents[2]
ENTRY = REPO / "Tools/Scripts/sync-catalog-entry.py"
CONFIG = json.loads((REPO / "Tools/Configuration/metadata-validation.json").read_text(encoding="utf-8-sig"))
POLICY = json.loads((REPO / "Docs/Workflow/workflow-policy.json").read_text(encoding="utf-8-sig"))
ROOT = Path(tempfile.mkdtemp(prefix="coldwarheat-catalog-sync-tests-"))
checks = []
serial = 0


def write(root, path, obj):
    p = root / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(obj, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def read(root, path):
    return json.loads((root / path).read_text(encoding="utf-8-sig"))


def group(kind):
    return next(g for g in CONFIG["catalogs"] if g["kind"] == kind)


def descriptor(kind):
    return group(kind)["directory"] + "/fixture." + kind + ".json"


def fixture():
    global serial
    serial += 1
    root = ROOT / ("case %03d" % serial)
    root.mkdir()
    write(root, "Tools/Configuration/metadata-validation.json", CONFIG)
    write(root, CONFIG["policy_path"], POLICY)
    for g in CONFIG["catalogs"]:
        kind = g["kind"]
        d = {field: [] for field in POLICY["descriptor_policy"]["required_fields"]}
        d.update(schema_version=1, id=kind + ".fixture", kind=kind, description="初始 " + kind, status="ready", entrypoints=[])
        if kind == "data":
            d["dataset_id"] = "fixture.data"
        if kind == "script":
            write(root, "Code/sentinel.json", {"never_execute": True})
            source = root / "Code/sentinel.py"
            source.write_text("raise RuntimeError('ENTRYPOINT MUST NEVER EXECUTE')\n", encoding="utf-8")
            d["entrypoints"] = [{"path": "Code/sentinel.py", "symbol": None}]
        write(root, descriptor(kind), d)
        write(root, g["path"], {"schema_version": 1, "description": "目录", "extra": {"keep": 17}, "entries": [{
            "id": d[g["id_field"]], "description": d["description"], "status": "verified" if kind == "data" else d["status"],
            "descriptor": descriptor(kind), "extension": "保留"}]})
    return root


def change(root, path, action):
    data = read(root, path)
    action(data)
    write(root, path, data)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def snapshot(root):
    return {p.relative_to(root).as_posix(): digest(p) for p in root.rglob("*") if p.is_file()}


def invoke(root, selected=None, extra=()):
    args = [sys.executable, str(ENTRY), "--project-root", str(root)]
    if selected is not None:
        args += ["--descriptor", selected]
    env = dict(os.environ, PYTHONDONTWRITEBYTECODE="1")
    p = subprocess.run(args + list(extra), capture_output=True, text=True, encoding="utf-8", timeout=20, env=env)
    assert not p.stderr.strip(), p.stderr[:400]
    return p.returncode, json.loads(p.stdout)


def expect_failure(root, selected, error, extra=()):
    before = snapshot(root)
    code, result = invoke(root, selected, extra)
    assert code == 1 and result.get("error_code") == error, (code, result, error)
    assert snapshot(root) == before, "Failure mutated the fixture"


def check(name, action):
    try:
        action()
        checks.append({"name": name, "passed": True})
    except Exception as error:
        checks.append({"name": name, "passed": False, "detail": str(error)[:800]})


def lifecycle(kind):
    root = fixture(); selected = descriptor(kind); catalog = group(kind)["path"]
    change(root, selected, lambda d: d.update(description="更新摘要 ✓", status="implemented"))
    original = read(root, catalog); before = snapshot(root)
    code, preview = invoke(root, selected)
    assert code == 0 and preview["status"] == "preview", preview
    assert snapshot(root) == before
    assert preview["catalog_sha256"] == digest(root / catalog)
    assert set(preview["changes"]) == ({"description"} if kind == "data" else {"description", "status"})
    code, result = invoke(root, selected, ("--write", "--expected-catalog-sha", preview["catalog_sha256"], "--expected-candidate-sha", preview["candidate_sha256"]))
    assert code == 0 and result["status"] == "updated", result
    after = read(root, catalog); expected = original
    expected["entries"][0]["description"] = "更新摘要 ✓"
    if kind != "data": expected["entries"][0]["status"] = "implemented"
    assert after == expected
    changed = [p for p, value in snapshot(root).items() if before.get(p) != value]
    assert changed == [catalog], changed
    stable = snapshot(root)
    code, result = invoke(root, selected, ("--write", "--expected-catalog-sha", digest(root / catalog), "--expected-candidate-sha", digest(root / catalog)))
    assert code == 0 and result["status"] == "unchanged", result
    assert snapshot(root) == stable


for kind in ("module", "script", "data"):
    check(kind + "-preview-write-idempotence-and-no-entry-execution", lambda k=kind: lifecycle(k))


def unchanged():
    root = fixture(); before = snapshot(root)
    code, result = invoke(root, descriptor("module"))
    assert code == 0 and result["status"] == "unchanged" and result["changes"] == {}, result
    assert snapshot(root) == before


check("aligned-preview-is-read-only", unchanged)
check("missing-descriptor-argument", lambda: expect_failure(fixture(), None, "INVALID_ARGUMENTS"))
check("unknown-cli-argument", lambda: expect_failure(fixture(), descriptor("module"), "INVALID_ARGUMENTS", ("--force",)))
check("write-needs-reviewed-sha", lambda: expect_failure(fixture(), descriptor("module"), "INVALID_ARGUMENTS", ("--write",)))
check("preview-does-not-accept-write-sha", lambda: expect_failure(fixture(), descriptor("module"), "INVALID_ARGUMENTS", ("--expected-catalog-sha", "0" * 64)))
check("malformed-sha", lambda: expect_failure(fixture(), descriptor("module"), "INVALID_ARGUMENTS", ("--write", "--expected-catalog-sha", "bad")))
check("stale-sha-is-no-write", lambda: expect_failure(fixture(), descriptor("module"), "CATALOG_CHANGED", ("--write", "--expected-catalog-sha", "0" * 64, "--expected-candidate-sha", "0" * 64)))
check("path-traversal", lambda: expect_failure(fixture(), "Docs/../outside.json", "INVALID_PATH"))
check("absolute-descriptor", lambda: expect_failure(fixture(), str(REPO / descriptor("module")), "INVALID_PATH"))
check("trailing-dot-alias", lambda: expect_failure(fixture(), "Docs./Descriptors/fixture.module.json", "INVALID_PATH"))
check("missing-descriptor-file", lambda: expect_failure(fixture(), "Docs/Descriptors/absent.json", "MISSING_PATH"))


def bad_case(path, action, error, selected=None):
    root = fixture(); change(root, path, action)
    selected = selected or descriptor("module")
    catalog = group("module")["path"]
    expect_failure(root, selected, error, ("--write", "--expected-catalog-sha", digest(root / catalog), "--expected-candidate-sha", "0" * 64))


check("no-automatic-entry-creation", lambda: bad_case(group("module")["path"], lambda c: c.update(entries=[]), "ENTRY_NOT_REGISTERED"))
check("duplicate-entry-rejected", lambda: bad_case(group("module")["path"], lambda c: c["entries"].append(c["entries"][0].copy()), "AMBIGUOUS_ENTRY"))
check("identity-not-rewritten", lambda: bad_case(group("module")["path"], lambda c: c["entries"][0].update(id="foreign.identity"), "IDENTITY_MISMATCH"))
check("wrong-kind-rejected", lambda: bad_case(descriptor("module"), lambda d: d.update(kind="script"), "INVALID_DESCRIPTOR"))
check("required-fields-validated", lambda: bad_case(descriptor("module"), lambda d: d.pop("outputs"), "INVALID_DESCRIPTOR"))
check("bad-entrypoint-blocks-write", lambda: bad_case(descriptor("module"), lambda d: d.update(entrypoints=[{"path": "missing.cs", "symbol": "Missing"}]), "MISSING_PATH"))
check("unrelated-mismatch-blocks-write", lambda: bad_case(descriptor("script"), lambda d: d.update(description="未同步的其它项"), "CATALOG_MISMATCH"))
check("unknown-data-dependency-blocks-write", lambda: bad_case(descriptor("module"), lambda d: d.update(data_dependencies=["unknown.data"]), "UNKNOWN_DATA_DEPENDENCY"))


def duplicate_json():
    root = fixture(); p = root / descriptor("module")
    p.write_text('{"id":"a","id":"b"}', encoding="utf-8")
    expect_failure(root, descriptor("module"), "DUPLICATE_KEY")


check("duplicate-json-key", duplicate_json)


def excessive_file():
    root = fixture(); p = root / descriptor("module")
    p.write_bytes(b" " * (CONFIG["limits"]["max_json_bytes"] + 1))
    expect_failure(root, descriptor("module"), "FILE_TOO_LARGE")


check("descriptor-size-bound", excessive_file)


def outside_group():
    root = fixture(); write(root, "Other/descriptor.json", read(root, descriptor("module")))
    expect_failure(root, "Other/descriptor.json", "UNCONFIGURED_DESCRIPTOR")


check("descriptor-must-belong-to-configured-directory", outside_group)


def unrelated_entry():
    root = fixture(); catalog = group("module")["path"]; selected = descriptor("module")
    d = read(root, selected); d["id"] = "module.second"
    path = group("module")["directory"] + "/second.module.json"; write(root, path, d)
    extra = {"id": d["id"], "description": d["description"], "status": d["status"], "descriptor": path, "keep": [1, 2]}
    change(root, catalog, lambda c: c["entries"].append(extra))
    change(root, selected, lambda d: d.update(description="only this entry"))
    code, preview = invoke(root, selected); assert code == 0, preview
    code, result = invoke(root, selected, ("--write", "--expected-catalog-sha", preview["catalog_sha256"], "--expected-candidate-sha", preview["candidate_sha256"]))
    assert code == 0 and result["status"] == "updated", result
    assert read(root, catalog)["entries"][1] == extra


check("unrelated-entry-and-extensions-preserved", unrelated_entry)


def missing_candidate_guard():
    root = fixture()
    expect_failure(root, descriptor("module"), "INVALID_ARGUMENTS", ("--write", "--expected-catalog-sha", digest(root / group("module")["path"])))


check("write-needs-reviewed-candidate-sha", missing_candidate_guard)
check("preview-does-not-accept-candidate-sha", lambda: expect_failure(fixture(), descriptor("module"), "INVALID_ARGUMENTS", ("--expected-candidate-sha", "0" * 64)))


def wrong_candidate_guard():
    root = fixture()
    expect_failure(root, descriptor("module"), "CANDIDATE_CHANGED", ("--write", "--expected-catalog-sha", digest(root / group("module")["path"]), "--expected-candidate-sha", "0" * 64))


check("wrong-candidate-sha-prevents-write", wrong_candidate_guard)


def changed_since_preview():
    root = fixture(); selected = descriptor("module")
    change(root, selected, lambda d: d.update(description="first preview"))
    code, preview = invoke(root, selected); assert code == 0, preview
    change(root, selected, lambda d: d.update(description="changed after preview"))
    expect_failure(root, selected, "CANDIDATE_CHANGED", ("--write", "--expected-catalog-sha", preview["catalog_sha256"], "--expected-candidate-sha", preview["candidate_sha256"]))


check("changed-description-after-preview-is-not-silently-written", changed_since_preview)

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location("catalog_sync_tested", ENTRY)
sync = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sync)


def pending_plan():
    root = fixture(); selected = descriptor("module")
    change(root, selected, lambda d: d.update(description="pending replacement"))
    return root, sync.plan_sync(str(root), selected)


def changed_after_validation():
    root, plan = pending_plan(); catalog = root / plan["catalog"]; before = catalog.read_bytes()
    change(root, descriptor("module"), lambda d: d.update(description="changed after validation"))
    try:
        sync.replace_catalog(plan)
        raise AssertionError("Expected input guard")
    except sync.metadata.ValidationError as error:
        assert error.code == "INPUT_CHANGED", error.code
    assert catalog.read_bytes() == before
    assert not list(catalog.parent.glob(".catalog-sync-*.tmp"))


check("descriptor-change-after-validation-is-not-written", changed_after_validation)


def changed_after_flush():
    root, plan = pending_plan(); catalog = root / plan["catalog"]; before = catalog.read_bytes()
    real_fsync = sync.os.fsync
    def mutation(fd):
        real_fsync(fd)
        change(root, descriptor("module"), lambda d: d.update(description="changed after temporary write"))
    try:
        with mock.patch.object(sync.os, "fsync", side_effect=mutation):
            sync.replace_catalog(plan)
        raise AssertionError("Expected second input guard")
    except sync.metadata.ValidationError as error:
        assert error.code == "INPUT_CHANGED", error.code
    assert catalog.read_bytes() == before
    assert not list(catalog.parent.glob(".catalog-sync-*.tmp"))


check("input-change-after-flush-cleans-only-own-temporary", changed_after_flush)


def failed_replace():
    root, plan = pending_plan(); before = snapshot(root)
    try:
        with mock.patch.object(sync.os, "replace", side_effect=OSError("injected atomic replace failure")):
            sync.replace_catalog(plan)
        raise AssertionError("Expected replace failure")
    except OSError:
        pass
    assert snapshot(root) == before


check("replace-failure-preserves-original-and-cleans-temporary", failed_replace)


def oversized_overlay():
    root = fixture(); before = snapshot(root)
    try:
        sync.metadata.validate(str(root), byte_overrides={group("module")["path"]: b" " * (CONFIG["limits"]["max_json_bytes"] + 1)})
        raise AssertionError("Expected overlay bound")
    except sync.metadata.ValidationError as error:
        assert error.code == "FILE_TOO_LARGE", error.code
    assert snapshot(root) == before


check("in-memory-candidate-still-obeys-size-bound", oversized_overlay)


def linked_descriptor():
    root = fixture(); link = root / "Linked"; target = root / group("module")["directory"]
    if os.name == "nt":
        quote = lambda p: "'" + str(p).replace("'", "''") + "'"
        command = "New-Item -ItemType Junction -Path " + quote(link) + " -Target " + quote(target) + " -ErrorAction Stop | Out-Null"
        result = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", command], capture_output=True, timeout=20)
        assert result.returncode == 0, "Could not create isolated test junction"
    else:
        link.symlink_to(target, target_is_directory=True)
    before = (root / group("module")["path"]).read_bytes()
    code, result = invoke(root, "Linked/fixture.module.json")
    assert code == 1 and result["error_code"] == "LINK_REJECTED", result
    assert (root / group("module")["path"]).read_bytes() == before


check("actual-junction-or-symlink-is-rejected", linked_descriptor)

passed = sum(c["passed"] for c in checks)
report = {"status": "passed" if passed == len(checks) else "failed", "total": len(checks), "passed": passed,
          "failed": len(checks) - passed, "fixture_root": str(ROOT), "checks": checks}
(ROOT / "summary.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
print(json.dumps(report, ensure_ascii=False, indent=2))
sys.exit(0 if passed == len(checks) else 1)
