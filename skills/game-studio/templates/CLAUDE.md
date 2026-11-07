# <Project> — notes for Claude

<One paragraph: what the game is, engine and target (e.g. Unity 6 URP, WebGPU web build), the source and what is off-limits (e.g. "the 1900 book, never the 1939 film").> Reply to the director in <language>.

## Read first
- `SourceArt/TEAM.md`: goal, roles, file ownership, rules, decision log. Update its progress section when you finish a step and record the director's decisions there.
- `docs/PRD.md`, `docs/chapters/`, `docs/QUALITY_BAR.md`, `SourceArt/STYLE_GUIDE.md`, `SourceArt/ART_QUEUE.md`.

## Where things are
- Scenes are generated from code; never hand-edit `.unity` files. Builders: `Assets/_Project/Scripts/Editor/<Name>Builder.cs` → menu `<Project>/Build/<Scene>`.
- Player-facing text: `Assets/_Project/Localization/Source/*.tsv` (key, <lang1>, <lang2>); import with `<Project>/Setup/Import Localization`. Never hard-code text.
- Blender build scripts: `SourceArt/blender/build/*.py`, run headless: `blender -b --factory-startup --python <script> -- --root <project path>`.
- Editor helper scripts (guard, lock, hand back, shots): `<tools dir>` (outside `Assets/`).

## Working with the director
- **The director does all testing.** Don't enter or drive Play mode. Compile checks, data checks (eval scripts reading the scene) and edit-mode screenshots for looks are fine. Write, check, commit, push, then say exactly what to look at and what is not verified.
- **Never change the project while the director plays.** Run `guard` before any recompile, rebuild, reimport or write into `Assets/`; exit 3 means playing: do other work and come back. Stage new scripts in your own folder outside `Assets/` and move them in together.
- **One editor**: only <custodian session> starts it; take `editor.lock`; save all and run `hand_back` when done.
- **Never take the camera away.** Story beats may hold the character still; the player can always look around. Lines spoken while the player can walk advance by themselves.
- **Colliders**: decide one for every placed object and say so in the commit. Solid: rocks, bushes, trunks, fences, furniture, buildings, door leaves in every state, characters (kinematic). Not solid: grass, flowers, pebbles, water, effects.
- **Check imported models' real size in the scene.**
- **Prefer ready-made assets** (CC0 packs) before modelling or generating; record sources in the art folder.
- **Characters come from AI 3D** (turnaround → Tripo); buildings and fine props from the modelling session in Blender.
- **Interact key is always <F>**, in code, prompts and docs.
- **Work continuously**: review → integrate → check → commit → next. Stop only for purchases, logins, paid generations, or creative calls with no sensible default; otherwise pick a default, log it for the director, keep going.
- **Three tries without improvement: stop**, report numbers to the commander, change approach.

## Git
- Only Claude runs git. Commit by path (shared index): `git commit -m "<msg>" -- <paths>`. Push after every commit. Nothing over 50 MB.
- Message: item number first (`N-12: …`), `Colliders: …` last.
- Daily devlog `docs/devlog/YYYY-MM-DD.md` (honest, mistakes included), written by the commander each evening.
- GitHub is the only backup: every decision and workflow lives in the repo, never only in chat.
