# Memory audit — 2026-11-15

Recommendation: keep Git-backed Markdown as Siphon memory now. Do not install mem0 in this slice. This is a documentation and workflow assessment, not a measured retrieval benchmark or audit of a deployed instance. No mem0 connector or repository integration was found in this session; no memory data was uploaded.

## Fit

Our current questions concern exact priorities, accepted tuning, source/build revisions, access blockers and paid generation job IDs. STATE, CURRENT-TASK, ROADMAP, CONTEXT and request journals already provide explicit ownership and revision history. mem0 would add semantic recall over accumulated decisions and preferences, useful when those become difficult to find across sessions. It would not replace these authoritative records.

The current [OSS overview](https://github.com/mem0ai/mem0/blob/main/docs/open-source/overview.mdx) supports a library or self-hosted server. Its defaults use an LLM, embeddings, vector storage and history storage. This adds extraction/embedding usage, persistence and backup configuration, and integration work to every fresh-agent workflow. Local storage alone does not imply local inference; provider configuration determines what leaves the machine.

The current [migration guide](https://github.com/mem0ai/mem0/blob/main/docs/migration/oss-v2-to-v3.mdx) describes ADD-only extraction: changed facts accumulate beside older ones, with retrieval responsible for ranking. For Siphon, retrieval must still verify current authoritative files before acting on tuning, job IDs, credentials or blockers. The guide also removes graph storage from OSS and places graph memory on the hosted Platform. Check a pinned release before any future installation; this assessment reads upstream main.

## When to reconsider

Revisit after repeated observed failures to recover decisions from the compact handoff and targeted repository search. Try a small project-scoped, derived index over approved decision summaries, with source path, revision and date on every item. Exclude credentials and raw transcripts. Keep it rebuildable from Git and preserve the Markdown workflow when unavailable.

Compare it against Markdown plus targeted search on a small set of real Siphon recall questions, including superseded decisions. Measure correct current answers, source attribution, stale-fact mistakes, latency and actual provider cost. Adopt only if recall improves enough to justify the extra moving parts. Vendor benchmark claims do not establish that benefit here.
