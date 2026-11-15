# Siphon

A persistent team shooter in which exploration and industry support a war to destroy the opposing team's core. There is no flag objective.

## Experience

Combat follows the player's Quake 3 Clan Arena and Starsiege Tribes preferences: fast movement, low time to kill, aerial fighting, skiing, grappling, and momentum around obstacles. Heavy equipment supports slower siege play. Progression unlocks capabilities rather than large health and damage multipliers.

The world contains dense frontlines, contested borderlands, and lonely wilderness with PvE and boss camps. Large biomes have increasing danger with depth. Cross-biome resource dependencies unlock environmental protection and further exploration. Gathering, crafting, automation, and unique boss drops fund defenses and assaults.

Players build defenses and forward respawn outposts. Defenses should have directional coverage, power and sensor dependencies, and exploitable weaknesses. Detection from sound, visibility, radar, and drones creates risk around valuable expeditions and remote operations. AI must act on information it has obtained rather than omniscient player positions.

## Bots

An AI captain assigns priorities and resource budgets. Squads execute objectives; individuals move, work, and fight. Roles include builders/repairers, gatherers, scouts who establish and maintain outposts, heavy siege crews, and guards. Roles and equipment classes are separate concepts. Player requests should influence assignments.

## Scale and persistence

Initial target: up to 32 human players in one war, on one authoritative dedicated server. Bot population is a separate performance budget, still to be measured. No regional server handoff in the initial architecture.

The war continues without online players. Active areas use full simulation; distant areas use coarse simulation; empty-server time can be advanced from persisted timestamps. Coarse outcomes must respect inputs, equipment, travel, and production constraints. Preserve an event history so returning players can understand changes.

Offline core destruction is an unresolved design question. It was proposed, not explicitly confirmed. Total eventual population, match duration, world reset behavior, and exact bot counts remain undecided.

## Current milestone

The workspace has a quiet movement lab and a local 600-meter assault graybox. The first player test accepted movement and weapon balance as a baseline. The following iteration preserves those values while adding a typed economy (ferrite/timber/crystal/alloy), harvestable trees, powered smelting, five equipment unlocks, cold exposure, proximity traps, walls, radar, overloadable force fields and temporary console hacking. There are fourteen team bots, five creatures, and a telegraphed Warden encounter with an extractable jet capacitor.

Bots use a baked terrain grid with A* routes plus dynamic structure blocking, local steering and separation. Workers choose needed supplies, return cargo, repair structures and reconstruct destroyed fixed defenses. Scouts maintain outposts, restore lost power and investigate noise. Raiders flank, strafe and jet during combat; heavies keep siege distance and workers retreat with supplies. Guards respond to threatened outposts. Boss armor and post-volley reactor vents reward fast flanking. Crafted visual cloaking has close-range, radar and noise counterplay. The captain remains rule-based and responds to the player's assault/defend/escort/balanced priorities. Sensor contacts are expiring position snapshots, not references to continuously updated enemy transforms.

Versioned local saves preserve bases, supplies, nodes, personal equipment and research, captain target designations, preferred respawn stations, bot state/cargo, boss recovery timers and unclaimed loot; atomic backups permit recovery from a damaged checkpoint. Empty-session advancement currently handles only powered production and depleted-resource regrowth, respecting stock reserves. Bots resume their positions and replan; death and loot-expiry timers advance, and projectiles restart. It does not simulate offline raids, mining labor or core destruction. This is a persistence prototype, not the authoritative server world. The capacitor is the first unique boss module; the full technology graph is not implemented.

Middle mouse marks expiring assault targets that commit combat roles and guide powered siege batteries. Siege calibration depends on survey optics and field engineering; charged arcing shells consume ferrite and alloy and favor structures. Friendly rally marks hold artillery fire. The player chooses future respawns through N, independently of bot priorities; powered outposts need a clear exit and have safe fallbacks.

The next architectural milestone remains authoritative networking and responsiveness for the 32-human target. Local systems must not be mistaken for server authority, anti-cheat, prediction, replication, or a measured player capacity. On 2026-11-15 the user moved a Tribes-scale map, actual guns/models/animations, sounds, VFX, UI/HUD, and defensive patrols into immediate follow-up, alongside establishing the development workflow and using existing Unity AI and Meshy credits. This revises the earlier expansion-after-networking sequence. A provisional 2 km × 2 km map is an agent starting assumption, not an accepted final dimension. See ROADMAP.md for current priorities and docs/WORKFLOW.md for agent memory, orchestration, release briefings, and telemetry.

## Rendering

URP 17.6.0 is the chosen pipeline for Unity 6000.6.0f1. See docs/adr/0002-universal-render-pipeline.md. The original Built-In rendering setup has been migrated; movement and combat remain the established first-pass baseline. The player reported both movement feel and combat balance acceptable for the first pass.
