# Studio roles: who does what, who owns which file, how orders flow

Read this when setting up the team, adding a session, writing a brief, or deciding where a piece of work belongs. Templates: `templates/TEAM.md`, `templates/brief.md`.

## Contents
1. The cast
2. How the structure grew (and why each change happened)
3. Ownership rules
4. How orders and deliveries flow
5. Choosing models and effort
6. When to split, when to stop splitting

## 1. The cast

| Role | Who | Owns (writes) | Never does |
|---|---|---|---|
| **Director** | The one human | Direction, every creative call, all playtesting, logins, purchases, paid generations, merging | Writes code |
| **Commander** | One Claude session, the strongest model, used sparingly | Decision log, backlog, briefs, image-AI orders, morning report, devlog, `CLAUDE.md`, shared-file assignments | Hands-on building; playtesting |
| **Slice session** (one per chapter/level, or a short-lived ticket session) | Claude, strong model, high effort | Its slice: builder, props, story director, text table, chapter docs, review folders | Edits shared scripts without an order; starts the editor; reimports characters |
| **Systems owner** | Claude | Cross-cutting code: player, camera, HUD, story director, terrain/roads, followers, menus, saves | Slice files |
| **Editor custodian** | A cheap Claude session | Starting/restarting the editor, editor safety settings, small shared UI | — |
| **Art pipeline** | Claude | The art folder, character/environment models and textures, the art task board and channel; the only session that reimports characters (behind its own lock) and the only one using the logged-in browser | Scene code |
| **Modelling** | Claude, high effort | Blender build scripts and model files for buildings and fine props, at fixed paths the builders read | Characters or anything with a face |
| **Modelling review** | Claude, any tier | Review reports on the modeller's output | Modelling |
| **Sound** | Claude | Audio folder, sound IDs, a local listening page | Scene wiring beyond its ask |
| **Image AI** | e.g. GPT image in the Codex app | Only the art folder: images, task statuses, its notes | Engine, code, Blender scripts, scenes, git, logins, purchases, playtesting |
| **AI 3D** | e.g. Tripo | Characters (and, early on, props) | — |

You don't need all of these on day one. Start with the director, a commander and two or three slice sessions; add a role when a recurring kind of work keeps landing on the wrong desk.

## 2. How the structure grew (and why each change happened)

| When | Structure | What forced the change |
|---|---|---|
| Days 1–3 | One Claude session does everything technical; the image AI draws from a task board in batches of 8, then waits for review | The batch stop was a bottleneck: "do the whole queue; Claude reviews as you go." The message board moved out of the team brief into its own two-way file because the brief got too long |
| Day 5 | Animation briefly given to the image AI (it can edit code), reversed the same evening | Split by capability, not topic: Claude = code, engine, git, motion; image AI = pictures. Both decisions stay in the log, the newer one marked as replacing the older |
| Day 5 evening | One session becomes **commander**: orders other sessions directly, posts image orders, assigns shared files; an approved overnight plan with a parallel art session | Overnight: quota limits hit three times; two sub-agents left half-written scripts in the project and broke compilation for everyone |
| Day 6 | The strongest model "burns too much quota": **commander only reviews, dispatches, decides; hands-on work goes to one high-effort session per chapter**, each owning its chapter's files | Cost. Also, the director could give feedback directly where the knowledge lived |
| Day 9 | **One editor**: only a cheap custodian session may start Unity; everyone takes a lock | Several sessions' queued rebuilds fired together; unsaved big scenes piled up 13 recovery backups (96 MB) |
| Day 12 | Efficiency review: chapter-per-session "helped while clearing ground, now drags". Proposed: commander + 1–2 **systems owners** + art + **short-lived ticket sessions** | 13 of 36 feedback items were game-wide with no owner; each chapter re-solved subtitles, cards and following; long-lived sessions drifted after context compaction ("five sessions, five interpretations of the same rule") |
| Day 13 | Characters only from AI 3D; buildings and fine props to a **modelling session**, a **modelling review session**, a **sound session** | AI-3D buildings looked like clay and burned credits; clipping in the city was obvious, so review was split from production |

The lesson in that history: **own files by slice early, but give cross-cutting systems one owner as soon as two slices share a problem. Knowledge lives in files; sessions are disposable.**

## 3. Ownership rules

- Every file has one owner. Only edit files you own; if unsure, ask the commander.
- Shared files are assigned by the commander before anyone edits them. A slice needing a shared change asks the commander, who dispatches it to the owner.
- Numbered resources (art IDs, sound IDs, feedback numbers) are handed out by their owner, never self-assigned: one brief claimed sound IDs that collided with another chapter's, and they had to be voided and reissued.
- Every new AI adds a row to the role table in `TEAM.md` and posts an intro saying which files it will change.
- Only Claude runs git. Only the art pipeline uses the browser where paid tools are logged in, and only after the director released the spend.
- Two sessions that might write the same file never run at the same time. If they must, give one a separate worktree.

## 4. How orders and deliveries flow

| From → to | Channel | Form |
|---|---|---|
| Commander → Claude session | Direct message (SendMessage or the app's session messaging) | A short order pointing at a brief file or backlog numbers. Long designs go to `docs/briefs/<topic>.md` |
| Claude → image AI | `CHANNEL.md` (newest on top) + full prompt in `ART_QUEUE.md` | The director relays "read CHANNEL" until the image AI can poll it itself |
| Image AI → Claude | Reply in `CHANNEL.md` | Full file paths, recommended version, known flaws; task set to "done, awaiting review" |
| Director → team | Talking (often dictated) to the commander or the slice session | The commander writes a numbered backlog live, with the director's words quoted |
| Team → director | Morning report + decisions file; a short "what changed, where to look, what is unverified" list before each playtest | See `templates/morning-report.md` |

**Whoever orders picks up the delivery.** After ordering, run a background watcher that exits when the channel gets a reply or the concept folder gets a new file. Every time it completes: read its output, list the newest files, review them, restart the watcher. Put those instructions in the watcher's command text itself, because the completion notice shows only the command: one session answered a completion with "no response" and ten deliveries sat for hours.

**Never report from intent, only from evidence.** A script that was supposed to write an order failed; the following commit ran anyway and the session reported "ordered". Chain with `&&`, read results, grep the file before saying it is written.

## 5. Choosing models and effort

Budget model tiers like staff: **the scarce one decides, the plentiful ones build.** In the source project the strongest model burned quota fast in hands-on work, so it became the commander only (review, dispatch, decisions, the devlog), and every hands-on session ran a strong mid-tier model at high effort. A cheap model kept the editor and did small shared UI.

Design for interruption: quota stops, context compaction and crashes will happen. Every step is committed and pushed; every plan, decision and status lives in a file; a session resumes by reading `CLAUDE.md` → `TEAM.md` → its brief → the backlog.

## 6. When to split, when to stop splitting

Split when: work is independent by file; a kind of work (art, sound, modelling) has its own tools and review; one session's context is filling with things only it needs.

Stop splitting (or merge back) when: the director keeps re-reporting the same problem in different slices; sessions disagree about a shared rule; a slice session spends most of its time waiting for a shared-code owner. Then move that system to one owner, and turn slice sessions into short tickets that read the chapter doc and the quality bar, do one job, and close.

Practical ceiling: up to ~8 sessions ran at once in the source project. Beyond that, the single shared editor, not the number of sessions, set the pace (see `references/editor-safety.md`).
