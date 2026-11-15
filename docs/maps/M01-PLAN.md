# M01 terrain dependency audit and proposed first slice

2026-11-15. Planning only; the accepted 600m assault scene and movement lab remain the baseline. [ROADMAP M01](../../ROADMAP.md) proposes 2km × 2km and roughly 1km between bases; neither is a settled design decision. This audit uses source reads and arithmetic, **not a fresh Unity measurement, build, playtest, or performance result**.

## Current geometry, calculated from source

| Property | Current source-derived value | Evidence |
| --- | --- | --- |
| Terrain extent | X/Z −300…300m; 0.36km² | `AssaultSceneBuilder.MakeTerrain`: size 600 |
| Mesh | 100 × 100 quads, 6m vertex spacing, 10,201 vertices, 20,000 triangles; one mesh collider, three material regions | `MakeTerrain`; mesh stored at `Assets/Siphon/Meshes/Field.asset` |
| Core spacing | Blue (0,−185), red (0,185): 370m in X/Z | `Build` / `BuildBase` |
| Player spawn spacing | (−7,−178) to (7,175): approximately 353.28m in X/Z | `Build`; heights come from `Height` |
| Base deck | 38 × 32m; front defenses 20–24m toward the center | `BuildBase` |
| Grapple masts | Five, alternating X ±40, Z −70…70 in 35m steps; consecutive separation approximately 87.32m in X/Z | `Build`; bridge is 84 × 8m at Y=32 |
| Camps | Frost vault (228,92), forest ruins (−225,−20) | `Build`; also duplicated in HUD |
| Navigation | 151 × 151 = 22,801 sample nodes, 4m spacing; origin (−300,−300), last sample (300,300) | `NavigationBaker.Bake`, `NavigationField` |

Distances above exclude terrain-following distance. `Height` is procedural: ridges centered on X ±95m, a flat central corridor and base flattening starting at |Z|=145m. Forest/snow material bands switch at X ±110m. Extending only the mesh size would repeat the sinusoidal hills while leaving most landmarks, resources, bases and flattening logic concentrated in their old locations.

## Dependencies that must move together

| Boundary | Current behavior and expansion implication |
| --- | --- |
| Scene/asset generation | `AssaultSceneBuilder.Build` creates a new empty scene, overwrites `Field.asset` and bakes `FieldNavigation.asset`. A separate candidate scene alone would still alter shared geometry/navigation. Give a large-map candidate separate generated asset paths. Keep rifle prefab references, and preserve the movement lab and accepted candidate. |
| Navigation topology | A* uses eight neighbors, forbids diagonal corner cutting, rejects adjacent rise >0.9 × run and charges run +0.4 × rise. Bake requires terrain normal Y≥0.72 (approximately 43.95° maximum slope), plus capsule clearance. These are distinct checks; a visual ski slope need not be bot-traversable. Bake raycasts from Y=150 down 300m on terrain layer 9. |
| Search budget | `NavigationWorld.FindPath` caps heap pops at 12,000, including duplicate/closed entries. `NearestFree` searches at most eight rings (32m axis distance at current resolution). Grid indices clamp outside points to the edge. Enlarging coverage does not prove long routes fit the search budget. Do not simply increase the cap without timing routes. |
| Dynamic obstacles | Navigation refresh clears its full blocked array and marks expanded collider bounds for structures. All per-node arrays grow with map area. Measure invalidation cost and route latency together. |
| Bot recovery | `BotLocomotion.Tick` returns bots home beyond |X/Z|=295 or below Y=−40. Steering replans every approximately 4–4.3s, on goal displacement >8m or navigation revision; failed paths fall back to direct steering. Track failures and recovery teleports explicitly on long routes. |
| Player recovery | `PlaygroundPlayer` has a Y<−80 fall reset and manual R reset to its stored spawn; no matching X/Z recovery bound. Preserve movement tuning. Set an intentional map-edge policy instead of copying bot teleport behavior into the player controller. |
| Commands and grounding | `WarDirector.MarkTarget` rejects |X/Z|>295. `GroundPoint` casts from absolute Y=150 down 300m and may hit structures; build-site terrain casts have their own vertical range/support checks. Raising terrain substantially requires a separate height/raycast audit. |
| HUD | `FieldHud.Map` labels 600m, normalizes X/Z using +300 and /600, paints fixed biome fractions and draws camps at fixed coordinates. Derive extent/landmarks from layout data while retaining expiring sensor snapshots. |
| Save compatibility | `FieldSaveSystem` snapshot v3 has no map identity; one `field-war-v1.json` path stores absolute coordinates. Fixed structures/nodes match by position; player restore resets beyond |X/Z|=290. A moved map could silently mix incompatible state. Isolate candidate saves or add a checked layout ID before enabling it; keep legacy 600m load behavior. |
| Presentation and simulation | Camera far clip 900m, exponential fog density 0.0018 and shadow distance 160m are current builder values. A 1km base gap needs actual sightline inspection. WarDirector's 14 team bots and five creatures, wildlife coordinates, resource placement and base offsets remain local assumptions; map area is not a reason to multiply population. |

Source scope: `Assets/Siphon/Editor/{AssaultSceneBuilder,NavigationBaker}.cs`, `Assets/Siphon/Runtime/{WarDirector,BotLocomotion,NavigationWorld,NavigationField,PlaygroundPlayer,PlayerCombat,FieldHud,FieldSaveSystem,MovementTuning}.cs`, and `Assets/Siphon/Settings/Movement.asset`. This is a boundary audit, not a full AI/objective or respawn-network review; inspect their destination selection before relocating content.

## Provisional expansion experiment

- Candidate bounds X/Z −1000…1000m, with bases near (0,−500)/(0,500); retain original base-sized footprints. Put useful forward outpost sites about 200–300m toward the center from each base, with cover and two approach choices. These positions are design proposals, not measured placements.
- Author broad rolling ski routes around a central contested corridor and two flanking routes. Place safe local supply nodes near each base; distribute contested resources and camps deliberately. Do not scale every structure, weapon range, hill wavelength or movement value by 3.33.
- Start with 250m terrain tiles, 50 quads per side (5m spacing): 8 × 8 tiles, 166,464 vertices including duplicated borders, 320,000 triangles. Match border heights/normals and inspect collider seams. This is a budget proposal; a single mesh at this resolution exceeds 16-bit vertex indexing. Tiling introduces seams and does not itself establish streaming or adequate performance.
- Retaining 4m navigation spacing gives 501² = 251,001 nodes, approximately 11.01 times today's count. Keep resolution initially so clearance semantics remain comparable; benchmark before considering coarser or hierarchical navigation. No capacity or frame-rate claim follows from these counts.
- Straight-line 1km travel estimates: 62.5s at the accepted 16m/s ground speed, 11.76s at the 85m/s speed cap; bots roughly 143s heavy (7m/s), 91s worker (11m/s), 59s scout (17m/s), before acceleration, terrain, detours or combat. The speed cap is not a sustained route speed. Measure outbound and return runs with ordinary equipment before accepting the gap.

## Next implementation slice: shared layout contract at 600m

Keep this smaller than building the new terrain. Add one explicit assault-layout definition containing extent, safe inset(s), navigation spacing, base/spawn and camp anchors, with the present layout as default. Feed builder, baker, bot bounds, target validation, HUD projection and save player bounds from it. Retain today's distinct 5m recovery/target and 10m save insets. Scope a worker to the new definition plus these consumers; do not retune movement/combat or change map size. Resolve how runtime consumers obtain the definition without a scene singleton dependency in the movement lab. Defer save-schema changes and new terrain to the following candidate slice.

Acceptance for that implementation:

1. Confirm connected Unity `projectPath` equals this repository before editor mutations; otherwise use an explicit Siphon batch path. Run validators sequentially.
2. Regenerated default layout still has extent ±300, core/spawn/camp coordinates above, 10,201 mesh vertices and 22,801 navigation nodes; rifle prefab survives rebuilding. Accepted movement asset, damage and cadence remain unchanged.
3. Boundary checks preserve current inclusive thresholds: bot/mark at 295 versus just beyond, save restore at 290 versus just beyond; HUD corners map correctly. Legacy saves and the independent movement lab still load. Use targeted engine assertions for these behaviors, not only constant comparisons.
4. Run relevant existing navigation, persistence and scene integration checks plus an actual candidate smoke; record commands, fresh results and build inputs. Check README's current entry points before selecting validators. Do not call this slice a 2km map or reuse A01's results as new evidence.

Following slice: create opt-in large-map scene and distinct mesh/navigation/save destinations; audit bot objectives/respawn selection, then implement terrain and placement. Its acceptance must include edge/collider seams, both-direction base/flank/outpost route completion, failure/recovery counts, elapsed travel, and representative navigation/frame timings on named hardware. Capture the actual candidate, retaining the 600m baseline and movement lab. Final dimensions and budgets remain open until these measurements exist.

## Checks and limits of this planning slice

Read-only source inspection and Python arithmetic produced the counts/distances above. No engine validation, scene regeneration, build, gameplay tuning, asset generation or paid request was performed; paid-job IDs: none. Runtime collider bounds, terrain heights, path reachability, travel times and performance remain unmeasured for M01. Unity auth/connectivity diagnosis is tracked by the orchestrator; this document does not claim a fresh service check succeeded.
