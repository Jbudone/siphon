#!/usr/bin/env python3
"""Exercise Unity's token refresh and report failed access without printing tokens."""

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

STATE = Path(os.environ.get("XDG_STATE_HOME", Path.home() / ".local/state")) / "siphon/unity-auth.json"


def now():
    return datetime.now(timezone.utc)


def load():
    try:
        return json.loads(STATE.read_text())
    except (OSError, ValueError):
        return {}


def cached():
    state = load()
    try:
        fresh = (now() - datetime.fromisoformat(state["checked_at"])).total_seconds() < 26 * 3600
    except (KeyError, TypeError, ValueError):
        fresh = False
    if not fresh:
        print("Unity access has not been checked recently: run python3 tools/unity_auth.py", file=sys.stderr)
        return 1
    if state.get("status") != "ready":
        print(state.get("message", "Unity access needs attention"), file=sys.stderr)
        return 1
    return 0


def check(notify=False):
    previous = load()
    # A real authenticated GET lets the CLI refresh its stored OAuth session.
    # auth status alone is insufficient evidence that Cloud requests work.
    try:
        result = subprocess.run(["unity", "cloud", "org", "--json", "--non-interactive"],
                                capture_output=True, text=True, timeout=45)
        payload = json.loads(result.stdout)
        codes = {e.get("code") for e in payload.get("errors", [])}
        if result.returncode == 0 and payload.get("success"):
            status, message = "ready", "Unity login and Cloud access OK"
        elif codes & {"NOT_SIGNED_IN", "SESSION_EXPIRED", "AUTH_REQUIRED", "AUTH_SESSION_EXPIRED"}:
            status, message = "login_required", "Unity login needs attention: run unity auth login"
        else:
            status, message = "unavailable", "Unity Cloud check failed: run unity cloud org --json to diagnose"
    except (OSError, ValueError, subprocess.SubprocessError):
        status, message = "unavailable", "Unity access check failed: check the CLI and network"
    timestamp = now()
    state = {"checked_at": timestamp.isoformat(), "status": status, "message": message,
             "last_notified_at": previous.get("last_notified_at")}
    if notify and status != "ready":
        try:
            elapsed = (timestamp - datetime.fromisoformat(previous["last_notified_at"])).total_seconds()
        except (KeyError, TypeError, ValueError):
            elapsed = float("inf")
        if previous.get("status") != status or elapsed >= 24 * 3600:
            if shutil.which("notify-send"):
                try:
                    notification = subprocess.run(["notify-send", "--app-name=Siphon", "--urgency=critical",
                                                   "Siphon: Unity access needs attention", message],
                                                  capture_output=True, timeout=10)
                    if notification.returncode == 0:
                        state["last_notified_at"] = timestamp.isoformat()
                except (OSError, subprocess.SubprocessError):
                    pass  # Nonzero service status and the cached warning remain visible.
    STATE.parent.mkdir(parents=True, exist_ok=True)
    temporary = STATE.with_suffix(".tmp")
    temporary.write_text(json.dumps(state, indent=2) + "\n")
    temporary.replace(STATE)
    print(message, file=sys.stdout if status == "ready" else sys.stderr)
    return 0 if status == "ready" else 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cached", action="store_true", help="show a local warning only; no network")
    parser.add_argument("--notify", action="store_true", help="desktop alert on failure; at most daily unless status changes")
    args = parser.parse_args()
    sys.exit(cached() if args.cached else check(args.notify))
