#!/usr/bin/env python3
"""Small local status, restart and telemetry helpers. No agent/API calls."""

import argparse
from datetime import datetime, timezone
import fcntl
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TELEMETRY = ROOT / "telemetry"


def git(*args):
    return subprocess.run(["git", *args], cwd=ROOT, check=True,
                          capture_output=True, text=True).stdout.strip()


def bounded_int(value):
    number = int(value)
    if number < 0:
        raise argparse.ArgumentTypeError("Must be nonnegative")
    return number


def task_id(value):
    if not re.fullmatch(r"[A-Z][0-9]{2,4}", value):
        raise argparse.ArgumentTypeError("Use a roadmap ID such as W01")
    return value


def snapshot():
    TELEMETRY.mkdir(exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    destination = TELEMETRY / ("codeburn-" + stamp + ".json")
    result = subprocess.run(
        ["codeburn", "export", "--format", "json", "--project", str(ROOT),
         "--output", str(destination)], cwd=ROOT, capture_output=True, text=True,
        timeout=120)
    if result.returncode:
        raise ValueError("CodeBurn export failed; run codeburn doctor locally")
    data = json.loads(destination.read_text())
    if data.get("schema") != "codeburn.export.v3":
        raise ValueError("Unknown CodeBurn schema; inspect the local export before using it")
    foreign = [p for p in data.get("projects", [])
               if Path(p["project"]).resolve() != ROOT]
    if foreign:
        destination.unlink()
        raise ValueError("CodeBurn project filter included another project; export removed")
    print("Local raw snapshot:", destination.relative_to(ROOT))
    print("Calculated/estimated usage, not a bill; cumulative export, not per-slice cost.")
    print(json.dumps({"schema": data["schema"], "summary": data.get("summary", [])}, indent=2))


def record(args):
    entry = {
        "recorded_at": datetime.now(timezone.utc).isoformat(),
        "task": args.task, "model": args.model, "effort": args.effort,
        "minutes": args.minutes, "outcome": args.outcome,
        "rework_count": args.rework, "checks": args.check,
        "source_revision": git("rev-parse", "HEAD"),
        "working_tree_dirty": bool(git("status", "--porcelain")),
    }
    TELEMETRY.mkdir(exist_ok=True)
    destination = TELEMETRY / "slices.jsonl"
    with destination.open("a", encoding="utf-8") as output:
        fcntl.flock(output, fcntl.LOCK_EX)
        output.write(json.dumps(entry, sort_keys=True) + "\n")
        output.flush()
    print("Recorded", args.task, args.outcome, "in", destination.relative_to(ROOT))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("status", help="show Git status and durable current task")
    commands.add_parser("handoff", help="print a compact prompt for a fresh session")
    commands.add_parser("snapshot", help="save a project-scoped local CodeBurn export")
    logging = commands.add_parser("record", help="append observed slice outcome locally")
    logging.add_argument("task", type=task_id)
    logging.add_argument("--model", required=True)
    logging.add_argument("--effort", required=True)
    logging.add_argument("--minutes", type=bounded_int,
                         help="observed elapsed minutes; omit if unknown")
    logging.add_argument("--outcome", required=True,
                         choices=["accepted", "reworked", "blocked", "abandoned"])
    logging.add_argument("--rework", type=bounded_int,
                         help="observed rework count; omit if unknown")
    logging.add_argument("--check", action="append", default=[],
                         help="short check/result, repeatable; do not include secrets")
    args = parser.parse_args()
    try:
        if args.command == "status":
            print("Source:", git("rev-parse", "--short", "HEAD"))
            print(git("status", "--short") or "Working tree clean")
            print("\n" + (ROOT / "docs/agent/CURRENT-TASK.md").read_text())
        elif args.command == "handoff":
            print("Continue Siphon in", ROOT)
            print("Read AGENTS.md, docs/agent/STATE.md and docs/agent/CURRENT-TASK.md.")
            print("Inspect Git status before editing. Follow ROADMAP.md; finish one bounded slice.")
            print("Use fresh worker context and an appropriate available model. Preserve user edits.")
            print("Verify paid generation job state before submitting. Keep human requests minimal.")
            print("Update task, state, roadmap and relevant briefing before handing off.")
            print("Current source:", git("rev-parse", "HEAD"))
            print("Current working tree:\n" + (git("status", "--short") or "clean"))
        elif args.command == "snapshot":
            snapshot()
        else:
            record(args)
        return 0
    except (OSError, ValueError, KeyError, subprocess.SubprocessError) as error:
        print("Workflow helper failed:", type(error).__name__, file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
