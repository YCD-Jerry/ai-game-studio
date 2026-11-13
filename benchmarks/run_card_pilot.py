"""Run one matched Codex trial per condition for the Lantern Duel pilot."""
import argparse
import io
import json
from pathlib import Path
import random
import shutil
import subprocess
import sys
import time
import zipfile


HERE = Path(__file__).resolve().parent
BASELINE = "bdbfcab"
CONDITIONS = ("no-skill", "v1.1", "candidate")


def baseline_skill():
    archive = subprocess.check_output(
        ["git", "archive", "--format=zip", BASELINE, "skills/game-studio"], cwd=HERE.parent, timeout=20
    )
    return zipfile.ZipFile(io.BytesIO(archive))


def prepare(workspace, condition):
    task = HERE / "tasks/card-save"
    workspace.mkdir()
    for filename in ("game.py", "AGENTS.md"):
        shutil.copyfile(task / filename, workspace / filename)
    prompt = (task / "prompt.txt").read_text(encoding="utf-8").strip()
    if condition != "no-skill":
        skill = workspace / "game-studio"
        if condition == "candidate":
            shutil.copytree(HERE.parent / "skills/game-studio", skill,
                            ignore=shutil.ignore_patterns("__pycache__"))
        else:
            with baseline_skill() as archive:
                for name in archive.namelist():
                    if name.startswith("skills/game-studio/") and not name.endswith("/"):
                        path = workspace / name.removeprefix("skills/")
                        path.parent.mkdir(exist_ok=True, parents=True)
                        path.write_bytes(archive.read(name))
        prompt = "Read and apply the bundled game-studio/SKILL.md for this task.\n\n" + prompt
    return prompt


def grade(workspace):
    result = subprocess.run([sys.executable, str(HERE / "grade_card_save.py"), str(workspace)],
                            capture_output=True, text=True, timeout=40)
    return json.loads(result.stdout)


def events(path):
    usage = None
    completed = False
    failed = False
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            event = json.loads(line)
        except ValueError:
            continue
        if event.get("type") == "turn.completed":
            completed = True
            usage = event.get("usage")
        if event.get("type") == "turn.failed":
            failed = True
    return completed, failed, usage


def trial(output, condition, model, timeout):
    workspace = output / condition
    prompt = prepare(workspace, condition)
    before = grade(workspace)
    log = output / f"{condition}.jsonl"
    command = ["codex", "exec", "--json", "--ephemeral", "--ignore-user-config", "--skip-git-repo-check",
               "--sandbox", "workspace-write", "--model", model, "--cd", str(workspace), "-"]
    started = time.monotonic()
    returncode = None
    with log.open("wb") as stdout, (output / f"{condition}.stderr").open("wb") as stderr:
        try:
            run = subprocess.run(command, input=prompt.encode("utf-8"), stdout=stdout, stderr=stderr,
                                 timeout=timeout)
            returncode = run.returncode
        except subprocess.TimeoutExpired:
            pass
    elapsed = round(time.monotonic() - started, 3)
    completed, failed, usage = events(log)
    after = grade(workspace)
    return {"condition": condition, "wall_seconds": elapsed, "exit_code": returncode,
            "turn_completed": completed and not failed, "usage": usage, "checks_before": before,
            "checks_after": after}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True, help="new directory outside this checkout")
    parser.add_argument("--model", required=True)
    parser.add_argument("--timeout", type=int, default=120, help="per-condition limit in seconds")
    parser.add_argument("--seed", type=int, default=17, help="trial order seed")
    args = parser.parse_args()
    output = args.output.resolve()
    if output.is_relative_to(HERE.parent) or args.timeout <= 0:
        parser.error("use a new output directory outside the repository and a positive timeout")
    output.mkdir()
    order = list(CONDITIONS)
    random.Random(args.seed).shuffle(order)
    report = {"model": args.model, "seed": args.seed, "timeout_seconds": args.timeout, "trials": []}
    for condition in order:
        result = trial(output, condition, args.model, args.timeout)
        report["trials"].append(result)
        (output / "summary.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result), flush=True)


if __name__ == "__main__":
    main()
