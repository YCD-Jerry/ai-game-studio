#!/usr/bin/env python3
"""Run one showreel shot in the editor that is already in Play (enter it with the guard and lock first).

run_shot.py <shot.cs> [every] [timeout_s]
  every    30 = preview (one frame in thirty, saved as <shot>_p), 1 = the real take. Default 30.
  timeout  seconds to wait for the shot's status file. Default 600.

Joins capture_lib.cs and the shot, runs it with `unity command script eval_file`, then waits for
$SHOWREEL_ROOT/frames/status.txt, which the shot writes through Finish(). Needs UNITY_PROJECT_PATH and SHOWREEL_ROOT.
"""
import os
import subprocess
import sys
import time

here = os.path.dirname(os.path.abspath(__file__))
if len(sys.argv) < 2:
    sys.exit(__doc__)
shot = sys.argv[1]
every = sys.argv[2] if len(sys.argv) > 2 else "30"
timeout = int(sys.argv[3]) if len(sys.argv) > 3 else 600
root = os.environ.get("SHOWREEL_ROOT")
if not root or not os.environ.get("UNITY_PROJECT_PATH"):
    sys.exit("set SHOWREEL_ROOT (frames, clips, output) and UNITY_PROJECT_PATH")

lib = open(os.path.join(here, "capture_lib.cs"), encoding="utf8").read()
body = open(shot, encoding="utf8").read()
src = (lib + "\n" + body).replace("ROOT", root.replace("\\", "/")) \
    .replace("EVERY", every).replace("SUFFIX", "" if every == "1" else "_p")

frames = os.path.join(root, "frames")
os.makedirs(frames, exist_ok=True)
status = os.path.join(frames, "status.txt")
if os.path.exists(status):
    os.remove(status)
built = os.path.join(frames, "_built_shot.cs")   # the bridge reads a file; keep it out of the project
open(built, "w", encoding="utf8").write(src)

r = subprocess.run(["unity", "command", "script", "eval_file", "--file", built],
                   capture_output=True, text=True, encoding="utf8", errors="replace")
print(r.stdout[-900:], r.stderr[-1500:])
if r.returncode != 0 or "started" not in r.stdout:
    sys.exit(1)

t0 = time.time()
while time.time() - t0 < timeout:
    if os.path.exists(status):
        print(round(time.time() - t0), "s:", open(status, encoding="utf8").read())
        sys.exit(0)
    time.sleep(2)
print("timed out: leave Play, hand the editor back and drop the lock before trying again")
sys.exit(2)
