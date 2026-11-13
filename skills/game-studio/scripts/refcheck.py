"""Check Unity scene references against one Git snapshot or the working tree."""

import argparse
import re
import subprocess
import sys
from pathlib import Path

GUID = re.compile(rb"^guid:[ \t]+([0-9a-f]{32})\b", re.MULTILINE)
REFERENCE = re.compile(rb"fileID: (-?\d+), guid: ([0-9a-f]{32}), type: 2")
OBJECT_ID = re.compile(rb"^--- !u!\d+ &(-?\d+)\b", re.MULTILINE)
TEXT_ASSETS = {
    ".asset", ".mat", ".controller", ".prefab", ".anim",
    ".overridecontroller", ".mask", ".physicmaterial", ".lighting",
}


def check(repo, work, prefix):
    def git(*args, allowed=(0,)):
        result = subprocess.run(["git", "-C", str(repo), *args], capture_output=True)
        if result.returncode not in allowed:
            raise RuntimeError(result.stderr.decode(errors="replace").strip() or "Git failed")
        return result.stdout

    def read(path):
        return (repo / path).read_bytes() if work else git("show", "HEAD:" + path)

    def paths(output):
        return {part.decode("utf-8") for part in output.split(b"\0") if part}

    # Use the repo root even when invoked from a nested directory.
    repo = Path(git("rev-parse", "--show-toplevel").decode("utf-8").strip())
    if work:
        files = paths(git("ls-files", "-z", "--cached", "--others", "--exclude-standard"))
        meta_guids = []
        for path in sorted(files):
            if path.endswith(".meta") and (repo / path).is_file():
                match = GUID.search(read(path))
                if match:
                    meta_guids.append((path, match[1]))
    else:
        files = paths(git("ls-tree", "-rtz", "--name-only", "HEAD"))
        matches = git(
            "grep", "-z", "-E", "^guid:[[:blank:]]+[0-9a-f]{32}", "HEAD", "--", "*.meta",
            allowed=(0, 1),
        )
        meta_guids = []
        for line in matches.splitlines():
            source, _, content = line.partition(b"\0")
            match = GUID.search(content)
            if match:
                meta_guids.append((source[len(b"HEAD:"):].decode("utf-8"), match[1]))

    asset_paths = {}
    for path, guid in meta_guids:
        asset_paths.setdefault(guid, set()).add(path[:-5])

    scenes = sorted(
        path for path in files
        if path.startswith(prefix) and path.endswith(".unity")
        and (not work or (repo / path).is_file())
    )
    if not scenes:
        raise RuntimeError(f"No scenes found under {prefix!r}")

    problems = set()
    unknown = set()
    object_ids = {}
    for scene in scenes:
        for file_id, guid in set(REFERENCE.findall(read(scene))):
            if file_id == b"0" or guid.startswith(b"0000000000000000"):
                continue
            targets = asset_paths.get(guid)
            if not targets:
                unknown.add((scene, guid))
                continue
            if len(targets) > 1:
                problems.add(f"{scene}: duplicate GUID {guid.decode()}: {', '.join(sorted(targets))}")
                continue
            target = next(iter(targets))
            exists = (repo / target).exists() if work else target in files
            if not exists:
                problems.add(f"{scene}: missing asset {target}")
                continue
            if Path(target).suffix.lower() not in TEXT_ASSETS or (work and (repo / target).is_dir()):
                continue
            if target not in object_ids:
                object_ids[target] = set(OBJECT_ID.findall(read(target)))
            ids = object_ids[target]
            if ids and file_id not in ids:
                problems.add(f"{scene}: {target} missing fileID {file_id.decode()}")

    if unknown:
        package_guids = set()
        for meta in (repo / "Library" / "PackageCache").rglob("*.meta"):
            match = GUID.search(meta.read_bytes())
            if match and meta.with_suffix("").exists():
                package_guids.add(match[1])
        problems.update(
            f"{scene}: unknown GUID {guid.decode()}"
            for scene, guid in unknown if guid not in package_guids
        )

    for problem in sorted(problems):
        print(problem)
    snapshot = "working tree" if work else "HEAD"
    print(f"{len(scenes)} scenes, {len(problems)} reference issues in {snapshot}")
    return int(bool(problems))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repo", type=Path)
    parser.add_argument("--work", action="store_true", help="Check the working tree instead of HEAD.")
    parser.add_argument("--scenes", default="Assets/", help="Git path prefix for scenes.")
    args = parser.parse_args()
    try:
        return check(args.repo, args.work, args.scenes)
    except (OSError, RuntimeError, UnicodeError) as error:
        print(f"refcheck: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
