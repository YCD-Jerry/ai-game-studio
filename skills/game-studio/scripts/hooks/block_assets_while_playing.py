#!/usr/bin/env python3
"""Claude Code PreToolUse hook: refuse writes into the Unity project's Assets/ while the editor is in Play mode.

Why: a file that lands in Assets/ during Play is imported (or recompiled) mid-Play; the domain reload kills every
coroutine and the director's playtest freezes. Rules and reminders failed twice; a hook can't be forgotten.

How it decides:
  * Edit / Write / MultiEdit / NotebookEdit: blocks when the target path is inside <project>/Assets/.
  * Bash / PowerShell: blocks when the command both mentions Assets/ (or Assets\\) and looks like it writes
    (cp, mv, copy, Copy-Item, Move-Item, Set-Content, Out-File, >, tee, git checkout/restore/pull, rsync, unzip).
  * Only while <project>/Temp/editor_state.txt (written by EditorStateBeacon.cs every 2 s) is fresh and says
    playing=True or willPlay=True. No beacon, or a stale one, means "unknown": the hook allows the call and leaves
    the decision to scripts/guard.* (so a closed editor doesn't block all work).

Exit code 2 blocks the tool call; the message on stderr goes back to Claude.
Setup: see references/editor-safety.md ("Hard stop: a Claude Code hook"). Needs Python 3, no packages.
Project root: UNITY_PROJECT_PATH, else the hook's working directory.
"""
import json
import os
import re
import sys
import time

WRITE_TOOLS = {"Edit", "Write", "MultiEdit", "NotebookEdit"}
SHELL_TOOLS = {"Bash", "PowerShell"}
WRITE_WORDS = re.compile(
    r"(\bcp\b|\bmv\b|\bcopy\b|\bxcopy\b|\brobocopy\b|Copy-Item|Move-Item|Set-Content|Add-Content|Out-File|"
    r"New-Item|\btee\b|\brsync\b|\bunzip\b|\btar\b|git\s+(checkout|restore|pull|merge|reset|stash)|>{1,2})",
    re.IGNORECASE)
ASSETS = re.compile(r"(^|[\\/\s\"'])Assets[\\/]", re.IGNORECASE)


def playing(project):
    state = os.path.join(project, "Temp", "editor_state.txt")
    try:
        if time.time() - os.path.getmtime(state) > 10:
            return False  # stale: editor closed or busy; the guard script decides
        with open(state, encoding="utf-8", errors="replace") as f:
            text = f.read()
    except OSError:
        return False
    return "playing=True" in text or "willPlay=True" in text


def inside_assets(path, project):
    if not path:
        return False
    full = os.path.normcase(os.path.abspath(os.path.join(project, path)))
    assets = os.path.normcase(os.path.abspath(os.path.join(project, "Assets")))
    return full == assets or full.startswith(assets + os.sep)


def main():
    try:
        event = json.load(sys.stdin)
    except ValueError:
        return 0
    project = os.environ.get("UNITY_PROJECT_PATH") or event.get("cwd") or os.getcwd()
    tool = event.get("tool_name", "")
    args = event.get("tool_input") or {}

    hit = False
    if tool in WRITE_TOOLS:
        hit = inside_assets(args.get("file_path") or args.get("notebook_path"), project)
    elif tool in SHELL_TOOLS:
        cmd = args.get("command") or ""
        hit = bool(ASSETS.search(cmd) and WRITE_WORDS.search(cmd))

    if hit and playing(project):
        sys.stderr.write(
            "BLOCKED: Unity is in Play mode (the director is playtesting). Nothing may be written into Assets/ "
            "until Play ends: an import or recompile now would kill the running story. Stage the file outside "
            "Assets/, do other work, and move it in after scripts/guard returns 0.\n")
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
