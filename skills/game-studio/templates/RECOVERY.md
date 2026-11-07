# Recovery: resume this project on a new machine or account

GitHub is this project's only backup. Chat history, local memory and `~/.claude` live on one machine and can vanish. Everything needed to continue is in this repo.

## Resume prompt (paste into a new Claude Code session at the repo root)

> Read `CLAUDE.md`, `SourceArt/TEAM.md`, `docs/PRD.md`, `docs/RECOVERY.md` and the latest file in `docs/backlog/`, then continue the work.

## Read order for a new session
1. `CLAUDE.md` → 2. `SourceArt/TEAM.md` → 3. `docs/PRD.md`, `SourceArt/STYLE_GUIDE.md`, `SourceArt/ART_QUEUE.md`, `SourceArt/CHANNEL.md` → 4. this file → 5. the newest backlog and devlog.

## Restore table
| Thing | How to restore |
|---|---|
| Project | `git clone <repo>`; `git lfs pull` if LFS is used |
| Engine | Unity <version> with Web Build Support; open the project once, let it import |
| Claude setup | copies in `docs/claude-setup/`: CLAUDE.md, settings, skills, memory notes, editor tool scripts. Copy them back to `~/.claude/` |
| Editor tools | `<tools dir>` (guard, lock, hand back, shots): copies in `docs/claude-setup/tools/` |
| Blender | Blender <version>; plugins: <…> |
| Accounts | which service is used for what (never passwords): image AI, AI 3D, Mixamo, hosting |
| Network notes | <proxy or region notes, if any> |

## Pipelines learned the hard way
<Exact UI steps for the AI-3D character pipeline, the import step, the web build and upload. Mark outdated sections "obsolete, kept for history" instead of deleting them.>
