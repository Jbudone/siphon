# Human feedback and essential actions

Updated: 2026-11-15.

## Playtesting needed now

**None.** This update changes workflow and tooling. Existing movement and weapon balance are already accepted. Do not repeat the broad field-test checklist just for this update.

## Access needed from you

One reply can unblock generation:

1. Provide the location of an existing Meshy API credential, or make `MESHY_API_KEY` available to the agent environment. Do not paste the secret into chat.
2. Name the Unity organization and Siphon Cloud project to use, or confirm a new Siphon project under that organization. Unity CLI reports expired login; reauthentication may require your browser. The connected editor currently points to Yendor.

Agents should first use available credential paths and account tools; only request the unavoidable account action. This is setup, not a game test.

## Rules for adding a playtest request

Maximum three open requests; prefer zero or one. Only request feedback that blocks a concrete product decision and cannot be resolved with automation or agent inspection. Combine related checks, aim for five minutes total, and remove answered or obsolete requests. Do not ask for generic feedback, screenshots the agent can capture, or confirmation that automated checks passed.

Each request must provide:

- **Decision:** what the answer will change, and why human judgment is essential.
- **Build:** exact path/version/source revision, already checked by the agent.
- **Time:** honest estimate.
- **Steps:** numbered inputs, starting state, location/landmark, and stopping point.
- **Reply:** one specific comparison or short answer, with a suggested format.

Future candidates, not requests yet: one side-by-side in-game art sample; one measured large-map route. Add them only when a verified build and exact reproduction steps exist.
