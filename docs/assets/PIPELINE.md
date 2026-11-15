# Generated asset pipeline

Updated: 2026-11-15. Existing Meshy API and Unity AI credits are authorized for Siphon. Use them deliberately for the roadmap rather than letting them expire. Do not purchase credits or enable overages automatically.

## Access status

| Provider | Observed access | Next step |
| --- | --- | --- |
| Meshy | Working: .envrc loads vault entry meshy-key. Balance 1870 after one rifle preview (20 credits) and refinement (10). Outputs downloaded; see [rifle record](PULSE-RIFLE.md). | Use direnv exec . for commands; submit/resume the rifle preview. |
| Unity AI | CLI 1.0.0 and AI Generators 0.1.0-beta.1 installed. Login, renewable auth broker, subscription/terms and service verified; 884/1000 credits available. | Use the verified default org josh-none (1375988431442). CLI generation is independent of the Editor connection; do not mutate Yendor. |

The Unity automation bridge and Unity's paid AI generators are different capabilities. CLI generators are now available through `unity ai`; they do not require linking Siphon in the Editor. A six-hour access monitor alerts on login failures; see [workflow](../WORKFLOW.md). A working bridge does not establish subscription access. Unity documents project linking, account setup, and its [AI menu](https://docs.unity.com/en-us/engine/6000.5/manual/unity-ai/ai-menu-access); verify packages and support against installed Unity 6000.6.0f1 before installation.

## First assets

1. **Meshy:** original pulse-rifle geometry preview, then inspect/refine/import one accepted variant. Disc launcher, soldier, and structures follow. Use Meshy rigging/animation where it fits, after proving the import path.
2. **Unity AI:** sound variations for pulse fire/impact, then jet/grapple; materials/textures for the accepted art sample; compatible humanoid animation tests. Confirm which generators are supported on this editor/platform and usable through available automation. If a generator needs UI unavailable to the agent, record the exact limitation rather than claiming it ran.
3. **Unity authoring:** animation controllers, VFX, HUD, and UI integrate those assets. A generated model alone is not a finished gun or animated character.

Working style assumption: original functional sci-fi industrial gear, clear silhouettes, consistent materials, restrained team-color accents. Review a small actual-build sample before producing a whole set. Do not recreate proprietary Tribes assets.

## Job and import loop

1. Define the asset's use, scale, silhouette, parts that must move, rig needs, target file formats, topology/texture budgets, and acceptance scene. First rifle: static model, approximately 0.8m long, sensible grip/muzzle, separate gameplay muzzle transform; initial budget 15k triangles and 2K textures, subject to in-engine inspection.
2. Query [Meshy balance](https://docs.meshy.ai/en/api/balance) or Unity Dashboard points/expiry. Record observed balance and expected cost from current provider pricing. Track API and web-plan credit pools separately unless the account confirms sharing.
3. Submit one job and persist its request and returned ID immediately. Resume by ID after interruption; never blindly retry a POST after a timeout. Stage downloads under ignored `artifacts/meshy/`, outside Unity Assets.
4. Inspect geometry first, refine only useful candidates, and download outputs promptly. Log accepted/rejected candidates and reason so the next agent does not regenerate the same failure.
5. Import chosen FBX/textures with Unity-generated `.meta` files. Verify pivot/scale, normals, materials under URP, colliders, rig/animation where applicable, LODs and rendering cost. Put accepted assets under `Assets/Siphon/Art/`; ensure scene builders preserve prefab references. Add LFS before committing large binary batches.
6. Compile and capture the actual game/build. Guns must not change accepted damage/cadence. Record build/source identity, provider/model, prompt/request path, job ID, observed credits used (or unknown), import settings, license/source, output hashes, screenshot, and any remaining issues in an asset record under this directory.

## Meshy helper

Use `direnv exec . python3 tools/meshy.py --help` for exact syntax. It provides balance, submission, status, and staged downloads. Preview/refine uses the current [Text to 3D API](https://docs.meshy.ai/en/api/text-to-3d); [authentication](https://docs.meshy.ai/en/api/authentication) uses a bearer API key from the environment. Dry-run the committed request before submitting. No secret belongs in a request JSON, Git, logs, or a briefing.

```sh
direnv exec . python3 tools/meshy.py balance
direnv exec . python3 tools/meshy.py submit docs/assets/requests/pulse-rifle-preview.json --dry-run
direnv exec . python3 tools/meshy.py submit docs/assets/requests/pulse-rifle-preview.json
direnv exec . python3 tools/meshy.py status TASK_ID
direnv exec . python3 tools/meshy.py download TASK_ID
```

After inspecting the preview, copy the refine example to a local request, replace its deliberately invalid placeholder with the accepted preview ID, and submit it once. Keep `artifacts/meshy/requests/` across sessions. An uncertain submission reserves its fingerprint; reconcile Meshy Request Logs before removing that reservation. Changing request JSON can incur a new paid job. Use `python3 -B -m unittest discover -s tools -p 'test_meshy.py' -v` for reproducible offline verification.

Provider-generated outputs still need game-ready materials, animation, rigging, collisions, LODs, and inspection. Budget credit use around accepted assets and upcoming expiry; do not promise a fixed number of accepted assets per generation.
