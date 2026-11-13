# Set up a project

Use this when starting a project or when the existing instructions omit something the current task needs. Keep a single source for each decision and link to it from the agent entry file.

| Source | Keep here |
|---|---|
| Active agent instructions | Links to project rules, task entry points, editor access and Git conventions. Use AGENTS.md or CLAUDE.md as appropriate for the host. |
| Project configuration | Engine and package versions, build targets, input bindings, scene ownership and generation commands, target device, performance and asset budgets, test commands and sandbox paths. Read values from executable configuration when it already owns them. |
| Design and style guides | Player verbs, tone, visual references, pacing, camera intent, accepted review examples and source-material boundaries. Store personal taste here when the user adopts it for this project. |
| Active decisions | Date, scope, decision owner, replacement for a previous rule if any, and link to the owning file. Update that file as part of the same change. |
| Handoff and archive | Current revision, next action, relevant commands, last artifacts and known gaps. Keep old decisions and incidents available for investigation. |

For a new project, write a short [brief](../templates/PRD.md), select one playable slice, and agree on evidence for it. Choose a project-specific acceptance bar before investing in production assets. The Yellow Brick Road used Unity URP, a storybook look, a single interaction binding and generated scenes. Those are example choices. Use this project's approved equivalents.

For a single session, the project entry file and one short handoff are usually enough. If authorized sessions need concurrent write access, add explicit owners, an integration owner, and a shared-editor or isolated-checkout plan. Use [TEAM.md](../templates/TEAM.md) as a starting point if useful.
