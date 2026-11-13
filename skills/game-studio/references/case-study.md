# Case study: The Yellow Brick Road

The project this skill was distilled from. Use it to calibrate expectations (what a team like this can do in a few weeks, and where it struggles), and as an example when explaining the method to someone.

## In one paragraph

Jerry, a human director who wrote no code by hand, built a 3D storybook web game adapting L. Frank Baum's 1900 novel *The Wonderful Wizard of Oz* in Unity 6 URP for WebGPU over 15 days of active development. About 20 named Claude Code sessions worked on it over its life, up to ~8 at once, with an image AI for concepts, narration pages and lettering, Tripo for characters, and Blender driven headless by Python. The team brief, file ownership, shared-editor coordination, generated scenes, and numbered playtest backlog are documented in the [production loop](production-loop.md) and [storybook quality bar](quality-bar.md).

## Numbers (counted from the repository on 2026-11-08)

| Item | Count |
|---|---|
| Active development days | 15 |
| Commits | 1,325 (busiest day 237) |
| Chapters | 9 (prologue + 8), playable end to end from the menu |
| Claude sessions | ~20 named, up to ~8 concurrent |
| C# | ~71.7k lines in 233 files (61 editor scripts: scene builders and checks) |
| Blender Python | ~40.5k lines in 171 scripts |
| Shaders | 32 files |
| Unity scenes | 17, all generated from code |
| Localized string rows | ~1,350 (Chinese + English) |
| Image-AI orders | 356, many with 2–6 revisions |
| Numbered playtest items | 256, over 13 rounds |
| Review folders (evidence for fixes) | 232 |
| Daily devlog entries | 16 |

Honest caveats: most late changes were checked only by data and screenshots and were not yet played; frame rate on the integrated-GPU development machine was still under target in the densest scene; one "no sound at all" report was never reproduced; the heroine's model was scheduled for complete regeneration.

## Five phases

1. **Foundations** — PRD, style choice, team brief, generated scenes, first Tripo characters, and grayboxes for the prologue through chapter 2.
2. **Overnight build (~160 commits)** — An approved overnight plan brought chapters 4–6 to playable graybox. The team found the gait root cause, assigned one session per chapter, and stitched four chapters into one streamed world.
3. **Repair loops** — Playtests and fixes on the shared world, environment work on forest, grass and clouds, and the one-editor rule.
4. **Quality turn** — Numbered feedback, written quality criteria, a story-text editor, a dedicated Blender modelling session, and image briefs based on gameplay screenshots.
5. **Final sprint** — Three to five playtest rounds a day while the team built and reworked chapters 7 and 8.

## What made it work

- **Files over memory.** `TEAM.md`, `CLAUDE.md`, chapter docs, backlogs and the decision log let any session (or a new account) pick up where another stopped.
- **Code-generated scenes.** Nobody merged scene files; a rebuild was a menu item.
- **One human doing taste and play.** The director's time went to judgement; everything else was delegated.
- **Rules born from incidents.** Each rule in the team brief carries the story of what broke, so sessions understood and followed it.
- **Reference-driven looks.** Overview sheets and three-camera comparisons gave every look pass a target.

## What slowed it down

- **One shared editor** for up to eight sessions, and the human playing in the same editor. A second project copy for rebuilds and robot runs is the top recommendation for the next project.
- **Only the human saw the game run.** About two-thirds of one feedback round should have been caught by a checklist or a robot before reaching the director.
- **Long-lived chapter sessions** drifted and re-solved shared problems separately; systems owners and short ticket sessions fix that.
- **Manual relay to the image AI** ("read CHANNEL") cost hours; a polling image AI or an image API removes it.

## Inspiration

The method started from Sac's *The Little Prince: Among the Stars* (littleprince.dev), a 3D story web game made with Opus, Tripo, Blender and Unity, and his public write-up of the pipeline (reference sheets, asset breakdown, Tripo multiview characters, Mixamo, IK touch-ups). This project added the multi-session studio structure, the safety rails for a shared editor, and the playtest and quality loop.
