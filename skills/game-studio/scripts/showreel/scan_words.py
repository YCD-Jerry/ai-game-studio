#!/usr/bin/env python3
"""scan_words.py <word_list.txt> <file> [file ...]

Searches each file, byte by byte and ignoring case, for every word in the list (one per line): private account names,
email addresses, local paths. Keep the list OUTSIDE the repository, so the private words themselves are never committed.
Exit code 1 if anything is found. Run it on every video, image and build before it goes out.
"""
import sys

if len(sys.argv) < 3:
    sys.exit(__doc__)
words = [w.strip().lower() for w in open(sys.argv[1], encoding="utf8") if w.strip()]
found = False
for path in sys.argv[2:]:
    data = open(path, "rb").read().lower()
    hits = [w for w in words if w.encode("utf8") in data or w.encode("utf-16-le") in data]
    print(path, "hits:", len(hits))
    found = found or bool(hits)
sys.exit(1 if found else 0)
