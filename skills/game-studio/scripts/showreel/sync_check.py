#!/usr/bin/env python3
"""sync_check.py <master.mp4> <song> <song_cut_in_s> [expected_film_s]

Finds where the music really starts in the finished film by cross-correlating the source song with the decoded master
(mono, 8 kHz). song_cut_in_s is where the edit starts the song (e.g. 0.25 if the edit trims its first 0.25 s).
Prints the film time the song starts at; compare it with the frame the edit meant (e.g. the cut to the first big shot).
Needs ffmpeg and numpy.
"""
import subprocess
import sys

import numpy as np

if len(sys.argv) < 4:
    sys.exit(__doc__)
master, song, cut_in = sys.argv[1], sys.argv[2], float(sys.argv[3])
expected = float(sys.argv[4]) if len(sys.argv) > 4 else None
RATE = 8000
LEAD = 2.0   # correlate on a stretch 2 s into the song, past any fade-in


def pcm(path, start=0.0, dur=None):
    a = ["ffmpeg", "-v", "error", "-ss", str(start)] + (["-t", str(dur)] if dur else []) + \
        ["-i", path, "-ac", "1", "-ar", str(RATE), "-f", "f32le", "-"]
    return np.frombuffer(subprocess.run(a, capture_output=True, check=True).stdout, dtype=np.float32)


ref = pcm(song, cut_in + LEAD, 6.0)
window = (expected or 0.0) + 12.0
mix = pcm(master, 0.0, window)
c = np.correlate(mix, ref, "valid")
start = int(np.argmax(c)) / RATE - LEAD
line = "song starts at film %.3f s" % start
if expected is not None:
    line += " (expected %.3f, off by %.0f ms)" % (expected, (start - expected) * 1000)
print(line)
