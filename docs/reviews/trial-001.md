# Paired review trial 001

PR: https://github.com/Jbudone/siphon/pull/1 — closed historical replay; not merged into its old base. Source changes are already on main.

- Base: `1cc509d14ab9ee75e3e5824632b41f31d3fcff35`.
- Head: `275642e61edb89847f38c6cc4ea3758078411960`.
- Both configured model routes: OpenRouter `anthropic/claude-sonnet-5.5`.
- PR-Agent wrapper: `a76cb9111b6a944094e874b3d467c4e546946e7b`; wrapper pulls rolling Docker runtime. Record resolved runtime evidence when available.
- OpenCodeReview action: `7e5ce06c88de5ce81df5dc4f24e2e485b517fae8`; CLI `1.13.10`.
- Full merge-base range, internal non-draft PR. Custom exclusions align on the ten C# files in the historical layout preview. Actual hosted coverage remains to be checked.

## Hosted runs

Both workflows completed with GitHub success, and both posted distinct comments. Actual OCR coverage was partial, so this is **not a valid paired source-review comparison**.

| Tool | Hosted evidence | Actual result |
| --- | --- | --- |
| PR-Agent | [Run 43782869330](https://github.com/Jbudone/siphon/actions/runs/43782869330), [review](https://github.com/Jbudone/siphon/pull/1#issuecomment-6904305694) | Reported no major issues. Log says full diff fit at 14397 estimated context tokens; this is not billed token usage. |
| OpenCodeReview | [Run 43782869368](https://github.com/Jbudone/siphon/actions/runs/43782869368), [summary](https://github.com/Jbudone/siphon/pull/1#issuecomment-6904300406) | Raw status partial: configuration group reviewed, ten C# items failed/undispatched by projected budget guard; 0 findings does not establish clean source. |

[Preserved machine report and posted output](trial-001-results.json). OCR reported 43889 total tokens (42494 input, 1395 output) and 15s internal elapsed. GitHub job durations were 53s PR-Agent and 29s OCR, including setup. Exact attributable costs were not reported. Do not compare these times or tokens as equivalent coverage. PR-Agent resolved Docker image digest `sha256:548e894a88071180832b2f4233f734d0acd0db1a879e2fcbaa70bac8b8372ebe`.

Correction validated by a fresh separate reviewer: increase the bounded OCR budget and check raw terminal/coverage status so partial runs become a visibly failed check. Keep model, effort and concurrency constant. Record the next run as a separate trial; preserve this evidence.

## Setup checks

Fresh Sol implementation worker and fresh separate Sol reviewer completed. No actionable findings from local review. JSON/TOML/YAML parsing, actionlint 1.7.11, supported action inputs, runtime checksum, C# custom-rule merge and non-billable file preview passed. Root verified documentation links and configured the existing local OpenRouter key as an Actions secret without printing it. User-modified `.envrc` and pre-existing `.vscode/` are excluded from this change.

## Audit limits

One integration trial is insufficient to prefer a tool. Prompt/context selection, budgets, language defaults and nondeterministic output differ even with a common model and aligned exclusions. Five findings is prompt guidance in OCR; its 100000-token guard permits a final round and is not a hard dollar cap. Do not infer exact review cost from elapsed Actions time or shared-account balance changes.
