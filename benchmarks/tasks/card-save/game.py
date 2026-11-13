"""Lantern Duel, a tiny seeded card battle."""

import argparse
import json
import random
import shlex
import sys
from pathlib import Path


class Duel:
    def __init__(self, seed):
        self.seed = seed
        self.rng = random.Random(seed)
        self.enemy_hp = 24
        self.energy = 4
        self.turn = 0
        self.strikes = 0

    def snapshot(self):
        return {
            "seed": self.seed,
            "enemy_hp": self.enemy_hp,
            "energy": self.energy,
            "turn": self.turn,
            "strikes": self.strikes,
        }

    def play(self, card):
        if self.enemy_hp == 0:
            return {"error": "duel complete"}
        if card == "guard":
            self.energy = min(4, self.energy + 1)
            self.turn += 1
            return {"card": card, **self.snapshot()}
        if card != "strike":
            return {"error": "unknown card"}
        if self.energy == 0:
            return {"error": "not enough energy"}
        damage = self.rng.randint(1, 6)
        self.energy -= 1
        self.turn += 1
        self.strikes += 1
        self.enemy_hp = max(0, self.enemy_hp - damage)
        return {"card": card, "damage": damage, **self.snapshot()}

    def save(self, path):
        path.write_text(json.dumps({"version": 1, **self.snapshot()}), encoding="utf-8")

    @classmethod
    def load(cls, path):
        data = json.loads(path.read_text(encoding="utf-8"))
        if data["version"] != 1:
            raise ValueError("unsupported save version")
        duel = cls(data["seed"])
        duel.enemy_hp = data["enemy_hp"]
        duel.energy = data["energy"]
        duel.turn = data["turn"]
        duel.strikes = data["strikes"]
        return duel


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--seed", type=int, default=9)
    args = parser.parse_args()
    duel = Duel(args.seed)

    for line in sys.stdin:
        try:
            command = shlex.split(line)
            match command:
                case ["state"]:
                    result = duel.snapshot()
                case ["play", card]:
                    result = duel.play(card)
                case ["save", name]:
                    duel.save(Path(name))
                    result = {"saved": True}
                case ["load", name]:
                    duel = Duel.load(Path(name))
                    result = {"loaded": True, **duel.snapshot()}
                case ["quit"]:
                    return
                case _:
                    result = {"error": "invalid command"}
        except (OSError, ValueError, KeyError) as error:
            result = {"error": str(error)}
        print(json.dumps(result), flush=True)


if __name__ == "__main__":
    main()
