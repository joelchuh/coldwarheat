"""Read-only local Player evidence. Does not launch programs, repair files, invoke Git or infer provenance."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import stat
import sys

sys.dont_write_bytecode = True
_spec = importlib.util.spec_from_file_location("_player_metadata_paths", Path(__file__).with_name("validate-project-metadata.py"))
_paths = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_paths)
VerificationError = _paths.ValidationError
fail = _paths.fail
CONFIG = "Tools/Configuration/player-verification.json"
MANIFEST = "Tools/Configuration/player-payloads.json"
_SEGMENT = re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]{0,127}\Z", re.ASCII)
_DEVICE = re.compile(r"(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])\Z", re.IGNORECASE | re.ASCII)


def relative(value, *, flat=False, source=False, code="INVALID_PATH"):
    if not isinstance(value, str) or not value:
        fail(code, "Nonempty relative path required")
    parts = value.split("/")
    if flat and len(parts) != 1:
        fail(code, "Flat filename required")
    for part in parts:
        if not _SEGMENT.fullmatch(part) or part.endswith(".") or _DEVICE.fullmatch(part.split(".")[0]):
            fail(code, value)
    if source and not value.startswith(("Data/", "Docs/Contracts/", "Docs/References/ThirdParty/")):
        fail(code, "Source outside approved data directories")
    return value


def signature(info):
    return info.st_dev, info.st_ino, info.st_size, info.st_mtime_ns


class FileEvidence:
    def __init__(self, root):
        self.root, self.observed = root, {}

    def read(self, path, maximum, keep=False):
        _paths.no_links(path)
        if not path.is_file():
            fail("MISSING_PATH", path)
        digest, chunks, count = hashlib.sha256(), [], 0
        with path.open("rb") as stream:
            before = os.fstat(stream.fileno())
            if not stat.S_ISREG(before.st_mode):
                fail("MISSING_PATH", path)
            if before.st_size > maximum:
                fail("FILE_TOO_LARGE", path)
            while True:
                chunk = stream.read(min(1048576, maximum - count + 1))
                if not chunk:
                    break
                count += len(chunk)
                if count > maximum:
                    fail("FILE_TOO_LARGE", path)
                digest.update(chunk)
                if keep:
                    chunks.append(chunk)
            after = os.fstat(stream.fileno())
        _paths.no_links(path)
        observed = signature(after)
        if signature(before) != observed or observed != signature(path.stat()) or count != before.st_size:
            fail("INPUT_CHANGED", path)
        if path in self.observed and self.observed[path][0] != observed:
            fail("INPUT_CHANGED", path)
        sha = digest.hexdigest()
        self.observed[path] = (observed, sha)
        return sha, count, b"".join(chunks) if keep else None

    def json(self, name, maximum):
        path = _paths.child(self.root, name)
        sha, _, raw = self.read(path, maximum, keep=True)
        try:
            value = json.loads(raw.decode("utf-8-sig"), object_pairs_hook=_paths.unique_object,
                               parse_constant=lambda v: fail("INVALID_JSON", v))
        except (ValueError, UnicodeError, RecursionError) as error:
            fail("INVALID_JSON", error)
        _paths.check_depth(value, 16)
        return value, sha

    def confirm_unchanged(self):
        for path, (observed, _) in self.observed.items():
            _paths.no_links(path)
            if signature(path.stat()) != observed:
                fail("INPUT_CHANGED", path)


def configuration(evidence):
    c, sha = evidence.json(CONFIG, 65536)
    _paths.fields(c, ("schema_version", "dataset_id", "description", "executable", "data_directory",
        "payload_directory", "managed_directory", "managed_files", "success_marker", "limits"), "INVALID_CONFIG")
    if type(c["schema_version"]) is not int or c["schema_version"] != 1 or c["dataset_id"] != "workflow.player_verification":
        fail("INVALID_CONFIG", "Unsupported policy")
    _paths.text(c["description"], "INVALID_CONFIG")
    for key in ("executable", "data_directory", "managed_directory"):
        relative(c[key], flat=True, code="INVALID_CONFIG")
    relative(c["payload_directory"], code="INVALID_CONFIG")
    marker = _paths.text(c["success_marker"], "INVALID_CONFIG")
    if len(marker) > 128 or "\n" in marker or "\r" in marker:
        fail("INVALID_CONFIG", "Invalid success marker")
    files = c["managed_files"]
    if not isinstance(files, list) or not 1 <= len(files) <= 16:
        fail("INVALID_CONFIG", "Required managed files")
    for name in files:
        relative(name, flat=True, code="INVALID_CONFIG")
        if not name.startswith("ColdWarStruggle.") or not name.endswith(".dll"):
            fail("INVALID_CONFIG", "Project assembly required")
    if len({name.casefold() for name in files}) != len(files):
        fail("INVALID_CONFIG", "Duplicate assembly")
    caps = {"max_payload_bytes": 67108864, "max_total_payload_bytes": 268435456,
            "max_executable_bytes": 268435456, "max_assembly_bytes": 67108864, "max_log_bytes": 67108864}
    limits = _paths.fields(c["limits"], caps, "INVALID_CONFIG")
    if any(type(value) is not int or not 1 <= value <= caps[key] for key, value in limits.items()):
        fail("INVALID_CONFIG", "Resource limit outside supported bounds")
    return c, sha


def manifest(evidence):
    m, sha = evidence.json(MANIFEST, 1048576)
    _paths.fields(m, ("schema_version", "dataset_id", "description", "entries"), "INVALID_MANIFEST")
    if type(m["schema_version"]) is not int or m["schema_version"] != 1 or m["dataset_id"] != "workflow.player_payloads":
        fail("INVALID_MANIFEST", "Unsupported manifest")
    if len(_paths.text(m["description"], "INVALID_MANIFEST")) > 512:
        fail("INVALID_MANIFEST", "Description length")
    rows = m["entries"]
    if not isinstance(rows, list) or not 1 <= len(rows) <= 256:
        fail("INVALID_MANIFEST", "Entry count")
    sources, targets = set(), set()
    for row in rows:
        _paths.fields(row, ("source", "destination"), "INVALID_MANIFEST")
        source = relative(row["source"], source=True)
        target = relative(row["destination"], flat=True)
        if source.casefold() in sources or target.casefold() in targets:
            fail("DUPLICATE_ENTRY", "Manifest source or destination collision")
        sources.add(source.casefold()); targets.add(target.casefold())
    return rows, sha


def verify(project_root, player_directory, build_log):
    root = _paths.project_root(project_root)
    relative(player_directory); relative(build_log)
    if not player_directory.startswith("Builds/") or not build_log.startswith("TestResults/"):
        fail("INVALID_PATH", "Player must be under Builds and log under TestResults")
    evidence = FileEvidence(root)
    policy, policy_sha = configuration(evidence)
    rows, manifest_sha = manifest(evidence)
    limits = policy["limits"]
    player = _paths.child(root, player_directory)
    if not player.is_dir():
        fail("MISSING_PATH", player_directory)
    data = _paths.child(player, policy["data_directory"])
    payload = _paths.child(data, policy["payload_directory"])
    if not payload.is_dir():
        fail("MISSING_PATH", payload)
    expected = {r["destination"].casefold() for r in rows}
    actual = set()
    for path in payload.iterdir():
        _paths.no_links(path)
        name = path.name.casefold()
        if not path.is_file() or name in actual or name not in expected:
            fail("UNEXPECTED_PAYLOAD", path.name)
        actual.add(name)
    if actual != expected:
        fail("MISSING_PATH", "Player payload set is incomplete")
    payloads, total = [], 0
    for row in rows:
        source = _paths.child(root, row["source"])
        source_sha, size, _ = evidence.read(source, limits["max_payload_bytes"])
        total += size
        if total > limits["max_total_payload_bytes"]:
            fail("TOTAL_LIMIT", "Total source payload bytes")
        target = _paths.child(payload, row["destination"])
        target_sha, target_size, _ = evidence.read(target, limits["max_payload_bytes"])
        if source_sha != target_sha or size != target_size:
            fail("CONTENT_MISMATCH", row["destination"])
        payloads.append({"source": row["source"],
            "player_path": "/".join((player_directory, policy["data_directory"], policy["payload_directory"], row["destination"])),
            "bytes": size, "sha256": source_sha})
    exe = _paths.child(player, policy["executable"])
    exe_sha, _, _ = evidence.read(exe, limits["max_executable_bytes"])
    managed = _paths.child(data, policy["managed_directory"])
    assemblies = {name: evidence.read(_paths.child(managed, name), limits["max_assembly_bytes"])[0]
                  for name in policy["managed_files"]}
    log = _paths.child(root, build_log)
    log_sha, _, raw = evidence.read(log, limits["max_log_bytes"], keep=True)
    try:
        log_text = raw.decode("utf-8-sig")
    except UnicodeError:
        fail("INVALID_BUILD_LOG", build_log)
    if policy["success_marker"] not in log_text:
        fail("BUILD_MARKER_MISSING", build_log)
    evidence.confirm_unchanged()
    return {"schema_version": 1, "status": "passed", "project_root": str(root),
        "player_directory": player_directory, "build_log": build_log,
        "manifest_sha256": manifest_sha, "policy_sha256": policy_sha,
        "payload_count": len(payloads), "payload_bytes": total, "payloads": payloads,
        "executable_sha256": exe_sha, "managed_sha256": assemblies, "build_log_sha256": log_sha,
        "limitations": ["Hashes and marker confirm these local files against this manifest; they do not prove a source commit or trusted build provenance.",
                        "No game-rule, UI or execution acceptance is performed.",
                        "Requires trusted stable local inputs; does not claim to defeat malicious concurrent path replacement."]}


class Parser(argparse.ArgumentParser):
    def error(self, message):
        fail("INVALID_ARGUMENTS", message)


def main():
    try:
        parser = Parser(description=__doc__)
        parser.add_argument("--project-root", required=True)
        parser.add_argument("--player-directory", required=True)
        parser.add_argument("--build-log", required=True)
        args = parser.parse_args()
        result = verify(args.project_root, args.player_directory, args.build_log)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    except VerificationError as error:
        print(json.dumps({"status": "failed", "error_code": error.code, "detail": error.detail}, ensure_ascii=False))
    except (OSError, ValueError, UnicodeError, OverflowError, RecursionError) as error:
        print(json.dumps({"status": "failed", "error_code": "IO_ERROR", "detail": str(error)[:512]}, ensure_ascii=False))
    return 1


if __name__ == "__main__":
    sys.exit(main())
