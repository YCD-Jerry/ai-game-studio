#!/usr/bin/env python3
"""contact.py <shot> <first> <last> <step> <out.png> [cols]

A contact sheet of frames first..last (every step-th) from $SHOWREEL_ROOT/frames/<shot>/, flipped upright, 640x360 each.
Look at it before recording a shot in full: framing, timing, T-poses, flat-cyan materials, gizmos, debug text.
"""
import os
import subprocess
import sys

if len(sys.argv) < 6:
    sys.exit(__doc__)
root = os.environ.get("SHOWREEL_ROOT") or sys.exit("set SHOWREEL_ROOT")
shot, out = sys.argv[1], sys.argv[5]
first, last, step = map(int, sys.argv[2:5])
cols = int(sys.argv[6]) if len(sys.argv) > 6 else 3
d = os.path.join(root, "frames", shot)
files = [os.path.join(d, f"{i:05d}.png") for i in range(first, last + 1, step)]
files = [f for f in files if os.path.exists(f)]
if not files:
    sys.exit("no frames found in " + d)
w, h = 640, 360
args = ["ffmpeg", "-y", "-v", "error"]
for f in files:
    args += ["-i", f]
graph = "".join(f"[{i}]vflip,scale={w}:{h}[v{i}];" for i in range(len(files)))
if len(files) == 1:
    graph = f"[0]vflip,scale={w}:{h}"
else:
    layout = "|".join(f"{i % cols * w}_{i // cols * h}" for i in range(len(files)))
    graph += "".join(f"[v{i}]" for i in range(len(files))) + f"xstack=inputs={len(files)}:layout={layout}"
subprocess.run(args + ["-filter_complex", graph, "-frames:v", "1", out], check=True)
print(out, len(files), "frames")
