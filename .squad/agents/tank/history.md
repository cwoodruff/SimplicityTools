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
- 2026-04-29T10:58:25.595-04:00: `ReportGenerator.cs` footer contained an external `https://github.com/cwoodruff/SimplicityTools` hyperlink, violating the self-contained HTML contract. Fixed by replacing the anchor tag with plain text. Test: `ReportCommand_GeneratesSelfContainedHtmlForBothSamples` in `SimplicityTools.Cli.Tests`. All 4 CLI tests now pass.
- 2026-04-29T10:58:25.595-04:00: When re-verifying a previously identified fix, always reproduce with `--no-build` to separate build-cache noise from real test failures. MSBuild `error MSB3492` on `.msCoverageSourceRootsMapping_*` cache files (caused by macOS `com.apple.provenance` extended attributes) can block incremental builds but is resolved by cleaning `obj/` and `bin/` artifacts before re-running. The underlying test logic was sound; only the build cache was corrupted.

📌 **Session update (2026-04-29T14:58:25Z):** Regression investigation session completed. Confirmed fix already in commit b8d1d17 for HTML report self-contained contract. Test suite passing; no source edits required.
- 2026-04-29T21:22:50.867-04:00: For TCA review, one happy-path fixture is not enough. If a package depends on three named filter verdicts and emits a book-facing currency summary, approval should wait until tests cover missing-verdict failure behavior and non-default-culture formatting.

📌 **Sprint 2 issue #11 TCA review (2026-04-30T01:22:50Z):** Reviewed Trinity's TCA calculator implementation. Verdict: **Rejected** for revision. Five category formulas (Infrastructure, Operational, Coordination, Cognitive, Opportunity) align with Milestone 2 spec, but regression suite only validates single happy-path fixture. Required gap coverage before approval: (1) Required-filter failure-path: Behavior when required filter verdicts (TwoAmTest, HalfRule, PrimaryPathFirst) are missing; (2) Non-default-culture executive-summary formatting: Culture-invariant formatting in `ToExecutiveSummary()`. Revision ownership transferred to Switch under reviewer lockout. Decision logged.
- 2026-04-29T21:22:50.867-04:00: The TCA package cleared review once the suite proved both failure behavior for a missing required filter verdict and culture-invariant executive-summary formatting under `fr-FR`. For book-facing strings, a tiny culture scope test buys real regression safety.

📌 **Sprint 2 issue #11 TCA rereview approved (2026-04-30T01:40:30Z):** Rereview of Switch's TCA calculator revision completed. Verdict: **Approved**. Both gap coverage requirements now met: `TcaEstimateTests.Create_ThrowsWhenARequiredFilterVerdictIsMissing` proves required-filter failure path for `PrimaryPathFirst`; `TcaEstimateTests.ToExecutiveSummary_UsesSpecifiedFormat_IndependentlyOfCurrentCulture` proves culture-invariant money formatting under `fr-FR`. Local test run: 4 tests, 0 failures. Regression bar is now met for both calculator contract and book/CLI-facing summary output. Issue #11 approved for closure.
- 2026-04-30T06:57:15.306-04:00: Trend-report review is stronger when I seed `.simplicity-history` with real JSON snapshots and inspect the generated HTML for actual filter-score rows and delta cells, not just section headers. The report promise is user-facing, so the evidence needs to show the inline SVG and the historical tables rendered together.

📌 **Sprint 3 issue #25 trend-report review approved (2026-04-30T06:57:15.306-04:00):** Reviewed Link's HTML trend analysis implementation. Verified `SnapshotHistory` reads `.simplicity-history/*.json`, the report renders a no-JS inline SVG trend view once two historical snapshots exist, and the HTML includes historical filter-score rows plus complexity delta rows. Focused validation: `dotnet test tests/SimplicityTools.Cli.Tests/SimplicityTools.Cli.Tests.csproj --nologo --filter "FullyQualifiedName~ReportCommand_"` passed locally (3 tests, 0 failures), and a seeded sample workspace produced the expected SVG/chart/table output.
- 2026-04-30T06:57:15.306-04:00: Analyzer reviews need adversarial edge cases, not just one positive and one negative per rule. For primary-path heuristics, mixed-mode fixtures (explicit annotations plus conventional folders) catch real baseline bugs that simple happy paths miss.
- 2026-04-30T06:57:15.306-04:00: When an issue scope says "classes," write the regression that proves structs stay silent. Constructor analyzers are eager to overreach if the symbol filter is even slightly loose.

📌 **Sprint 3 issues #16-#22 analyzer review (2026-04-30T06:57:15.306-04:00):** Reviewed Switch's analyzer implementation. Verdict: **Rejected** for revision. Baseline validation passed (`dotnet build src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj --nologo`; `dotnet test tests/SimplicityTools.Analyzers.Tests/SimplicityTools.Analyzers.Tests.csproj --nologo`, 14 tests). Reviewer scratch checks exposed two uncovered contract failures: SF0005 currently flags 8-parameter structs even though issue #20 scopes the rule to classes, and SF0007 incorrectly exempts conventional primary-path folders after `[PrimaryPath]` annotations become the baseline. Revision ownership transferred to Trinity under reviewer lockout. Decision logged in inbox.

---

## 2026-04-30T10:57:15Z — Orchestration Snapshot
**From:** Scribe cross-agent sync
- **Trend Chart Review:** Approved. Status: Closed.
- **Analyzers Wave 1 Review:** Rejected. SF0005 + SF0007 issues identified.
- **Lockout:** Tank reviewer lockout applied. Trinity owns analyzer revision.
- **Availability:** Tank remains open for #26 (Integration Testing) and other Sprint 3 tasks.

- 2026-04-30T06:57:15.306-04:00: On a shared sprint branch, analyzer rereview can stay honest by running an analyzer-only harness that reuses the real test infrastructure and strips code-fix tests from the scratch copy, instead of patching teammates' in-flight files. That lets me verify the diagnostics contract without contaminating unrelated work.

📌 **Sprint 3 issues #16-#22 analyzer rereview approved (2026-04-30T06:57:15.306-04:00):** Reviewed Trinity's analyzer revision for the prior SF0005 and SF0007 rejection points. `ConstructorParameterCountAnalyzer` now limits diagnostics to `TypeKind.Class`, and `NonPrimaryPathOverReferencedAnalyzer` now uses `[PrimaryPath]` annotations as the sole baseline whenever any annotation exists. Validation: analyzer-only rereview harness passed 16 analyzer tests with 0 failures, and the two targeted regressions (`ConstructorParameterCountAnalyzer_DoesNotReportStructPrimaryConstructorAboveThreshold` and `NonPrimaryPathOverReferencedAnalyzer_TreatsConventionalFilesAsSupportingWhenAnnotationsExist`) passed again in a focused 2-test rerun. Verdict: **Approved**.

---

## 2026-04-30T10:57:15Z — Scribe cross-agent sync
**Decision merged:** Tank's analyzer rereview verdict is now in `.squad/decisions.md` alongside the prior rejection notice. This provides the team with the complete decision arc (rejection → revision → approval).
- **Status:** Decisions archived and synced.
- **Next:** Issues #16-#22 approved for closure. No blocking reviewers on other Sprint 3 tasks.
