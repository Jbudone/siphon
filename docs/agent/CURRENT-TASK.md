# M01 — 600m layout foundation complete; expansion next

Completed slice 2026-11-15. Fresh Sol/medium `terrain_audit` wrote [dependency plan](../maps/M01-PLAN.md); fresh Sol/medium `layout_contract` implemented bounded code; fresh separate Sol/medium `layout_review` found no actionable correctness findings. Root integrated and ran sequential engine checks. No paid jobs.

## Outcome

New immutable runtime AssaultLayout.Default owns existing 600m extent,5m recovery/target inset,10m save inset, terrain/nav density, base/spawn/camp anchors and HUD projection. Builder/baker, navigation defaults, bot recovery/restore, target validation, HUD and player-save restore use it. No map enlargement, save-schema, movement tuning or weapon balance changes. Movement lab remains untouched; full movement validator not rerun. See [briefing](../briefings/2026-11-15-layout.md) and [tested build inputs/outputs](../maps/M01-BUILD.json).

GitHub-facing UPDATES.md links all briefings and actual-build screenshots, with README entry point and dated archive. Relative image links and regular Git storage preserve GitHub rendering. GitHub Markdown API rendered the table, expandable gallery and 3images successfully. No Pages hosting or downloadable release is implied.

## Exact engine checks

Working directory `/home/jbud/jdrive/jstuff/work/personal/siphon`; explicit Siphon project path used. Run sequentially:

```sh
UNITY=/home/jbud/Unity/Hub/Editor/6000.6.0f1/Editor/Unity
"$UNITY" -batchmode -nographics -projectPath /home/jbud/jdrive/jstuff/work/personal/siphon -executeMethod Siphon.Editor.LayoutValidation.RebuildAndCheck -quit -logFile /tmp/siphon-layout-generation.log
"$UNITY" -batchmode -nographics -projectPath /home/jbud/jdrive/jstuff/work/personal/siphon -executeMethod Siphon.Editor.LayoutValidation.Run -logFile /tmp/siphon-layout-runtime.log
"$UNITY" -batchmode -nographics -projectPath /home/jbud/jdrive/jstuff/work/personal/siphon -executeMethod Siphon.Editor.FieldValidation.Run -logFile /tmp/siphon-layout-field-validation.log
"$UNITY" -batchmode -nographics -projectPath /home/jbud/jdrive/jstuff/work/personal/siphon -executeMethod Siphon.Editor.PrototypeArtifacts.BuildLinuxCandidate -quit -logFile /tmp/siphon-layout-candidate-build.log
xvfb-run -a -s '-screen 0 1280x720x24' Builds/SiphonCandidate/Siphon.x86_64 -siphon-smoke -siphon-soak 25 -siphon-capture-weapons -siphon-capture /home/jbud/jdrive/jstuff/work/personal/siphon/artifacts/m01/layout-baseline.png -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile /tmp/siphon-layout-smoke.log
```

All five exited 0. Generation: original terrain topology/extent, nav coverage, anchors, rifle prefab/reload and HUD projection passed. Runtime: inclusive target/bot295m and player-save290m edges, plus just beyond, on all four axes passed. Field: navigation, legacy version 1 readability/atomic recovery, save roundtrip, economy, shields, sensors, respawn, bot movement and victory passed. Candidate smoke:25s, 19 bots, 4 deliveries, 145 navigation searches / 0 failures, weapon switching/firing/recoil passed. Actual screenshot inspected and committed as `docs/images/layout-baseline.png`. Software-rendered startup worst frame 2527ms is not a performance benchmark. Existing Builds/Siphon not replaced.

## Next bounded slice

M01 expansion: opt-in large candidate with separate scene, mesh/navigation paths and save/layout identity before relocating bases/content. Read plan and only AssaultLayout, scene builder, NavigationBaker, WarDirector, FieldSaveSystem and bot objective/respawn selection as required. Provisional 2km × 2km and 1km base spacing require measured traversal, seam/recovery/reachability checks and named-hardware timing; no such large-map result exists yet. Preserve 600m baseline and movement lab. Exact first command: `git status --short`.

## Access and publication

Initial auth check failed because CLI unexpectedly reported beta.8 and printed help instead of Cloud JSON. `unity self-update --channel stable --yes --json --non-interactive` restored 1.0.0; repeated `python 3 tools/unity_auth.py` passed. No expired-login diagnosis or user action needed. No Yendor editor mutation.

GitHub destination question remains pending. No remote configured and no matching Siphon repo found under Jbudone. User authorized push but did not supply destination; do not create/push to a guessed repo. PR-Agent hosted execution also needs its Actions secret. No repo created, push, secret upload or hosted execution is claimed. `.vscode/` remains pre-existing/untracked/untouched.
