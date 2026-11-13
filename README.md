# game-studio

An AI skill for planning, building, and verifying playable game slices. It starts with the player outcome and works through the game's existing entry points. Use one session by default and add coordination when the work can be owned independently.

[中文说明](README.zh.md)

## Install the skill

Copy the entire [game-studio folder](skills/game-studio/) from this checkout into the target game's skill directory. Open the host in that game project and invoke the skill.

| Host | Project-level destination | Invoke |
|---|---|---|
| Codex | `.agents/skills/game-studio/` | `$game-studio` |
| Claude Code | `.claude/skills/game-studio/` | `/game-studio` |

Keep `SKILL.md`, `references/`, `templates/`, `assets/`, and `scripts/` together. See the installation paths in the [Codex skill docs](https://developers.openai.com/codex/skills/) and [Claude Code skill docs](https://code.claude.com/docs/en/skills).

## Try it on one task

> Use game-studio to reproduce a door that fails after loading a save. Use the project's normal entry point in an isolated checkout. Show the failing case, fix it, and rerun that case and the normal path.

The [skill entry point](skills/game-studio/SKILL.md) links to [project setup](skills/game-studio/references/project-setup.md), [genre profiles](skills/game-studio/references/genre-profiles.md), and [verification](skills/game-studio/references/verification.md). The profiles give example acceptance paths for narrative, action, strategy, and competitive games. The project supplies its controls, engine settings, acceptance criteria, and test commands.

## Optional Unity tools

The [Unity adapter](skills/game-studio/references/unity-adapter.md) covers generated scenes, reference scans, isolated PlayMode checks, and working with a live shared editor. For the shared editor workflow, start with [editor safety](skills/game-studio/references/editor-safety.md). To install the guard tools and editor beacon into a target project, close its Unity editor first, then run these commands from this repository with Python 3.10 or later.

```sh
python skills/game-studio/scripts/setup_unity.py install --project /path/to/game --editor-closed
python skills/game-studio/scripts/setup_unity.py doctor --project /path/to/game
```

Run `doctor` again with the target editor open to check the live probe. The optional host hook covers specific tool calls. The shared editor guard and project workflow cover the operation as a whole.

The [Unity key-and-door example](examples/unity-key-door/README.md) shows a PlayMode check that enters a playable scene, uses the key, saves, reloads, and finishes the objective. Follow its instructions to run it separately from your own game. Run the repository's Python tests with `python -B -m unittest discover -s tests -v` from the root with Python and Git available.

The [benchmark protocol](benchmarks/README.md) defines a comparison with a no-skill baseline and the previous skill revision.

## Where it comes from

The skill grew from *The Yellow Brick Road*, a 3D storybook web game adapting L. Frank Baum's 1900 *The Wonderful Wizard of Oz*. Its 15 days of active development produced nine playable chapters and 13 rounds of playtest notes. The [case study](skills/game-studio/references/case-study.md) describes the workflow. The [pitfalls](skills/game-studio/references/pitfalls.md) document what broke, why, and which checks helped.

Created by Jerry [@ycdjry](https://github.com/ycdjry). Inspired by Sac's *The Little Prince: Among the Stars* and its public production write-up. [Project site](https://littleprince.dev)
