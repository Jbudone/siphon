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

W02 precedes A01 at the user's request: finish PR-Agent setup, use a separate fresh reviewer, and assess mem0. GitHub auth works as Jbudone; no remote is configured and no Siphon repository was found under that account. The user has been asked for a repository destination. `OPENROUTER_API_KEY` is not loaded by this project's direnv; no secret value was printed. Hosted PR-Agent activation and an observed Actions review remain outstanding.

Memory recommendation: retain Git-backed Markdown now; see [audit](MEMORY-AUDIT.md). No mem0 service was installed or given project data. Require evidence of improved project recall before adding it; current OSS ADD-only extraction retains superseded facts alongside new ones.

Unity login and Cloud access passed at this session start. No engine mutation or paid generation was performed. A01 remains deferred; its saved packet remains in CURRENT-TASK.md. `.vscode/` is pre-existing and untouched.

A user systemd timer `siphon-unity-auth.timer` is enabled every six hours. It exercises CLI token refresh with a Cloud GET. Failures produce desktop notifications (delivery verified), a failed service and a cached directory-entry warning. Five offline monitor tests pass; network failure does not get mislabeled as expiry. The timer cannot bypass revoked credentials; never ignore the alert. No auth tokens are copied to the project.

## Session handoff

W02 local configuration passed TOML parsing, workflow YAML/config assertions, local Markdown link checks and Git whitespace checks. Separate fresh Sol/medium implementation and review agents completed; independent review found no actionable correctness findings. Hosted review remains unverified; hosted activation awaits the GitHub destination and Actions secret. Next command after a destination is selected: `gh repo view OWNER/REPO --json nameWithOwner,url`, then inspect `gh secret list --repo OWNER/REPO` before configuring credentials. Do not print secrets. Resume the current task, not the deferred paid rifle request.

The verified local slice is committed with its handoff; use `git log -1 --oneline` for the revision. No workers remain in flight. `.vscode/` remains the only pre-existing untracked path and must stay untouched. No paid jobs are in flight. Check Git status for final committed state and CURRENT-TASK for actual verification evidence.
