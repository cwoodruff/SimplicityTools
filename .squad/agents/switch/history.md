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
- 2026-04-30T06:57:15.306-04:00: SF0002 is safest when it only evaluates package references that actually contribute compile-time metadata references and then proves usage through Roslyn-bound symbols; build-only packages should not be punished for having nothing to bind against.
- 2026-04-30T06:57:15.306-04:00: SF0007 needs an explicit primary-path baseline (annotation first, convention second). If the analyzer uses inbound-reference popularity to decide what counts as primary path, it teaches a circular rule and over-reports support code that merely became central by accident.

## Sprint 5 Launch — Release Packaging (Milestone 5)

**2026-04-30T19:09:43.583-04:00: Morpheus Lead Spawned, Wave 1 Routed**

- **Branch:** `sprint/5-release-packaging` created from main and pushed to origin.
- **Switch's M5 Assignment:** Own #38 (Package SimplicityTools.Analyzers) in Wave 1 parallel with Trinity's #35.
  - **Leverage:** Analyzer package layout established in Sprint 4 (Milestone 4) — uses `analyzers/dotnet/cs/` path from Roslyn consumption spec.
  - **Wave 1 (Ready Now):** #38 is foundational and self-contained. No compile-time library dependencies. Can proceed in parallel with Trinity's #35.
  - **Analyzer Packaging Spec:** csproj uses `PrivateAssets="all"` for all dependencies. Analyzer assembly packed under `analyzers/dotnet/cs/`, not `lib/`. No build-output packing.
- **#38 DoD (Wave 1):** All 7 SF00X analyzers included in pack. All code fix DLLs included. Test: add package to Sample.Simplified, build succeeds, SF0001 diagnostic fires. Test: add package to Sample.OverEngineered, build succeeds, all SF00X diagnostics present. Unit tests pass.
- **Validation Proof:** Package validation script must inspect .nupkg for `analyzers/dotnet/cs/SimplicityTools.Analyzers.dll` and fail if `lib/net10.0/SimplicityTools.Analyzers.dll` present (prevents regression to Sprint 4 blocker).
- **Critical Path Synchronization:** #38 runs parallel with #35 (Metrics), unblocking Tank's #39 integration validation after both complete. No blocking dependencies between Switch and Trinity.
