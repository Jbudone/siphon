# A01 — First generated gun

Status: Ready for Meshy; Unity AI awaits browser sign-in. Owner: root orchestrator. User authorization: 2026-11-15 roadmap and existing-credit use request.

The executable task packet is below. W01 is complete; its summary preserves setup evidence.

## Completed: W01 — Establish the development workflow

Outcome: a fresh agent can resume cheaply, select one task, verify it, and report clearly; the user has a clear roadmap and minimal feedback queue.

Scope: workflow/roadmap documentation, Git setup, agent profiles, local telemetry helpers, and a resumable Meshy helper. No gameplay or movement changes.

Acceptance:

- All requested immediate priorities appear in ROADMAP with honest status and a first slice.
- Git captures existing prototype separately from new workflow changes; generated caches/builds and secrets stay ignored.
- Fresh-session packet, worker model policy, release briefing format, and human-feedback admission rules exist.
- Workflow helper works; CodeBurn snapshot is project-scoped. No savings are invented.
- Meshy helper verifies offline behavior; no live generation is claimed without a real job and output.
- Unity and Meshy access blockers are recorded with exact next steps.

Worker slice: `asset_pipeline`, gpt-6.1-sol / medium, fresh context. Reason: bounded API integration with durable paid-job handling needs normal implementation reasoning. Owns `tools/meshy.py`, its offline checks, and `docs/assets/requests/` only. Root owns integration and docs.

Evidence: `python3 -B -m unittest discover -s tools -p 'test_meshy.py' -v` passed all seven checks. Preview dry-run made zero network calls. Workflow help/status/handoff/snapshot commands passed; CodeBurn exported only Siphon; TOML parsed; local Markdown links resolved; Git whitespace check passed. Profiles were written and project config loaded without error by the CLI; automatic scheduling/session resets were not implemented. No gameplay or engine validation was needed or claimed. Find the workflow commit with `git log -1 --oneline`.

## Active packet: A01 — First generated gun

Status: Meshy credentials work through direnv (1900 credits observed). Unity AI plugin is installed; a browser sign-in is still needed. These services work independently.

Read: AGENTS.md, docs/assets/PIPELINE.md, current request JSON, PlayerCombat.cs, AssaultSceneBuilder.cs, PlaygroundBuilder.cs. Inspect only relevant builder methods before editing.

Outcome: one pulse-rifle model in a repeatable import/prefab path, visible in the actual game, with reviewed materials and recorded generation provenance. Use existing API credits; start one preview, inspect, refine the accepted geometry, then download promptly. Avoid whole-character or weapon-set generation until one import works.

Acceptance: correct scale/pivot/muzzle, no changed damage/cadence, URP materials, scene regeneration preserves references, no compile errors, actual build screenshot, job IDs/credit observations and source revision in briefing. Record any style assumption as provisional.

If blocked: report exact missing access and preserve request/job state. Do useful independent roadmap work; do not repeatedly resubmit or repeatedly ask the user for the same credential.

Next command: `direnv exec . python3 tools/meshy.py submit docs/assets/requests/pulse-rifle-preview.json`. Balance was checked successfully; recheck if needed. If the journal already has a job, resume it by ID. Unity AI account setup can proceed independently; a Meshy rifle does not require Unity AI to generate.
