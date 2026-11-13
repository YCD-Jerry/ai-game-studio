---
name: game-studio
description: Plan, build, review, and verify playable game slices, levels, and asset integrations. Use for turning a game idea or story into a vertical slice, checking gameplay or visuals against a quality bar, investigating a game regression, or coordinating authorized game-development work across agents. Includes optional Unity scene, editor, and web-build tools.
license: MIT
---

# Game Studio

Use the smallest workflow that can produce and verify the requested change. Start from the existing project and its normal player entry point. For a new game, pick one playable loop or story beat and define how the player enters it, acts, receives feedback, and reaches an outcome.

## Resolve project rules once

1. Apply the user's current instructions and the project's active instruction files for the affected paths. For project decisions, use the latest applicable approved decision that explicitly supersedes an older one. Keep archived notes, incident reports, and case studies as evidence. If two active rules conflict and the choice changes the result materially, surface the conflict and continue work independent of it.
2. Use existing project sources for engine and package versions, paths, input, asset budgets, scene ownership, test environment, and deployment. Read the style guide for look, references, pacing, and review criteria. Ask only for material decisions that cannot be inferred or safely defaulted.
3. Treat this skill's templates and references as defaults and examples. Store a new project decision in the owning document, link it from the active instructions if needed, and mark the superseded rule there. Use [project setup](references/project-setup.md) when those sources are missing.

## Build and review a slice

- Agree on the observable player outcome and the cheapest useful proof before changing assets or code. Prototype the interaction in the existing engine and test its normal entry and completion path. Add alternate entry, retry, or save/resume cases when the change touches them.
- For a look pass, capture the same camera, time, and settings before and after. Choose viewpoints that show what changed. A wide shot, a gameplay view, and a close detail are useful for a full environment. For an asset, confirm dimensions, silhouette, integration, and any measured budget relevant to its use.
- Capture playtest feedback in the player's words with a reproduction path. Find the owning system when a defect recurs. Verify the fix against the original case and other affected callers.
- Report evidence by type: static checks, visual review, runtime checks, and human playtest. Tie each result to its input or revision. Record any skipped or blocked check and the specific remaining question. Human playtesting decides feel, pacing, and readability in context.

## Select the relevant workflow

| Task | Read or use |
|---|---|
| Define a slice or run a look pass | [Production loop](references/production-loop.md), [chapter brief](templates/chapter.md) |
| Verify a change, choose checks, or set up repeatable Unity runs | [Verification](references/verification.md) |
| Set acceptance criteria or review a storybook slice | [Quality bar example](references/quality-bar.md) |
| Coordinate authorized parallel work | [Studio roles](references/studio-roles.md), [work brief](templates/brief.md) |
| Modify a live shared Unity editor | [Editor safety](references/editor-safety.md), including guard, lock, and deadlines |
| Work with Unity generators, narrative, camera, or localization | [Unity architecture](references/unity-architecture.md) and the relevant starters in `assets/unity/` |
| Order or review art | [Art pipeline](references/art-pipeline.md), [characters](references/characters.md), [props](references/props-and-buildings.md), or [layout](references/layout-and-ui.md) |
| Check Unity scene references before a build | [Web packaging](references/web-packaging.md) and `scripts/refcheck.py` |
| Prepare a release, trailer, or jam plan | [Shipping](references/shipping.md), [showreel](references/showreel.md), or [game jam](references/hackathon.md) |
| Investigate a recurring problem or study the source project | [Pitfalls](references/pitfalls.md), [Yellow Brick Road case study](references/case-study.md) |

Single-session work is the default. Use parallel work when requested or authorized and tasks have independent ownership. Integrate through the owning project workflow and record scope, touched files, evidence, and next steps in the existing handoff. Resolve bundled paths relative to this file.
