# Restart state

Updated 2026-11-15. Read this first, then [current task](CURRENT-TASK.md). ROADMAP owns priorities; CONTEXT owns terminology/design constraints.

## Completed slice

M01 foundation: one immutable 600m AssaultLayout definition now feeds builder, navigation/defaults, base/spawn/camp anchors, bot recovery/restore, target validation, HUD projection and player-save bounds. Current map remains 600m. Accepted movement, weapon damage/cadence and save schema unchanged; movement lab untouched. Fresh Sol implementation plus separate fresh Sol review completed; no actionable findings. [Dependency plan](../maps/M01-PLAN.md), [briefing](../briefings/2026-11-15-layout.md), exact commands/results and [build hashes](../maps/M01-BUILD.json) in CURRENT-TASK.

Sequential generation/runtime edge checks, existing FieldValidation, Linux candidate build and 25s actual-player smoke all passed exit 0. Save compatibility/nav/economy/respawn checks passed;145 nav searches/0 failures and switching/firing/recoil passed. Screenshot inspected: unchanged 600m baseline and accepted rifle. Full movement validator was not rerun. Candidate is Builds/SiphonCandidate; original Builds/Siphon untouched.

GitHub-facing [UPDATES.md](../../UPDATES.md) holds showcases/briefing archive; README and dated briefings link it. Commit screenshots/briefings together with relative links and keep showcase images in regular Git. GitHub Markdown API render passed; this is not a hosted Pages site or downloadable release.

## Next action

M01 opt-in larger-map candidate: isolate scene, generated mesh/navigation and save/layout identity before moving content. Exact next command `git status --short`; then read CURRENT-TASK and M01-PLAN. Proposed 2km × 2km,1km base gap and tile budgets remain provisional. Measure both-direction routes, recovery/seams/reachability and performance before accepting dimensions. Preserve 600m baseline, rifle and movement lab.

## Access and workflow

Unity 6000.6.0f1/URP17.6.0. Startup auth check failed because CLI had drifted to beta.8/help output. Stable 1.0.0 restored by self-update; Unity login/Cloud check passed again for publication. No user login needed. MCP was previously Yendor; do not mutate unless projectPath is verified Siphon. Engine work used explicit Siphon batch path. Six-hour auth monitor remains enabled; report actual failures.

No paid generation this slice. Prior Meshy rifle preview/refine IDs and 1870 credit observation live in [asset record](../assets/PULSE-RIFLE.md); both downloaded, never resubmit. Use direnv for credentials. Recheck balance before new jobs. Git LFS stores rifle binary art and source assets retain .meta pairs.

Git-backed Markdown remains memory; mem0 deferred. One fresh worker, then separate fresh reviewer, then root integration. Public repository: https://github.com/Jbudone/siphon; main tracks origin/main. All six rifle LFS assets previously uploaded (27MB); public screenshots previously hash-verified. OPENROUTER_API_KEY configured in Actions secrets from direnv without printing its value.

W03 complete: PR-Agent and Alibaba OpenCodeReview enabled for internal non-draft PRs, with separate comments and advisory outputs. [Paired trial evidence](../reviews/README.md) preserves both runs. Trial 001 exposed OCR partial coverage; trial 002 completed 13/13 selected items, including ten C# files, after increasing its bounded guard and adding manifest verification. Both posted no major findings. PR-Agent pruned its diff under its 16k cap; equal coverage and tool preference are not established. Replay PRs #1/#2 closed without merging; audit branches retained. No new game build or asset generation in W03.

## Handoff

Implementation and independent review completed sequentially with fresh Sol agents. Final PR-Agent host-only settings moved into trusted workflow environment after local loader/config checks; no third paid trial was run for that placement correction. No model or asset jobs in flight. Check actual Git status: user-modified `.envrc` and pre-existing untracked `.vscode/` remain untouched and uncommitted. No blocker or human playtest request. Next exact command: `git status --short`, then read CURRENT-TASK and the named M01 plan before selecting the next bounded worker task.
