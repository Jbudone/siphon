# Overnight iteration

Historical work record. Current priorities are in [ROADMAP.md](../ROADMAP.md); current restart state is in [agent/STATE.md](agent/STATE.md). The old overnight authorization below records that session and is not a new instruction to run indefinitely. Final integration status below has not yet been reconciled against today's source/build.

User authorization: continue improving the game throughout the offline night until available tokens are exhausted. Preserve the accepted movement and weapon baseline. Keep each playable milestone tested and maintain a Linux playtest build; do not simulate offline attacks on cores.

## Completed in source

Siege batteries and captain target designation:

- Middle mouse commits the combat squad to a copied target position for 45 seconds. Workers/scouts keep support jobs. Friendly rally marks hold artillery fire; marked shielded enemy cores prioritize their power.
- Siege calibration requires survey optics and field engineering. Batteries cost ferrite, timber and alloy, require power, and consume 2 ferrite + 1 alloy per shell.
- Ballistic shells reach 35–230m, telegraph a two-second firing setup and reload for six seconds. They have a structure damage bonus rather than changing the player's weapons. Power cuts and console hacks interrupt charging.
- Version 3 checkpoints retain research and captain designations and read the previous versions.
- Field, encounter and siege validators passed. The first siege artifact passed a 25-second graphical smoke run with 19 bots, five deliveries and 146 successful paths. Its research and battery screenshots were inspected.

Respawn network:

- N opens a network screen, including while down. The player can select automatic, home or a powered friendly outpost. Bots keep independent spawn priorities.
- Outpost exits are checked on terrain for body clearance, with alternate exits and another-base/home fallback if obstructed or unpowered.
- Spawn preferences persist. A live validator passed selection, power/obstruction fallback, independent bot priorities, save restoration and timed player respawning.

## In progress

Run the combined field/encounter/siege/spawn checks as appropriate, package the current respawn/curved-trajectory/HUD revisions, inspect graphics, then run extended stability validation. Update this file and the playtest guide with actual results before the next feature pass.

## Next useful work

Deepen PvE personality and existing biome progression without expanding beyond the current map. In particular, forest creatures should investigate audible activity and ambush rather than merely walk directly at a visible target. Validate movement impulses and preserve low combat time to kill. The 32-human authoritative networking milestone remains separate; local simulations do not establish network capacity.

## Operations

Unity 6000.6.0f1: `/home/jbud/Unity/Hub/Editor/6000.6.0f1/Editor/Unity`. Only one editor process may hold this project. Validators exit on their own and must be called without `-quit`; builds use `-quit`.

The build is `Builds/Siphon/Siphon.x86_64`. `PrototypeArtifacts.BuildLinuxCandidate` stages to `Builds/SiphonCandidate` when a prior game is running. Do not overwrite a running build. Smoke flags skip user saves; extended runs raise only core health to keep squads active. All verification processes should exit or be interrupted before replacing their artifact.
