# game-studio

**Plan, build, and verify playable game slices. Start with one session and add coordination only when the work benefits from it.**

One human directs. A commander session dispatches and reviews. Worker sessions own chapters and systems. An image AI paints, an AI-3D tool makes characters, Blender scripts make buildings, Unity (URP, WebGPU) puts it together. This skill is the playbook: the roles, the rules, the templates, the scripts and the 30 mistakes that taught them.

[中文说明](README.zh.md)

## Where it comes from

Distilled from *The Yellow Brick Road*, a 3D storybook web game of L. Frank Baum's 1900 *Wonderful Wizard of Oz* (the book, not the film: silver shoes, not ruby slippers):

| | |
|---|---|
| Time | 15 days |
| Team | 1 human director, ~20 Claude Code sessions (up to ~8 at once), an image AI, Tripo, Blender |
| Output | 9 chapters, 17 code-generated scenes, ~72k lines of C#, ~40k lines of Blender Python |
| Process | 1,325 commits, 356 image orders, 256 numbered playtest notes over 13 rounds, 16 daily devlog entries |

The interesting part is not the speed but the organisation: a team brief every AI reads first, one owner per file, a single shared editor guarded by a lock, scenes built from code, a numbered playtest backlog in the director's own words, and a rule book that grew out of every expensive mistake.

## What's inside

```
skills/game-studio/
├── SKILL.md                    rule precedence, slice workflow, and task-specific references
├── references/
│   ├── studio-roles.md         who does what, file ownership, how orders flow, model tiers
│   ├── production-loop.md      PRD → chapter → graybox → look passes → playtest loop → overnight runs
│   ├── quality-bar.md          12 principles, checklists marked [data]/[image]/[play], when to stop
│   ├── layout-and-ui.md        the director's eye for alignment, and a drag-and-drop layout tool
│   ├── art-pipeline.md         overview sheets, asset breakdown, screenshot-first image orders, reviews
│   ├── characters.md           turnaround → Tripo → six-angle check → rig → Mixamo → a normal walk
│   ├── props-and-buildings.md  Blender from code, in parts, QA gates, Blender↔Unity traps
│   ├── unity-architecture.md   builders, story director, camera, colliders, localization, web build
│   ├── editor-safety.md        many sessions, one editor: guard, lock, rounds, honest checks
│   ├── pitfalls.md             35 lessons: symptom → root cause → fix → rule
│   ├── hackathon.md            one scene in a day with three sessions
│   ├── shipping.md             web build, the 60-second video, launch, opening the source safely
│   ├── web-packaging.md        loading chapter by chapter, measured first; when the web build differs from the editor
│   ├── showreel.md             the launch film from the running game: capture, drone shots, cuts on the bar
│   └── case-study.md           the project in numbers, what worked, what slowed it down
├── templates/                  kickoff messages, CLAUDE.md, TEAM.md, PRD, chapter, brief, backlog,
│                               morning report, night plan, devlog, recovery, art board, 11 prompts
├── assets/unity/               C# starting points (compile against Unity 6):
│                               scene builder, story director, TSV localization, interact key,
│                               grip socket, runtime NavMesh, review shots, editor state beacon, web build
└── scripts/                    guard, lock, play-state probe, hand back, a full editor round,
                                a dialog watcher, a reference check before builds,
                                a crop sheet, showreel/ (frame capture in Play,
                                cuts and checks), and a Claude Code hook that blocks writes into
                                Assets/ while Unity is playing
```

## Install

From a checkout of this repository, copy the entire [game-studio folder](skills/game-studio/) into your project's skill directory:

| Host | Destination |
|---|---|
| Codex | `.agents/skills/game-studio/` |
| Claude Code | `.claude/skills/game-studio/` |

The destination should contain `SKILL.md` alongside `references/` and `scripts/`. Open the host in that project and invoke `$game-studio` in Codex or `/game-studio` in Claude Code. The project-level paths are documented by [Codex](https://developers.openai.com/codex/skills/) and [Claude Code](https://code.claude.com/docs/en/skills).

## Try it on one task

> Use game-studio to reproduce a door that fails after loading a save. Use the existing smoke test in an isolated checkout and report what passed and what needs a human playtest.

Start with [SKILL.md](skills/game-studio/SKILL.md). The [project setup guide](skills/game-studio/references/project-setup.md) shows where to keep current decisions, engine settings, and art direction. The [verification guide](skills/game-studio/references/verification.md) covers reference scans, matched captures, runtime checks and playtests. Unity helpers are optional and have their own prerequisites. Runtime tests use the project's own scene, fixtures and assertions.

To run the bundled reference checker's regression tests, use `python -B -m unittest discover -s tests -v` from the repository root with Python and Git installed.

## Credits

Inspired by Sac's *The Little Prince: Among the Stars* ([littleprince.dev](https://littleprince.dev)) and his public write-up of building it with Opus, Tripo, Blender and Unity. Made by @雷霆俊叶 [@ycdjry](https://github.com/ycdjry) with Claude.
