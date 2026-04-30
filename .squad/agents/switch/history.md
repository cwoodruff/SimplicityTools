# Project Context

- **Owner:** Chris Woody Woodruff
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- The toolkit includes a dedicated analyzer package for IDE and MSBuild integration.
- Primary path heuristics and abstraction signals should stay explainable to developers.
- First-run usefulness matters more than exhaustive cleverness.

## Recent Updates

📌 Team hired on 2026-04-29T06:47:51.656-04:00
📌 **Sprint 1 issue #2 completed (2026-04-29T11:57:01.000Z):** Sample.OverEngineered scaffold delivered with 12-project topology (composition root + 11 layers). Real Roslyn/MSBuild facts ready for metrics. Decision logged.

## Learnings

- My initial focus is Roslyn analysis, diagnostics, and compiler-backed heuristics.

- 2026-04-29T07:32:23.826-04:00: I used the existing sample executable as a composition root and pushed the overengineering into 11 supporting libraries so future metrics work can count project fan-out, single-implementation interfaces, and mediator-style hops without inventing fake files later.
- 2026-04-29T11:57:01.000Z: Structural overengineering with real projects makes sample differences measurable and avoids placeholder theater.
- 2026-04-29T07:32:23.826-04:00: For the primary-path heuristic pass, I treated inbound references as a file-level score across the named types declared in a file and refused to promote the percentile signal when every candidate had zero inbound references; otherwise the heuristic would mark noise as intent.
- 2026-04-29T21:22:50.867-04:00: TCA executive-summary formatting is part of the contract and must stay invariant under non-default `CurrentCulture`; locale drift in money formatting is noise, not value.
- 2026-04-29T21:22:50.867-04:00: TCA estimation depends on all three filter verdicts being present; missing a required verdict should fail fast with the absent filter named explicitly.

📌 **Sprint 2 issue #11 TCA revision assigned (2026-04-30T01:22:50Z):** Tank rejected Trinity's TCA calculator implementation. Revision ownership now under my lockout. Gap analysis: (1) Required-filter failure-path coverage — add tests for missing TwoAmTest, HalfRule, or PrimaryPathFirst verdicts; (2) Non-default-culture executive-summary formatting — ensure `ToExecutiveSummary()` uses culture-agnostic formatting. Task: Implement missing edge-case tests and resubmit for Tank approval. Decision logged.
