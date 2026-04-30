# Squad Decisions

## Active Decisions

### 2026-04-29T06:47:51.656-04:00: Project scope anchor
**By:** Chris Woody Woodruff
**What:** The squad is being set up around the Simplicity-First .NET Toolkit plan in `docs/SimplicityFirst_DotNet_Toolkit_Plan.docx`. Initial delivery centers on five packages: Metrics, Analyzers, Filters, Tca, and the `dotnet-simplicity` CLI, supported by sample solutions.
**Why:** This is the authoritative product brief the squad should use for routing and implementation planning.

### 2026-04-29T06:47:51.656-04:00: Zero-config first run is mandatory
**By:** Chris Woody Woodruff
**What:** The toolkit must run in CI/CD with zero configuration and provide useful signal on the first run.
**Why:** This is the key product constraint from the project plan and should influence CLI, analyzer, and documentation decisions.

### 2026-04-29T06:47:51.656-04:00: Initial squad composition
**By:** Squad
**What:** The project starts with a five-member Matrix cast: Morpheus, Trinity, Switch, Tank, and Link, supported by Scribe and Ralph.
**Why:** The work naturally splits into architecture, core .NET implementation, analyzer/compiler work, testing, and developer experience.

### 2026-04-29T07:03:10.371-04:00: Analyzer package stays isolated in the scaffold
**By:** Morpheus
**What:** The initial scaffold keeps `SimplicityTools.Analyzers` as its own package and wires `SimplicityTools.Cli` to it as an analyzer asset reference, while executable and library code dependencies remain `Metrics -> Filters/Tca -> Cli`.
**Why:** This preserves the plan's package direction without forcing the CLI to take a normal compile-time dependency on analyzer internals, keeping the analyzer package separable for IDE/MSBuild use from the first scaffold.

### 2026-04-29T07:21:03.149-04:00: Sprint structure decomposition
**By:** Morpheus
**What:** Mapped the 13-step implementation order into three milestones: Metrics & Core Collection (M1, steps 1–8), Filters + TCA + CLI Extensions (M2, steps 9–11), and Roslyn Analyzers + Code Fixes (M3, steps 12–13). Each milestone represents a meaningful delivery gate aligned with the book's chapter structure and the three-tier package architecture (Metrics → Filters/Tca → Cli).
**Why:** Grouping steps into three sprints provides narrative clarity (measurement → decision support → IDE feedback), aligns with package dependency boundaries, and syncs with the book chapters. All 26 resulting issues have been created in GitHub with milestone assignments and prerequisite tracking. DoD criteria defined per milestone.

### 2026-04-29T07:32:23.826-04:00: Sprint 1 kick-off
**By:** Morpheus
**What:** Sprint 1 covers the foundation of the Simplicity-First .NET Toolkit: building the core data model (SimplicitySnapshot), implementing the three collection passes (structural, semantic, heuristic), scaffolding sample solutions, and delivering the CLI `analyze` and `report` commands. The sprint follows a hard dependency chain: Issue #1 (SimplicitySnapshot) unblocks Wave 2 (#2, #3 samples in parallel), which unblock Wave 3+ through #7 (CLI analyze). Full implementation sequence, work wave assignments, and critical decisions documented in inbox/morpheus-sprint1-kickoff.md.
**Why:** Provides clarity on Sprint 1 scope, dependency enforcement, and team wave assignments to ensure systematic execution without blocked parallelism or speculative work.

### 2026-04-29T07:32:23.826-04:00: SimplicitySnapshot contract finalized
**By:** Trinity and Tank (consensus)
**What:** `SimplicitySnapshot` public contract is fixed to the 10 positional constructor properties, 2 derived ratio properties, and `ToSummary()` method only. Legacy compatibility properties (old `SolutionName`, `Metrics`) are not preserved. Static `Empty(string)` helper retained as migration aid.
**Why:** Downstream packages (Filters, TCA, CLI) and book chapters reference a single stable shape. Tank's contract tests enforce this as regression signal. Trinity preserved the spec-aligned contract in implementation, rejecting stale instance members despite compilation compatibility, to avoid downstream ambiguity.

### 2026-04-29T07:32:23.826-04:00: Overengineered sample topology
**By:** Switch
**What:** Issue #2 starts from a real 12-project solution under `samples/Sample.OverEngineered`, with the existing root executable kept as the composition root and 11 additional class libraries representing Domain, Application, Infrastructure, Persistence, ReadModel, WriteModel, Messaging, Cache, Validation, Authorization, and Telemetry.
**Why:** This keeps the sample buildable on the shared sprint branch while making the overengineering structural, not theatrical. Future metrics and analyzers can measure project count, interface density, and mediator-style indirection from real Roslyn/MSBuild facts instead of placeholder comments.

### 2026-04-29T07:32:23.826-04:00: Simplified sample keeps one real interface seam
**By:** Tank
**What:** Issue #3 needs a buildable `Sample.Simplified` scaffold that demonstrates the intended lower-abstraction shape without collapsing into a trivial hello-world. Model the sample as a 2-project modular monolith (`App` and `App.Tests`) with concrete catalog, ordering, and payment services. Keep `IFulfillmentPolicy` as the only interface seam because it already has two live implementations (`StandardFulfillmentPolicy` and `ExpressFulfillmentPolicy`).
**Why:** This gives metrics and analyzer work a credible "good" sample while avoiding interface-per-handler noise. It also leaves one legitimate polymorphic branch in place so future analyzer and regression tests can prove the toolkit distinguishes useful abstraction from cargo-cult abstraction.

### 2026-04-29T07:32:23.826-04:00: Structural pass parsing strategy
**By:** Trinity
**What:** Pass 1 should use `Microsoft.Build.Construction.SolutionFile` for the solution walk and raw project-file parsing for `Compile`/`PackageReference` items, including explicit glob expansion where needed, instead of full evaluated project loading.
**Why:** The structural pass only needs declared shape, not resolved compilations. Keeping Pass 1 at the project-file layer preserves deterministic counting, avoids semantic-pass coupling, and keeps the fast path lighter for larger solutions.

### 2026-04-29T07:32:23.826-04:00: Semantic package usage resolution stays reference-backed
**By:** Trinity
**What:** The semantic metrics pass treats a declared package as used only after matching it to Roslyn metadata references from the project compilation and then finding either namespace usage or symbol usage from those assemblies in source.
**Why:** This keeps `UnusedDependencyCount` tied to what the compiler actually sees, avoids fragile string-only heuristics, and gives future filter/TCA work a deterministic definition that can be reused without requiring a full analyzer pipeline.

### 2026-04-29T07:32:23.826-04:00: Primary path percentile guard
**By:** Switch
**What:** Pass 3 treats inbound-reference percentile as a file-level score aggregated across named types declared in the file, and it suppresses percentile matches when every candidate has zero inbound references.
**Why:** The metric is reported at file granularity, so the heuristic needs a file-level signal even when a source file declares more than one type. Suppressing the all-zero case avoids turning "nothing stands out" into a noisy false-positive blanket across the solution.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

### 2026-04-29T07:32:23.826-04:00: CLI sample baselines stay date-agnostic
**By:** Tank
**What:** Keep `tests/SimplicitySampleBaselines.json` limited to numeric snapshot metrics plus solution-relative paths. CLI analyze tests should derive the expected summary date from actual output instead of storing `CollectedAt` in the baseline file.
**Why:** `CollectedAt` is runtime state, not a product baseline. Keeping the baselines date-agnostic lets the suite catch real metric drift in the samples without false failures every day the CLI runs.

### 2026-04-29T07:32:23.826-04:00: HTML Report Design & Execution
**By:** Link
**What:** Implemented `dotnet simplicity report` to generate a self-contained, styled HTML report capturing all required sections with no external dependencies. All CSS embedded inline; dark theme (`#0D0D0D`) with brand red (`#E31B23`) accents. Report structure: Executive Summary (metric cards), Filter Verdicts (domain health badges), Metric Detail (full table), Complexity Budget (simplified scorecard), Trend Analysis, Appendix. Simplicity Score algorithm uses composite 0–100 penalty system (premature abstraction up to 30 pts, unused dependencies up to 20 pts, high method complexity up to 20 pts, low primary path coverage up to 30 pts). Output to `./simplicity-report/index.html` (~11–12 KB, <1 sec generation).
**Why:** Self-contained HTML works offline and in CI/CD with zero configuration. Embedded CSS and brand colors reflect professionalism; responsive grid and status badges provide visual health signals. Composite Simplicity Score guides teams toward highest-impact improvements. Three test methods validate HTML structure, self-contained output, and metric inclusion across Sample.Simplified and Sample.OverEngineered.

### 2026-04-29T21:22:50.867-04:00: Sprint 2 Execution Plan
**By:** Morpheus
**What:** Sprint 2 delivers the decision-support layer: filter evaluators (TwoAmTest, HalfRule, PrimaryPathFirst), TCA cost model, simplicity.json configuration schema, and CLI extensions (baseline, diff, budget, watch). Seven open issues (#9–#15) organized in four waves with hard dependencies: Wave 1 (Ready Now) includes #9 Filter evaluators → Trinity and #10 simplicity.json schema → Link; Wave 2 (After #9 complete) includes #11 TCA calculator → Trinity; Wave 3 (After #9 + #10 + #11 complete) includes #12 CLI baseline → Link; Wave 4 (After #12 complete) includes #13 CLI diff, #14 CLI budget, #15 CLI watch → Link. Critical path: #9 → #11 → #14; #10 → #14; #9 → #12 → #13; #9 → #15.
**Why:** Wave structure enforces implementation order while maximizing parallelism. Aligns with package dependencies and book chapter structure (Measurement → Decision Support → Feedback). Success criteria: All 7 issues closed with passing tests, CLI commands functional on both samples, zero-config promise maintained.

### 2026-04-29T21:22:50.867-04:00: Filter evaluator metric mapping
**By:** Trinity
**What:** Issue #9 implements filter evaluators directly against the existing `SimplicitySnapshot` contract. For Wave 1, the filters map "primary path hop count" to `PrimaryPathFileCount`, and they apply the Primary Path First project-count target (`<= 5`) unconditionally because the snapshot does not yet carry a dedicated hop-count or LOC metric.
**Why:** This keeps the Filters package deterministic and shippable without expanding the Metrics contract mid-sprint. If a later milestone adds explicit hop-count or LOC inputs, the evaluators can swap to those metrics without changing the public `FilterVerdict` shape.

### 2026-04-29T21:22:50.867-04:00: simplicity.json partial override policy
**By:** Link
**What:** Issue #10 introduces `simplicity.json` for team-specific TCA inputs and filter thresholds. Treat `simplicity.json` as a partial override file: any omitted supported property falls back to the documented default, while unsupported properties fail validation with a clear error.
**Why:** This keeps first-run customization lightweight for teams that only need to tune one or two inputs, while protecting the CLI from silent typos and drift between the schema and runtime behavior.

### 2026-04-29T21:22:50.867-04:00: TCA calculation boundary
**By:** Trinity
**What:** `SimplicityTools.Tca` keeps the cost math pure by accepting explicit `SimplicitySnapshot`, `FilterVerdict` values, and `TcaInputs` assumptions. It does not read `simplicity.json` directly.
**Why:** This keeps the core package deterministic and testable while leaving environment-specific configuration loading in the CLI layer.

### 2026-04-29T21:22:50.867-04:00: TCA review verdict
**By:** Tank
**What:** Rejected Trinity's issue #11 TCA calculator revision for another pass. The five category formulas in `TcaEstimate` line up with the Milestone 2 spec, but the regression suite only proves one happy-path fixture. Revision ownership moves to Switch for the next cycle under reviewer lockout. Needed coverage: culture-invariant executive-summary formatting and failure behavior when one of the required filter verdicts is missing.
**Why:** This package produces book-facing and CLI-facing money summaries. Before approval, tests need to prove these edge cases with executable evidence instead of confidence.

### 2026-04-30T01:40:30Z: TCA calculator rereview approved
**By:** Tank
**What:** Switch's revision closes the two gaps from the prior rejection without needing further production changes. `TcaEstimateTests.Create_ThrowsWhenARequiredFilterVerdictIsMissing` now proves the required-filter failure path for `PrimaryPathFirst`. `TcaEstimateTests.ToExecutiveSummary_UsesSpecifiedFormat_IndependentlyOfCurrentCulture` now proves culture-invariant money formatting under `fr-FR`. `dotnet test tests/SimplicityTools.Tca.Tests/SimplicityTools.Tca.Tests.csproj --nologo` passed locally (4 tests, 0 failures).
**Why:** The regression bar is now met for both the calculator contract and the book/CLI-facing summary output. Issue #11 approved for closure.
