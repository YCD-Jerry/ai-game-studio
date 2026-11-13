"""Run the editor guard, hook and shared lock against disposable projects."""

from datetime import datetime, timezone
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest


SCRIPTS = Path(__file__).resolve().parents[1] / "skills/game-studio/scripts"


class EditorSafetyTest(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.project = Path(temp.name) / "Unity project"
        for name in ("Assets", "Packages", "ProjectSettings", "Temp"):
            (self.project / name).mkdir(parents=True)
        (self.project / "ProjectSettings/ProjectVersion.txt").touch()
        self.state = self.project / "Temp/editor_state.txt"
        self.locks = self.project / ".game-studio/locks"
        self.env = {k: v for k, v in os.environ.items() if not k.startswith("GAME_STUDIO_")}
        self.env.update(UNITY_PROJECT_PATH=str(self.project), GAME_STUDIO_PLAY_PROBE="beacon")

    def beacon(self, *, playing=False, will_play=False, compiling=False, updating=False,
               phase="idle", age=0, pid=None):
        timestamp = datetime.fromtimestamp(time.time() - age, timezone.utc).isoformat()
        record = (f"playing={playing} willPlay={will_play} compiling={compiling} updating={updating} "
                  f"scene=Assets/Scene With Spaces.unity dirty=False untitled=False phase={phase} utc={timestamp}")
        if pid is not None:
            record += f" pid={pid}"
        self.state.write_text(record, encoding="utf-8-sig")

    def run_script(self, script, *args, input=None, env=None):
        return subprocess.run([sys.executable, str(SCRIPTS / script), *args], input=input,
                              env=self.env if env is None else env, text=True, capture_output=True, timeout=8)

    def guard(self, expected, *args, env=None):
        result = self.run_script("editor_guard.py", "guard", *args, env=env)
        self.assertEqual(expected, result.returncode, result.stdout + result.stderr)
        return result.stdout

    def lock(self, expected, action, *args):
        result = self.run_script("editor_lock.py", action, *args)
        self.assertEqual(expected, result.returncode, result.stdout + result.stderr)
        return result.stdout

    def hook(self, expected, tool, args, cwd=None):
        event = {"cwd": str(cwd or self.project), "tool_name": tool, "tool_input": args}
        result = self.run_script("hooks/block_assets_while_playing.py", input=json.dumps(event))
        self.assertEqual(expected, result.returncode, result.stdout + result.stderr)

    def test_beacon_requires_complete_fresh_state_and_checks_busy_flags(self):
        self.guard(4)
        for changes, expected in (({"playing": True}, 3), ({"will_play": True}, 3),
                                  ({"compiling": True}, 7), ({"updating": True}, 7),
                                  ({"phase": "reloading"}, 7), ({"age": 60}, 4),
                                  ({"age": -60}, 4), ({"pid": os.getpid()}, 0), ({}, 0)):
            with self.subTest(changes=changes):
                self.beacon(**changes)
                self.guard(expected)
        self.state.write_text("playing=False", encoding="utf-8")
        self.guard(4)

    def test_cli_requires_success_and_one_full_record(self):
        stub = self.project / "probe.py"
        env = dict(self.env, GAME_STUDIO_PLAY_PROBE="cli", UNITY_CLI=sys.executable,
                   GAME_STUDIO_EVAL_CMD='"' + stub.as_posix() + '"')
        line = ("GAME_STUDIO_STATE playing=False paused=False compiling=False updating=False "
                "compileFailed=False scenes=1 dirty=False active=Assets/A.unity")
        for program, expected in ((f"print({line!r})", 0),
                                  (f"print({line.replace('updating=False', 'updating=True')!r})", 7),
                                  (f"print({line!r}); raise SystemExit(1)", 4),
                                  (f"print({line!r}); print({line!r})", 4),
                                  ("print('playing=False')", 4),
                                  ("import time; time.sleep(5)", 4)):
            with self.subTest(program=program):
                stub.write_text(program, encoding="utf-8")
                self.guard(expected, "--timeout", "1", env=env)

    def test_hook_gates_direct_and_indirect_writes_when_state_is_unknown_or_busy(self):
        nested = self.project / "Assets" / "Nested"
        nested.mkdir()
        for key in ("missing", "play", "compile", "import", "idle"):
            with self.subTest(state=key):
                if key != "missing":
                    self.beacon(playing=key == "play", compiling=key == "compile", updating=key == "import")
                expected = 0 if key == "idle" else 2
                self.hook(expected, "Write", {"file_path": "./Thing.cs"}, cwd=nested)
                self.hook(expected, "Edit", {"file_path": "Packages/manifest.json"})
                self.hook(expected, "Bash", {"command": "git restore ."})
                self.hook(expected, "Bash", {"command": "python make_assets.py"})
                self.hook(expected, "PowerShell", {"command": "Set-Content Thing.cs x"}, cwd=nested)
                self.hook(0, "Write", {"file_path": "notes.txt"})
                self.hook(0, "Bash", {"command": "pwd"})

    def test_old_records_are_never_assumed_free(self):
        self.beacon()
        self.locks.mkdir(parents=True)
        legacy = self.locks / "editor.lock"
        legacy.write_text("a 2000-01-01T01:00:00 old lock", encoding="utf-8-sig")
        os.utime(legacy, (1, 1))
        self.guard(6, "--owner", "a")
        self.lock(1, "take", "--owner", "a", "--wait-minutes", "0")
        self.assertTrue(legacy.is_file())

    def test_recovery_with_old_fingerprint_cannot_delete_refreshed_record(self):
        self.beacon()
        self.lock(0, "take", "--owner", "chapter", "--wait-minutes", "0")
        fingerprint = self.lock(0, "status").split()[-1]
        self.lock(0, "refresh", "--owner", "chapter", "--note", "working")
        self.lock(1, "recover", "--fingerprint", fingerprint, "--confirm-no-work")
        self.guard(6, "--owner", "another")

    def test_live_holder_cannot_be_replaced_after_heartbeat_expiry(self):
        self.beacon()
        self.lock(0, "take", "--owner", "chapter", "--holder-pid", str(os.getpid()), "--wait-minutes", "0")
        record = self.locks / "editor.lock"
        os.utime(record, (1, 1))
        fingerprint = self.lock(0, "status").split()[-1]
        self.guard(6, "--owner", "Chapter")
        self.guard(0, "--owner", "chapter")
        self.lock(1, "recover", "--fingerprint", fingerprint, "--confirm-no-work")
        self.lock(1, "release", "--owner", "different")
        self.lock(1, "refresh", "--owner", "chapter")
        self.lock(1, "release", "--owner", "chapter")
        self.lock(0, "refresh", "--owner", "chapter", "--holder-pid", str(os.getpid()))
        self.lock(0, "release", "--owner", "chapter", "--holder-pid", str(os.getpid()))

    def test_holder_crash_needs_safe_state_and_current_fingerprint_to_recover(self):
        self.beacon()
        holder = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(30)"])
        try:
            self.lock(0, "take", "--owner", "lost", "--holder-pid", str(holder.pid), "--wait-minutes", "0")
        finally:
            holder.kill()
            holder.wait(timeout=5)
        self.assertIn("(gone)", self.lock(0, "status"))
        fingerprint = self.lock(0, "status").split()[-1]
        self.guard(6, "--owner", "lost")
        self.lock(1, "recover", "--fingerprint", "wrong", "--confirm-no-work")
        self.beacon(updating=True)
        self.lock(1, "recover", "--fingerprint", fingerprint, "--confirm-no-work")
        self.beacon()
        self.lock(0, "recover", "--fingerprint", fingerprint, "--confirm-no-work")
        self.lock(0, "take", "--owner", "restart", "--wait-minutes", "0")

    def test_concurrent_acquirers_for_editor_and_import_get_one_winner(self):
        self.beacon()
        contenders = [("editor", "a"), ("import", "b"), ("editor", "c")]
        processes = []
        try:
            for name, owner in contenders:
                cmd = [sys.executable, str(SCRIPTS / "editor_lock.py"), "take", "--name", name,
                       "--owner", owner, "--wait-minutes", "0.012"]
                processes.append(subprocess.Popen(cmd, env=self.env, text=True, stdout=subprocess.PIPE))
            for process in processes:
                process.communicate(timeout=6)
            self.assertEqual([0, 1, 1], sorted(process.returncode for process in processes))
        finally:
            for process in processes:
                if process.poll() is None:
                    process.kill()
                    process.communicate(timeout=5)
        winner = processes.index(next(p for p in processes if p.returncode == 0))
        name, owner = contenders[winner]
        self.lock(0, "release", "--name", name, "--owner", owner)
        self.lock(0, "take", "--owner", "next", "--wait-minutes", "0")


if __name__ == "__main__":
    unittest.main()
