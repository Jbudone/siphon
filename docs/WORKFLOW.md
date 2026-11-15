# Development workflow

Keep the process small: pick one useful change, build it, check it, and commit it.

- **Roadmap:** [ROADMAP.md](../ROADMAP.md) is the single priority list. Update it when work changes status.
- **Memory:** [STATE.md](agent/STATE.md) holds the next action, blockers and important decisions. Read relevant code rather than loading every historical note.
- **Workers:** use one fresh worker at a time for a bounded task. Give it file paths, constraints and a clear completion condition. Choose Luna for straightforward work, Sol for normal implementation, Astra for difficult problems. Trivial edits stay with the orchestrator.
- **Review:** PR-Agent reviews non-draft PRs and new commits, reports at most five concrete issues, and remains advisory. Use the existing OpenRouter review model/configuration. It does not rewrite descriptions or merge changes. Local configuration is ready; activation needs a GitHub remote and its OPENROUTER_API_KEY Actions secret.
- **Updates:** write a concise briefing when there is something useful to show. Include new features/assets, screenshots when relevant, important bugs and uncertain decisions. Avoid a report for every small edit.
- **Human testing:** [HUMAN-FEEDBACK.md](HUMAN-FEEDBACK.md) contains only essential requests with exact steps and time estimates. No generic playtest homework.
- **Telemetry:** CodeBurn already captures usage. Inspect it when useful; manual per-task bookkeeping is optional.

## Environment

`.envrc` uses the existing vault entry `meshy-key` through the user's `use_secret` helper. No credential is written to the repo. Direnv loads it for interactive shells; agents should run Meshy commands through `direnv exec .`.

```sh
direnv exec . python3 tools/meshy.py balance
python3 tools/workflow.py handoff
```

## Unity sign-in

The CLI was upgraded from beta.8 to 1.0.0, which includes newer authentication fixes. The CLI owns refresh tokens in the system credential store; do not copy tokens into .envrc or write a second token-refresh implementation.

`tools/unity_auth.py` makes a real Cloud request so the CLI can refresh its session. A user timer runs it every six hours. Failed access produces a desktop alert, a failed service, and a warning when entering this directory. A cached check older than 26 hours also warns. Network failures are distinguished from a required login; the monitor never repeatedly opens a browser.

```sh
python3 tools/unity_auth.py
unity auth login
systemctl --user status siphon-unity-auth.timer siphon-unity-auth.service
```

If Unity revokes the refresh token, a one-time browser sign-in is still necessary. Monitoring makes that visible; it cannot bypass Unity's authentication requirements. The timer units live in tools/systemd and are installed for this user on this machine.
