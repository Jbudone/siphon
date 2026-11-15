# Pulse rifle — first generated asset

Generated 2026-11-15 through Meshy Text-to-3D, `meshy-7.1`, with the repository request prompts. Preview `01a1e181-2bca-751e-9815-3fb18f1e17fd`; refinement `01a1e182-185a-7182-8f86-e2a18314e377`. Observed credits: 1900 → 1880 → 1870 (20 preview, 10 texture). Monthly credits reset at provider timestamp 1795639708000; permanent credits remained 970. Both jobs succeeded and were downloaded promptly; no jobs remain in flight.

[Generation provenance and download hashes](pulse-rifle-provenance.json) · [Build source/asset hashes](pulse-rifle-build.json) · [Preview request](requests/pulse-rifle-preview.json) · [Refine request](requests/pulse-rifle-refine.json)

## Import

`Assets/Siphon/Art/PulseRifle` contains source FBX, 2K maps, URP material and reusable prefab, with Unity-generated metadata. Source model: 7553 triangles. Native FBX import yields a 1.90m-long mesh; prefab fitting sets length to 0.8m and centers its geometry. Unity's imported barrel points along +X; rotate -90° about Y for +Z aim. Visual muzzle sits at (0, 0.065, 0.4), derived from the source tip ring center. It does not move the gameplay firing origin.

Metallic red and inverse roughness alpha are packed into a URP metallic/smoothness texture; normal map imports as a normal map. Collision generation is disabled and the prefab contains no colliders. Binary art is stored using Git LFS. `RifleAssetBuilder.Build` recreates the prefab; the assault scene builder references it and preserves the existing recoil-root name and position. Disc selection uses the previous placeholder. No PlayerCombat, damage, cadence or movement tuning changes.

Source/license: generated from an original text prompt through the user's Meshy API account; no reference image or third-party mesh supplied. Model created with Meshy. Account plan tier was not queried; applicable rights follow [Meshy terms](https://www.meshy.ai/terms-of-use) and its [ownership explanation](https://help.meshy.ai/en/articles/10137554-what-is-the-ownership-of-the-generated-models). Do not infer a specific plan/license from the credit balance.

## Verification

Explicit Siphon project path used for batch operations; MCP remained connected to Yendor and was not mutated. Import and candidate build exited 0. Rebuild/reload checks passed for prefab reference, 0.8m length, URP materials, no colliders and forward muzzle. Actual-build screenshot review caught and corrected the initial reversed FBX orientation.

Final candidate: `Builds/SiphonCandidate/Siphon.x86_64`, built from base `d8d735e` plus this slice; source/asset hashes above identify exact build inputs. The binary hash identifies the executable only, not the whole player data directory. The 25-second smoke run passed, including rifle/disc visual selection and rifle firing/recoil; 19 bots, 4 deliveries, 155 navigation searches / 0 failures. Worst frame was 2559ms including software-rendered startup; this is not a performance benchmark.

Actual build images: [rifle](../images/pulse-rifle.png), [disc selection](../images/pulse-rifle-disc.png), [firing](../images/pulse-rifle-fire.png). Independent separate Sol reviewer reported no actionable correctness findings in source, references, materials and final fixes.

First runtime smoke failed on a pre-existing Stalker native allocation in a field initializer; allocation moved to Awake. Subsequent runtime smoke passed. The focused Stalker validator initially failed because its setup left the stationary player collider disabled, preventing visibility raycasts. Re-enabling the collider after teleport fixed the test; final isolated validation exited 0 with occlusion/sound, warning, four pounces/four bites and legacy-save checks passing.

Visual limitations: style is provisional, with provider-generated pseudo markings and no authored hand/equip animation or world model. This slice establishes the import path and first-person sample; G01 still owns the remaining weapon presentation work.
