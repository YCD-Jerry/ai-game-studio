"""Black-box grader for a Lantern Duel workspace. Needs Python 3.10+."""

import argparse
import json
import random
import subprocess
import sys
import tempfile
from pathlib import Path


def play(workspace, seed, commands, saves):
    result = subprocess.run(
        [sys.executable, str(workspace / "game.py"), "--seed", str(seed)],
        cwd=saves,
        input="\n".join(commands) + "\n",
        capture_output=True,
        text=True,
        check=True,
        timeout=10,
    )
    return [json.loads(line) for line in result.stdout.splitlines()]


def uninterrupted(workspace, saves):
    seed = 61
    rng = random.Random(seed)
    first = rng.randint(1, 6)
    second = rng.randint(1, 6)
    steps = play(workspace, seed, ["play strike", "play guard", "play strike", "state"], saves)
    return (
        len(steps) == 4
        and steps[0].get("damage") == first
        and steps[1].get("card") == "guard"
        and steps[2].get("damage") == second
        and steps[3] == {"seed": seed, "enemy_hp": 24 - first - second, "energy": 3, "turn": 3, "strikes": 2}
    )


def round_trip(workspace, saves):
    seed = 19
    opening = ["play strike", "play guard", "play strike", "play guard"]
    ending = ["play strike", "play guard", "play strike", "state"]
    reference = play(workspace, seed, opening + ending, saves)[len(opening):]
    saved = play(workspace, seed, opening + ["save current.json"], saves)
    resumed = play(workspace, 999, ["load current.json", *ending], saves)
    return (
        len(reference) == len(ending)
        and saved[-1] == {"saved": True}
        and resumed[0].get("loaded") is True
        and resumed[1:] == reference
    )


def legacy(workspace, saves):
    seed = 73
    rng = random.Random(seed)
    previous = rng.randint(1, 6) + rng.randint(1, 6)
    damage = rng.randint(1, 6)
    snapshot = {"seed": seed, "enemy_hp": 24 - previous, "energy": 3, "turn": 3, "strikes": 2}
    (saves / "legacy.json").write_text(json.dumps({"version": 1, **snapshot}), encoding="utf-8")
    result = play(workspace, 999, ["load legacy.json", "play missing", "play strike"], saves)
    return result == [
        {"loaded": True, **snapshot},
        {"error": "unknown card"},
        {"card": "strike", "damage": damage, **snapshot, "turn": 4, "strikes": 3, "energy": 2,
         "enemy_hp": snapshot["enemy_hp"] - damage},
    ]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("workspace", type=Path)
    args = parser.parse_args()
    checks = {}
    for check in (round_trip, legacy, uninterrupted):
        with tempfile.TemporaryDirectory(prefix="game-studio-grade-") as folder:
            try:
                checks[check.__name__] = bool(check(args.workspace.resolve(), Path(folder)))
            except (OSError, ValueError, IndexError, subprocess.SubprocessError) as error:
                checks[check.__name__] = False
                print(f"{check.__name__}: {error}", file=sys.stderr)
    print(json.dumps(checks))
    return 0 if all(checks.values()) else 1


if __name__ == "__main__":
    raise SystemExit(main())
