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

## 2026-04-30T17:29:31.278-04:00: Sprint 4 package review rejected
Strong release proof needs one real consumer install for each delivery surface; metadata-only pack validation missed that the analyzer nupkg was laid out as lib/ instead of analyzers/dotnet/cs, so consumer builds loaded zero SimplicityTools diagnostics.

**Archived Sprint 1–3 entries to `.squad/agents/tank/history-archive.md` due to size threshold (↦ 6.8 KB). See archive for full orchestration snapshots and cross-agent sync records.**

---

## 2026-04-30T22:15:00Z: Sprint 4 Milestone 4 analyzer-package rereview
- **Learning:** Analyzer package rereview cleared once both gates matched the real Roslyn load path: the nupkg contained `analyzers/dotnet/cs/SimplicityTools.Analyzers.dll` with no `lib/net10.0/` copy, and a downstream package consumer emitted `warning SF0001` during `dotnet build`.

📌 **Sprint 4 Milestone 4 analyzer-package rereview approved (2026-04-30T22:15:00Z):** Re-reviewed Trinity's analyzer-packaging revision for the prior publish blocker. `SimplicityTools.Analyzers.csproj` now suppresses normal build output packing and explicitly packs the analyzer assembly under `analyzers/dotnet/cs/`. New regression `AnalyzerPackageValidationTests.PackedAnalyzerPackage_UsesAnalyzerLayout_AndReportsDiagnosticsInConsumer` passed locally, and a repo-local consumer restore/build against the packed nupkg emitted `warning SF0001`. Workflow `nuget-publish.yml` now enforces the same release gate by failing if the analyzer ships under `lib/net10.0/` or if the consumer build does not emit SF0001. Verdict: **Approved**; publish blocker closed for Sprint 4 Milestone 4.

## Sprint 5 Launch — Release Packaging (Milestone 5)

**2026-04-30T19:09:43.583-04:00: Morpheus Lead Spawned, Wave 4 Routed**

- **Branch:** `sprint/5-release-packaging` created from main and pushed to origin.
- **Tank's M5 Assignment:** Own #39 (Validate NuGet library package dependencies and metadata) in Wave 4.
  - **Wave 4 (After all libraries):** #39 runs after Trinity completes #35, #36, #37 and Switch completes #38. All four packages must exist before integration validation begins.
  - **Integration Scope:** Publish all four packages to a local test NuGet feed. Create a fresh test project that PackageReferences all four. Restore and build succeeds. Dependency graph resolves correctly in Visual Studio Package Manager and `dotnet add package`. Run Sample.Simplified and Sample.OverEngineered against local packages. Verify analyzer diagnostics fire and no warnings/errors introduced. Document validation script in CONTRIBUTING.md for future releases.
  - **Quality Gate:** Tank's #39 validation is the final gate before publish approval. Zero unintended symbols or internal types exposed. Zero NuGet warnings during pack or restore.
- **Critical Path:** Tank's validation blocks publish readiness. No publish to nuget.org until #39 closes with approval.
- **Dependencies:** Waits for Trinity (#35, #36, #37 complete) and Switch (#38 complete) before starting.

## 2026-04-30T19:09:43.583-04:00: Sprint 5 Milestone 5 package review
- **Learning:** Passing local package-validation tests is not enough if the release workflow cannot execute its own analyzer gate. CI scripts are part of the release artifact; a missing import can invalidate otherwise-correct packages.

📌 **Sprint 5 Milestone 5 package review rejected (2026-04-30T19:09:43.583-04:00):** Re-reviewed Metrics, Filters, Tca, and Analyzers from the outside in. Evidence that held: `dotnet restore` and `dotnet build -c Release` succeeded locally (known NU1903 noise only); all four package-validation tests passed; repo-local packed nupkgs showed the expected shapes and dependency chain (`Filters -> Metrics`, `Tca -> Filters + Metrics`, analyzer under `analyzers/dotnet/cs/` with no `lib/`); and a fresh external consumer referencing all four packages built successfully and emitted `warning SF0001`. Verdict: **Rejected for release** because `.github/workflows/nuget-publish.yml` still breaks the analyzer consumer gate — the Python block calls `ET.fromstring(...)` without importing `xml.etree.ElementTree as ET`, which reproduces as `NameError: name 'ET' is not defined`. Impact: branch/tag validation will fail in CI before publish, so Milestone 5 is not release-ready. Revision ownership reassigned to **Link** under reviewer lockout for the workflow fix.

---

## 2026-04-30T19:52:08.101-04:00: Sprint 5 workflow rereview
- **Learning:** Workflow rereviews need one adversarial rerun, not just a clean first pass. If the gate writes its own validation workspace, inject stale state and prove the next run deletes it before rebuilding.

📌 **Sprint 5 Milestone 5 workflow rereview approved (2026-04-30T19:52:08.101-04:00):** Re-reviewed Link's repair to `.github/workflows/nuget-publish.yml`. The analyzer-consumer validation block now imports `xml.etree.ElementTree as ET` before parsing the nuspec, so the prior `NameError` path is gone. Independent local proof passed on the relevant workflow path: `dotnet restore SimplicityTools.sln`, `dotnet build SimplicityTools.sln --configuration Release --no-restore`, `dotnet test tests/SimplicityTools.Analyzers.Tests/SimplicityTools.Analyzers.Tests.csproj --configuration Release --no-build --filter FullyQualifiedName~AnalyzerPackageValidationTests`, and `dotnet pack src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj --configuration Release --no-build --output artifacts/packages -p:Version=0.4.0-ci.tankreview`. I then reran the workflow's analyzer-consumer validation logic twice against the packed analyzer package; both runs emitted `warning SF0001`, and an injected stale sentinel inside `artifacts/analyzer-consumer-validation` was removed before the second pass completed. Verdict: **Approved**; the Milestone 5 publish workflow blocker is cleared.
