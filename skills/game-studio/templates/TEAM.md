# Team Guide

> Example for a team working on a story game. Fill in active roles and retain only relevant rules. Record which older rule each decision supersedes.

## 1. What we are making

**<Title>** — <one line: what the player does, where, in what mood>.

- Story: <source, 2–3 sentences>.
- Theme: <one sentence>.
- Feel: <e.g. like turning the pages of a picture book that moves: slow, gentle, safe, no fail states; every chapter has one moment worth a screenshot>.
- Platform: <e.g. desktop browser, WebGPU with WebGL2 fallback, languages>.
- Design doc: `docs/PRD.md`. Chapter scripts: `docs/chapters/`. Quality bar: `docs/QUALITY_BAR.md`.

| # | Chapter | Status |
|---|---|---|
| 0 | <name> | graybox / art / look pass N / awaiting playtest / signed off |

**Current focus:** <one line>.

## 2. Shared goals and quality standard

1. **Rather small than rough.** Small scenes, every corner worth looking at.
2. **One style.** Every image, model and animation looks like it comes from the same book (`SourceArt/STYLE_GUIDE.md`).
3. **Source boundary.** <May use: …> / <May not use: …>.
4. **Runs well where players are.** <target hardware and browsers>.
5. **Everything traceable.** Versions go up, nothing is overwritten, decisions are recorded below.

## 3. Team and roles

| Session | Model | Owns (writes) | Notes |
|---|---|---|---|
| Commander | <strongest> | this file's decisions, backlog, briefs, image orders, CLAUDE.md, devlog | reviews, dispatches, decides; no hands-on work |
| <Chapter 1> | <strong, high effort> | `Ch1Builder.cs`, `Ch1Director.cs`, `Ch1.tsv`, `docs/chapters/ch1*` | shared scripts only via the commander |
| Systems | <strong> | player, camera, HUD, story director, followers, menus, saves | |
| Art pipeline | <strong> | `SourceArt/`, `Art/Characters`, `Art/Environment`, ART_QUEUE, CHANNEL | only session that reimports characters and uses the logged-in browser |
| Editor custodian | <cheap> | starts the editor; editor safety settings | |

| Member | Role | Does | Does not |
|---|---|---|---|
| <Director> (human) | Director, final say | direction, human playtesting, logins, purchases, paid generations | write code |
| Claude sessions | Engineering, tech art, animation, reviews, git | all development | log in, pay |
| Image AI | Concept art, chores | images per ART_QUEUE; edits only `SourceArt/` | engine, code, git, logins |

**When a new AI joins:** read this file and your domain doc, add a row above, post an intro in CHANNEL saying which files you will change.

## 4. File map

| Path | Content | Who edits |
|---|---|---|
| `SourceArt/TEAM.md` | this file | everyone (add, never delete) |
| `SourceArt/STYLE_GUIDE.md` | style and character prompt blocks | Claude; director approves changes |
| `SourceArt/ART_QUEUE.md` | image task board | Claude adds/reviews; image AI updates status |
| `SourceArt/CHANNEL.md` | Claude ↔ image AI messages | both |
| `SourceArt/concept/` | images, `ref/<TASK>/` inputs | image AI adds, never overwrites |
| `Assets/` | the game | Claude only |
| `docs/` | PRD, chapters, backlog, reviews, devlog | Claude |

## 5. How we work

- Pipeline: chapter script → overview sheet → asset breakdown → graybox from code → assets → three-camera look rounds → director playtest → numbered feedback → fixes.
- Image tasks: `todo → in progress → done → passed / rework`; `waiting`; `needs human`.
- Handoff: whoever finishes a step updates the status and leaves one line so the next owner knows it's their turn.
- Disagreement: write the reasons in CHANNEL or the backlog; the director decides.

## 6. Shared rules

Each rule: one line, then the date and the incident that created it. Examples to start from:

1. **Never overwrite files.** New versions are `_v2`, `_v3`.
2. **Edit only files you own.** Shared files are assigned by the commander first.
3. **Git workflow:** <who integrates, how to scope commits, when to push, asset limits>.
4. **Interact key is always <KEY>**, in code, prompts and docs.
5. **No hard-coded player text**; everything in the string tables.
6. **Scenes are generated from code**; never hand-edit scene files.
7. **Verification:** <isolated runtime test environment, shared editor rules, human playtest owner>.
8. **The project must not change while the director is playing**: run the guard before any compile, rebuild, import or write into `Assets/`; stage files outside and move them in on exit 0.
9. **One editor**: only the custodian starts it; take `editor.lock`; save and hand back when done; every job has a total deadline.
10. **Decide a collider for every object** and state it in the commit (`Colliders: …`).
11. **Never take the camera away**; lines spoken while the player can walk advance by themselves.
12. **Three tries, then stop**: report numbers, change approach.
13. **Anything needing an account, payment or terms acceptance goes to the director.**

## 7. Progress (newest first)

- **<Deliverable> (<date time>, <session>, <item ref>)**: <paths>. <What it is, numbers>. <QA result>. Colliders: <…>. <What another session must do next>. Not played.

## 8. Decision log (newest first)

| Date | Decision (with the director's words) | Decided by |
|---|---|---|
| <YYYY-MM-DD> | **<Decision>**: <details>. "<director's words>". Replaces <older decision>. | Director |

## Archive
