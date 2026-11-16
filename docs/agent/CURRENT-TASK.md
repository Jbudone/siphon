# W03 — paired hosted review trial complete

Status: Done. User requested both PR-Agent and Alibaba OpenCodeReview for later comparison. Both are enabled on internal non-draft PRs, share the initial OpenRouter model alias, and remain advisory. Root configured Actions OPENROUTER_API_KEY from existing direnv without exposing it. User-modified `.envrc` and pre-existing `.vscode/` remain untouched and unstaged.

## Evidence

- [Trial 001](../reviews/trial-001.md): PR #1, base `1cc509d`, head `275642e`. PR-Agent posted no major issues; OCR was partial despite a green job because its projected usage guard prevented ten C# files being dispatched. Raw results preserved.
- [Trial 002](../reviews/trial-002.md): PR #2, same base, head `7c96685`. PR-Agent run `43783375052` and OCR run `43783374966` succeeded. OCR manifest complete: 13 selected/completed, 0 failed/reused/waived, ten C# files included. Both posted no major findings. PR-Agent pruned its diff under the 16000 context cap; full or equal coverage is not proven. No winner or exact cost claim.
- Replay PRs closed without merging; branches retained. [Review index](../reviews/README.md) links committed machine reports and comments. OCR artifacts also retained for 90 days.
- OCR guard now 300000, with manifest verification after artifact upload. Missing, partial, failed, waived or unaccounted coverage fails the job; findings remain advisory. Final PR-Agent retry/auto-approval settings moved from ignored repo keys to trusted workflow env. That placement fix passed local loader checks; no third billable trial.

Fresh Sol implementation and separate fresh Sol reviews completed. Checks: actionlint 1.7.11 for both workflows; TOML/YAML/JSON/schema and runtime pin checks; coverage checker against actual partial report and synthetic complete/empty/waived/unaccounted cases; exact Dynaconf 3.3.5 loader test for host-only env settings. No new Unity build or gameplay source changes in W03. Existing game evidence is historical, in [layout briefing](../briefings/2026-11-15-layout.md) and [build record](../maps/M01-BUILD.json).

## Next slice

Resume M01 opt-in larger-map candidate using [M01 plan](../maps/M01-PLAN.md). Isolate scene, generated mesh/navigation and save/layout identity before moving content. Preserve accepted 600m layout, movement/weapons and movement lab. Proposed 2km dimensions remain provisional until route/performance measurement.

Exact next command: `git status --short`, then read `docs/maps/M01-PLAN.md` and select one bounded implementation slice. No active model/asset jobs, no blocker and no human retest request. `.envrc` and `.vscode/` belong to the user; do not stage them.
