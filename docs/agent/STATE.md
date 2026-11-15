# Restart state

Updated: 2026-11-15. Read this first, then [current task](CURRENT-TASK.md). Keep this file short.

## Intent

The user made large Tribes-scale terrain, actual guns/models/animations, sound, VFX, UI/HUD, and defensive patrols immediate priorities. First establish a cheap, reliable agent workflow and begin using existing Unity AI/Meshy credits. Keep user communication concise and human testing minimal.

## Authority

- [ROADMAP](../../ROADMAP.md): priority and status.
- [CONTEXT](../../CONTEXT.md): design constraints; accepted movement/weapons stay the baseline.
- [WORKFLOW](../WORKFLOW.md): one fresh worker at a time; Git + Markdown memory; model tiers; release and telemetry rules.
- [Asset pipeline](../assets/PIPELINE.md): provider access, generation/import steps, provenance.

## Current facts

- Git was absent. Local `main` now preserves the existing prototype in baseline commit `9ff5803`; no remote configured.
- Unity 6000.6.0f1, URP 17.6.0. Local graybox is 600m; larger map is planned, not implemented.
- Unity MCP status returned project `/home/jbud/jdrive/jstuff/work/personal/yendor/game`, not Siphon. Never mutate that connection for this task.
- Siphon has blank Cloud project/organization IDs and no AI generator packages in manifest. Unity CLI auth reports expired. Asset generation via Unity AI is not verified.
- No MESHY_API_KEY was found in session environment or project .env. User was asked for a credential location and intended Unity organization/project. No paid jobs submitted.
- CodeBurn project-scoped export works; raw data stays ignored under telemetry. Historical validation results do not verify the present source/build.
- Source already contains StalkerBehaviour/StalkerValidation beyond the last iteration notes; inspect before assigning duplicate PvE work.

## Next action

W01 is complete: seven offline Meshy tests, preview dry-run, workflow status/handoff/snapshot, TOML parsing, Markdown links and Git whitespace checks passed. One worker (`asset_pipeline`, Sol/medium, fresh context) completed; no workers or paid jobs remain in flight. No gameplay edits or new engine validation.

Use A01 in [current task](CURRENT-TASK.md). Once credentials arrive, run `python3 tools/meshy.py balance`, then submit/resume the saved rifle preview. If access remains blocked, take one bounded map-planning/measurement slice from M01; do not start a broad content rewrite. `python3 tools/workflow.py status` shows current Git state; `git log -1 --oneline` identifies the workflow commit. Raw telemetry is local/ignored. `.vscode/` was already present and remains untracked/untouched.

## Session handoff rule

Before stopping, replace this section with actual completed work, exact next command, uncommitted paths, in-flight worker/job IDs, and blockers. Do not leave session-specific assumptions as durable facts. Check Git and current task for the final W01 commit and verification.
