#!/usr/bin/env python3
"""Optional PreToolUse hook for a shared Unity project with the state beacon."""
import json
import os
from pathlib import Path
import re
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from editor_guard import OK, check_beacon
from editor_lock import lock_gate


WRITE_TOOLS = {"Edit", "Write", "MultiEdit", "NotebookEdit", "apply_patch"}
# Only a small set of commands whose complete syntax we recognize. Git is excluded
# here because project config, filters and subprocesses can affect what it runs.
SAFE_SHELL = re.compile(
    r"\s*(?:pwd|Get-Location|(?:ls|dir|Get-ChildItem)(?: +[a-zA-Z0-9_./\\:-]+)*)\s*",
    re.IGNORECASE,
)
PROTECTED = ("Assets", "Packages", "ProjectSettings")
PATCH_PATH = re.compile(r"^\*\*\* (?:Add File|Update File|Delete File|Move to): (.+)$", re.MULTILINE)


def project_root(cwd):
    explicit = os.environ.get("UNITY_PROJECT_PATH")
    if explicit:
        return Path(explicit).resolve()
    for candidate in (cwd, *cwd.parents):
        if (candidate / "Assets").is_dir() and (candidate / "ProjectSettings/ProjectVersion.txt").is_file():
            return candidate
    return None


def overlaps(path, cwd, project):
    target = cwd / path
    protected = [project / directory for directory in PROTECTED]
    return any(
        location == root or root in location.parents or location in root.parents
        for location in {Path(os.path.abspath(target)), target.resolve()}
        for root in protected + [directory.resolve() for directory in protected]
    )


def needs_gate(tool, args, cwd, project):
    if tool in WRITE_TOOLS:
        paths = [args.get("file_path") or args.get("notebook_path")]
        if tool == "apply_patch":
            patch = args.get("patch") or args.get("input")
            paths = PATCH_PATH.findall(patch) if isinstance(patch, str) else []
        return not paths or any(not isinstance(path, str) or not path or overlaps(path, cwd, project)
                                for path in paths)
    # Shell commands can write indirectly (git, interpreters, aliases, relative paths,
    # nested commands). Opaque tools are treated the same when the matcher selects them.
    command = args.get("command")
    return not (tool in {"Bash", "PowerShell"} and isinstance(command, str)
                and SAFE_SHELL.fullmatch(command))


def evaluate(event):
    cwd = Path(event.get("cwd") or os.getcwd()).resolve()
    project = project_root(cwd)
    if project is None:
        return None
    tool = event.get("tool_name")
    args = event.get("tool_input") or {}
    if not isinstance(args, dict) or not needs_gate(tool, args, cwd, project):
        if isinstance(args, dict):
            return None
        return "malformed tool input"
    code, message = check_beacon(project)
    if code != OK:
        return message
    lock_dir = Path(os.environ.get("GAME_STUDIO_LOCK_DIR") or project / ".game-studio/locks")
    code, message = lock_gate(lock_dir, os.environ.get("GAME_STUDIO_SESSION", ""))
    if code != OK:
        return message
    return None


def main():
    try:
        event = json.load(sys.stdin)
        message = evaluate(event) if isinstance(event, dict) else "malformed hook event"
    except (OSError, ValueError, TypeError, RuntimeError) as error:
        message = f"hook could not determine editor safety: {error}"
    if message:
        print(f"Game Studio blocked this tool call: {message}. "
              "Wait for idle Edit mode, or work in an isolated project.", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
