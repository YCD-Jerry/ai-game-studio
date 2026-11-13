# Compare game-development workflows

Use the same tasks, model, tool permissions, and limits across the following conditions.

| Condition | Instructions |
|---|---|
| No skill | Task prompt and the project's own instructions |
| v1.1 | Same prompt and project, plus the frozen skill at `bdbfcab` |
| Candidate | Same prompt and project, plus the skill from the checkout under test |

## First task

Lantern Duel is a small, engine-neutral card game. The task is to fix a mismatch between uninterrupted play and play resumed from a saved game. The grader launches the normal CLI with predetermined seeds, checks fresh save/restart/load, checks a version 1 save, and tests uninterrupted play. It lives outside the workspace given to the agent. The original fixture should fail the first two checks and pass uninterrupted play.

```sh
python benchmarks/grade_card_save.py benchmarks/tasks/card-save
python benchmarks/run_card_pilot.py --output /path/to/new/external-directory --model <model>
```

The runner requires Python 3.10+, Git with the baseline revision, and an authenticated Codex CLI with workspace-write tools available. It creates separate workspace copies and a fresh agent session for each condition. It records raw JSONL, wall time, exit code, grader results before and after, and token usage when supplied by the host. Keep raw logs private until reviewed for account identifiers, local paths, and other session metadata.

## Score and report

- **Task completion.** All task-specific acceptance cases must pass. Report the number of passing trials over eligible trials per condition.
- **Regressions.** Count trials where a previously passing case now fails. Report the underlying checks alongside the count.
- **Time.** Use wall time from launch to end of the agent run. Keep timeout and environment errors visible. Count a timeout in a functioning environment as an incomplete trial.
- **Cost.** Record input, cached input, and output tokens when available. Convert to currency only with the actual provider and pricing date. Record manual intervention and paid tool calls separately.
- **Environment failure.** Mark sessions prevented from starting or operating by the host as invalid. Report the number and reason per condition. Do not count a run that never acquired tools as evidence for or against the skill.

The first task is a harness smoke test. For a measured comparison, pin the candidate revision, fixture, grader, model, and tool configuration before running. Repeat each condition with fresh workspaces and a varied trial order. Include a separate Unity task and a separate task that requires visual or gameplay judgment before drawing conclusions across genres. Publish the pass counts, time distribution, missing cost data, and limitations with the protocol.

### Pilot on Windows

One launch per condition used Codex CLI 0.167.0-alpha.4.1, `gpt-6.1-sol`, and a 70-second limit. The host rejected shell commands in all three sessions under the workspace-write policy. Every workspace retained the initial grader results. All three trials are invalid for skill comparison. Their wall times measure the failed launches and have been excluded from any effectiveness or cost estimate.

### Matched desktop smoke test

After the CLI attempt, three fresh Codex desktop subagents ran the same task in separate workspaces. The two skill conditions received the same instruction to read their bundled skill. The grader remained outside all three workspaces. Each agent reproduced the bug through the CLI before fixing it.

| Condition | Fresh save and resume | Existing version 1 save | Uninterrupted play | Completed trials |
|---|---|---|---|---|
| No skill | Pass | Pass | Pass | 1/1 |
| v1.1 | Pass | Pass | Pass | 1/1 |
| Candidate | Pass | Pass | Pass | 1/1 |

The desktop smoke test did not collect comparable timing or token metrics. The three conditions tied on the acceptance cases. Add harder tasks and more trials before using the benchmark to estimate a difference in completion rate or cost.
