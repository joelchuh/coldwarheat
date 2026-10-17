"""Real local CLI fixtures; keeps every fresh temporary artifact and prints a JSON summary."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
from unittest.mock import patch

sys.dont_write_bytecode = True
REPO = Path(__file__).absolute().parents[2]
ENTRY = REPO / "Tools/Scripts/verify-player-payloads.py"
POLICY = json.loads((REPO / "Tools/Configuration/player-verification.json").read_text(encoding="utf-8-sig"))
ROOT = Path(tempfile.mkdtemp(prefix="coldwarheat-player-verifier-"))
checks = []
serial = 0
PLAYER = "Builds/Test"
LOG = "TestResults/build.log"
CONFIG = "Tools/Configuration/player-verification.json"
MANIFEST = "Tools/Configuration/player-payloads.json"
PAYLOAD = PLAYER + "/" + POLICY["data_directory"] + "/" + POLICY["payload_directory"]


def write(root, name, value):
    p = root / name
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes(value if isinstance(value, bytes) else (json.dumps(value, ensure_ascii=False) + "\n").encode("utf-8"))


def fixture():
    global serial
    serial += 1
    root = ROOT / ("case %03d" % serial)
    root.mkdir()
    write(root, CONFIG, POLICY)
    write(root, MANIFEST, {"schema_version": 1, "dataset_id": "workflow.player_payloads", "description": "fixture",
        "entries": [{"source": "Data/value.bin", "destination": "value.bin"},
                    {"source": "Docs/Contracts/value.json", "destination": "renamed.json"}]})
    for name in ("Data/value.bin", PAYLOAD + "/value.bin"):
        write(root, name, b"\x00\x01\x02\xff")
    for name in ("Docs/Contracts/value.json", PAYLOAD + "/renamed.json"):
        write(root, name, b"data")
    write(root, PLAYER + "/" + POLICY["executable"], b"exe!")
    for name in POLICY["managed_files"]:
        write(root, PLAYER + "/" + POLICY["data_directory"] + "/Managed/" + name, b"dll!")
    write(root, LOG, (POLICY["success_marker"] + " 100\n").encode())
    return root


def invoke(root, player=PLAYER, log=LOG, extra=()):
    result = subprocess.run([sys.executable, "-B", str(ENTRY), "--project-root", str(root),
        "--player-directory", player, "--build-log", log, *extra], capture_output=True, text=True,
        encoding="utf-8", timeout=20)
    assert not result.stderr.strip(), result.stderr[:300]
    return result.returncode, json.loads(result.stdout)


def expect(root, code, **kwargs):
    status, report = invoke(root, **kwargs)
    assert status == 1 and report.get("error_code") == code, (status, report, code)


def check(name, action):
    try:
        action()
        checks.append({"name": name, "passed": True})
    except Exception as error:
        checks.append({"name": name, "passed": False, "detail": str(error)[:600]})


def fingerprint(root):
    return {p.relative_to(root).as_posix(): (hashlib.sha256(p.read_bytes()).hexdigest(), p.stat().st_mtime_ns)
            for p in root.rglob("*") if p.is_file()}


def success():
    root = fixture()
    before = fingerprint(root)
    status, report = invoke(root)
    assert status == 0 and report["status"] == "passed", report
    assert report["payload_count"] == 2 and len(report["managed_sha256"]) == 5
    assert report["payloads"][0]["sha256"] == hashlib.sha256(b"\x00\x01\x02\xff").hexdigest()
    assert "source_commit" not in report
    assert fingerprint(root) == before, "Verifier changed fixture files"
    status2, repeat = invoke(root)
    assert status2 == 0 and repeat == report, "Identical immutable inputs must report identically"


def mutate_json(name, mutation, expected):
    root = fixture()
    data = json.loads((root / name).read_text(encoding="utf-8"))
    mutation(data)
    write(root, name, data)
    expect(root, expected)


def bad_bytes(name, value, expected):
    root = fixture()
    write(root, name, value)
    expect(root, expected)


def missing(name):
    root = fixture()
    # Rename one exact synthetic fixture file. Never recurse or touch repository inputs.
    p = root / name
    assert p.is_file() and p.absolute().is_relative_to(ROOT.absolute())
    p.rename(p.with_name(p.name + ".held"))
    expect(root, "MISSING_PATH")


check("exact binary bytes, renamed target, stable hashes and read-only repeat", success)
for key, value in [("schema_version", 2), ("schema_version", 1.0), ("schema_version", True),
                   ("dataset_id", "wrong"), ("description", " "), ("unknown", 1), ("entries", []),
                   ("entries", [None])]:
    check("manifest invalid " + key + "=" + str(value),
          lambda k=key, v=value: mutate_json(MANIFEST, lambda j: j.update({k: v}), "INVALID_MANIFEST"))
check("duplicate source ignoring case", lambda: mutate_json(MANIFEST,
    lambda j: j["entries"].append({"source": "Data/VALUE.bin", "destination": "third.bin"}), "DUPLICATE_ENTRY"))
check("duplicate destination ignoring case", lambda: mutate_json(MANIFEST,
    lambda j: j["entries"].append({"source": "Data/third.bin", "destination": "VALUE.BIN"}), "DUPLICATE_ENTRY"))
check("unknown entry field", lambda: mutate_json(MANIFEST,
    lambda j: j["entries"][0].update({"execute": True}), "INVALID_MANIFEST"))
for value in ["value.json.meta", "value.json.META"]:
    check("Unity metadata target reserved " + value, lambda v=value: mutate_json(MANIFEST,
        lambda j: j["entries"][0].update({"destination": v}), "INVALID_PATH"))
def unicode_description(count, accepted):
    root = fixture()
    data = json.loads((root / MANIFEST).read_text(encoding="utf-8"))
    data["description"] = "\U0001f30d" * count
    write(root, MANIFEST, data)
    if accepted:
        code, report = invoke(root)
        assert code == 0 and report["status"] == "passed", report
    else:
        expect(root, "INVALID_MANIFEST")
check("description 512 Unicode code points accepted", lambda: unicode_description(512, True))
check("description 513 Unicode code points rejected", lambda: unicode_description(513, False))
for value in ["../value.bin", "Data/../value.bin", "Data//value.bin", "C:/value.bin", "//server/value.bin",
              "Data\\value.bin", "Data/value.bin.", "Data/value.bin ", "Data/CON.txt", "Data/COM1.txt",
              "Data/x:y.bin", "Tools/secret.bin", "Docs/secret.bin"]:
    check("unsafe source " + value, lambda v=value: mutate_json(MANIFEST,
        lambda j: j["entries"][0].update({"source": v}), "INVALID_PATH"))
for value in ["sub/value.bin", "..", "AUX.json", "LPT9.txt", "value.bin."]:
    check("unsafe destination " + value, lambda v=value: mutate_json(MANIFEST,
        lambda j: j["entries"][0].update({"destination": v}), "INVALID_PATH"))
check("JSON duplicate key", lambda: bad_bytes(MANIFEST, b'{"schema_version":1,"schema_version":1}', "DUPLICATE_KEY"))
check("JSON invalid encoding", lambda: bad_bytes(MANIFEST, b'\xff', "INVALID_JSON"))
check("JSON comments", lambda: bad_bytes(MANIFEST, b'/*no*/{}', "INVALID_JSON"))
check("manifest over byte limit", lambda: bad_bytes(MANIFEST, b' ' * 1048577, "FILE_TOO_LARGE"))
for key, value in [("schema_version", 2), ("managed_files", []), ("unknown", True),
                   ("data_directory", "../escape"), ("success_marker", "")]:
    check("policy invalid " + key, lambda k=key, v=value: mutate_json(CONFIG,
        lambda j: j.update({k: v}), "INVALID_CONFIG"))
check("policy duplicate assembly", lambda: mutate_json(CONFIG,
    lambda j: j["managed_files"].append(j["managed_files"][0].upper()), "INVALID_CONFIG"))
for key, value, error in [("max_payload_bytes", 3, "FILE_TOO_LARGE"),
                         ("max_total_payload_bytes", 7, "TOTAL_LIMIT"),
                         ("max_executable_bytes", 3, "FILE_TOO_LARGE"),
                         ("max_assembly_bytes", 3, "FILE_TOO_LARGE"),
                         ("max_log_bytes", 3, "FILE_TOO_LARGE"),
                         ("max_payload_bytes", True, "INVALID_CONFIG"),
                         ("max_payload_bytes", 67108865, "INVALID_CONFIG")]:
    check("resource limit " + key + "=" + str(value), lambda k=key, v=value, e=error:
        mutate_json(CONFIG, lambda j: j["limits"].update({k: v}), e))
for name in ["Data/value.bin", MANIFEST, CONFIG, LOG, PLAYER + "/" + POLICY["executable"],
             PLAYER + "/" + POLICY["data_directory"] + "/Managed/ColdWarStruggle.Core.dll"]:
    check("missing " + name, lambda n=name: missing(n))
# A missing payload also leaves no extra file in that directory.
def missing_payload():
    root = fixture(); p = root / (PAYLOAD + "/value.bin")
    assert p.absolute().is_relative_to(ROOT.absolute())
    p.rename(root / "held-value.bin")
    expect(root, "MISSING_PATH")
check("missing player payload", missing_payload)
check("extra player payload", lambda: bad_bytes(PAYLOAD + "/stale.json", b"old", "UNEXPECTED_PAYLOAD"))
check("wrong payload bytes", lambda: bad_bytes(PAYLOAD + "/value.bin", b"oops", "CONTENT_MISMATCH"))
check("missing success marker", lambda: bad_bytes(LOG, b"Build failed\n", "BUILD_MARKER_MISSING"))
check("invalid log encoding", lambda: bad_bytes(LOG, b"\xff", "INVALID_BUILD_LOG"))
check("relative root", lambda: expect("relative", "INVALID_ROOT"))
check("drive root", lambda: expect(Path(ROOT.anchor), "INVALID_ROOT"))
check("player must be below Builds", lambda: expect(fixture(), "INVALID_PATH", player="Data"))
check("log must be below TestResults", lambda: expect(fixture(), "INVALID_PATH", log="Data/value.bin"))
check("player traversal", lambda: expect(fixture(), "INVALID_PATH", player="Builds/../Data"))
check("unknown CLI argument", lambda: expect(fixture(), "INVALID_ARGUMENTS", extra=("--execute",)))


def junction(which):
    assert os.name == "nt", "This case verifies actual Windows junctions"
    root = fixture()
    if which == "root":
        link, target = ROOT / ("linked-root-" + str(serial)), root
    elif which == "player":
        link, target = root / "Builds/Link", root / PLAYER
    else:
        link, target = root / "Data/Link", root / "Docs/Contracts"
        j = json.loads((root / MANIFEST).read_text())
        j["entries"][0]["source"] = "Data/Link/value.json"
        write(root, MANIFEST, j)
    proc = subprocess.run(["cmd.exe", "/d", "/c", "mklink", "/J", str(link), str(target)],
                          capture_output=True, text=True, timeout=10, creationflags=subprocess.CREATE_NO_WINDOW)
    assert proc.returncode == 0, proc.stderr
    expect(link if which == "root" else root, "LINK_REJECTED", player="Builds/Link" if which == "player" else PLAYER)
for where in ["root", "source", "player"]:
    check("real junction " + where, lambda w=where: junction(w))


def changes_during_read():
    root = fixture()
    spec = importlib.util.spec_from_file_location("tested_player_verifier", ENTRY)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    original = Path.open
    target = root / "Data/value.bin"
    class Reader:
        def __init__(self, inner): self.inner = inner; self.changed = False
        def __enter__(self): return self
        def __exit__(self, *args): return self.inner.__exit__(*args)
        def __getattr__(self, name): return getattr(self.inner, name)
        def read(self, *args):
            data = self.inner.read(*args)
            if not self.changed:
                self.changed = True
                before = target.stat()
                with original(target, "wb") as w: w.write(b"swap")
                os.utime(target, ns=(before.st_atime_ns, before.st_mtime_ns + 1000000000))
            return data
    def opened(path, mode="r", *args, **kwargs):
        f = original(path, mode, *args, **kwargs)
        return Reader(f) if path == target and mode == "rb" else f
    with patch.object(Path, "open", opened):
        try: module.verify(str(root), PLAYER, LOG)
        except Exception as error: assert getattr(error, "code", None) == "INPUT_CHANGED", repr(error)
        else: raise AssertionError("Accepted a changing source")
check("real source mutation during read is rejected", changes_during_read)

summary = {"status": "passed" if all(c["passed"] for c in checks) else "failed",
    "total": len(checks), "passed": sum(c["passed"] for c in checks),
    "failed": sum(not c["passed"] for c in checks), "fixture_root": str(ROOT), "checks": checks}
(ROOT / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
print(json.dumps(summary, ensure_ascii=False, indent=2))
sys.exit(0 if summary["status"] == "passed" else 1)
