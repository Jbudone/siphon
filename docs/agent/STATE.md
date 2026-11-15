# Restart state

Updated: 2026-11-15. Read this first, then [current task](CURRENT-TASK.md). ROADMAP owns priority; CONTEXT owns terminology and design constraints.

## Completed slice

A01: generated/imported one pulse rifle, .8m reusable prefab, 2K URP materials, scene-builder reference and rifle/disc visual selection. Accepted movement, damage, cadence and firing origin unchanged. Candidate: `Builds/SiphonCandidate/Siphon.x86_64`; existing Builds/Siphon untouched. [Briefing](../briefings/2026-11-15-rifle.md), [asset/provenance](../assets/PULSE-RIFLE.md), exact commands/results and input hashes in CURRENT-TASK.

Candidate build/integration checks and 25s actual-player smoke passed; switching/firing/recoil and screenshots verified. Fixed a pre-existing Stalker native allocation during component construction, and a disabled-collider bug in its validator setup. Final isolated Stalker validation passed (4 pounces, 4 bites, occlusion/sound and legacy save). Separate Sol reviewer found no actionable issues. Fresh worker creation hit platform thread limit; root implemented directly and reused the separate reviewer. Do not claim a fresh worker ran.

Meshy jobs completed/downloaded: preview `01a1e181-2bca-751e-9815-3fb18f1e17fd`, refine `01a1e182-185a-7182-8f86-e2a18314e377`. Observed balance 1870 (900 monthly, 970 permanent), 30 spent. No paid jobs or workers in flight. Journals in ignored artifacts/meshy; never resubmit these jobs. Use `direnv exec .` for credentials. Git LFS now stores binary rifle art with Unity .meta files.

## Next action

M01 bounded terrain planning/measurement, then implementation in small slices. Exact next command: `git status --short`, then read ROADMAP M01, CONTEXT and only terrain/navigation-boundary code in AssaultSceneBuilder, WarDirector and BotLocomotion. Current map is 600m; proposed 2km × 2km is provisional. Hard-coded ~295m recovery bounds require inspection. No broad map rebuild before checking dependencies. G01 still owns hands/animation/world model/disc follow-ups.

Unity 6000.6.0f1 / URP 17.6.0. Session login/Cloud check passed. MCP still points to `/home/jbud/jdrive/jstuff/work/personal/yendor/game`; do not mutate it. All Siphon engine work used explicit batch projectPath. Unity AI CLI auth/service access was verified previously (884 credits then); recheck balance before generation. The six-hour auth monitor remains enabled; report real login failures.

## Workflow and pending setup

Git-backed Markdown remains memory; mem0 deferred after [audit](MEMORY-AUDIT.md). Worker → separate reviewer → orchestrator integration; one child at a time. PR-Agent local config verified, but no remote is configured. GitHub auth works as Jbudone; destination question and Actions OPENROUTER_API_KEY remain unresolved. No repo was created/pushed and no secret uploaded. Hosted review is unverified and does not block local slices.

## Handoff

Slice committed with handoff; `git log -1 --oneline` identifies it. `.vscode/` was already untracked and remains untouched. Check Git status for actual remaining paths; no intentional uncommitted work remains after integration. Source/build input hashes preserve exact tested provenance. No human feedback request is needed.
