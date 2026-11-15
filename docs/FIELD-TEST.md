# Updated field test

Launch `Builds/Siphon/Siphon.x86_64`, or open `Assets/Siphon/Scenes/AssaultPlayground.unity` in Unity 6000.6.0f1. Movement and weapon damage/cooldowns retain the accepted first-pass baseline. URP 17.6.0 replaces the deprecated Built-In pipeline. The quiet movement lab remains available separately.

Suggested run:

1. Ski, jet and grapple out of Blue's base. Fight a Red squad with pulse and disc; check directional hit feedback, projectile visibility and nearby weapon sounds.
2. Press Tab to view captain assignments and the contact map. J cycles orders. Enemy pips fade from their last known position. Middle mouse marks a point for a 45-second assault; combat bots commit while support roles keep working. Watch workers deliver supplies, scouts build and restore powered forward bases, raiders flank and jet, and heavies hold siege distance.
3. Mine golden ferrite and harvest a forest tree with F. Deposit at your core or a friendly outpost. V lists construction costs and functions; G/H/B remain quick shortcuts.
4. Place a wall, generator and turret. Q rotates the preview. Check supported-ground placement, turret coverage and generator range. Try a trap on an approach route and build a powered outpost for forward respawns.
5. Visit the forest relay ruins west of center for guarded crystals. Return and use T at a friendly workbench to craft insulation. Equipment costs shared stock, so deposit cargo first.
6. Explore the eastern icefield. Cold exposure accumulates without insulation or friendly shelter. The far vault's Warden marks ground barrages and airbursts before firing. Flank its armored front or strike the glowing reactor after a volley. Recover its capacitor cache with F and deposit it at a friendly core/outpost to equip 25% more jet fuel. Dying before deposit drops the module.
7. Check the main-base smelter or build another. It consumes ferrite and timber to produce alloy while reserving basic supplies. Use the alloy and crystals to unlock radar or force-field construction.
8. Craft cloak weave with T and toggle it with C. It hides you beyond twelve meters for up to eight seconds. Radar still detects you, jets remain audible, and firing, work or damage breaks concealment. Test a field with hostile fire: overload it, flank the side console, or cross the barrier physically. Hold F on an enemy console for three seconds to hack it. Hacking a generator also exposes a nearby core temporarily.
9. Press N to choose home or a particular powered outpost for future respawns. This screen also works while down. Power failures or blocked exits use a safe fallback; bots keep independent priorities.
10. Research siege calibration after survey optics and field engineering. Build a powered siege battery via V and mark an enemy installation with middle mouse. Watch the charging arc and alloy ammunition cost; disable its generator to cancel charging. Friendly marks hold fire.
11. Save with F5, then use F9 to resume. Bases, resource stocks, equipment, node depletion, bot positions/cargo, boss recovery timers and dropped loot persist. Bots replan after loading. Closing the standalone also saves. Offline progression covers production, depleted-node regrowth and respawn/loot-expiry timers, with no offline combat. Damaged checkpoints can recover the previous atomic backup.
12. Break Red's power and destroy its core. Workers can rebuild fixed defenses when supplies permit. Enter after victory begins a fresh war.

Most useful feedback: whether combat stays readable at speed; where bots get stuck or behave strangely; whether defenses produce interesting choices; whether the gathering, equipment and exploration loop gives you a reason to leave the front; and whether building/crafting controls explain themselves.

This remains a local graybox, with fourteen team bots, five creatures and one Warden. The full tech tree, broader boss equipment, large biome depths, networked humans and production offline warfare are still future work.

## Verification

The engine movement validator exercises CharacterController ground contact, jumping, jet fuel/recharge, ski momentum, walking friction, grappling, wall collision and targets. The field validator runs a 45-second economy check, including actual worker deliveries, scout-built outposts and live alloy production. It exercises atomic disk-save creation/replacement, resource costs, equipment crafting, routes between both bases and into both biomes, hostile/friendly force-field casts, overload, disabled-generator power, expiring sensor snapshots, save round trips, bounded offline production, shields, repairs, depletion, respawns, bot travel and victory.

The encounter validator also checks stealth against radar and damage collision, boss armor, carried/banked capacitor state, bot/loot persistence, airborne AI hits, worker retreat and scout power restoration.

The standalone has an explicit verification mode:

```sh
Builds/Siphon/Siphon.x86_64 -batchmode -force-glcore -siphon-smoke -logFile /tmp/siphon-smoke.log
```

Add `-siphon-soak 600` for a ten-minute AI run. The extended test raises only core health to keep squads active; normal playtest tuning is unchanged. It reports worker deliveries, path searches/failures and world-bound checks. This is a local stability check, not proof of 32-player performance or long-match balance. Verification runs skip world saves.

Recorded results and limitations are in [the iteration notes](ITERATION-NOTES.md). Final captures include [construction](images/runtime-hud-build.png), [equipment](images/runtime-hud-gear.png), [the tactical map](images/runtime-hud-tactical.png), [the Warden](images/runtime-hud-warden.png), and [its capacitor cache](images/runtime-hud-salvage.png). Encounter captures use the explicit smoke-only `-siphon-capture-encounters` flag; they do not change normal playtest state.
