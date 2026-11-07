# Case study: The Yellow Brick Road

The project this skill was distilled from. Use it to calibrate expectations (what a team like this can do in a few weeks, and where it struggles), and as an example when explaining the method to someone.

## In one paragraph

One human director, writing no code by hand, built a 3D storybook web game adapting L. Frank Baum's 1900 novel *The Wonderful Wizard of Oz* (the book, never the 1939 film: silver shoes, not ruby slippers) in Unity 6 URP for WebGPU, in 17 calendar days (2026-10-23 → 11-08). About 20 named Claude Code sessions worked on it over its life, up to ~8 at once, with an image AI for concepts, narration pages and lettering, Tripo for characters, and Blender driven headless by Python. The interesting part is not the speed but the organisation: a team brief every AI reads first, file ownership per session, a single-editor lock, code-generated scenes, a numbered playtest backlog in the director's own words, and a rule book that grew from every expensive mistake.

## Numbers (counted from the repository on 2026-11-08)

| Item | Count |
|---|---|
| Calendar days | 17 |
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

1. **Foundations (days 1–5)**: PRD, style choice, the team brief, code-generated scenes, the first Tripo characters, prologue to chapter 2 as graybox.
2. **The big night (day 6, ~160 commits)**: an approved overnight plan took chapters 4–6 to playable graybox while the director slept; the gait root cause was found; work was reorganised into one session per chapter; four chapters were stitched into one streamed world.
3. **Repair loops (days 7–10)**: playtest → fix on the shared world; forest, grass, clouds; the one-editor rule.
4. **Quality turn (days 11–13)**: the triangle budget cancelled ("quality first"), numbered feedback lists, a written quality bar, a story-text editor for the director, a dedicated Blender modelling session, "every image starts from a game screenshot".
5. **Sprint (days 14–17)**: three to five playtest rounds a day; chapters 7 and 8 built and reworked.

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

The method started from Sac's *The Little Prince: Across the Stars* (littleprince.dev), a 3D story web game made with Opus, Tripo, Blender and Unity, and his public write-up of the pipeline (reference sheets, asset breakdown, Tripo multiview characters, Mixamo, IK touch-ups). This project added the multi-session studio structure, the safety rails for a shared editor, and the playtest and quality loop.
