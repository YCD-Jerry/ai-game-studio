"""Regression tests for the reference checker, using real temporary Git repositories."""

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / "skills/game-studio/scripts/refcheck.py"
GUID = "1234567890abcdef1234567890abcdef"
OTHER_GUID = "abcdef1234567890abcdef1234567890"


class ReferenceCheckTest(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.repo = Path(temp.name)
        assets = self.repo / "Assets"
        assets.mkdir()
        self.asset = assets / "Mesh.asset"
        self.meta = assets / "Mesh.asset.meta"
        self.scene = assets / "Level.unity"
        self.asset.write_bytes(b"--- !u!43 &4300000\n")
        self.meta.write_text(f"guid: {GUID}\n", encoding="utf-8")
        self.scene.write_text(
            f"m_Mesh: {{fileID: 4300000, guid: {GUID}, type: 2}}\n",
            encoding="utf-8",
        )
        self.git("init", "-q")
        self.commit()

    def git(self, *args):
        subprocess.run(
            ["git", "-C", str(self.repo), *args], check=True, capture_output=True,
        )

    def commit(self):
        self.git("add", "-A")
        self.git(
            "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
            "-c", "commit.gpgsign=false", "commit", "-qm", "fixture",
        )

    def run_check(self, expected, *args):
        result = subprocess.run(
            [sys.executable, "-I", str(SCRIPT), str(self.repo), *args],
            text=True, capture_output=True,
        )
        self.assertEqual(result.returncode, expected, result.stdout + result.stderr)
        return result.stdout + result.stderr

    def test_valid_reference_and_changed_file_id(self):
        self.run_check(0)
        self.run_check(0, "--work")
        self.asset.write_bytes(b"--- !u!43 &4300001\n")
        self.assertIn("missing fileID 4300000", self.run_check(1, "--work"))
        self.run_check(0)
        self.commit()
        self.assertIn("missing fileID 4300000", self.run_check(1))

    def test_missing_asset_with_meta_is_rejected(self):
        self.asset.unlink()
        self.assertIn("missing asset Assets/Mesh.asset", self.run_check(1, "--work"))
        self.run_check(0)
        self.commit()
        self.assertIn("missing asset Assets/Mesh.asset", self.run_check(1))

    def test_head_ignores_working_tree_meta(self):
        self.meta.write_text(f"guid: {OTHER_GUID}\n", encoding="utf-8")
        self.run_check(0)
        self.assertIn(f"unknown GUID {GUID}", self.run_check(1, "--work"))

    def test_new_untracked_assets_are_scanned_in_work_mode(self):
        replacement = self.asset.with_name("New.asset")
        self.asset.rename(replacement)
        self.meta.rename(replacement.with_suffix(".asset.meta"))
        self.run_check(0, "--work")
        self.run_check(0)

    def test_binary_asset_only_checks_existence(self):
        self.asset.write_bytes(b"\0binary\0")
        self.run_check(0, "--work")
        self.commit()
        self.run_check(0)

    def test_resolved_package_guid_is_accepted(self):
        meta = self.repo / "Library/PackageCache/sample/mesh.asset.meta"
        meta.parent.mkdir(parents=True)
        (self.repo / ".gitignore").write_text("Library/\n", encoding="utf-8")
        meta.with_suffix("").write_bytes(b"package asset")
        meta.write_text(f"guid: {OTHER_GUID}\n", encoding="utf-8")
        self.scene.write_text(
            f"m_Mesh: {{fileID: 4300000, guid: {OTHER_GUID}, type: 2}}\n",
            encoding="utf-8",
        )
        self.run_check(0, "--work")

    def test_empty_scope_fails_explicitly(self):
        self.assertIn("No scenes found", self.run_check(2, "--scenes", "Missing/"))


if __name__ == "__main__":
    unittest.main()
