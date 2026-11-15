# Restart state

Updated: 2026-11-15. Read this first, then [current task](CURRENT-TASK.md). Keep this file short.

## Intent

The user made large Tribes-scale terrain, actual guns/models/animations, sound, VFX, UI/HUD, and defensive patrols immediate priorities. Use a small development loop and begin using existing Unity AI/Meshy credits. The user rejected the extra workflow recommendations as overkill; keep process minimal and include PR-Agent. Keep user communication concise and human testing minimal.

## Authority

- [ROADMAP](../../ROADMAP.md): priority and status.
- [CONTEXT](../../CONTEXT.md): design constraints; accepted movement/weapons stay the baseline.
- [WORKFLOW](../WORKFLOW.md): one fresh worker at a time; Git + Markdown memory; model tiers; release and telemetry rules.
- [Asset pipeline](../assets/PIPELINE.md): provider access, generation/import steps, provenance.

## Current facts

- Git was absent. Local `main` now preserves the existing prototype in baseline commit `9ff5803`; no remote configured.
- Unity 6000.6.0f1, URP 17.6.0. Local graybox is 600m; larger map is planned, not implemented.
- Unity MCP status returned project `/home/jbud/jdrive/jstuff/work/personal/yendor/game`, not Siphon. Never mutate that connection for this task.
- Unity CLI was upgraded from beta.8 to 1.0.0. Unity AI Generators 0.1.0-beta.1 is installed and the auth broker is running. `unity ai doctor` now verifies login, renewable broker tokens, AI subscription/terms, generation service and 884/1000 available Unity Credits. Default org is josh-none (1375988431442). Doctor has zero failures; warnings concern registry lookup and the optional unmirrored agent skill. CLI generation does not require a Siphon Editor Cloud link; editor-specific integration remains separate.
- `.envrc` now loads MESHY_API_KEY from the existing vault entry meshy-key using use_secret. Direnv is allowed. `direnv exec . python3 tools/meshy.py balance` verified 1900 credits (930 monthly, 970 permanent). No paid jobs submitted.
- CodeBurn project-scoped export works; manual per-task metrics are optional. PR-Agent config/workflow mirrors the existing Yendor setup, with advisory review only. No Siphon GitHub remote exists, so review is configured but inactive; it will need OPENROUTER_API_KEY in Actions secrets.
- Source already contains StalkerBehaviour/StalkerValidation beyond the last iteration notes; inspect before assigning duplicate PvE work.

## Next action

W01 is complete: seven offline Meshy tests, preview dry-run, workflow status/handoff/snapshot, TOML parsing, Markdown links and Git whitespace checks passed. One worker (`asset_pipeline`, Sol/medium, fresh context) completed; no workers or paid jobs remain in flight. No gameplay edits or new engine validation.

Use A01 in [current task](CURRENT-TASK.md). Meshy is available: use `direnv exec . python3 tools/meshy.py` to submit/resume the saved rifle preview. The user completed Unity login. Cloud access and AI access are verified; the monitor service now succeeds and the cached login warning is cleared. No access action is needed from the user. If access remains blocked, take one bounded map-planning/measurement slice from M01; do not start a broad content rewrite. `python3 tools/workflow.py status` shows current Git state; `git log -1 --oneline` identifies the workflow commit. Raw telemetry is local/ignored. `.vscode/` was already present and remains untracked/untouched.

A user systemd timer `siphon-unity-auth.timer` is enabled every six hours. It exercises CLI token refresh with a Cloud GET. Failures produce desktop notifications (delivery verified), a failed service and a cached directory-entry warning. Five offline monitor tests pass; network failure does not get mislabeled as expiry. The timer cannot bypass revoked credentials; never ignore the alert. No auth tokens are copied to the project.

## Session handoff rule

Before stopping, replace this section with actual completed work, exact next command, uncommitted paths, in-flight worker/job IDs, and blockers. Do not leave session-specific assumptions as durable facts. Check Git and current task for the final W01 commit and verification.
