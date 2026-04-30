# Project Context

- **Owner:** Chris Woody Woodruff
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- The toolkit is meant to teach Simplicity-First through practical output and clear examples.
- The global tool is `dotnet-simplicity`.
- Zero-config first-run experience is a product requirement, not just a docs goal.

## Recent Updates

📌 Team hired on 2026-04-29T06:47:51.656-04:00

## Learnings

- My initial focus is CLI experience, docs, and sample-driven guidance.

### 2026-04-29T07:32:23.826-04:00: HTML Report Design & Implementation ✓

**Issue #8 Completed.** UX Decision: Dark theme (#0D0D0D) with brand red accents (#E31B23); all CSS embedded inline for self-contained, offline-safe generation.

**Implementation:** Shipped `dotnet simplicity report <solution.sln>` command generating `./simplicity-report/index.html` (~11–12 KB, <1 sec). Six-section report structure: Executive Summary (metric cards), Filter Verdicts (health badges), Metric Detail (full table), Complexity Budget (scorecard), Trend Analysis (guidance), Appendix (definitions + metadata).

**Simplicity Score Algorithm:** Composite 0–100 scale penalizing premature abstraction (up to 30 pts), unused dependencies (up to 20 pts), method complexity (up to 20 pts), low primary path coverage (up to 30 pts). Guides teams toward highest-impact improvements.

**Testing:** Added three test methods validating HTML structure, self-contained output (no external assets), and metric inclusion across both samples (Sample.Simplified, Sample.OverEngineered).

**Outcome:** Milestone 1 issue chain #1–#8 now complete on `sprint/1-metrics-core-collection`. Core collection passes, samples, analyze command, and report command all shipping together.

### 2026-04-29T21:22:50.867-04:00: simplicity.json schema and defaults ✓

**Issue #10 Completed.** Added `docs/simplicity-schema.json` as the contract for `simplicity.json`, covering TCA inputs (`teamSize`, salary, incidents, on-call rate, attrition) plus filter thresholds.

**Implementation:** `dotnet-simplicity analyze` and `report` now load `simplicity.json` from the solution root, warn clearly when the file is absent, merge partial overrides with sensible defaults, and fail fast on invalid or unsupported values.

**Testing:** Expanded CLI tests to cover default-warning behavior, partial override merging, invalid configuration rejection, and kept end-to-end analyze/report coverage intact using repo-local workspaces instead of OS temp folders.

**Outcome:** Sprint 2 now has a documented, validated configuration surface that teaches teams what can be tuned without blocking zero-config first run.

### 2026-04-29T21:22:50.867-04:00: baseline command first-run confirmation ✓

**Issue #12 Completed.** Added `dotnet-simplicity baseline <solution.sln>` to run collection, overwrite `.simplicity-baseline.json` beside the solution, and print a clear confirmation path after the snapshot summary.

**Implementation:** Baseline files are emitted as indented camelCase JSON so they read cleanly in-repo and are ready for future diff workflows. CLI tests now verify both write/overwrite behavior and restore any pre-existing sample baseline file so local worktrees do not get dirtied by the test run.

**Outcome:** Teams now have a concrete “capture today’s shape” command for CI and change tracking, with console output that answers what happened and where the file landed.

### 2026-04-29T21:22:50.867-04:00: diff command regression feedback ✓

**Issue #13 Completed.** Added `dotnet-simplicity diff <solution.sln> [--fail-on-regression]` so teams can compare the current snapshot with `.simplicity-baseline.json` and see the delta in plain language.

**Implementation:** The command now loads the baseline snapshot, prints metric deltas plus filter score deltas, and lists exactly which regression rules fired. Missing baselines fail with a next-step message that tells users to run `dotnet simplicity baseline <solution.sln>` first.

**Testing:** Expanded CLI coverage to validate diff output formatting and `--fail-on-regression` exit behavior while restoring any pre-existing sample baseline file after each run.

**Outcome:** Sprint 2 now has a CI-friendly regression gate that teaches what changed instead of returning a silent red build.

### 2026-04-29T21:22:50.867-04:00: budget command threshold mapping ✓

**Issue #14 Completed.** Added `dotnet-simplicity budget <solution.sln>` so the CLI now prints a four-line Complexity Budget scorecard with human-readable status, ASCII budget bars, configured targets, and a next-step hint.

**Implementation:** Budget output maps the existing `simplicity.json` filter thresholds directly onto the four budget dimensions: Cognitive Load → onboarding hours, Operational Surface → premature abstraction ratio, Change Safety → average method complexity, Discoverability → primary path ratio as a minimum target. That keeps the command zero-config on first run while making overrides visible immediately when teams tune `simplicity.json`.

**Testing:** Expanded CLI coverage to verify default budget output includes all four dimensions and that custom `simplicity.json` thresholds change the rendered targets and over-budget statuses. Full solution tests passed after the change.

## Team Decision: Budget Dimension Mapping

**Decision:** Map the four Complexity Budget dimensions to the existing `simplicity.json` filter thresholds so the command stays zero-config and immediately honors team overrides. Cognitive Load uses `maxOnboardingHours`, Operational Surface uses `prematureAbstractionRatioTarget`, Change Safety uses `maxMethodComplexity`, and Discoverability uses `primaryPathRatioTarget` as a minimum target.

**Rationale:** These four thresholds already exist, are documented, and line up with the budget dimensions without expanding the configuration schema mid-sprint. This keeps the first-run experience clear: teams can tune one config file and see budget output change right away.

**Logged:** 2026-04-30T02:01:24Z
