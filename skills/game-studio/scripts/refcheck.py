# Read only. For every committed scene, every reference into a text asset (.asset/.mat/.controller/...) of this repo:
# does the referenced sub-object (fileID) exist in the committed copy of that asset (HEAD), and in the working copy?
# Why: a scene builder that writes its generated meshes into a .asset afresh on each rebuild gives them new fileIDs.
# Commit the scene without that .asset and a build made from git points at nothing: walls, road ends and whole tree
# rows were simply missing in the web build while the editor looked fine.
# Usage: python -I refcheck.py <repo> [--work] [--scenes Assets/]   exit code 1 when any scene has a broken reference.
# --work: check the scenes and assets on disk instead of HEAD (before committing a rebuild: what HEAD will be once both go in).
# --scenes: path prefix of the scenes to check (default Assets/).
# Run it in the build checkout before every build: a broken reference means no build.
import os, re, subprocess, sys, collections
repo = sys.argv[1]
work = '--work' in sys.argv[2:]
prefix = sys.argv[sys.argv.index('--scenes') + 1] if '--scenes' in sys.argv else 'Assets/'
def git(*a):
    return subprocess.run(['git', '-C', repo, *a], capture_output=True).stdout
files = git('ls-files', '-z').decode('utf-8', 'replace').split('\0')
guid_path = {}
for f in files:
    if f.endswith('.meta'):
        try:
            with open(os.path.join(repo, f), 'rb') as h:
                m = re.search(rb'guid: ([0-9a-f]{32})', h.read(400))
            if m:
                guid_path[m.group(1).decode()] = f[:-5]
        except OSError:
            pass
scenes = [f for f in files if f.endswith('.unity') and f.startswith(prefix)]
ref_re = re.compile(rb'fileID: (-?\d+), guid: ([0-9a-f]{32}), type: 2')
id_re = re.compile(rb'^--- !u!\d+ &(-?\d+)', re.M)
cache_head, cache_work = {}, {}
def ids_head(p):
    if p not in cache_head:
        cache_head[p] = set(id_re.findall(git('show', 'HEAD:' + p)))
    return cache_head[p]
def ids_work(p):
    if p not in cache_work:
        try:
            with open(os.path.join(repo, p), 'rb') as h:
                cache_work[p] = set(id_re.findall(h.read()))
        except OSError:
            cache_work[p] = set()
    return cache_work[p]
gone = collections.defaultdict(set)   # guid no file in this repo has -> scenes
bad = collections.defaultdict(lambda: [0, 0, 0, set()])   # asset -> refs, missing in HEAD, missing in work, scenes
for s in scenes:
    if work:
        with open(os.path.join(repo, s), 'rb') as h:
            text = h.read()
    else:
        text = git('show', 'HEAD:' + s)
    for fid, guid in set(ref_re.findall(text)):
        p = guid_path.get(guid.decode())
        if p is None:
            g = guid.decode()
            if not g.startswith('0000000000000000'):
                gone[g].add(s.split('/')[-1])
            continue
        if not p.endswith(('.asset', '.mat', '.controller', '.prefab', '.anim', '.overrideController', '.mask', '.physicMaterial', '.lighting')):
            continue
        head = ids_work(p) if work else ids_head(p)
        if not head:   # binary or not in HEAD
            continue
        e = bad[p]; e[0] += 1
        if fid not in head:
            e[1] += 1; e[3].add(s.split('/')[-1])
            if fid not in ids_work(p):
                e[2] += 1
dirty = set(git('diff', '--name-only').decode().split('\n'))
n = 0
for p, (refs, mh, mw, sc) in sorted(bad.items()):
    if mh:
        n += 1
        print(f'{p}: {mh}/{refs} refs missing in HEAD, {mw} still missing in working copy, uncommitted change: {p in dirty}; scenes: {", ".join(sorted(sc))}')
# A guid that no .meta here has may still be a package's (Packages/ and Library/PackageCache are not in git): list them, count only ours by name.
pkg = set()
for root in ('Packages', 'Library/PackageCache'):
    base = os.path.join(repo, root)
    for d, _, fs in os.walk(base):
        for f in fs:
            if f.endswith('.meta'):
                try:
                    with open(os.path.join(d, f), 'rb') as h:
                        m = re.search(rb'guid: ([0-9a-f]{32})', h.read(400))
                    if m:
                        pkg.add(m.group(1).decode())
                except OSError:
                    pass
missing_guids = {g: sc for g, sc in gone.items() if g not in pkg}
for g, sc in sorted(missing_guids.items()):
    n += 1
    print(f'guid {g}: no such asset in the repo or packages; scenes: {", ".join(sorted(sc))}')
print(f'{len(scenes)} scenes, {len(bad)} assets referenced, {n} with broken references in {"the working copy" if work else "HEAD"}')
sys.exit(1 if n else 0)
