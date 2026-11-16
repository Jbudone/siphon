# Siphon

[Updates & showcase](UPDATES.md) · [Roadmap](ROADMAP.md) · [Latest briefing](docs/briefings/LATEST.md) · [Essential feedback](docs/HUMAN-FEEDBACK.md) · [Development workflow](docs/WORKFLOW.md)

A local prototype of persistent team warfare with fast skiing, grappling, and aerial combat. Includes local saves and bounded offline production; multiplayer networking remains future work.

## Play the assault

A Linux development build is available at `Builds/Siphon/Siphon.x86_64`. Run it directly, or use the Unity editor below. A suggested first run is in [FIELD-TEST.md](docs/FIELD-TEST.md).

Open this folder through Unity Hub with **Unity 6000.6.0f1**. Open `Assets/Siphon/Scenes/AssaultPlayground.unity` and press Play. You join Blue. Destroy the Red generator to switch off its turrets and expose its core, then destroy the core to win. Your bots also fight toward that objective. Press Enter after a victory to restart.

Click the Game view to capture the mouse; Escape releases it. F1 toggles help, and Tab shows the Blue captain's current bot assignments. J cycles between assault, defense, escort, and autonomous balanced orders; workers and scouts keep their support jobs. The initial help panel disappears after nine seconds. The tactical overview also shows a contact map.

| Control | Action |
| --- | --- |
| WASD / mouse | Move / aim |
| Left Shift (hold) | Ski; preserve momentum and accelerate downhill |
| Space | Jump, with a short grace period after leaving a ledge |
| Right mouse (hold) | Jet thrust; fuel recharges when released |
| E (hold) | Grapple the aimed surface; release to detach |
| Left mouse | Fire |
| 1 / 2 | Pulse rifle / explosive disc |
| F (hold) | Mine a nearby deposit, deposit cargo at a friendly core/outpost, repair a friendly structure, recover dropped cargo, or hack an enemy console |
| G / H / B | Preview a turret, generator, or outpost; costs include ferrite and timber |
| Left mouse / Q / right mouse during placement | Confirm / rotate 45 degrees / cancel |
| V / T | Construction menu / equipment crafting |
| C | Toggle crafted cloak weave |
| Middle mouse | Mark a squad assault and siege target for 45 seconds |
| N | Select future respawn base, including while down |
| F5 / F9 | Save local checkpoint / reload checkpoint |
| R | Debug return to starting position |
| F1 / Tab / J | Controls / tactical overview / cycle captain orders |

You start with 100 **shared team ferrite** and 50 timber. Workers gather supplies, deliver them, repair structures, and rebuild destroyed fixed defenses when affordable. Scouts establish and maintain forward spawns. Golden deposits provide ferrite, forest trees provide timber, and purple deposits provide crystals. Your cargo holds 60 resources; deposit it at a friendly core or outpost before spending it. Death drops half your carried resources in a recoverable cache. Harvesting, gunfire, jets, construction and smelting create noise that scouts investigate. Workers retreat with cargo when threatened. Scouts replace lost outpost power; guards respond to threatened forward spawns, raiders use opposing flanks, and heavies hold siege distance. Mobile assault bots strafe, lead shots and use short jet bursts.

Generators power structures within 35 meters and shield main cores within 40 meters. Turrets cover a 150-degree arc and fire dodgeable projectiles. Walls provide physical cover; traps burst on nearby hostiles and rearm after eight seconds. Force fields block hostile shots, allow players and friendly fire through, and overload under concentrated fire. Their side control unit is vulnerable. Hold F on an enemy generator, turret, radar, trap, or field console for three seconds to disable it for 25 seconds. Destroying or hacking power also drops the core shield.

A powered smelter converts four ferrite and two timber into one alloy every twelve seconds, leaving a reserve for basic defenses. Both main bases start with one. Build more through V. At a friendly core, outpost, or smelter, T crafts insulation, survey optics, field engineering, or cloak weave. Insulation protects against eastern frost exposure; optics unlock radar and resource map markers; field engineering unlocks force-field construction. Equipment persists through respawns. Cloak weave enables eight seconds of visual concealment beyond twelve meters; C toggles it. Radar still detects you, and firing, working, building or taking damage exposes you for three seconds. Jets remain audible. Siege calibration is a research branch requiring both survey optics and field engineering. It unlocks a siege battery through V. These are experimental unlocks, not the planned full tech tree.

Radar samples nearby hostiles every three seconds. The tactical map displays last-seen positions with a short expiry, never continuous hidden tracking. Visual sightings also contribute contacts. Main cores and two exploration landmarks are known locations. The forest relay ruins offer a smaller guarded crystal supply; the eastern vault has a Warden whose ground barrages and airbursts mark their target before firing. Its frontal armor reduces incoming damage; flank it or attack during the reactor vent after a volley. Defeating it drops crystals, alloy and a capacitor. Recover the cache and deposit at a friendly core or outpost to equip 25% more jet fuel. Death drops an unbanked capacitor. The Warden returns after three minutes.

A siege battery requires power and fires arcing shells at middle-mouse target marks within 35–230m. It charges for two seconds, reloads for six, and consumes two ferrite and one alloy per shot. Its shell has a structure damage bonus; players can dodge its blast and exploit its exposed generator or hack the gun. The charge shows its projected arc and makes considerable noise. Friendly rally marks hold artillery fire. Marked shielded cores redirect attacks to their power.

An outpost provides forward respawns while powered and alive. N opens the respawn network to choose automatic, home or a particular friendly outpost. Blocked exits and lost power fall back to another available base or home; bots keep their own forward-base priorities. Placement requires a clear, supported footprint on relatively flat terrain. Repairs consume one team ferrite per twelve health. Construction previews show the actual structure and either power range or turret direction.

Standalone play autosaves every thirty seconds and on normal exit; F5 saves immediately and F9 resumes. The versioned local snapshot preserves team stocks, built and fixed structures, health, resource depletion, personal cargo, position, equipment, research, captain target marks and spawn preferences, bot positions/cargo/health, boss recovery timers and unclaimed loot. Offline smelters consume their actual inputs while respecting supply reserves; depleted resources regrow. Offline combat and core destruction are not simulated. Bot and boss respawn timers advance, and dropped loot expires. Living bots resume from saved positions and replan; transient projectiles restart. Each atomic checkpoint retains the previous save as a backup and can recover it if the latest file is damaged. Saves live under Unity's application persistent-data directory as `field-war-v1.json`; smoke tests skip saves. Editor play loads only when explicitly requested with F9. Enter after victory starts a fresh war.

The pulse rifle takes three hits to kill a standard 80-health bot; the disc has splash damage and movement knockback. Friendly damage is disabled. Heavies have 130 health and slower siege weapons. Player death gives a three-second respawn; team bots respawn after twelve seconds. All numbers are prototype tuning values.

## The movement lab

`Assets/Siphon/Scenes/MovementPlayground.unity` retains the small, quiet slope-and-grapple course with moving shooting targets. Use it to judge movement without bot combat.

Change `Assets/Siphon/Settings/Movement.asset` to tune acceleration, friction, gravity, jump, jets, grapple, speed cap, and mouse sensitivity. Movement runs in small collision sweeps. The command interface also permits automated engine checks, but does not implement network prediction or guarantee deterministic network simulation.

## Rebuild and check

`Siphon > Rebuild prototypes` recreates both scenes, meshes, and materials while preserving movement tuning. It replaces the open scene; save custom scene work first. Both scenes use Universal Render Pipeline (URP 17.6.0) and the built-in Input API.

```sh
UNITY=/home/jbud/Unity/Hub/Editor/6000.6.0f1/Editor/Unity
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.PlaygroundBuilder.Build -quit -logFile /tmp/siphon-build.log
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.PlaygroundValidation.Run -logFile /tmp/siphon-movement-validation.log
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.FieldValidation.Run -logFile /tmp/siphon-field-validation.log
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.EncounterValidation.Run -logFile /tmp/siphon-encounter-validation.log
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.SiegeValidation.Run -logFile /tmp/siphon-siege-validation.log
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.SpawnValidation.Run -logFile /tmp/siphon-spawn-validation.log
```

The validators exit Unity themselves: omit `-quit`. Run these commands sequentially because Unity locks an open project. Movement checks cover ground contact, jumping, jet launch/fuel/recharge, skiing momentum, wall collision, grappling, and target hits. Field checks cover typed and atomic resource transactions, crafting, navigation across both ridges, field interception/overload, hacking, contact snapshots, save restoration/offline production, power/shields, repairs, friendly fire, mining, respawning, bot travel, and victory. Encounter checks cover cloak/radar/collision, signature breaks, boss armor, agent/loot/death-timer persistence, carrying and banking the capacitor, airborne bot hits, worker retreat and scout power restoration.

`Siphon > Build Linux prototype` generates `Builds/Siphon/Siphon.x86_64`. Batch equivalent:

```sh
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.PrototypeArtifacts.BuildLinux -quit -logFile /tmp/siphon-linux-build.log
```

## Isolated map candidate

The opt-in candidate is a separate **600m clone**, ready for a later terrain experiment. It uses `AssaultCandidate600.unity`, `Candidate600.asset`, `Candidate600Navigation.asset`, and save identity `assault-candidate600-v1`. It does not expand the playable area. Default scene, generated assets, build paths and the movement lab retain their existing destinations.

```sh
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.CandidateIsolationValidation.Run -logFile /tmp/siphon-candidate-isolation.log
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.PrototypeArtifacts.BuildLinuxMapCandidate -quit -logFile /tmp/siphon-map-candidate-build.log
```

The first command regenerates only the candidate and exits after source-byte, reference and persistence checks; omit `-quit`. To regenerate without validation, use `AssaultSceneBuilder.BuildCandidate` with `-quit`, or **Siphon > Generate isolated 600m candidate**. The build opens the candidate first and retains the lab as its second scene, at `Builds/SiphonMapCandidate600/Siphon.x86_64`.

Checkpoint schema v4 records layout identity. Legacy saves without identity remain readable on the default map. Candidate saves use `field-war-candidate600-v1.json` alongside the unchanged default `field-war-v1.json` in Unity's persistent data directory; cross-layout restores are rejected. Changed candidate geometry must receive a new identity and save destination.

## Scope and next steps

This is a local field test with fourteen team bots, five creatures, and one Warden. Bots follow baked terrain paths and update navigation around built or destroyed structures, with local steering and separation. The captain still uses a small rule-based set of priorities; this is not the final squad AI or a large-language-model runtime. Main enemy bases are known strategic objectives.

The 600-meter graybox has forests, icefields, supply routes, ruins and a guarded vault. The new economy, survival, automation and persistence systems are small playable slices. Large biome depths, a full tech tree, more boss equipment, drones, industrial logistics, authoritative networking, distant-agent simulation and a production persistent world remain future work. Human playtesting is needed for feel, readability and balance.

The design and open questions live in [CONTEXT.md](CONTEXT.md). Initial server scope is recorded in [the architecture decision](docs/adr/0001-initial-server-scope.md). A Unity MCP bridge is not installed or connected; the installed editor provides direct automation.
