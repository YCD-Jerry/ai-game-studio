#!/usr/bin/env python3
"""Serialize cooperating editor/import sessions on one machine and local disk."""

import argparse
from contextlib import contextmanager
import hashlib
import json
import math
import os
from pathlib import Path
import tempfile
import time

from editor_guard import (EDITOR_LOCK, IMPORT_LOCK, OK, UNKNOWN, add_settings,
                          probe_editor, process_alive)


class LockBusy(Exception):
    pass


@contextmanager
def transaction(directory):
    directory.mkdir(parents=True, exist_ok=True)
    # Keep this file in place. Replacing it would create two independent mutexes.
    with (directory / "coordination.mutex").open("a+b") as handle:
        handle.seek(0)
        try:
            if os.name == "nt":
                import msvcrt
                msvcrt.locking(handle.fileno(), msvcrt.LK_NBLCK, 1)
            else:
                import fcntl
                fcntl.flock(handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except OSError as error:
            raise LockBusy("lock transaction is unavailable") from error
        yield  # Closing the descriptor releases the OS lock, including after a crash.


def read_record(path):
    try:
        with path.open("rb") as handle:
            raw = handle.read(8193)
            age = time.time() - os.fstat(handle.fileno()).st_mtime
    except FileNotFoundError:
        return None
    fingerprint = hashlib.sha256(raw).hexdigest()
    record = {"fingerprint": fingerprint, "age": age, "owner": None, "pid": None, "note": ""}
    try:
        data = json.loads(raw.decode("utf-8-sig"))
    except (UnicodeError, ValueError):
        return record  # Legacy records also block until explicitly recovered.
    if isinstance(data, dict) and data.get("version") == 1:
        owner, pid, note = data.get("owner"), data.get("pid"), data.get("note")
        if (isinstance(owner, str) and owner and not any(c.isspace() for c in owner)
                and (pid is None or type(pid) is int and 0 < pid < 2**31)
                and isinstance(note, str)):
            record.update(owner=owner, pid=pid, note=note)
    return record


def write_record(path, owner, pid, note):
    data = {"version": 1, "owner": owner, "pid": pid, "note": note, "heartbeat_ns": time.time_ns()}
    with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", dir=path.parent, delete=False) as handle:
        temporary = Path(handle.name)
        try:
            json.dump(data, handle)
        except BaseException:
            handle.close()
            temporary.unlink()
            raise
    try:
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


def lock_gate(directory, owner):
    try:
        for name, code in (("import", IMPORT_LOCK), ("editor", EDITOR_LOCK)):
            record = read_record(directory / f"{name}.lock")
            if record and (not owner or record["owner"] != owner
                           or record["pid"] and process_alive(record["pid"]) is not True):
                return code, f"{name}.lock held by {record['owner'] or 'legacy/unknown holder'}"
    except OSError:
        return UNKNOWN, "lock status is unreadable"
    return OK, "locks are available to this session"


def describe(name, record, stale_minutes):
    if not record:
        return f"{name}.lock: free"
    age = record["age"] / 60
    state = "expired heartbeat" if age >= stale_minutes else "recent heartbeat"
    pid = record["pid"]
    liveness = {True: "alive", False: "gone", None: "unknown"}[process_alive(pid) if pid else None]
    return (f"{name}.lock: {record['owner'] or 'legacy/unknown holder'}, {state} ({age:.1f} min), "
            f"note {record['note']!r}; "
            f"holder PID {pid or 'unset'} ({liveness}); fingerprint {record['fingerprint']}")


def take(args, directory, path):
    deadline = time.monotonic() + args.wait_minutes * 60
    last = None
    while True:
        try:
            with transaction(directory):
                code, message = lock_gate(directory, args.owner)
                if code == OK:
                    # Do not hold both lock names, even when a caller reuses its token.
                    other = "import" if args.name == "editor" else "editor"
                    if read_record(directory / f"{other}.lock"):
                        code, message = EDITOR_LOCK, f"{other}.lock is held"
                if code == OK:
                    record = read_record(path)
                    if record and record["pid"] != args.holder_pid:
                        return 1, "session token is already associated with another holder"
                    if record and record["pid"] and process_alive(record["pid"]) is not True:
                        return 1, "recorded holder is unavailable; inspect and recover the lock"
                    budget = args.timeout if args.wait_minutes == 0 else min(args.timeout, deadline - time.monotonic())
                    if budget <= 0:
                        return 1, f"timed out waiting for {args.name}.lock"
                    code, message = probe_editor(args.project, args.probe, args.eval_dir, budget)
                if code == OK:
                    pid = record["pid"] if record else args.holder_pid
                    if pid is not None and process_alive(pid) is not True:
                        return 1, "holder PID is no longer alive"
                    write_record(path, args.owner, pid, args.note or (record["note"] if record else ""))
                    return OK, f"acquired {args.name}.lock"
        except LockBusy as error:
            message = str(error)
        if message != last:
            print(f"waiting: {message}", flush=True)
            last = message
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            return 1, f"timed out waiting for {args.name}.lock"
        time.sleep(min(1, remaining))


def change(args, directory, path):
    deadline = time.monotonic() + 5
    while True:
        try:
            with transaction(directory):
                record = read_record(path)
                if args.action == "recover":
                    if not record or record["fingerprint"] != args.fingerprint:
                        return 1, "lock changed since status; inspect it again"
                    if record["pid"] and process_alive(record["pid"]) is not False:
                        return 1, "recorded holder is alive or its liveness is unknown"
                    if args.editor_closed:
                        temp = args.project / "Temp"
                        try:
                            beacon_age = time.time() - (temp / "editor_state.txt").stat().st_mtime
                        except FileNotFoundError:
                            beacon_age = float("inf")
                        if beacon_age <= 10 or any((temp / name).exists()
                                                   for name in ("UnityLockfile", "UnityLockFile")):
                            return 1, "project has evidence of an open editor"
                    else:
                        code, message = probe_editor(args.project, args.probe, args.eval_dir, args.timeout)
                        if code != OK:
                            return 1, message
                    path.unlink()
                    return OK, f"recovered {args.name}.lock"
                if not record and args.action == "release":
                    return OK, f"{args.name}.lock was already free"
                if not record or record["owner"] != args.owner:
                    return 1, "lock is unavailable to this session"
                if record["pid"] != args.holder_pid:
                    return 1, "holder PID does not match the lock"
                if record["pid"] and process_alive(record["pid"]) is not True:
                    return 1, "recorded holder is unavailable; inspect and recover the lock"
                if args.action == "release":
                    path.unlink()
                    return OK, f"released {args.name}.lock"
                write_record(path, args.owner, record["pid"], args.note or record["note"])
                return OK, f"refreshed {args.name}.lock"
        except LockBusy:
            if time.monotonic() >= deadline:
                return 1, "lock transaction is unavailable"
            time.sleep(0.1)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("status", "take", "refresh", "release", "recover"))
    add_settings(parser)
    parser.add_argument("--name", choices=("editor", "import"), default="editor")
    parser.add_argument("--note", default="")
    parser.add_argument("--wait-minutes", type=float, default=40)
    parser.add_argument("--holder-pid", type=int)
    parser.add_argument("--fingerprint")
    parser.add_argument("--confirm-no-work", action="store_true")
    parser.add_argument("--editor-closed", action="store_true")
    args = parser.parse_args()
    if args.project:
        args.project = args.project.resolve()
    if (not args.project and not args.lock_dir
            or args.action in ("take", "recover") and not args.project
            or args.action in ("take", "refresh", "release")
            and (not args.owner or any(c.isspace() for c in args.owner))
            or args.action == "recover" and (not args.fingerprint or not args.confirm_no_work)
            or args.probe not in ("beacon", "cli") or args.timeout <= 0
            or not math.isfinite(args.wait_minutes) or args.wait_minutes < 0
            or args.holder_pid is not None and not 0 < args.holder_pid < 2**31
            or len(args.note) > 512 or len(args.owner) > 128):
        parser.error("check project path, session token, deadlines and recovery confirmation")
    directory = args.lock_dir or args.project / ".game-studio" / "locks"
    path = directory / f"{args.name}.lock"
    try:
        if args.action == "status":
            default = 30 if args.name == "import" else 20
            stale = float(os.environ.get(f"GAME_STUDIO_{args.name.upper()}_LOCK_STALE_MIN") or default)
            code, message = OK, describe(args.name, read_record(path), stale)
        elif args.action == "take":
            code, message = take(args, directory, path)
        else:
            code, message = change(args, directory, path)
    except (OSError, ValueError) as error:
        code, message = 1, f"lock operation failed: {error}"
    print(message)
    return code


if __name__ == "__main__":
    raise SystemExit(main())
