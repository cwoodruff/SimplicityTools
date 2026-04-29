# Project Context

- **Owner:** Chris Woody Woodruff
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- The toolkit turns architectural measurements into TCA-aligned cost signals.
- The core library surface spans Metrics, Filters, and Tca packages.
- The toolkit should be useful on the first CI run without configuration.

## Recent Updates

📌 Team hired on 2026-04-29T06:47:51.656-04:00
📌 **Sprint 1 issue #1 completed (2026-04-29T11:44:51.000Z):** SimplicitySnapshot record + ToSummary() delivered with spec-aligned 10-property contract. Contract finalized: legacy instance members excluded. Tank's contract tests validate regression signal. Notes: stale CLI reference to `snapshot.SolutionName` flagged for future cross-team remediation.

## Learnings

- My initial focus is the core measurement and cost-translation packages.
 - 2026-04-29T07:32:23.826-04:00: For contract-first sprint work, the exact public surface must win over scaffold compatibility, because downstream callers are cheaper to migrate than a public record shape that drifts from spec.
 - 2026-04-29T11:44:51.000Z: Wave 2 (sample scaffolds #2, #3) and Wave 3+ (structural, semantic, heuristic passes) now unblocked after issue #1 completion.
 - 2026-04-29T07:32:23.826-04:00: Pass 1 can stay fast and deterministic by using `SolutionFile` plus raw project-file parsing for declared `Compile` and `PackageReference` items, leaving full MSBuild/Roslyn evaluation to later semantic work.
 - 2026-04-29T07:32:23.826-04:00: For package-usage metrics, matching declared package IDs to Roslyn metadata reference paths and then confirming usage through semantic symbols keeps the collector incremental while still grounding unused-dependency counts in actual compilations.
