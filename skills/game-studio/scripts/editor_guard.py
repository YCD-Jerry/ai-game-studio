#!/usr/bin/env python3
"""Shared, read-only Unity guard for the shell wrappers and tool hook."""

import argparse
from datetime import datetime, timezone
import os
from pathlib import Path
import re
import shlex
import subprocess
import tempfile
import time


OK, PLAY, UNKNOWN, IMPORT_LOCK, EDITOR_LOCK, BUSY = 0, 3, 4, 5, 6, 7
FLAG = r"(True|False)"
BEACON = re.compile(
    rf"playing={FLAG} willPlay={FLAG} compiling={FLAG} updating={FLAG} "
    rf"scene=[^\r\n]* dirty={FLAG} untitled={FLAG} phase=(idle|reloading) utc=(\S+)"
    r"(?: pid=([1-9][0-9]*))?"
)
CLI_STATE = re.compile(
    rf"(?<![\w=])(?:GAME_STUDIO_STATE )?playing={FLAG} paused={FLAG} "
    rf"compiling={FLAG} updating={FLAG} compileFailed={FLAG} scenes=[0-9]+ "
    rf"dirty={FLAG} active=\S"
)


def process_alive(pid):
    """True, False or None when the OS cannot determine process liveness."""
    if pid <= 0 or pid > 2**31 - 1:
        return None
    if os.name == "nt":
        # os.kill(pid, 0) on Windows would terminate the process.
        import ctypes
        from ctypes import wintypes

        kernel = ctypes.WinDLL("kernel32", use_last_error=True)
        kernel.OpenProcess.argtypes = (wintypes.DWORD, wintypes.BOOL, wintypes.DWORD)
        kernel.OpenProcess.restype = wintypes.HANDLE
        kernel.WaitForSingleObject.argtypes = (wintypes.HANDLE, wintypes.DWORD)
        kernel.WaitForSingleObject.restype = wintypes.DWORD
        kernel.CloseHandle.argtypes = (wintypes.HANDLE,)
        handle = kernel.OpenProcess(0x100000, False, pid)  # SYNCHRONIZE
        if not handle:
            return False if ctypes.get_last_error() == 87 else None
        try:
            result = kernel.WaitForSingleObject(handle, 0)
            return {0: False, 258: True}.get(result)
        finally:
            kernel.CloseHandle(handle)
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return None
    return True


def flags_result(playing, will_play, compiling, updating, phase="idle"):
    if playing == "True" or will_play == "True":
        return PLAY, "Unity is playing or entering Play"
    if compiling == "True" or updating == "True" or phase == "reloading":
        return BUSY, "Unity is compiling, importing, or reloading"
    return OK, "Unity reports idle Edit mode"


def check_beacon(project):
    path = project / "Temp" / "editor_state.txt"
    try:
        # Read time and payload from the same file even if the beacon is replaced.
        with path.open("r", encoding="utf-8-sig") as handle:
            age = time.time() - os.fstat(handle.fileno()).st_mtime
            contents = handle.read(8193).strip()
    except (OSError, UnicodeError):
        return UNKNOWN, "Unity beacon is missing or unreadable"
    if not -2 <= age <= 10:
        return UNKNOWN, "Unity beacon is stale or its timestamp is in the future"
    match = BEACON.fullmatch(contents)
    if not match:
        return UNKNOWN, "Unity beacon is incomplete or malformed"
    playing, will_play, compiling, updating, _, _, phase, timestamp, pid = match.groups()
    try:
        utc = datetime.fromisoformat(timestamp.replace("Z", "+00:00"))
        age = (datetime.now(timezone.utc) - utc).total_seconds()
    except (ValueError, TypeError):
        return UNKNOWN, "Unity beacon timestamp is invalid"
    if not -2 <= age <= 10:
        return UNKNOWN, "Unity beacon timestamp is outside the freshness window"
    if pid and (len(pid) > 10 or process_alive(int(pid)) is not True):
        return UNKNOWN, "Unity beacon process could not be confirmed"
    return flags_result(playing, will_play, compiling, updating, phase)


def check_cli(project, eval_dir, timeout):
    probe = eval_dir / "playing.cs"
    if timeout <= 0 or not probe.is_file():
        return UNKNOWN, "Unity probe deadline reached or playing.cs is missing"
    try:
        command = [os.environ.get("UNITY_CLI") or "unity"]
        command.extend(shlex.split(os.environ.get("GAME_STUDIO_EVAL_CMD") or "command script eval_file"))
        command.extend(["--file", str(probe.resolve()), "--timeout", str(max(1, int(timeout)))])
        env = dict(os.environ, UNITY_PROJECT_PATH=str(project), UNITY_NO_BANNER="1",
                   UNITY_NO_PAGER="1", UNITY_NON_INTERACTIVE="1")
        # A subprocess can leave descendants holding its stdout open. A file avoids
        # waiting for pipe EOF after we kill the CLI at the deadline.
        with tempfile.TemporaryFile() as output:
            run = subprocess.run(command, cwd=project, env=env, stdout=output,
                                 stderr=subprocess.DEVNULL, timeout=timeout)
            output.seek(0)
            response = output.read(65537).decode("utf-8", errors="replace")
    except (OSError, ValueError, subprocess.TimeoutExpired):
        return UNKNOWN, "Unity CLI probe failed to start or exceeded its deadline"
    matches = list(CLI_STATE.finditer(response))
    if run.returncode or len(response) > 65536 or len(matches) != 1:
        return UNKNOWN, "Unity CLI probe failed or returned an ambiguous state"
    playing, _, compiling, updating, _, _ = matches[0].groups()
    return flags_result(playing, "False", compiling, updating)


def probe_editor(project, probe, eval_dir, timeout):
    if not project.is_dir():
        return UNKNOWN, "Unity project directory does not exist"
    if probe == "beacon":
        return check_beacon(project)
    return check_cli(project, eval_dir, timeout)


def add_settings(parser):
    parser.add_argument("--project", type=Path, default=os.environ.get("UNITY_PROJECT_PATH") or None)
    parser.add_argument("--lock-dir", type=Path, default=os.environ.get("GAME_STUDIO_LOCK_DIR") or None)
    parser.add_argument("--owner", default=os.environ.get("GAME_STUDIO_SESSION", ""))
    parser.add_argument("--probe", choices=("beacon", "cli"), default=os.environ.get("GAME_STUDIO_PLAY_PROBE") or "beacon")
    parser.add_argument("--eval-dir", type=Path, default=os.environ.get("GAME_STUDIO_EVAL_DIR") or Path(__file__).parent)
    parser.add_argument("--timeout", type=int, default=os.environ.get("GAME_STUDIO_EVAL_TIMEOUT") or 60)


def main():
    from editor_lock import lock_gate

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("guard",))
    add_settings(parser)
    args = parser.parse_args()
    if not args.project or args.probe not in ("beacon", "cli") or args.timeout <= 0:
        print("guard: set a project, valid probe and positive timeout")
        return UNKNOWN
    project = args.project.resolve()
    lock_dir = args.lock_dir or project / ".game-studio" / "locks"
    code, message = lock_gate(lock_dir, args.owner)
    if code == OK:
        code, message = probe_editor(project, args.probe, args.eval_dir, args.timeout)
    if code == OK:
        code, lock_message = lock_gate(lock_dir, args.owner)
        if code != OK:
            message = lock_message
    print(f"guard: {message}")
    return code


if __name__ == "__main__":
    raise SystemExit(main())
