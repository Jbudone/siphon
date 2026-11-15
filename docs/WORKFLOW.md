# Development workflow

## Chosen approach

Use **Git + Markdown**, with local agent notes. No Beads or mem0 initially. Our current need is a small, inspectable record of priorities, decisions, and the next action. Reconsider retrieval-backed memory only if targeted file search becomes a measured bottleneck.

| Record | Owner / purpose |
| --- | --- |
| [Roadmap](../ROADMAP.md) | Orchestrator: ordered outcomes and honest status. |
| [Agent state](agent/STATE.md) | Orchestrator: compact restart packet, ideally under 100 lines. |
| [Current task](agent/CURRENT-TASK.md) | One worker: scope, acceptance, evidence, next action. |
| [Domain context](../CONTEXT.md), [ADRs](adr/) | Stable vocabulary and consequential accepted decisions. |
| [Briefings](briefings/LATEST.md) | User: concise updates with build identity and screenshots. |
| [Human feedback](HUMAN-FEEDBACK.md) | Only questions that require human judgment. |
| `telemetry/` | Local development measurements; ignored raw exports. |

Facts have one owner. Link to them rather than copying large explanations across memory files. Date observations and distinguish user requirements from agent assumptions.

## Orchestrator and workers

1. Read STATE, current task, and Git status. Resolve unfinished work before choosing more.
2. Cut one vertical slice: a small visible behavior, asset import, or infrastructure outcome that can be verified independently. Split again if it has several unrelated acceptance criteria or cannot be handed off briefly.
3. Supply a fresh worker only the [task packet](agent/TASK-TEMPLATE.md), relevant paths, design constraints, and tool access. Give exclusive ownership of named files. One worker at a time; root remains responsible for integration.
4. Choose light / standard / heavy based on ambiguity and failure cost. The available profiles use Luna / Sol / Astra respectively. A simple bounded edit can stay with root. Record selected model/effort, not just the intended tier.
5. Worker returns changes, exact verification, uncertainty, and next action in a short report. Root reviews the diff and any unverified assumptions, and runs integration checks when needed.
6. Update task, STATE, roadmap, and metrics; commit the slice. A playable milestone gets a briefing with a build tied to its source revision.
7. At a slice boundary, start a fresh orchestrator session from the restart packet when context is cluttered. The helper prints that packet; it does not automatically reset or schedule the application.

```sh
python3 tools/workflow.py status
python3 tools/workflow.py handoff
```

The second command prints a prompt to paste into a fresh session. The receiving agent reads referenced files from disk. Avoid arbitrary resets every N turns: they discard useful context and can reduce cache reuse. OpenAI documents [custom agents and model selection](https://learn.chatgpt.com/docs/agent-configuration/subagents).

## Release briefing

Write one after a verified playable milestone; otherwise at workday end only when there are meaningful changes. Target 150–250 words plus up to three useful images. Include new features, known issues, build/source identity, important notes, uncertain agent decisions, new asset provenance, and the one next action. Use [the template](briefings/TEMPLATE.md). No new screenshots for a docs-only update; never relabel old screenshots as new.

## Development telemetry

CodeBurn is installed. The project-scoped export works. Its first snapshot during this setup showed 1 session, 15 API calls, and $0.30 in calculated usage; the active session was still running and the scanner can lag. These are model-rate calculations, **not an invoice or a completed-task baseline**. No savings claim is justified yet. Raw exports may contain prompts, paths, and commands; keep them local.

```sh
python3 tools/workflow.py snapshot
python3 tools/workflow.py record W01 --model gpt-6.1-sol --effort medium --minutes 20 --outcome accepted --rework 0
codeburn optimize --period week --project "$PWD"
codeburn web
```

The record command above is an example: enter observed duration and actual outcome. Capture a snapshot at completed milestones, then compare at least a few comparable slices. Track elapsed time, accepted/reworked/blocked outcome, actual model/effort, retries, relevant checks, token usage/cache reuse from CodeBurn, and generation credit usage from providers. Compare **cost and time per accepted slice**, not tokens alone. Do not auto-apply CodeBurn recommendations.

Following [Langfuse's optimization guidance](https://langfuse.com/academy/experiments/cost-optimization), establish output quality before claiming savings. We do not need to deploy Langfuse for this first local workflow.

| Opportunity | Explanation and why it works | Estimated savings at analyzed volume | Implementation effort | Risk to output quality |
| --- | --- | --- | --- | --- |
| Focused memory and fresh worker packets | Avoid repeated broad discovery and inherited logs. | Unknown; no completed-slice comparison yet. | Low | Omitting a dependency; include constraints and evidence links. |
| Route bounded work to a smaller model | Reduce cost on clear tasks; escalate difficult work. | Unknown at the observed 15-call partial baseline. | Low | Rework may outweigh savings; track acceptance and retries. |
| Resume paid asset jobs by ID | Avoid regenerating after a session handoff. | Unknown until first generation jobs. | Low | Verify the saved job belongs to the requested asset. |

## Additional improvements

- Keep a known-good playable build and stage candidates separately. Record source revision, Unity version, test results, and screenshots before promotion.
- Run a targeted compile/validator after behavioral changes; run an integration soak before a playable release. Automate checks before asking the user to test.
- Set practical art budgets before batch generation: silhouette, dimensions, topology, textures, rig, LODs, and materials. Inspect one asset in game first.
- Measure large-map navigation, rendering, and travel times early. Do not polish a whole map before validating its routes.
- Keep networking-facing domain rules explicit so expanding local content does not silently become the server architecture.
- Add Git LFS when large accepted binary assets arrive; keep provider downloads/staging out of Git. Local Git is initialized; no remote backup exists yet.
