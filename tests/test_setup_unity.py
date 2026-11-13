"""Exercise the Unity installer against disposable projects, without starting Unity."""

import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "skills/game-studio/scripts/setup_unity.py"
BEACON = Path("Assets/Editor/GameStudio/EditorStateBeacon.cs")
BEACON_SOURCE = SCRIPT.parents[1] / "assets/unity/Editor/EditorStateBeacon.cs"


class UnitySetupTest(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.project = Path(temp.name) / "Unity fixture with spaces"
        for directory in ("Assets", "Packages", "ProjectSettings"):
            (self.project / directory).mkdir(parents=True)
        (self.project / "ProjectSettings/ProjectVersion.txt").write_text(
            "m_EditorVersion: 6000.3.0f1\n", encoding="utf-8"
        )
        self.manifest = self.project / "Packages/manifest.json"
        self.manifest.write_text('{"dependencies":{}}', encoding="utf-8")

    def run_setup(self, code, action, *options, env=None):
        result = subprocess.run(
            [sys.executable, "-E", str(SCRIPT), action, "--project", str(self.project), *options],
            env=env,
            text=True,
            capture_output=True,
            timeout=10,
        )
        output = result.stdout + result.stderr
        self.assertEqual(result.returncode, code, output)
        return output

    def snapshot(self):
        return {
            path.relative_to(self.project).as_posix(): (path.read_bytes(), path.stat().st_mtime_ns)
            for path in self.project.rglob("*")
            if path.is_file()
        }

    def test_install_requires_closed_editor_and_is_idempotent(self):
        config = self.project / ".claude/settings.json"
        config.parent.mkdir()
        config.write_text('{"other":true}', encoding="utf-8")
        before = self.snapshot()
        self.assertIn("--editor-closed", self.run_setup(2, "install"))
        self.assertEqual(before, self.snapshot())

        self.run_setup(0, "install", "--editor-closed")
        installed = self.snapshot()
        self.assertEqual(installed[".claude/settings.json"], before[".claude/settings.json"])
        self.assertIn(BEACON.as_posix(), installed)
        self.assertIn("tools/game-studio/guard.sh", installed)
        self.assertIn("tools/game-studio/hooks/settings.example.json", installed)
        self.assertNotIn("tools/game-studio/setup_unity.py", installed)
        self.assertIn("already installed", self.run_setup(0, "install"))
        self.assertEqual(installed, self.snapshot())

    def test_conflict_preflight_keeps_other_files_from_being_written(self):
        target = self.project / BEACON
        target.parent.mkdir(parents=True)
        target.write_text("// existing beacon", encoding="utf-8")
        before = self.snapshot()
        self.assertIn("different file", self.run_setup(1, "install", "--editor-closed"))
        self.assertEqual(before, self.snapshot())

    def test_manual_beacon_location_does_not_get_a_duplicate(self):
        target = self.project / "Assets/MyTools/Editor/EditorStateBeacon.cs"
        target.parent.mkdir(parents=True)
        target.write_bytes(BEACON_SOURCE.read_bytes())
        self.run_setup(0, "install")
        self.assertEqual([target], list((self.project / "Assets").rglob(BEACON.name)))

    def test_editor_lock_or_fresh_beacon_blocks_new_beacon(self):
        temp = self.project / "Temp"
        temp.mkdir()
        lock = temp / "UnityLockfile"
        lock.touch()
        before = self.snapshot()
        self.assertIn("UnityLockfile", self.run_setup(2, "install", "--editor-closed"))
        self.assertEqual(before, self.snapshot())
        lock.unlink()
        (temp / "editor_state.txt").write_text("playing=True", encoding="utf-8")
        before = self.snapshot()
        self.assertIn("written recently", self.run_setup(2, "install", "--editor-closed"))
        self.assertEqual(before, self.snapshot())

    def test_doctor_validates_project_and_reports_missing_install(self):
        self.assertIn("Missing: tools/game-studio/guard.sh", self.run_setup(1, "doctor"))
        self.manifest.write_text("[", encoding="utf-8")
        self.assertIn("invalid JSON in manifest.json", self.run_setup(2, "doctor"))

    def test_cli_doctor_reports_prerequisites_without_invoking_bridge(self):
        self.run_setup(0, "install", "--editor-closed")
        env = dict(os.environ, UNITY_CLI=sys.executable)
        self.assertIn("Unity Pipeline: not found", self.run_setup(1, "doctor", "--probe", "cli", env=env))
        self.manifest.write_text(
            json.dumps({"dependencies": {"com.unity.pipeline": "0.1.0"}}), encoding="utf-8"
        )
        before = self.snapshot()
        self.assertIn("prerequisites found", self.run_setup(0, "doctor", "--probe", "cli", env=env))
        self.assertEqual(before, self.snapshot())


if __name__ == "__main__":
    unittest.main()
