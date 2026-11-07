# game-studio

**Run a studio of Claude Code sessions that builds a 3D game for the web.**

One human directs. A commander session dispatches and reviews. Worker sessions own chapters and systems. An image AI paints, an AI-3D tool makes characters, Blender scripts make buildings, Unity (URP, WebGPU) puts it together. This skill is the playbook: the roles, the rules, the templates, the scripts and the 30 mistakes that taught them.

[中文说明](README.zh.md)

## Where it comes from

Distilled from *The Yellow Brick Road*, a 3D storybook web game of L. Frank Baum's 1900 *Wonderful Wizard of Oz* (the book, not the film: silver shoes, not ruby slippers):

| | |
|---|---|
| Time | 17 days |
| Team | 1 human director, ~20 Claude Code sessions (up to ~8 at once), an image AI, Tripo, Blender |
| Output | 9 chapters, 17 code-generated scenes, ~72k lines of C#, ~40k lines of Blender Python |
| Process | 1,325 commits, 356 image orders, 256 numbered playtest notes over 13 rounds, a devlog every day |

The interesting part is not the speed but the organisation: a team brief every AI reads first, one owner per file, a single shared editor guarded by a lock, scenes built from code, a numbered playtest backlog in the director's own words, and a rule book that grew out of every expensive mistake.

## What's inside

```
skills/game-studio/
├── SKILL.md                    the method in one page: roles, loop, 15 hard rules, reading guide
├── references/
│   ├── studio-roles.md         who does what, file ownership, how orders flow, model tiers
│   ├── production-loop.md      PRD → chapter → graybox → look passes → playtest loop → overnight runs
│   ├── quality-bar.md          12 principles, checklists marked [data]/[image]/[play], when to stop
│   ├── art-pipeline.md         overview sheets, asset breakdown, screenshot-first image orders, reviews
│   ├── characters.md           turnaround → Tripo → six-angle check → rig → Mixamo → a normal walk
│   ├── props-and-buildings.md  Blender from code, in parts, QA gates, Blender↔Unity traps
│   ├── unity-architecture.md   builders, story director, camera, colliders, localization, web build
│   ├── editor-safety.md        many sessions, one editor: guard, lock, rounds, honest checks
│   ├── pitfalls.md             30 lessons: symptom → root cause → fix → rule
│   ├── hackathon.md            one scene in a day with three sessions
│   ├── shipping.md             web build, the 60-second video, launch, opening the source safely
│   └── case-study.md           the project in numbers, what worked, what slowed it down
├── templates/                  CLAUDE.md, TEAM.md, PRD, chapter, brief, backlog, morning report,
│                               night plan, devlog, recovery, art board, 11 image/review prompts
├── assets/unity/               C# starting points (compile against Unity 6):
│                               scene builder, story director, TSV localization, interact key,
│                               grip socket, runtime NavMesh, review shots, editor state beacon
└── scripts/                    guard, lock, play-state probe, hand back, a full editor round
```

## Install

**As a plugin** (inside Claude Code):

```
/plugin marketplace add ycdjry/ai-game-studio
/plugin install game-studio@ai-game-studio
```

Third-party marketplaces don't auto-update by default; run `/plugin marketplace update ai-game-studio` to get new versions.

**Or copy the skill folder** (pick one way, not both):

```bash
git clone https://github.com/ycdjry/ai-game-studio
cp -r ai-game-studio/skills/game-studio ~/.claude/skills/        # for every project
# or: cp -r ai-game-studio/skills/game-studio <your-project>/.claude/skills/
```

## Use

Just describe the game. The skill triggers on requests like:

- "Turn *Peter Pan* into a small 3D exploration game for the browser."
- "Set up a team of Claude sessions for my Unity game: one per level, one for art."
- "We have a hackathon tomorrow, 8 hours, I want a beautiful 3D scene people can play."
- "Two sessions keep breaking each other's compiles in the same Unity project."

Then start with `SKILL.md` → "Start a project", or `references/hackathon.md` for a one-day build.

## What you need

- Claude Code, ideally several sessions (the desktop app or terminals).
- Unity 6 with Web Build Support, driven by a CLI or MCP your sessions can call.
- Blender 4+ (headless scripts).
- An image model (e.g. GPT image in the Codex app) and an AI-3D tool (e.g. Tripo) with an account you control. Claude never logs in or pays; you do.

## Credits

Inspired by Sac's *The Little Prince: Across the Stars* ([littleprince.dev](https://littleprince.dev)) and his public write-up of building it with Opus, Tripo, Blender and Unity. Made by @雷霆俊叶 [@ycdjry](https://github.com/ycdjry) with Claude.
