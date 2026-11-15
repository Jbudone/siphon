# A01 complete — first generated gun

Completed 2026-11-15. User said to begin after W02 local setup. Root implemented after fresh worker spawn failed on platform thread limit; separate Sol/medium reviewer `independent_review` reviewed source, assets and follow-up fixes without edits, reporting no actionable findings. Reviewer was reused because a fresh worker could not be created; do not claim fresh-agent routing succeeded.

## Outcome and evidence

One 7553-triangle, 0.8m rifle with 2K URP material maps and reusable prefab under `Assets/Siphon/Art/PulseRifle`. AssaultSceneBuilder preserves the prefab through regeneration. WeaponPresentation selects rifle or previous disc placeholder; PlayerCombat and movement tuning unchanged. Visual muzzle aligned to the tip ring at (0,.065,.4); gameplay firing origin unchanged. Binary art uses Git LFS.

Meshy preview `01a1e181-2bca-751e-9815-3fb18f1e17fd`, refine `01a1e182-185a-7182-8f86-e2a18314e377`. Both SUCCEEDED/downloaded/inspected. Balance 1900 → 1880 → 1870; no jobs in flight. Journals remain under ignored `artifacts/meshy/requests/`. [Asset record](../assets/PULSE-RIFLE.md), [generation provenance](../assets/pulse-rifle-provenance.json), [tested build input hashes](../assets/pulse-rifle-build.json).

Exact engine commands (all explicit Siphon project path):

- `/home/jbud/Unity/Hub/Editor/6000.6.0f1/Editor/Unity -batchmode -nographics -projectPath /home/jbud/jdrive/jstuff/work/personal/siphon -executeMethod Siphon.Editor.RifleAssetBuilder.BuildCandidate -quit -logFile /tmp/siphon-rifle-candidate-final.log`: exit 0; compile, prefab persistence, .8m bounds, no colliders, URP and forward muzzle checks passed; Linux candidate built.
- `xvfb-run -a -s '-screen 0 1280x720x24' Builds/SiphonCandidate/Siphon.x86_64 -siphon-smoke -siphon-soak 25 -siphon-capture-weapons -siphon-capture /home/jbud/jdrive/jstuff/work/personal/siphon/artifacts/a01/rifle-final.png -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile /tmp/siphon-rifle-smoke-final.log`: exit 0; visual switching, rifle firing/recoil and smoke passed; 19 bots, 4 deliveries, 155 navigation searches/0 failures. Software-rendered run is not performance evidence.
- `/home/jbud/Unity/Hub/Editor/6000.6.0f1/Editor/Unity -batchmode -nographics -projectPath /home/jbud/jdrive/jstuff/work/personal/siphon -executeMethod Siphon.Editor.StalkerValidation.Run -logFile /tmp/siphon-rifle-stalker-target.log`: exit 0; sound snapshots, occlusion, warnings, 4 pounces/4 bites and legacy save passed.

Initial actual-build QA caught reversed imported orientation (corrected -90° Y) and a pre-existing Stalker native allocation exception (moved MaterialPropertyBlock construction to Awake). Existing Stalker validator also left its player collider disabled; restored after teleport so visibility raycasts work and added diagnostic assertion values. Independent reviewer inspected both fixes. First smoke and two earlier Stalker runs failed; final results above supersede them, not conceal them.

Actual build screenshots in `docs/images/pulse-rifle*.png` and [briefing](../briefings/2026-11-15-rifle.md). No human test requested. Existing Builds/Siphon remains untouched; candidate is Builds/SiphonCandidate. No Yendor mutation. Unity login/Cloud check passed this session.

## Next bounded slice

M01: plan/measure the provisional 2km × 2km terrain and base spacing before broad map replacement. Start `git status --short`, read ROADMAP M01 and CONTEXT, then inspect only terrain-generation and navigation boundary methods in AssaultSceneBuilder, WarDirector and BotLocomotion. Preserve accepted movement and weapons. Existing hard-coded 295m recovery bounds need inspection before expansion. Supply a fresh worker if thread capacity allows, then a separate reviewer.

G01 follow-ups remain hands/equip/fire animation, world gun and disc model. Do not generate another rifle just to recreate this accepted sample. Style/pseudo markings remain provisional.

W02 hosted activation remains pending a GitHub destination and OPENROUTER_API_KEY Actions secret; local independent review works. mem0 deferred per [audit](MEMORY-AUDIT.md). No repository has been created/pushed and no secret uploaded.
