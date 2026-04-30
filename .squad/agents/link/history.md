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

### 2026-04-29T21:22:50.867-04:00: watch command live feedback ✓

**Issue #15 Completed.** Added `dotnet-simplicity watch <solution.sln>` so the CLI now stays in the foreground, prints an initial snapshot immediately, and re-runs analysis with filter verdicts after debounced file changes under the solution root.

**Implementation:** The watch flow uses `FileSystemWatcher` with a 500ms debounce, reloads `simplicity.json` validation on each pass, and suppresses repeated missing-config warnings while the file remains absent. To protect first-run usability, the watcher ignores `bin`, `obj`, `.git`, `.vs`, and `simplicity-report` paths so analysis output and build artifacts do not trigger self-refresh loops.

**Testing:** Added CLI coverage for the debouncer and a real watch-runner flow that mutates a copied sample workspace, verifies one refreshed snapshot, and checks the console output includes all three filter verdicts. Full CLI tests and the full solution test suite passed after the change.

**Outcome:** Sprint 2 now has a teaching-friendly live mode that answers “what changed and what should I look at next?” without flooding the console.

### 2026-04-30T02:13:09Z: Watch command decision archived ✓

**Scribe Sync:** Watch command self-loop guard decision merged from inbox into `.squad/decisions.md` active decisions log. Sprint 2 all seven issues (#9–#15) now complete. Ready for Sprint 3 planning (Roslyn Analyzers + Code Fixes).

### 2026-04-30T06:57:15.306-04:00: HTML report trend wave ✓

**Issue #25 Completed.** The HTML report now scans `.simplicity-history/*.json` for serialized `SimplicitySnapshot` files, orders them by `CollectedAt`, and layers the current snapshot on top when enough history exists.

**Implementation:** Replaced the placeholder trend copy with an inline SVG “Trend Wave” that charts primary path coverage, average method complexity, onboarding hours, and simplicity score over time with no JavaScript. Added historical filter score and complexity delta tables so the report explains which signals are improving or regressing.

**Testing:** Expanded CLI coverage to assert the no-history on-ramp copy and the multi-snapshot trend rendering path using repo-local workspaces. Because the shared checkout currently has unrelated analyzer work in progress, I validated the CLI tests with `dotnet build tests/SimplicityTools.Cli.Tests/SimplicityTools.Cli.Tests.csproj --nologo --no-dependencies --verbosity quiet` followed by `dotnet test tests/SimplicityTools.Cli.Tests/SimplicityTools.Cli.Tests.csproj --nologo --no-build`.

**Outcome:** The report now answers “what changed over time?” in a self-contained HTML artifact, while still teaching teams exactly how to unlock trend history on the first run.

### 2026-04-30T11:13:32Z: Sprint 3 Launch Coordination ✓

**Team Coordination:** Morpheus published Sprint 3 execution plan covering 11 open Milestone 3 issues across three waves: Wave 1 (Ready Now) assigns Switch to analyzers #16–#22 (7 independent diagnostics, parallelizable) and Link to Trend Analysis #25 (already complete); Wave 2 (After #16/#17 complete) assigns Link to Code Fixes #23–#24 (depends on SF0001/SF0002 analyzer contracts); Wave 3 (After Waves 1 + 2 complete) assigns Tank to Integration Testing + Performance Validation #26 (final quality gate). Critical path: #16–#22 (~3–4 days) → #23–#24 (~2–3 days) → #26 (~1–2 days). Total: ~6–9 days.

**Wave 1 Status:** #25 (Trend Analysis) already delivered; Link ready to move to Wave 2 code fixes as soon as Switch completes #16 (SF0001 analyzer) and #17 (SF0002 analyzer) design.

**Scribe Sync:** Sprint 3 launch decision, trend history contract, SF0002 compiler-backed policy, and SF0007 explicit baseline policy all merged into `.squad/decisions.md`. Orchestration log created at 2026-04-30T11:13:32Z documenting this coordination.

### 2026-04-30T06:57:15.306-04:00: Analyzer code-fix wave 2 ✓

**Issues #23–#24 Completed.** Added Roslyn code fix providers for SF0001 and SF0002 so the IDE can turn both diagnostics into a next action instead of a dead-end warning.

**Implementation:** SF0001 now rewrites interface references to the sole concrete implementation across the solution, removes the interface declaration, strips implementation base-list entries, and converts explicit interface members into normal public members when needed. SF0002 now edits the `.csproj` through a preview-friendly `TextDocument` solution change, removing only the targeted `<PackageReference>` line span instead of rewriting the whole XML file.

**Testing:** Expanded analyzer tests with workspace-backed code-fix coverage that asserts preview operations exist, validates SF0001 by recompiling the rewritten project, and validates SF0002 by reparsing the updated project file and rerunning the analyzer against the rewritten `.csproj`. Final verification passed with `dotnet build src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj --nologo --verbosity minimal`, `dotnet test tests/SimplicityTools.Analyzers.Tests/SimplicityTools.Analyzers.Tests.csproj --nologo --verbosity minimal`, and `dotnet test --no-restore --verbosity minimal`.

**Outcome:** Sprint 3 now has the two mandatory fixers for first-run IDE ergonomics, and both prove out through Roslyn round-trip validation instead of text-only assertions.

### 2026-04-30T08:11:34.261-04:00: Full tools guide for first-run usage ✓

Added `docs/using-the-simplicity-tools.md` and refreshed `README.md` so the repo now has a discoverable, commit-ready guide for the current toolset.

Useful learning: the report's trend view is powered by raw `SimplicitySnapshot` JSON files in `.simplicity-history/`, not by a dedicated archive command, so docs need to teach that manual history flow explicitly. Also, the CLI's first-run missing-config warning is intentional product surface and should be documented as expected behavior, not troubleshooting noise.

### 2026-04-30T08:24:49.761-04:00: GitHub landing-page README rewrite ✓

Completely rewrote `README.md` as a GitHub repository landing page with:
- Opening value statement ("measures solution complexity, surfaces opportunities")
- Business problem framing ("Why is this expensive?")
- Five-tool overview table with use-case context for each
- Separate sections for developers (install, build, integrate) and stakeholders (cost/benefit, use cases)
- Project structure and design decision rationale
- Scannable navigation to full docs and schema

**Structure:** Problem → Value → 5 Tools → Dev/Stakeholder Guidance → Project Layout → Next Steps. Grounded every claim in shipped behavior (six CLI commands, two code fixes, seven analyzers, three filters, HTML report, zero-config first run).

**Key framing:** Positioned SimplicityTools as answering "What to fix and why it matters to the business"—not just "here are the metrics." TCA cost model and filter verdicts surface the *why*, not just the *what*.

**Outcome:** GitHub repository landing page that works for both engineering teams evaluating the toolkit and stakeholders considering investment in code quality. Full docs still live in `docs/using-the-simplicity-tools.md`; README serves as scannable entry point.

## 2026-04-30T12:27:33.382322Z - README Update Task Spawned
- **Requested by:** Chris Woody Woodruff
- **Scope:** Update repository README with project description, tool outline, problems solved, and developer/stakeholder value
- **Deliverables:** 
  - README.md rewritten as GitHub landing page
  - Link history updated with this session
  - Decision inbox entry link-readme-positioning.md created
- **Status:** In Progress

## 2026-04-30T18:13:05Z: Packaging & DX Assessment Merged

**Team Context:**
- Morpheus' parallel strategy assessment converged on same recommendation: NuGet packages + global tool
- Both decisions merged into decisions.md (same packaging shape, complementary insights)
- Morpheus provided versioning strategy; you identified DX gaps and next steps

**Your Contribution:**
- DX Assessment highlights concrete fixes before publishing: README badges, analyzer PrivateAssets docs
- Outcome call clear: "Ready to publish—gap is documentation polish, not architecture"

**Impact:** Packaging assessment complete. DX roadmap captured for implementation phase.
