# Siphon development updates

Screenshots, showcases and concise briefings live here in the repository so they can be viewed directly on GitHub. Images below come from actual game builds; plans are labeled separately.

[Roadmap](ROADMAP.md) · [Latest briefing](docs/briefings/LATEST.md) · [Development workflow](docs/WORKFLOW.md)

## Latest development — isolated map candidate

The terrain experiment now has separate scene, mesh/navigation, Linux build and save destinations. It remains a 600m clone; generation/persistence/reload checks and an actual-player smoke passed. [Briefing and screenshot](docs/briefings/2026-11-15-candidate.md) · [Build provenance](docs/maps/M01-CANDIDATE-BUILD.json)

## Latest playable showcase — first generated pulse rifle

![Pulse rifle in the actual Linux candidate build](docs/images/pulse-rifle.png)

The first generated rifle is in game: a 0.8m prefab with 7553 triangles and 2K URP materials. It survives scene rebuilds and preserves accepted movement and weapon balance. The style is provisional; hand/equip animation and a world model are still to come.

Source: `1cc509d` · Linux candidate: `Builds/SiphonCandidate/Siphon.x86_64` (local build; no downloadable release published). Candidate build, 25-second smoke, weapon switching/firing, focused Stalker validation and separate review passed. [Full briefing](docs/briefings/2026-11-15-rifle.md) · [Generation provenance and checks](docs/assets/PULSE-RIFLE.md)

<details>
<summary>See firing and weapon switching</summary>

![Pulse rifle firing in the actual build](docs/images/pulse-rifle-fire.png)

Rifle firing and recoil remain connected to the existing combat system.

![Disc selected in the actual build](docs/images/pulse-rifle-disc.png)

Switching to the disc keeps its current placeholder while the new rifle is hidden.

</details>

## Briefing archive

| Update | What changed | Evidence |
| --- | --- | --- |
| [2026-11-15 — Isolated map candidate](docs/briefings/2026-11-15-candidate.md) | Separate experiment assets/saves, legacy compatibility and editor reload | Targeted checks, default field validation, Linux build and rendered smoke passed |
| [2026-11-15 — Paired review-tool trial](docs/reviews/README.md) | PR-Agent and Alibaba OpenCodeReview; separate outputs for later audit | Hosted trial evidence and coverage limits; no gameplay change |
| [2026-11-15 — Map layout foundation](docs/briefings/2026-11-15-layout.md) | Shared current layout and GitHub update pages; larger map still planned | Generation/runtime/field checks and new candidate smoke passed |
| [2026-11-15 — First generated rifle](docs/briefings/2026-11-15-rifle.md) | First-person model, URP materials, persistent prefab; Stalker initialization and validator fixes | Actual-build screenshots and passing checks |
| [2026-11-15 — Independent review and memory audit](docs/briefings/2026-11-15-review.md) | Separate reviewer role, PR-Agent fixes; mem0 deferred | Configuration checks and independent review; no game build |
| [2026-11-15 — Roadmap and workflow](docs/briefings/2026-11-15.md) | Priorities, compact handoff and asset pipeline | Setup evidence; no new gameplay build |

## Next

Expand the isolated experiment toward the provisional 2km × 2km map. Plans and calculations are not screenshots or performance measurements of a larger playable build. See the roadmap for current status.
