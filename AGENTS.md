# Siphon agent instructions

## Start with a small context

Read `docs/agent/STATE.md`, then the current task and only its named references. `ROADMAP.md` owns priorities; `CONTEXT.md` owns game terminology and design constraints. Historical iteration notes are evidence, not a current task list. At session start, run `python3 tools/unity_auth.py`; report login failures and never silently skip Unity AI because of expired access. Use `direnv exec .` for Meshy credentials. Do not scan Library, Builds, Logs, Backups, or all agent transcripts by default.

## Work model

The root agent is the orchestrator. It selects a bounded slice, supplies a fresh worker with explicit file ownership, assigns a separate fresh reviewer, resolves findings, integrates the result, and updates the handoff. Use **one worker at a time** by default. Implementation and review run sequentially. The reviewer must not be the orchestrator or the implementing worker; it inspects the diff and evidence without editing source or merging. Workers must not spawn workers. Use `fork_turns="none"` when supported and provide the task text plus file paths; do not inherit the full conversation.

Keep the loop simple: pick a useful change, build it, check it, commit it. Choose an available model for the task: `gpt-6-luna` for bounded docs/search/mechanical edits; `gpt-6.1-sol` for normal implementation and integration; `gpt-6-astra` for difficult authority, concurrency, architecture, or persistent failures. Use supported effort settings; keep model choice proportionate to the task. Escalate on evidence of difficulty. Do trivial work directly when delegation would cost more than it saves. Never claim routing occurred without observing the actual selection.

Before a fresh orchestrator session, update STATE and the task with completed work, exact next command, blockers, uncommitted files, and evidence. Start fresh at a completed slice or when context becomes cluttered; do not reset midway through an unrecorded edit or paid generation request. Agent profiles are under `.codex/agents/`; a profile does not create a scheduled background service.

## Integration and verification

- Use Git; commit a verified slice with its roadmap/handoff updates. Never overwrite user edits. Source assets and Unity `.meta` files travel together.
- Preserve accepted movement tuning, weapon damage, and cadence unless the user changes the baseline.
- Confirm the Unity connection's `projectPath` is this repository before any mutation. Other projects may be open. One editor process may hold Siphon; run engine validators sequentially. Existing commands are in README.
- Generated scene rebuilds replace scene content. Integrate imported art as referenced prefabs/assets so rebuilding does not erase it; inspect builder behavior before changing it.
- Run checks proportionate to the change; record exact results and build provenance. Never reuse historical results as evidence for a new build.
- Meshy/Unity AI use of existing credits is authorized. Check balance/expiry, persist job IDs before continuing, avoid duplicate paid requests, stage and inspect output, and record prompts/provider/model/credits/provenance. Do not buy credits or enable paid overages without authorization. See `docs/assets/PIPELINE.md`.
- Write an update briefing when there are useful changes to show, not for every edit. Update `docs/briefings/LATEST.md` and ROADMAP's latest link. Keep it concise; label unverified work and uncertain agent decisions. Capture screenshots from the actual build for visible changes.
- Keep human requests in `docs/HUMAN-FEEDBACK.md`: at most three, preferably zero, with exact build, steps, time estimate, and a decision the answer unlocks. Do all automated checks first. Do not ask the user to retest accepted movement without evidence of regression.
- Use PR-Agent for advisory PR review; address concrete findings. CodeBurn tracks usage; manual per-slice metrics are optional. Avoid extra process that does not help deliver the current change.

## Communication

Use concise plain English. Lead with what changed, what was checked, and any blocker. Skip decorative language and long work logs. Ask only for information or decisions that cannot be recovered from the repo or connected tools.
