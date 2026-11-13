# Unity adapter

Use the project's Unity version, package manifest, scene ownership, and entry points. Each helper is optional. Match its prerequisites to the current project before copying scripts or changing the editor setup.

| Task | Entry point |
|---|---|
| Work against a live shared editor | [Editor safety](editor-safety.md) describes the guard, lock, and recovery workflow. |
| Verify gameplay in an isolated Unity instance | [Verification](verification.md) covers PlayMode paths, project-specific assertions, and what each result proves. |
| Reuse a scene builder, narrative runtime, camera, or localization starter | [Unity architecture](unity-architecture.md) explains dependencies and links the C# starters in `assets/unity/`. Match the project's input and scene workflows. |
| Verify serialized scene references or diagnose a build that differs from the editor | Use the [web packaging guide](web-packaging.md) and `scripts/refcheck.py` for the relevant Git snapshot or worktree. |

For another engine, use its native runtime and evidence tools with the [verification workflow](verification.md). If an editor or content database is shared, use the project's coordination mechanism for that resource.
