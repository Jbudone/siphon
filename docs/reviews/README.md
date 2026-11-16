# Review tool trial

We are trying [PR-Agent](https://github.com/The-PR-Agent/pr-agent) and Alibaba [OpenCodeReview](https://github.com/alibaba/open-code-review) on the same internal, non-draft PRs. Both are advisory. Keep the separate local reviewer and orchestrator integration; neither hosted tool merges or fixes source automatically.

## Compare later

Keep the PR URL, base/head revisions, action/runtime versions, model, run links and raw available reports. Reviews should remain separately identified. Start with the same OpenRouter model (`anthropic/claude-sonnet-5.5`) so the first comparison mostly concerns the review tools. This does not make their prompts, context selection, token budgets or nondeterministic outputs identical.

Use the PR discussions and retained reports to inspect confirmed correctness bugs, false alarms, duplicates and missed issues later. Record a cost only when attributable usage is available; elapsed Actions time includes setup and is not just model latency. A successful job does not by itself establish useful review coverage, especially if files were skipped or a budget was exhausted. Do not select a winner from a wiring smoke test.

## Trial record

First paired run is being set up. Links, exact revisions, provider/model and actual outcomes will be added after the hosted runs. No quality or cost result is claimed yet.

## Retention and limits

GitHub PR comments/reviews retain the discussion. Actions artifacts retain the supported machine reports for the configured period; copy useful records into this directory before they expire. Avoid committing credentials, signed download URLs or provider headers. Generated Unity scenes, imported assets and LFS pointers are poor inputs for source review; record each tool's actual exclusions rather than claiming identical coverage. Human checks of Unity references remain necessary.

No Langfuse service or automatic scoring is being installed for this trial. This directory captures evidence for the later audit requested by the user.
