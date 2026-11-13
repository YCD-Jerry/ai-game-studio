# <Project> — agent instructions

Adapt for CLAUDE.md or AGENTS.md. Keep only the project's active rules and link their owning documents. The paths and workflows below are examples.

<One paragraph: what the game is, engine and target (e.g. Unity 6 URP, WebGPU web build), the source and what is off-limits (e.g. "the 1900 book, never the 1939 film").> Reply to the director in <language>.

## Read first
- `SourceArt/TEAM.md`: goal, roles, file ownership, rules, decision log. Update its progress section when you finish a step and record the director's decisions there.
- `docs/PRD.md`, `docs/chapters/`, `docs/QUALITY_BAR.md`, `SourceArt/STYLE_GUIDE.md`, `SourceArt/ART_QUEUE.md`.

## Where things are
- Scenes are generated from code; never hand-edit `.unity` files. Builders: `Assets/Game/Scripts/Editor/<Name>Builder.cs` → menu `<Project>/Build/<Scene>`.
- Player-facing text: `Assets/Game/Localization/Source/*.tsv` (key, <lang1>, <lang2>); import with `<Project>/Setup/Import Localization`. Never hard-code text.
- Blender build scripts: `SourceArt/blender/build/*.py`, run headless: `blender -b --factory-startup --python <script> -- --root <project path>`.
- Editor helper scripts (guard, lock, hand back, shots): `<tools dir>` (outside `Assets/`).

## Working with the director
- **Verification:** <static commands, visual capture, isolated runtime test location, human acceptance owner>. Record results and outstanding checks.
- **Never change the project while the director plays.** Run `guard` before any recompile, rebuild, reimport or write into `Assets/`; exit 3 means playing: do other work and come back. Stage new scripts in your own folder outside `Assets/` and move them in together.
- **One editor**: only <custodian session> starts it; take `editor.lock`; save all and run `hand_back` when done.
- **Never take the camera away.** Story beats may hold the character still; the player can always look around. Lines spoken while the player can walk advance by themselves.
- **Colliders**: decide one for every placed object and say so in the commit. Solid: rocks, bushes, trunks, fences, furniture, buildings, door leaves in every state, characters (kinematic). Not solid: grass, flowers, pebbles, water, effects.
- **Check imported models' real size in the scene.**
- **Prefer ready-made assets** (CC0 packs) before modelling or generating; record sources in the art folder.
- **Characters come from AI 3D** (turnaround → Tripo); buildings and fine props from the modelling session in Blender.
- **Interact key is always <KEY>**, in code, prompts and docs.
- **Work continuously**: review → integrate → check → commit → next. Stop only for purchases, logins, paid generations, or creative calls with no sensible default; otherwise pick a default, log it for the director, keep going.
- **Three tries without improvement: stop**, report numbers to the commander, change approach.

## Git
- <Git owner, commit and push policy, asset limits and backup location>.
- Message: item number first (`N-12: …`), `Colliders: …` last.
- Daily devlog `docs/devlog/YYYY-MM-DD.md` (honest, mistakes included), written by the commander each evening.
- Record decisions and recovery steps in their owning project documents.
