# Human feedback and essential actions

Updated: 2026-11-15.

## Playtesting needed now

**None.** This update changes workflow and tooling. Existing movement and weapon balance are already accepted. Do not repeat the broad field-test checklist just for this update.

## Access needed from you

**Unity sign-in only** (about one minute):

1. Complete the Unity browser login already started, or run `unity auth login` if its window is no longer open.
2. Tell the agent when complete; it will verify Cloud and AI access and clear the alert.

Meshy credentials are connected through .envrc; no key needs to be supplied. Automatic Unity access checks and visible alerts are installed. The existing default Unity organization will be checked after login.

## Rules for adding a playtest request

Maximum three open requests; prefer zero or one. Only request feedback that blocks a concrete product decision and cannot be resolved with automation or agent inspection. Combine related checks, aim for five minutes total, and remove answered or obsolete requests. Do not ask for generic feedback, screenshots the agent can capture, or confirmation that automated checks passed.

Each request must provide:

- **Decision:** what the answer will change, and why human judgment is essential.
- **Build:** exact path/version/source revision, already checked by the agent.
- **Time:** honest estimate.
- **Steps:** numbered inputs, starting state, location/landmark, and stopping point.
- **Reply:** one specific comparison or short answer, with a suggested format.

Future candidates, not requests yet: one side-by-side in-game art sample; one measured large-map route. Add them only when a verified build and exact reproduction steps exist.
