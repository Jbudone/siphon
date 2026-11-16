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

Git-backed Markdown remains memory; mem0 deferred. One fresh worker, then separate reviewer, then root integration. User selected public `Jbudone/siphon`: https://github.com/Jbudone/siphon. Public main pushed and origin/main tracks it; all six LFS assets uploaded (27MB). GitHub-rendered README, UPDATES and latest briefing verified; all four public screenshot downloads matched local PNG hashes. User supplied OpenRouter through local direnv; root configured OPENROUTER_API_KEY in Actions secrets without printing its value. User requested a paired PR-Agent/OpenCodeReview trial; fresh Sol worker dual_review_setup owns workflow/config. Root owns trial docs, hosted run verification and handoff. Preserve user-modified .envrc and pre-existing .vscode. No hosted review success claimed yet.

## Handoff

Verified slice committed with handoff; `git log -1 --oneline` identifies it. No asset generation jobs in flight. dual_review_setup implementation worker active; model review runs will be recorded when started. `.vscode/` remains pre-existing/untracked/untouched. No intentional uncommitted paths after integration; check actual Git status. No human playtest request.
