# Project Context

- **Owner:** Chris Woody Woodruff
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- The sample projects are essential teaching assets, not optional extras.
- Metrics should show a meaningful difference between the overengineered and simplified samples.
- Zero-config first-run behavior needs test coverage, not assumptions.

## Recent Updates

📌 Team hired on 2026-04-29T06:47:51.656-04:00
📌 **Sprint 1 issue #1 completed (2026-04-29T11:44:51.000Z):** SimplicitySnapshot contract tests finalized. Validated 10 positional properties, 2 derived ratios, `ToSummary()` formatting, and culture-invariant output. Trinity's implementation aligns contract to spec; legacy instance members excluded. Notes: contract drift detection working as intended; Sprint 1 waves 2–7 now unblocked.
📌 **Sprint 1 issue #3 completed (2026-04-29T11:57:01.000Z):** Sample.Simplified scaffold delivered as 2-project modular monolith with single IFulfillmentPolicy interface seam (StandardFulfillmentPolicy and ExpressFulfillmentPolicy implementations). Six tests passing. Decision logged.
📌 **Sprint 1 issue #7 completed (2026-04-29T11:32:23Z):** CLI analyze wired to real collector, stale `SolutionName` reference removed. Baseline-backed sample validation and CLI process tests added. Solution-file fix restored full-solution test execution. Validation: `dotnet test SimplicityTools.sln --nologo` passing (existing vulnerability warnings only). Decision: CLI sample baselines remain date-agnostic to catch real metric drift without false daily failures.

## Learnings

- 2026-04-29T07:32:23.826-04:00: For Sample.Simplified, keeping one real interface seam (`IFulfillmentPolicy`) and leaving catalog, ordering, and payment flows concrete gives the metrics work a cleaner low-abstraction baseline without losing a legitimate polymorphic example.
- My initial focus is package tests, regression safety, and sample-project validation.
- 2026-04-29T07:32:23.826-04:00: When a book-facing summary string is part of the spec, test it under a non-default culture so formatting drift gets caught before it leaks into chapter output or CLI text.
- 2026-04-29T11:44:51.000Z: Exact contract tests succeed when implementation aligns specs first and rejects compatibility debt. Regression signal is strong when public surface is predictable.
- 2026-04-29T11:57:01.000Z: Real interface seams with multiple active implementations make future analyzer work trustworthy without noise.

- 2026-04-29T07:32:23.826-04:00: CLI regression tests are steadier when the tool is executed as a built process against both sample solutions and the sample baselines stay numeric-only, so `CollectedAt` does not create fake churn.
