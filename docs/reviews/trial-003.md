# Paired review trial 003 — actual candidate isolation PR

[PR #3](https://github.com/Jbudone/siphon/pull/3) adds opt-in 600m candidate assets and layout-aware checkpoints. Unlike the historical replays, this is a real source change. Base `b6f71c1a7ac9bc15b2f0ade317b11d6e4299b3ce`, reviewed head `0cdbbe66606d80ae88998ed56b38c11daacd48b2`; merged as `589d90b85b58eea700f0fe1063355dd0d4c3c2a0` after local verification and both hosted reports.

| Tool | Output | Coverage and usage evidence |
| --- | --- | --- |
| PR-Agent | [No major issues](https://github.com/Jbudone/siphon/pull/3#issuecomment-6904905146), [successful run](https://github.com/Jbudone/siphon/actions/runs/43787081142) | Estimated context 17850 tokens, diff pruned under 16000 cap; no per-file completion manifest or billed usage. Job 78s including setup. |
| OpenCodeReview | [Complete, zero findings](https://github.com/Jbudone/siphon/pull/3#issuecomment-6904913018), [successful run](https://github.com/Jbudone/siphon/actions/runs/43787081123) | Manifest complete: all seven selected C# files completed; zero failed/reused/waived, 18 excluded. Reported 237476 aggregate tokens (225644 input, 11832 output), internal 1m36s; job 113s including setup. Coverage-verification step passed. |

[Preserved machine report, comments and run metadata](trial-003-results.json). Same model alias `anthropic/claude-sonnet-5.5`, action/CLI pins and guards as trial 002. PR-Agent's pinned wrapper still pulls a rolling runtime; observed base image digest `sha256:548e894a88071180832b2f4233f734d0acd0db1a879e2fcbaa70bac8b8372ebe`. Trusted host-only retry/auto-approval settings were present for this run; prior ignored-key warnings did not recur.

Both reports are advisory. Source review and engine checks addressed the excluded-scene reload risk before this head was published. No known bug remained for these hosted tools to catch, so zero findings does not measure their recall. Context pruning prevents equal-coverage claims. Reported OCR aggregate usage and PR-Agent context estimate are different measurements; no exact cost or tool preference established. Keep both enabled for future samples.

Raw local logs/reports are under ignored `artifacts/reviews/trial-003/`; OCR Actions artifacts retain 90 days. Committed report contains no credentials/provider headers.
