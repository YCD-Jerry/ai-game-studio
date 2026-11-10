#!/usr/bin/env python3
"""frames_to_clip.py <shot> [start_frame] -> $SHOWREEL_ROOT/clips/<shot>.mp4

Turns $SHOWREEL_ROOT/frames/<shot>/NNNNN.png into a near-lossless intermediate (H.264, CRF 10, 60 fps, BT.709),
flipped the right way up (frames grabbed from the back buffer arrive upside down). The edit cuts from these clips.
"""
import os
import subprocess
import sys

if len(sys.argv) < 2:
    sys.exit(__doc__)
root = os.environ.get("SHOWREEL_ROOT") or sys.exit("set SHOWREEL_ROOT")
shot = sys.argv[1]
start = sys.argv[2] if len(sys.argv) > 2 else "0"
src = os.path.join(root, "frames", shot, "%05d.png")
os.makedirs(os.path.join(root, "clips"), exist_ok=True)
out = os.path.join(root, "clips", shot + ".mp4")
subprocess.run(["ffmpeg", "-y", "-v", "error", "-framerate", "60", "-start_number", start, "-i", src,
                "-vf", "vflip,format=yuv420p", "-c:v", "libx264", "-preset", "slow", "-crf", "10",
                "-colorspace", "bt709", "-color_primaries", "bt709", "-color_range", "tv", out], check=True)
print(out)
