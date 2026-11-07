---
name: game-studio
description: Run a studio of Claude Code sessions that builds a 3D game for the web — one human director, a commander session, worker sessions that own files, an image AI for concept art, Tripo (or another AI-3D tool) for characters, Blender scripts for buildings, Unity URP with WebGPU. Covers team setup (TEAM.md, CLAUDE.md, briefs, file ownership), scenes generated from code, story directors, the art pipeline (overview sheets, screenshot-first image orders, turnarounds, six-angle model checks, Mixamo), three-camera look passes, playtest feedback loops, overnight runs, safety rails for a shared Unity editor (play-mode guard, editor lock, staged scripts), a quality bar, a one-day hackathon plan, and shipping (web build, 60-second video, open-sourcing safely). Use whenever someone wants to make a 3D, story, exploration or web game with AI, turn a book or story into a playable world, coordinate several Claude sessions on one game, set up Unity/Blender/Tripo/GPT-image pipelines, plan a game jam or hackathon build, or fix multi-agent problems like broken compiles, lost deliveries or a frozen editor — even if they never say "studio".
license: MIT
---

# Game Studio

Distilled from *The Yellow Brick Road*: a 3D storybook web game of the 1900 *Wizard of Oz*, built in 17 days by one human director and ~20 Claude Code sessions (1,325 commits, 9 chapters, 13 playtest rounds). See `references/case-study.md`.

## The idea in five lines

1. **One human directs; AIs build.** The director decides, playtests, logs in and pays. Claude sessions do all engineering, scenes, animation, reviews and git. An image AI only draws. An AI-3D tool only makes characters.
2. **Files, not memory.** Roles, rules, decisions, plans and feedback live in the repo (`TEAM.md`, `CLAUDE.md`, briefs, backlog, devlog). Sessions are disposable; any of them can resume from the files.
3. **Scenes are generated from code.** No hand-edited scene files, so many sessions can rebuild, review and recover without merge pain.
4. **Looks are measured against references.** An overview sheet before building; three fixed cameras compared against it after every pass; every image order starts from a game screenshot.
5. **Rules are born from incidents, and backed by mechanisms.** Each rule carries the story of what broke. Where possible a script enforces it (guard, lock, size check) instead of a reminder.

## First: which mode?

- **A real project (days to weeks)**: follow "Start a project" below, then the production loop.
- **A hackathon or game jam (hours)**: read `references/hackathon.md` and follow its plan. Same principles, one scene, three sessions.
- **Joining an existing project**: read its `CLAUDE.md` and `TEAM.md` first; this skill fills gaps, the project's own rules win.

## Start a project

1. **Ask the director only what has no sensible default**: the story or source, the feel in a sentence, the platform, languages, the interact key. Default the rest and say so.
2. **Write the PRD** from `templates/PRD.md` (the "Lite" part first). Include a source boundary: what may and may not be used (a public-domain book may have a famous copyrighted adaptation whose inventions are off-limits).
3. **Set up the repo**: GitHub from hour one, push after every commit. Unity 6 URP project with WebGPU + WebGL2 web target; assembly definitions for Core/Gameplay/Narrative/Editor; copy `assets/unity/` and rename the namespaces. Copy `templates/CLAUDE.md`, `templates/TEAM.md`, `templates/RECOVERY.md` into place and fill them in.
4. **Install the safety rails** (`references/editor-safety.md`): `EditorStateBeacon.cs` in an Editor folder, `scripts/` into a tools folder outside `Assets/`, a lock folder all sessions share.
5. **Pick the style**: the same three scenes in two candidate styles from the image AI; the director picks; write the style and character blocks into `STYLE_GUIDE.md` (`templates/art-board.md`, `templates/prompts.md`).
6. **Plan the team** (`references/studio-roles.md`): a commander on the strongest model; one high-effort session per chapter or per system; an art pipeline session; a cheap editor custodian. Write each one a brief (`templates/brief.md`) that lists the files it owns.
7. **First chapter** through the production loop below. Write a devlog entry every day (`templates/devlog.md`).

## The production loop (per chapter)

```
chapter script (templates/chapter.md: beats, the screenshot moment, lines as keys)
→ overview reference sheet from the image AI → asset breakdown (AI 3D / Blender / CC0 / code / illustration)
→ graybox from a builder script, a collider decided for every object, layout checks
→ story director wired, all text from string tables
→ look pass: three cameras vs the sheet → real assets at the same paths → look pass → lighting → look pass
→ self-check against the quality bar → the director playtests → numbered feedback in their words → fixes → replay
→ stop at "99": checks pass, no new "ugly / stuck / clipping / ridiculous", they would share a screenshot
```

Details: `references/production-loop.md`, `references/art-pipeline.md`, `references/quality-bar.md`.

## Hard rules (and why each exists)

1. **The director does all playtesting; no session drives Play mode.** Play in a shared editor interrupts everyone and the human's judgement is the scarce resource. Edit-mode screenshots are fine for looks, never as a test of gameplay.
2. **The project must not change while the director plays.** Run the guard before any recompile, rebuild, reimport or write into `Assets/`; exit 3 = playing, go do something else. A file dropped mid-Play killed every coroutine and froze the story at its climax.
3. **One editor, one holder.** Only the custodian starts Unity; everyone takes `editor.lock`, works, saves, hands back to a light scene, releases. Every job holding the lock has a total deadline and releases in `finally` (one job without a deadline froze everyone for an hour).
4. **Stage co-dependent scripts outside `Assets/` and move them in together.** Unity compiles between file writes; half-written batches broke the build for every session.
5. **Verify artefacts, not exit codes.** A builder prints `Built <scene>` last; check that line or the scene file's time. Menu calls reported success after a builder threw.
6. **Commit by path, push every commit, nothing over 50 MB.** Sessions share one git index; one 105 MB file blocked every push. GitHub is the only backup.
7. **Decide a collider for every placed object** and write `Colliders: …` in the commit. Moving things go on Ignore Raycast; NavMesh bakes from physics colliders at load.
8. **Never take the camera away.** Story beats may hold the character still; the player can always look around. Lines spoken while the player can walk advance by themselves.
9. **No hard-coded player text**; every string in the tables. One interact key everywhere, in code, prompts and docs.
10. **Characters come from AI 3D, never from primitives; check the white model from six angles before paying for texture.** A second face on the back of a head survived two texture passes because only the front was checked.
11. **One T-pose avatar for every Mixamo clip.** Per-clip avatars bent every character's knees for two days of symptom patching.
12. **Every image order starts from a game screenshot** and is reviewed against that screenshot and the 3D models, not only against the order.
13. **Three tries without measured improvement: stop**, keep the last good version, report numbers, change approach. And confirm the direction before polishing details.
14. **Report only from evidence, and say what is unverified.** "Done" means built and checked; only the director's play makes it "verified". A complaint the director makes twice is the team's failure: find the root cause and fix it game-wide.
15. **Anything needing an account, payment, credits or accepting terms goes to the director.** Never type passwords; log every paid generation.

The full set with incidents: `references/pitfalls.md` (30 lessons) and `references/editor-safety.md`.

## How sessions talk

- Commander → session: a short message pointing at a brief or backlog item numbers. Long designs live in `docs/briefs/`.
- Claude → image AI: a message on top of `CHANNEL.md` plus the full prompt in `ART_QUEUE.md`. **Whoever orders picks up the delivery**: run a background watcher on the channel and concept folder, and on every completion read, review and restart it.
- Director → team: they talk; the commander writes a numbered backlog live, with their words quoted (`templates/backlog.md`).
- Team → director: before each playtest, "what changed, where to look, what is unverified"; after a night, a morning report and a decisions file (`templates/morning-report.md`). Never wait for an answer overnight: take a default, log it, continue.

## Reading guide

| When you are… | Read |
|---|---|
| Setting up roles, adding a session, writing a brief | `references/studio-roles.md` |
| Planning a chapter, running look passes, handling feedback, leaving work overnight | `references/production-loop.md` |
| About to deliver anything | `references/quality-bar.md` |
| Ordering or reviewing images, deciding who makes an asset | `references/art-pipeline.md`, `templates/prompts.md` |
| Making a character, importing animations, judging a walk | `references/characters.md` |
| Modelling a building or prop, accepting a model | `references/props-and-buildings.md` |
| Writing engine code: builders, story, camera, colliders, localization, web build | `references/unity-architecture.md`, `assets/unity/` |
| Touching the shared editor: guard, lock, rounds, shots | `references/editor-safety.md`, `scripts/` |
| Something smells familiar | `references/pitfalls.md` |
| In a hackathon | `references/hackathon.md` |
| Releasing: web build, video, launch post, open source | `references/shipping.md` |
| Explaining the method, calibrating expectations | `references/case-study.md` |

## Bundled files

- `templates/`: `CLAUDE.md`, `TEAM.md`, `PRD.md`, `chapter.md`, `brief.md`, `backlog.md`, `morning-report.md`, `night-plan.md`, `devlog.md`, `RECOVERY.md`, `art-board.md` (style guide, task board, channel), `prompts.md` (11 image and review prompts).
- `assets/unity/Runtime/`: `StoryDirectorBase` (Conversation / Monologue / Musing, camera never taken), `Loc` + `LocText` (TSV string tables), `InteractPoint` + `PlayerInteractor` (one rebindable key, press and hold), `GripSocket` (props keep real size in scaled rigs), `RuntimeNavMeshBaker`. `assets/unity/Editor/`: `Level01Builder` (scene-from-code template), `ReviewShots` (edit-mode review cameras), `EditorStateBeacon` (state file for the guard; pauses imports during Play). `assets/unity/Examples/Level01Director.cs`. They compile against Unity 6; treat them as starting points.
- `scripts/`: `guard` (PowerShell and bash), `lock`, `playing.cs`, `hand_back.cs`, `round.ps1` (a complete editor round with a deadline). Paths come from environment variables; nothing is hard-coded to one machine.

Scripts live next to this file; resolve them relative to this SKILL.md's folder (for a plugin install, that is under the plugin's `skills/game-studio/`).
