# Restart state

Updated 2026-11-15. Read this first, then [current task](CURRENT-TASK.md). ROADMAP owns priorities; CONTEXT owns terminology/design constraints.

## Completed slice

M01 candidate isolation: opt-in **600m clone**, separate scene/mesh/navigation/build/save identity `assault-candidate600-v1`; default scene/generated assets, movement tuning, weapon source and lab unchanged. v4 saves record identity, legacy no-ID saves load only on default, cross-layout primary/backup restores reject before mutation. Candidate editor reload and restart exercised. [Briefing](../briefings/2026-11-15-candidate.md), [exact checks](../maps/M01-CANDIDATE-CHECKS.md), [build provenance](../maps/M01-CANDIDATE-BUILD.json).

Targeted generation/persistence/reload validator exit 0; default FieldValidation exit 0; separate Linux build exit 0; rendered 25s smoke exit 0, 19 bots/four deliveries/153 navigation searches/zero failures, weapon switching/firing/recoil passed. Screenshot inspected. Full movement validator not rerun. Xvfb/llvmpipe is not performance evidence. Candidate output Builds/SiphonMapCandidate600; existing builds untouched. No 2km geometry or reachability claim.

GitHub-facing UPDATES.md links briefings/screenshots in regular Git. Fresh Sol implementation, separate initial review and uninvolved independent follow-up source review completed. Initial scene-index assumption corrected against actual engine behavior; final outcome tests passed. Both hosted reviews completed on merged PR #3; [trial 003](../reviews/trial-003.md) retains outputs. OCR completed seven selected C# files; PR-Agent pruned context; zero major findings.

## Next action

Select one bounded terrain-expansion slice using [M01 plan](../maps/M01-PLAN.md), with new layout/save identity before geometry changes. Proposed 2km dimensions and tile budgets remain provisional. Preserve accepted 600m map, rifle and movement lab. Measure routes/seams/recovery/performance before accepting expansion.

## Access and workflow

Unity 6000.6.0f1/URP17.6.0. Startup auth check failed because CLI had drifted to beta.8/help output. Stable 1.0.0 restored by self-update; Unity login/Cloud check passed again for publication. No user login needed. MCP was previously Yendor; do not mutate unless projectPath is verified Siphon. Engine work used explicit Siphon batch path. Six-hour auth monitor remains enabled; report actual failures.

No paid generation this slice. Prior Meshy rifle preview/refine IDs and 1870 credit observation live in [asset record](../assets/PULSE-RIFLE.md); both downloaded, never resubmit. Use direnv for credentials. Recheck balance before new jobs. Git LFS stores rifle binary art and source assets retain .meta pairs.

Git-backed Markdown remains memory; mem0 deferred. One fresh worker, then separate fresh reviewer, then root integration. Public repository: https://github.com/Jbudone/siphon; main tracks origin/main. All six rifle LFS assets previously uploaded (27MB); public screenshots previously hash-verified. OPENROUTER_API_KEY configured in Actions secrets from direnv without printing its value.

W03 complete: PR-Agent and Alibaba OpenCodeReview enabled for internal non-draft PRs, with separate comments and advisory outputs. [Paired trial evidence](../reviews/README.md) preserves both runs. Trial 001 exposed OCR partial coverage; trial 002 completed 13/13 selected items, including ten C# files, after increasing its bounded guard and adding manifest verification. Both posted no major findings. PR-Agent pruned its diff under its 16k cap; equal coverage and tool preference are not established. Replay PRs #1/#2 closed without merging; audit branches retained. No new game build or asset generation in W03.

## Handoff

Candidate isolation merged via PR #3 as 589d90b; source head 0cdbbe6. Engine tests/build/smoke, independent source reviews and both hosted reports complete; no model/asset jobs in flight. Final docs archive published on main. Next exact command: `git status --short`, then read CURRENT-TASK and M01-PLAN to choose a bounded terrain-expansion task. Preserve user-modified `.envrc` and pre-existing untracked `.vscode/`, untouched and uncommitted. No blocker or human playtest request. Build artifacts/logs ignored. Check actual Git status before the next slice.
