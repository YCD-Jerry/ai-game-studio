# Web packaging: chapter by chapter, measured first

Read this when the web build is too big to start quickly, when a web build looks different from the editor, or before you add optional content (a reward, an extra scene) to a web game.

The case: a 9-chapter 3D story game, about 1.9 GB of raw content. One web build was 842 MB to download before the title page. The fix was to load each chapter's content when it is needed, keep the full-quality assets, and spread the download along the game.

## Contents
1. Measure before you split
2. The structure: one boot scene, everything else in packs
3. Group automatically by "who uses it"
4. One gate for all loading
5. An open map: her tile first, seams shut until loaded
6. Optional content on demand
7. Building and checking
8. When the web build differs from the editor

## 1. Measure before you split

Guesses were wrong four times in a row, so measure each one before you act on it.

- **Parse the finished build.** Read every object's real size and format (UnityPy, read only), then follow references from each scene and `Resources` back to source files. Check the two totals against each other before trusting either.
- **Know which leg a ratio measures.** The web data file was already LZ4 inside, so "Brotli saves 23%" measured LZ4 → Brotli, not raw → download. Multiplying it onto raw sizes overestimated the download by about 70%.
- **The web is not the desktop build.** For the same content:
  - Meshes were 24% bigger: vertex channels were not compressed.
  - Audio was 2.7× bigger.
  - The two largest shaders were about 10× bigger.
  - Tree textures were smaller, because the web had a lower import cap.

  Measure on the web target.
- **Find out what the first pack really pulls in:**
  - The menu scene pulled in 93 uncompressed images.
  - A warm-up list in `Resources` pulled in the whole of chapter 1.
  - One scene referenced every audio clip.

  Each of these, not the size of the chapters, set the size of the first pack.
- **Check that compression actually happens.** Non-power-of-two images marked "High Quality" compressed were stored as raw RGBA32: 451 MB. Run a probe that reports each texture's real stored format. Never infer it from the import settings.

## 2. The structure: one boot scene, everything else in packs

- **Only a nearly empty `Boot` scene is built in.** Everything else goes into Addressables, including the menu.
- An asset used by both a built-in scene and a pack is stored twice. The first split kept the menu built in and *grew* the build from 1126 MB to 1405 MB. Moving the menu into the packs too brought it to 1053 MB, with 21 MB of data before the title page.
- Keep the desktop build fully local and untouched. The split is applied only in the web build step (see §7), and restored from git afterwards.

## 3. Group automatically by "who uses it"

- **Group assets by the exact set of scenes that reference them.** Assets with the same set go in one pack. Sets under about 2 MB merge into a shared pack.
- Hand-made "chapter" and "shared" groups go stale as content changes. Worse, one small shared file then drags a whole shared pack into a chapter that barely needs it.
- **Generate the groups at build time** from the references, and print a table of what each entry point must download.
- Run an overlap check after each build (`pack_overlap`): no asset should be in two packs, and nothing should be in a pack and the data file at once.

## 4. One gate for all loading

- **Every scene load and asset fetch goes through one class** (call it `ContentGate`): `OpenAdditive`, `Fetch`, `FetchAll`, `Release`. It owns retries, timeouts, a progress number for the loading screen, and a clear error if a download fails.
- Fill in Addressables' default `RetryCount 0` and `Timeout 0` yourself.
- **The web player cannot load Addressables synchronously.** A web build was entirely black, with 19 console lines saying so. The sync reads were a localization "selected locale" call in five files. The first search had looked only for the string-lookup API, so it missed them. Search for every API that can block, not one name.
- List every entry path that assumed content was local, and route each through the gate:
  - menu to chapter
  - map to chapter
  - save to chapter
  - skip
  - overlays (a map or menu opened mid-game)
- **Release what an overlay loaded when it closes.** Unloading the scene does not free its assets.

## 5. An open map: her tile first, seams shut until loaded

The world is one big map in tiles. The director picked this plan:

- Load the world shell plus the tile she stands on first. Prefetch the other tiles in the background, nearest first.
- **A seam stays shut until the tile behind it has loaded**, with a small waiting sign.
- Never "open anyway after 20 s": walking into empty ground is worse than waiting.
- Watch memory as well as download. A pack stays in the browser heap until it is unloaded. One seam briefly had four tiles in memory.

## 6. Optional content on demand

The reward ride flies over the whole map and starts in the real capital city.

- **Reuse, don't copy.** A generator builds the ride's city scene **with zero scripts, referencing only the original chapter's meshes and materials**. It refuses to save if it would create a new asset or script.
  - The source chapter's rebuild rule gains a line: "rerun the reward generator after this chapter rebuilds".
  - Interiors invisible from the sky (the throne room, the gatehouse) are left out: 28 MB saved.
- **Load and unload under cover.** The city loads before the ride. During a white-out in the clouds the scene is unloaded, its pack is released and its assets are collected, before the next region fades in.
- Put it in its own on-demand group, about 125 MB plus 5 MB for the ride. Tell the player before the extra download, about a minute at 2 MB/s.
- **Quality first:** the ride flies low past the buildings, so the top LOD stays. Dropping it would have saved about 80 MB unpacked. The director had ruled that quality is never cut to make the web build smaller.

## 7. Building and checking

- **Build in a separate checkout** (a release clone), never in the shared editor. A full Brotli web build of this game took 107 minutes.
- The split step rewrites groups, texture settings and the scene list. It restores them from git in `finally`. A build that dies mid-way leaves those changes behind, so the next build restores tracked files first, and untracked leftovers (the generated `Boot` scene) are deleted.
- Keep three build flavours:
  - **Release:** Brotli with a decompression fallback.
  - **Fast test:** uncompressed, never shipped.
  - **Fast development test:** adds a diagnostics overlay (F9) that only exists in development builds, for web-only bugs. Examples: the worst camera frame in the last few seconds, mouse counts, pointer-lock mode, LOD state.
- **Before every build, in that checkout:**
  1. Run `scripts/refcheck.py` (see §8); it must exit 0.
  2. Grep the output for private names and addresses.
  3. **Rename** the previous build folder; never delete it. A session once deleted the build the director was testing.
- Version directories stay immutable on the server; keep the old one a while. Browsers revalidate cached packs, so the host must send `ETag` or `Last-Modified`.

## 8. When the web build differs from the editor

The director's report: "the pit in front of her is a flat green plane, the bricks under her feet are gone, and the trees appear only when I walk to the cliff edge; step back ten metres and they vanish. I even suspect you optimised something and here it became a burden."

- **Guesses that were wrong:**
  - shader stripping
  - readable-mesh flags
  - the mobile quality level's LOD bias, which did matter for distant trees but was not the cause of the holes
- **The real cause.** Scene generators wrote their meshes into a `.asset` afresh on every rebuild, with new fileIDs. Rebuilt scenes were committed **without** those `.asset` files. The editor read the working copy and looked fine. The build checkout was made from git, so its scenes pointed at sub-objects that did not exist: holes, missing road ends, whole rows of trees.
- **The fixes, in layers:**
  1. Commit the missing assets.
  2. Add `scripts/refcheck.py` (checks direct scene references against HEAD; `--work` checks the working tree before committing), run before every build.
  3. Make the generators overwrite existing meshes in place, so fileIDs stay stable across rebuilds.
  4. Add the rule "a rebuilt scene is committed together with its generated assets".
- **Rule:** when a build differs from the editor, first compare what the build was made from (git) with what the editor shows (the working copy), before suspecting the platform.

Also web-only, and only visible in a real browser:

- **The camera "switched angle" on fast mouse flings.** There were two causes:
  - The camera's obstacle avoidance snapped in and out with no damping.
  - The browser delivered occasional huge mouse deltas.

  The first fix (damping, plus a filter for lone spikes) passed a robot sweep in the editor, which cut jumps over 1.5 m from 89 to 9, but did not fix the browser. Round two asked the browser for raw mouse movement (Pointer Lock with `unadjustedMovement`, falling back where unsupported) and logged which mode it got. **Test input fixes in the real browser.**
