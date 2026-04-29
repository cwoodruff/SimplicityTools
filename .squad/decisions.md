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
