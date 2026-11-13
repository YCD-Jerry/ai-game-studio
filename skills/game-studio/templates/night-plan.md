# Night plan <YYYY-MM-DD> → next morning (approved by the director)

Director's words before sleeping: "<verbatim>"
Back by: ~<HH:MM>. Spending: <e.g. "quality first, don't count credits" | "keep credits for X">. Logged in: <services>; only <art session> may use them.

## Order of work
1. Fix N-<x> … N-<y> (`docs/backlog/<file>`).
2. Then <…>.
3. When this list runs out: <next useful thing>.

## Who does what
| Session | Does | Output |
|---|---|---|
| Commander | dispatch, review, merge, morning report, devlog; checks every <30–60> min | commits, docs |
| Art pipeline | order → review → generate → import → replace placeholders | models in game |
| <Chapter N> | <…> | <…> |

## Rules for tonight
- Shared-editor work follows the guard, lock and hand-back procedure. Runtime tests use the designated isolated environment.
- No long exclusive jobs (full web builds) that block the shared editor.
- If using a shared Unity editor, stage related scripts outside `Assets/`. Keep checkpoints under the project's Git policy.
- Log reversible defaults. Keep progressing on independent tasks when a decision requires input.
- Three versions without improvement on one problem: stop, log the numbers, switch approach.
- Expect quota stops: everything committed, plans in files, so any session can resume.
- The commander's check timer is session-scoped: recreate it after a restart.

## Morning deliverables
Morning report · morning questions · devlog entry · backlog statuses updated.
