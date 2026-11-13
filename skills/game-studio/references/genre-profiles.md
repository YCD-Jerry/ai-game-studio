# Genre profiles

Use a profile when planning a slice or choosing what to verify. Mix criteria for hybrid games and turn the relevant ones into project acceptance criteria with observable thresholds. The project owns its controls, camera, difficulty, budgets, style, and supported save or network behavior.

| Loop | First acceptance path | Regressions to exercise when affected | Human review |
|---|---|---|---|
| Narrative exploration | Enter from the normal route, learn an objective, take the intended action, and reach the next beat with usable feedback. | Re-enter from a save or alternate route. Skip, repeat, or interrupt a beat if supported. Check prompt and subtitle fit. | Did the player understand their next action? Did the staging and pacing support the story? |
| Real-time action or platforming | Enter a challenge, use the normal input to overcome it, see the outcome, then retry after failure. | Compare supported frame rates, controllers, or inputs. Re-enter from a checkpoint and check that old hitboxes, timers, and pickups reset. | Are the threat, recovery window, and results of each input readable? |
| Strategy or card games | Reach a known board state, take a legal action through the game UI, and resolve the expected state change. | Exercise invalid actions, ordering of simultaneous effects, undo where supported, and save/load or replay from a controlled seed. | Can the player predict the consequences and read the board after resolution? |
| Competitive multiplayer | Join a match from two independent clients, take an action, and verify that both reach the same authoritative outcome. | Exercise delayed or duplicate messages, reconnection, and decisions made during disconnection where supported. | Does feedback stay understandable and fair when the connection degrades? |

For a concrete narrative workflow, see [The Yellow Brick Road production loop](production-loop.md), [its storybook quality bar](quality-bar.md), and the [chapter brief](../templates/chapter.md). Use [Verification](verification.md) to choose which checks need a test, a capture, or a person.
