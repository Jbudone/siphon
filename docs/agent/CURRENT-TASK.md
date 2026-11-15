# W02 — Independent review and memory audit

Status: Local setup verified; hosted activation blocked on destination and secret. User moved PR-Agent setup, separate review and mem0 assessment ahead of A01 on 2026-11-15.

Scope: verify and repair PR-Agent configuration, add a separate fresh reviewer role, assess mem0 against Git-backed Markdown. Root owns workflow/handoff and memory audit; fresh Sol/medium `review_setup` worker owns PR-Agent files and reviewer profile.

Local evidence: fresh Sol/medium implementation worker completed; TOML parsing, workflow YAML/config assertions, local Markdown links and Git whitespace checks passed. Upstream runner/config inspected and existing model confirmed in live OpenRouter catalog. Independent fresh Sol/medium reviewer `independent_review` found no actionable correctness findings and independently verified upstream event/config handling and the mem0 migration claims. No hosted review or engine validation is claimed.

Hosted activation needs a GitHub destination: checkout has no remote, GitHub auth works as Jbudone, no matching Siphon repository was found under that account. Destination question is pending. Do not claim a live review until an actual Actions run and review are observed. A01 remains the next gameplay slice.

The deferred A01 task packet is below. W01 is complete; its summary preserves setup evidence.

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

## Deferred packet: A01 — First generated gun

Status: Meshy credentials work through direnv (1900 credits observed). Unity AI access is verified with 884/1000 credits available. These services work independently.

Read: AGENTS.md, docs/assets/PIPELINE.md, current request JSON, PlayerCombat.cs, AssaultSceneBuilder.cs, PlaygroundBuilder.cs. Inspect only relevant builder methods before editing.

Outcome: one pulse-rifle model in a repeatable import/prefab path, visible in the actual game, with reviewed materials and recorded generation provenance. Use existing API credits; start one preview, inspect, refine the accepted geometry, then download promptly. Avoid whole-character or weapon-set generation until one import works.

Acceptance: correct scale/pivot/muzzle, no changed damage/cadence, URP materials, scene regeneration preserves references, no compile errors, actual build screenshot, job IDs/credit observations and source revision in briefing. Record any style assumption as provisional.

If blocked: report exact missing access and preserve request/job state. Do useful independent roadmap work; do not repeatedly resubmit or repeatedly ask the user for the same credential.

Next command: `direnv exec . python3 tools/meshy.py submit docs/assets/requests/pulse-rifle-preview.json`. Balance was checked successfully; recheck if needed. If the journal already has a job, resume it by ID. Unity AI account setup can proceed independently; a Meshy rifle does not require Unity AI to generate.
