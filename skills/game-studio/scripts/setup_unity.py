#!/usr/bin/env python3
"""Install and diagnose the optional Game Studio tools for a Unity project."""

import argparse
import json
import os
import shutil
import sys
import time
from pathlib import Path


SCRIPTS = Path(__file__).resolve().parent
BEACON = Path("Assets/Editor/GameStudio/EditorStateBeacon.cs")
VERSION_FILE = Path("ProjectSettings/ProjectVersion.txt")
MANIFEST = Path("Packages/manifest.json")


def project_version(project):
    if not (project / "Assets").is_dir() or not (project / MANIFEST).is_file():
        raise ValueError("expected a Unity project root with Assets/ and Packages/manifest.json")
    package_dependencies(project / MANIFEST)
    version = (project / VERSION_FILE).read_text(encoding="utf-8-sig")
    for line in version.splitlines():
        key, _, value = line.partition(":")
        if key == "m_EditorVersion" and value.strip():
            return value.strip()
    raise ValueError(f"no m_EditorVersion in {VERSION_FILE.as_posix()}")


def bundle_files(project):
    for source in sorted(SCRIPTS.rglob("*")):
        relative = source.relative_to(SCRIPTS)
        if any(part.startswith(".") or part == "__pycache__" for part in relative.parts):
            continue
        if source.is_file() and source.name != "setup_unity.py":
            yield source, project / "tools/game-studio" / relative

    # Existing manual installations may have placed this script in another Editor folder.
    beacons = sorted((project / "Assets").rglob(BEACON.name))
    if len(beacons) > 1:
        paths = ", ".join(path.relative_to(project).as_posix() for path in beacons)
        raise ValueError(f"multiple beacon files would compile the same types: {paths}")
    target = beacons[0] if beacons else project / BEACON
    if "Editor" not in target.relative_to(project).parts[:-1]:
        raise ValueError(f"move the existing {BEACON.name} into an Editor folder before installing")
    yield SCRIPTS.parent / "assets/unity/Editor" / BEACON.name, target


def installation_plan(project):
    missing = []
    conflicts = []
    for source, target in bundle_files(project):
        label = target.relative_to(project).as_posix()
        wanted = source.read_bytes()
        if target.resolve() != target:
            conflicts.append(f"redirected path: {label}")
            continue
        try:
            current = target.read_bytes()
        except FileNotFoundError:
            missing.append((source, target))
            continue
        except OSError as exc:
            conflicts.append(f"cannot read {label}: {exc.strerror}")
            continue
        if target.name == BEACON.name:
            current = current.replace(b"\r\n", b"\n")
            wanted = wanted.replace(b"\r\n", b"\n")
        if current != wanted:
            conflicts.append(f"different file: {label}")
    return missing, conflicts


def editor_may_be_running(project):
    for name in ("UnityLockfile", "UnityLockFile"):
        try:
            (project / "Temp" / name).lstat()
        except FileNotFoundError:
            continue
        return f"Temp/{name} exists. Close the project and resolve any stale Unity lock first."
    try:
        age = time.time() - (project / "Temp/editor_state.txt").stat().st_mtime
    except FileNotFoundError:
        return None
    if age <= 10:
        return "The editor beacon was written recently. Close the project and wait for the beacon to expire."
    return None


def install(project, missing, editor_closed):
    if not missing:
        print("Game Studio Unity tools are already installed.")
        return 0
    if any(target.relative_to(project).parts[0] == "Assets" for _, target in missing):
        if not editor_closed:
            raise ValueError("close this Unity project, then rerun with --editor-closed to install the beacon")
        reason = editor_may_be_running(project)
        if reason:
            raise ValueError(reason)

    for source, target in missing:
        content = source.read_bytes()
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.resolve() != target:
            raise ValueError(f"redirected path: {target.relative_to(project).as_posix()}")
        # Exclusive creation preserves a file added by another process after preflight.
        with target.open("xb") as destination:
            try:
                destination.write(content)
                destination.flush()
            except OSError:
                destination.close()
                target.unlink()
                raise
        shutil.copymode(source, target)
        print(f"Installed {target.relative_to(project).as_posix()}")
    print("Open this project in Unity and run doctor to verify the installation.")
    print("For shared-editor use, ignore .game-studio/locks/ in version control.")
    return 0


def package_dependencies(path):
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
    except json.JSONDecodeError as exc:
        raise ValueError(f"invalid JSON in {path.name}: {exc}") from exc
    dependencies = data.get("dependencies") if isinstance(data, dict) else None
    if not isinstance(dependencies, dict):
        raise ValueError(f"expected a dependencies object in {path.name}")
    return dependencies


def pipeline_version(project):
    for relative in (Path("Packages/packages-lock.json"), MANIFEST):
        try:
            value = package_dependencies(project / relative).get("com.unity.pipeline")
        except FileNotFoundError:
            continue
        if isinstance(value, dict):
            value = value.get("version")
        if isinstance(value, str) and value:
            return value
    return None


def doctor(project, probe):
    if probe == "beacon":
        sys.dont_write_bytecode = True
        from editor_guard import check_beacon

        code, message = check_beacon(project)
        print(f"Beacon: {message}")
        if code == 4:
            print("Open this project in Unity, let it finish importing, then rerun doctor.")
        return code

    executable = shutil.which(os.environ.get("UNITY_CLI") or "unity")
    pipeline = pipeline_version(project)
    print(f"Unity CLI: {executable or 'not found; install it or set UNITY_CLI'}")
    print(f"Unity Pipeline: {pipeline or 'not found in the package manifest or lock file'}")
    if not executable or not pipeline:
        return 1
    print("CLI prerequisites found. Run the guard with the editor open to test the live bridge.")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="action", required=True)
    install_args = sub.add_parser("install", help="copy the tools into a Unity project")
    doctor_args = sub.add_parser("doctor", help="check a Unity project and editor-probe prerequisites")
    for command in (install_args, doctor_args):
        command.add_argument("--project", type=Path, required=True, help="Unity project root")
    install_args.add_argument("--editor-closed", action="store_true", help="confirm the project is closed in Unity")
    doctor_args.add_argument("--probe", choices=("beacon", "cli"), help="override GAME_STUDIO_PLAY_PROBE")
    args = parser.parse_args()

    try:
        project = args.project.resolve()
        print(f"Unity project version: {project_version(project)}")
        missing, conflicts = installation_plan(project)
        for conflict in conflicts:
            print(f"Conflict: {conflict}", file=sys.stderr)
        if args.action == "install":
            if conflicts:
                print("Resolve these files before installing. Existing files were left in place.", file=sys.stderr)
                return 1
            return install(project, missing, args.editor_closed)

        for _, target in missing:
            print(f"Missing: {target.relative_to(project).as_posix()}", file=sys.stderr)
        if conflicts or missing:
            return 1
        probe = args.probe or os.environ.get("GAME_STUDIO_PLAY_PROBE") or "beacon"
        if probe not in ("beacon", "cli"):
            raise ValueError("GAME_STUDIO_PLAY_PROBE must be beacon or cli")
        return doctor(project, probe)
    except (OSError, ValueError) as exc:
        print(f"{args.action}: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
