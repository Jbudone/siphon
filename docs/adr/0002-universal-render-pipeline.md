# Universal Render Pipeline

Status: accepted.

Use URP 17.6.0, the version bundled with Unity 6000.6.0f1. Unity 6.6 deprecates the Built-In Render Pipeline and recommends URP for new projects. Migrating now avoids expanding a library of incompatible materials and effects.

A Forward renderer with 2× MSAA and a 160-meter main-light shadow distance is assigned to Graphics and all Quality levels. Terrain, structures, bots, and weapon materials use URP/Lit. Build previews use a transparent URP/Unlit material. Combat lines use a small URP shader that preserves LineRenderer and TrailRenderer vertex colors and alpha. Both runtime materials are serialized in Resources so player builds retain their shaders.

Scene generators configure URP and create compatible materials. Existing scene geometry, player movement tuning, combat rules, and bot behavior are preserved. Material originals are copied to Backups/BeforeURP/Materials before conversion. Preview capture uses the URP render-request API.

This is a rendering compatibility change, not an art pass. The prototype remains visually rough. Lighting and performance budgets can be tuned after representative gameplay profiling.

Reference: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/upgrading-from-birp.html
