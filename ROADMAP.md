# Siphon roadmap

Updated: 2026-11-15. This is the authoritative priority list. No delivery dates are committed.

[Latest briefing](docs/briefings/2026-11-15-review.md) · [Human feedback](docs/HUMAN-FEEDBACK.md) · [Agent handoff](docs/agent/STATE.md) · [Workflow](docs/WORKFLOW.md)

## Immediate follow-up

Work in small playable slices. Finish and verify one slice before assigning the next worker. Independent work can proceed when credentials block asset generation.

| ID | Priority | Status | Deliverable and completion evidence |
| --- | --- | --- | --- |
| W01 | First | Done | Git, compact agent notes, model routing, briefings, CodeBurn and advisory PR-Agent configuration. Keep the process lightweight; manual task metrics are optional. Seven offline Meshy checks pass; workflow commands, project-scoped export, TOML and documentation links verified. See [briefing](docs/briefings/2026-11-15.md). |
| W02 | Next | Local setup verified; hosted activation blocked | Verify PR-Agent configuration and activate on a selected GitHub repository; separate fresh reviewer from orchestrator/implementer. Assess mem0; recommendation is to retain Git-backed Markdown for now. See [audit](docs/agent/MEMORY-AUDIT.md). |
| A01 | After W02 | Access ready | Use existing Meshy and Unity AI credits. Generate and import one original pulse-rifle asset with materials; capture it in a playable build and record job IDs, credits, and provenance. See [asset pipeline](docs/assets/PIPELINE.md). |
| M01 | Immediate | Planned | A large outdoor map at Starsiege Tribes scale: broad skiing terrain, long sightlines, separated bases, and useful forward outposts. Start with a **provisional 2 km × 2 km playable area**, roughly 1 km between bases. Measure route times and performance before settling dimensions. Keep the movement lab. |
| G01 | Immediate | Planned | Actual first-person and world gun models for pulse rifle and disc launcher, with hold/fire/equip animation and muzzle alignment. Preserve accepted damage, cadence, and movement. Ship one complete gun first. |
| C01 | Immediate | Planned | Actual character and structure models, materials, and animation. First slice: one team-readable soldier with idle, run, jump/jet, fire, and death states. Then replace remaining placeholders in small sets. |
| S01 | Immediate | Planned | Authored/generated spatial weapon, impact, jet, grapple, movement, and alert sounds, using Unity AI where supported. First slice: pulse rifle and its impacts; compare in game at normal volume. |
| V01 | Immediate | Planned | Readable muzzle, projectile, impact, explosion, jet, and shield VFX. First slice: pulse rifle fire/hit effects, verified against both terrain and team silhouettes. |
| U01 | Immediate | Planned | Intentional UI for construction, equipment, research, respawns, and pause/settings. First slice: one complete construction flow with clear cost, placement, and cancel feedback. |
| H01 | Immediate | Planned | A coherent HUD: aim/hits, health, jet fuel, cargo, objectives, and warnings readable while moving. First slice: combat HUD with consistent hierarchy and scaling. |
| B01 | Immediate | Planned | Defensive bots patrol base/outpost routes, investigate acquired contacts, intercept threats, then return to patrol. Validate route coverage, leash, recovery from blocked paths, and absence of omniscient tracking. Existing guards react to threats; deliberate patrol behavior needs its own slice. |
| Q01 | Ongoing | Pending verification | Reconcile the latest source and Linux artifact, run relevant validators and a representative soak, then publish a briefing. Existing overnight notes leave the final siege/respawn/HUD integration checks open. Human review only for specific decisions automation cannot settle. |

Map and presentation work are now immediate priorities at the user's request. This updates the earlier sequencing that put broader map work after networking. Sequence within this group can change when dependencies or access require it; record the reason here.

## Next architectural milestone

**N01 — Authoritative multiplayer:** one dedicated server, up to 32 humans, separately measured bot budget. Establish authority, replication, movement responsiveness, persistence boundaries, and representative assault performance. Select networking/persistence technology through an ADR. A larger local map does not establish multiplayer capacity.

## Later

Deeper biomes and progression; full technology graph; more boss equipment; drones and industrial logistics; stronger captain/squad coordination; coarse distant-agent simulation and a production persistent war with a useful event history.

## Open decisions

- Final map dimensions and travel times: provisional until measured and playtested.
- Visual direction: begin with readable original sci-fi industrial equipment; review a small in-game asset sample before mass generation.
- Offline core destruction, war duration/reset rules, eventual population, and bot counts remain undecided.
- Production offline battles are not implemented or newly authorized by this roadmap.

## Already playable

The local 600m graybox includes accepted movement/weapons, gathering and crafting, construction/power/defenses, role-based bots, a Warden and capacitor reward, siege, respawn selection, and local saves with bounded offline production. See [README](README.md) for controls and [iteration notes](docs/ITERATION-NOTES.md) for historical checks. Do not read these as verification of today's source or binary.

## Updating this file

The orchestrator updates status after every accepted slice and commits it with the work. Use Planned / In progress / Blocked with reason / Done with evidence. Link the latest briefing above. Keep detailed task instructions outside this page; preserve completed history in Git and briefings.
