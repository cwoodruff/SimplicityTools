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

# Tank Decision — Integration Wave 3

- **Date:** 2026-04-30T06:57:15.306-04:00
- **Decision:** Use a process-level xUnit performance gate in `tests/SimplicityTools.Cli.Tests` and a separate BenchmarkDotNet harness in `tests/SimplicityTools.Benchmarks` for issue #26.
- **Why:** The repo had sample integration coverage and baseline-tolerance checks already, but it lacked a persistent performance harness and no existing workflow enforced the 5-second budget. This is the narrowest addition that both exposes benchmark evidence and makes the existing `dotnet test` build fail when the threshold regresses.
- **Impact:** No GitHub Actions workflow change was needed. Any CI path that already runs `dotnet test SimplicityTools.sln --nologo` now picks up the p95 gate, and the benchmark project remains available for deeper runtime inspection.

### 2026-04-30T22:15:00Z: Sprint 4 Milestone 4 analyzer package rereview
**By:** Tank
**What:** Approved. Trinity's revision closes the prior publish blocker for the analyzer package. The package now ships under `analyzers/dotnet/cs/`, emits `warning SF0001` in downstream consumers, and is validated by `AnalyzerPackageValidationTests.PackedAnalyzerPackage_UsesAnalyzerLayout_AndReportsDiagnosticsInConsumer` and `.github/workflows/nuget-publish.yml` gates.
**Why:** The first packaging attempt installed cleanly but emitted zero diagnostics in consumers. This revision proves the packaged analyzer actually loads and fires the expected diagnostic before publish is approved.

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

### 2026-04-29T21:22:50.867-04:00: Diff output should teach the next step
**By:** Link
**What:** `dotnet-simplicity diff` should always print the baseline file path, baseline/current snapshot dates, metric deltas, filter score deltas, and explicit regression bullets. If the baseline file is missing, the CLI should fail with a direct instruction to run `dotnet simplicity baseline <solution.sln>` first.
**Why:** Diff is both a CI gate and a first-run learning surface. Teams need the command to explain what changed and what to do next without digging through docs or guessing why the build failed.

### 2026-04-29T21:22:50.867-04:00: Watch command self-loop guard
**By:** Link
**What:** `dotnet-simplicity watch` should run an initial snapshot immediately, then re-run analysis after a 500ms debounce for source-level changes under the solution root. The watcher should ignore generated and tooling-owned paths (`bin`, `obj`, `.git`, `.vs`, and `simplicity-report`) and only warn once while `simplicity.json` remains missing.
**Why:** A live CLI that retriggers itself on analyzer/build output or repeats the same missing-config warning on every save turns feedback into noise. This guard keeps watch mode useful in the first five minutes while still reacting to real code and config edits.

### 2026-04-30T06:57:15.306-04:00: Trend history contract
**By:** Link
**What:** Treat each `*.json` file under the solution-root `.simplicity-history/` directory as a serialized `SimplicitySnapshot`, order the files by `CollectedAt`, and layer the current report snapshot on top when rendering HTML trends.
**Why:** This keeps the trend input format aligned with the existing snapshot JSON shape instead of inventing a second history schema. The report can stay zero-config on the first run, teach teams how to unlock trends, and upgrade automatically once at least two historical snapshots exist.

### 2026-04-30T06:57:15.306-04:00: Sprint 3 Launch: Roslyn Analyzers + Code Fixes
**By:** Morpheus
**What:** Sprint 3 delivers the complete Roslyn analyzer suite (SF0001–SF0007, 7 diagnostics) and two code fix providers (SF0001, SF0002). This milestone completes the IDE integration layer, enabling real-time architectural feedback. The sprint also adds trend analysis to the HTML report and comprehensive integration testing with performance baselines. 11 open issues in Milestone 3 organized in three waves: Wave 1 (Ready Now) includes Switch → Analyzers #16–#22 (7 independent diagnostics, parallelizable) and Link → Trend Analysis #25 (parallelizable); Wave 2 (After #16/#17 complete) includes Link → Code Fixes #23–#24; Wave 3 (After Waves 1 + 2 complete) includes Tank → Integration Testing + Performance Validation #26. Critical path: #16–#22 (~3–4 days) → #23–#24 (~2–3 days) → #26 (~1–2 days). Total: ~6–9 days.
**Why:** Wave structure enforces implementation order while maximizing parallelism. Seven analyzers are semantically independent and can parallelize. Code fixes serialize after their corresponding analyzers are design-complete. Integration testing serves as the final quality gate before closing Sprint 3. This structure keeps forward motion while enforcing quality gates and baseline tolerances.

### 2026-04-30T06:57:15.306-04:00: SF0002 package-usage truth stays compiler-backed
**By:** Switch
**What:** SF0002 should only diagnose `<PackageReference>` items that map to compile-time metadata references. The analyzer parses the project file, maps package IDs to referenced assemblies by normalized NuGet package path, and marks a package as used only when Roslyn binding resolves symbols or types from those assemblies in C# source.
**Why:** This keeps the warning tied to what the compiler can actually see instead of namespace-string guesses. It also avoids false positives for build-only or analyzer-only packages that contribute no compile assets.

### 2026-04-30T06:57:15.306-04:00: SF0007 primary-path baseline is explicit, not circular
**By:** Switch
**What:** SF0007 treats primary-path files as `[PrimaryPath]`-annotated files when any annotation exists; otherwise it falls back to the existing directory conventions (`Controllers`, `Endpoints`, `Handlers`, `Pages`). It does not use inbound-reference percentile fallback to define the comparison set for this analyzer.
**Why:** Using inbound references to define the primary-path baseline for an over-reference diagnostic would be circular and noisy. The analyzer needs a stable comparison set that developers can explain and intentionally shape.

### 2026-04-30T06:57:15.306-04:00: Sprint 3 Analyzer Wave 1 Rejection Notice
**By:** Tank
**Verdict:** Rejected for revision
**Revision owner:** Trinity

## What I checked
- Local implementation for SF0001-SF0007 in `src/SimplicityTools.Analyzers/`
- Analyzer regression suite in `tests/SimplicityTools.Analyzers.Tests/`
- Local validation: `dotnet build src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj --nologo`
- Local validation: `dotnet test tests/SimplicityTools.Analyzers.Tests/SimplicityTools.Analyzers.Tests.csproj --nologo`

## Why this is rejected
1. **SF0005 is diagnosing structs even though issue #20 scopes the rule to classes.** `ConstructorParameterCountAnalyzer` explicitly analyzes both `TypeKind.Class` and `TypeKind.Struct`, so an 8-parameter struct primary constructor produces SF0005. That is a false positive against the stated analyzer contract.
2. **SF0007 suppresses conventional primary-path files even after annotations take over the baseline.** Team decision says `[PrimaryPath]` annotations define the primary-path set when any annotation exists. The analyzer uses annotations for the baseline, but still exempts any file in `Controllers/`, `Endpoints/`, `Handlers/`, or `Pages/` from diagnostics. In mixed mode, an over-referenced conventional controller can slip through with no warning.
3. **The current tests do not cover either failure mode.** Existing analyzer tests are happy-path plus one basic negative case per rule. They do not protect against the struct false positive or the mixed annotated/conventional primary-path scenario.

## Required revision bar
- Narrow SF0005 to classes only and add regression coverage proving structs are ignored.
- Fix SF0007 mixed-mode behavior so conventional folders are only treated as primary-path files when no annotations exist anywhere in the compilation.
- Add regression tests for both cases before resubmitting.

### 2026-04-30T06:57:15.306-04:00: Sprint 3 Analyzer Wave 1 Rereview Approved
**By:** Tank
**Verdict:** Approved
**What:** The revised analyzer artifact clears both prior rejection points.

1. **SF0005 is back inside scope.** `ConstructorParameterCountAnalyzer` now exits unless the named type is a source `TypeKind.Class`, so 8-parameter structs no longer get warned.
2. **SF0007 baseline is now explicit when annotations exist.** `NonPrimaryPathOverReferencedAnalyzer` builds the comparison set from `[PrimaryPath]`-annotated files whenever any annotation exists, and conventional `Controllers/Endpoints/Handlers/Pages` files are treated as supporting files in that mixed mode.

**Evidence:**
- Analyzer-only rereview harness passed: 16 tests, 0 failures
- Focused rerun for the prior rejection cases passed: 2 tests, 0 failures
  - `ConstructorParameterCountAnalyzer_DoesNotReportStructPrimaryConstructorAboveThreshold`
  - `NonPrimaryPathOverReferencedAnalyzer_TreatsConventionalFilesAsSupportingWhenAnnotationsExist`

**Why:** The earlier contract breaks are now covered by executable regressions instead of optimistic prose. Issues #16-#22 approved for closure.

### 2026-04-30T06:57:15.306-04:00: Tank review — Sprint 3 code fixes (#23, #24)
**By:** Tank
**Verdict:** Rejected for revision
**Revision owner:** Trinity

#### Evidence

- Baseline validation passed:
  - `dotnet build src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj --nologo`
  - `dotnet test tests/SimplicityTools.Analyzers.Tests/SimplicityTools.Analyzers.Tests.csproj --nologo`
  - Result: 18 analyzer/code-fix tests passed locally.
- Focused scratch validation for SF0002 passed:
  - Removing an unused multiline `PackageReference` still produced preview operations and valid XML after rewrite.
- Focused scratch validation for SF0001 failed the contract:
  - Scenario: `ICheckoutPricer : IPricer`, `DefaultPricer : ICheckoutPricer`, and a caller typed to `ICheckoutPricer`.
  - Applying `SingleImplementationInterfaceCodeFixProvider` to `IPricer` removed `IPricer`, left `ICheckoutPricer` in place, and stripped the inherited `Price()` member path.
  - Result: the updated project no longer compiled because callers typed to `ICheckoutPricer` lost access to `Price()`.

#### Required revision

SF0001 is not approval-ready. The code fix must either:

1. refuse to offer the fix when dependent interfaces still rely on the target interface contract, or
2. rewrite the dependent-interface chain safely and prove the result still compiles.

Add a regression that covers the dependent-interface scenario so this bug does not come back.


### 2026-04-30T06:57:15.306-04:00: Trinity decision — SF0001 code-fix revision
**By:** Trinity
**Scope:** Sprint 3 issues #23 and #24, focused on the SF0001 code fix.
**Decision:** When removing a single-implementation interface that sits at the base of a source interface chain, the code fix must preserve downstream compileability by copying the removed interface's members into each direct dependent interface before dropping the base-interface reference.
**Why:** Rejecting the fix whenever a dependent interface exists would leave the diagnostic without a safe remediation path for a common Roslyn case. Rewriting only consumer types is not sufficient, because callers typed to the dependent interface still need the inherited member surface after the base interface is removed.
**Notes:** Keep SF0002 package-removal behavior unchanged unless a concrete defect appears. Preserve explicit-interface implementation cleanup so implementations become callable through the surviving surface.

### 2026-04-30T06:57:15.306-04:00: Tank rereview — Sprint 3 code fixes (#23, #24) — APPROVED
**By:** Tank
**Revision author:** Trinity
**Verdict:** Approved
**Why:** The prior SF0001 rejection point is resolved: removing `IPricer` now preserves the dependent-interface chain by copying inherited members onto `ICheckoutPricer` and rewriting explicit `IPricer.Price()` implementations to public concrete members. SF0002 still holds up. Added a multiline `PackageReference` regression and the code fix removed the unused dependency without breaking XML shape or preview/apply behavior.
**Evidence:**
- `dotnet build src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj --nologo`
- Focused reruns for `SingleImplementationInterfaceCodeFixProvider_PreservesDependentInterfaceChains`, `UnusedDependencyCodeFixProvider_RemovesPackageReferenceWithPreviewSafeRewrite`, and `UnusedDependencyCodeFixProvider_RemovesMultilinePackageReferenceWithoutBreakingXml`
- `dotnet test tests/SimplicityTools.Analyzers.Tests/SimplicityTools.Analyzers.Tests.csproj --nologo --no-build` → 20 tests passed
# README Positioning Decision

**By:** Link (DevRel)  
**Date:** 2026-04-30T08:24:49.761-04:00  
**Status:** Ready for merge to decisions.md

## What

Rewrote `README.md` as a GitHub repository landing page that speaks to both engineering teams and stakeholders.

## Structure

1. **Opening hook** — Value statement + problem framing (2 paragraphs)
2. **The Problem** — Why complexity matters to teams (3 short questions)
3. **What You Get** — Five tools table with use-case context
4. **Zero-Config First Run** — Prove the zero-config promise with three inline commands
5. **Build Into Your Workflow** — CI baseline/diff, live watch mode
6. **What Gets Measured** — Four categories: structural, code, verdicts, cost
7. **Analyzer Code Fixes** — Surface SF0001 and SF0002 as immediate value
8. **For Developers** — Install path, build path, library usage, link to full guide
9. **For Stakeholders** — Cost framing, use cases, ROI story
10. **Project Structure** — Directory tree + tests location
11. **Key Design Decisions** — Five bullet points on zero deps, zero config, self-contained reports, teaching approach, validation
12. **Next Steps** — Four clear calls-to-action

## Key Claims (All grounded in shipped behavior)

- Six CLI commands: `analyze`, `report`, `baseline`, `diff`, `budget`, `watch` ✓
- Zero config + sensible defaults ✓
- Self-contained HTML reports ✓
- Seven analyzers (SF0001–SF0007) ✓
- Two code fixes (SF0001, SF0002) ✓
- Three filter verdicts (TwoAmTest, HalfRule, PrimaryPathFirst) ✓
- TCA cost model (team size, salary, incidents, on-call rate, attrition) ✓
- Two sample solutions (Simplified: 2 projects; OverEngineered: 12 projects) ✓

## Why

The current README was a three-line stub pointing at docs. This failed the landing-page test:
- Didn't surface the value proposition
- Didn't explain *what* SimplicityTools does or *why*
- Didn't distinguish between developer use cases and stakeholder communication
- Assumed visitors already knew what "Simplicity-First" meant

The new README:
- Opens with a problem statement that resonates across roles
- Foregrounds the zero-config promise and five-tool ecosystem
- Separates developer onboarding from stakeholder cost/benefit messaging
- Keeps the full guide link but makes the README itself scannable and complete
- Frames TCA and filter verdicts as the "why" that metrics alone don't provide

## When to Merge

After next review cycle or immediately if no additional context changes README positioning.

### 2026-04-30T14:13:05.628-04:00: Packaging & DX Assessment — NuGet + Global Tool
**By:** Link
**What:** Recommendation to publish SimplicityTools on two channels: (1) SimplicityTools.Cli as a .NET global tool via `dotnet tool install --global`, (2) SimplicityTools.Analyzers, Metrics, Filters, Tca as NuGet packages. No architectural changes needed; gaps are documentation (install badges, PrivateAssets callout in examples).
**Why:** CLI already configured with `PackAsTool=true`; this is the ecosystem's standard distribution for both tools and libraries. Global tool delivers zero-config first-run. Analyzer auto-load pattern is low-friction for IDE integration. Library distribution enables custom tooling.

### 2026-04-30T14:13:05.628-04:00: Packaging Strategy — Three Independent NuGet Distributions
**By:** Morpheus
**What:** Ship three decoupled NuGet packages: (1) SimplicityTools.Cli as global tool, (2) SimplicityTools.Analyzers as standalone analyzer, (3) SimplicityTools.Metrics/Filters/Tca as cohesive library stack. Library packages keep versions in sync; CLI and Analyzer version independently.
**Why:** Flexible adoption path: CI/CD uses tool, IDE uses analyzer, custom tooling uses libraries. Decoupled versioning allows faster iteration on analyzer rules without blocking tool releases. Multiple audiences already documented in README and implied by five-package plan.

### 2026-04-30T16:59:28.031-04:00: Packaging Rollout — Four Milestones
**By:** Morpheus
**What:** Execute packaging in four sequential milestones: M4 (metadata, CI/CD, versioning; issues #27–#29), M5 (NuGet libraries Metrics/Filters/Tca/Analyzers; #30–#34), M6 (CLI global tool, validation, dry-run; #35–#38), M7 (packaging UX and docs; #39–#44). Five packages total: four core libraries versioned together, CLI versioned independently. All libraries use SemVer tagged on main; CI/CD reads tags and builds .nupkg. Analyzer package uses PrivateAssets=all to avoid transitive runtime dependency.
**Why:** Strict milestone sequencing prevents blocked parallelism and speculative work. M4 gates all packaging; M5 gates CLI; M6 gates documentation. Metadata-first approach ensures proper .nuspec, license, icon, docs URLs from day one. PrivateAssets=all keeps consumer library graphs clean. Decoupled CLI versioning allows independent release cadence. Zero-config promise validated in M6 before any production publish. M7 can run parallel to M6; go/no-go gate after M6 dry-run.
# 2026-04-30T17:29:31.278-04:00: Sprint 4 package release grouping

**By:** Link

**Decision:** Package releases will be cut as three SemVer tag families: `libraries/vX.Y.Z` for `SimplicityTools.Metrics`, `SimplicityTools.Filters`, and `SimplicityTools.Tca`; `analyzers/vX.Y.Z` for `SimplicityTools.Analyzers`; and `cli/vX.Y.Z` for `SimplicityTools.Cli`.

**Why:** The three reusable libraries form one public API line and need to stay in lockstep, while the analyzer package and global tool need room to ship on their own cadence. Encoding that split in tag names makes the GitHub Actions release workflow readable, keeps dry-run packaging simple on branch pushes, and gives contributors a clear answer for “which version do I cut next?”.

**Packaging note:** All five packages share the repo README, the MIT license expression, the docs URL, and a single NuGet icon so the first NuGet page mirrors the same product story as the repository landing page.
---
date: 2026-04-30T17:29:31.278-04:00
author: Morpheus
decision: Sprint 4 Launch — Package Foundation (Milestone 4)
---

# Sprint 4 Launch: Package Foundation

**Decision Date:** 2026-04-30T17:29:31.278-04:00

## Context

Sprint 4 launches Milestone 4: Package Foundation. Three issues total, all assigned to Link (DevRel).

**Branch:** `sprint/4-package-foundation` — created from origin/main, pushed to origin.

**Scope:** Foundation for NuGet and global tool packaging: .nuspec metadata, CI/CD pipeline, versioning strategy.

## Issue Breakdown

| Issue | Title | Assignee | Type | Dependency |
|-------|-------|----------|------|-----------|
| #32 | Setup .nuspec metadata for all packages | Link | Infrastructure | None — Wave 1 |
| #33 | Setup GitHub Actions CI/CD for NuGet publish | Link | Infrastructure | None — Wave 1 (parallel with #32) |
| #34 | Document versioning strategy and release process | Link | Documentation | #32, #33 — Wave 2 |

## Wave Structure

**Wave 1 (Ready Now):**
- Link → #32 (Setup .nuspec metadata)
- Link → #33 (Setup GitHub Actions CI/CD)
- **Why:** Both are foundational infrastructure tasks with no inter-dependency. Metadata defines what gets packaged; CI/CD pipeline orchestrates the publish. Can proceed in parallel.

**Wave 2 (After #32 + #33 complete):**
- Link → #34 (Document versioning strategy)
- **Why:** Documentation requires understanding the concrete metadata structure (from #32) and the CI/CD flow (from #33) to provide accurate instructions.

## Critical Path

#32 → #34 and #33 → #34. All work serializes through documentation, which is the final gate before packaging pipeline moves to Milestone 5.

## Reasoning

**Three issues only.** Milestone 4 is the smallest foundation phase: metadata setup, pipeline infrastructure, and release documentation. It unblocks Milestones 5–7 (library packaging, global tool, and UX).

**Link owns all three.** DevRel (Link's charter) encompasses package metadata, CI/CD usability, and release documentation. Link has context from Milestone 3 completion and understands the zero-config promise that drives packaging strategy.

**No speculative work.** Each issue has a concrete, measurable deliverable. #32 produces .nuspec files; #33 produces a GitHub Actions workflow; #34 produces CONTRIBUTING.md + release documentation.

**Wave 1 parallelization is aggressive but safe.** Metadata and CI/CD are independent concerns; Link can context-switch between them without blocking. Once both are done, documentation becomes trivial (summarizing decisions made in #32/#33).

## DoD

- #32: All five packages (.csproj or .nuspec) have complete metadata; PrivateAssets=all is set on analyzer; `dotnet pack` runs without warnings.
- #33: GitHub Actions workflow builds on push, runs tests, generates .nupkg, includes dry-run validation; workflow passes locally.
- #34: CONTRIBUTING.md has release section; versioning strategy documented; local test-publish instructions included.

**Integration Test:** After all three close, verify `dotnet pack` works for all packages and workflow dry-run produces valid .nupkg files (no publish).

## Next Gates

- **M4 → M5 Gate:** M4 must complete before Trinity begins M5 (library packaging). M4 establishes the metadata schema and CI/CD foundation that M5 builds upon.
- **Coordinator Action:** When M4 closes, promote M5 issues to "ready" and spawn Trinity for Wave 1 (package four libraries).

## Signed Off

Morpheus, 2026-04-30T17:29:31.278-04:00

## Sprint 4 Foundation Review — Tank Verdict

- **Date:** 2026-04-30T17:29:31.278-04:00
- **Branch:** `sprint/4-package-foundation`
- **Scope reviewed:** Milestone 4 issues #32, #33, #34
- **Verdict:** **REJECTED**
- **Revision owner:** **Trinity**

### Why rejected

1. `SimplicityTools.Analyzers.0.4.0-local.nupkg` is packed as a normal library (`lib/net10.0/SimplicityTools.Analyzers.dll`) instead of an analyzer package layout (`analyzers/dotnet/cs/...`). That means the published analyzer package will not execute diagnostics for consumers.
2. Tank verified the failure path with a repo-local scratch consumer: after `dotnet add package SimplicityTools.Analyzers --version 0.4.0-local --source ../../packages`, a build of a single-implementation-interface fixture completed with **0 warnings**, so SF0001 never loaded.
3. The new workflow validates metadata presence and package creation, but it does not validate package usability. In its current form it would greenlight a broken analyzer release.

### Evidence

- `dotnet build SimplicityTools.sln --nologo --verbosity minimal` ✅
- `dotnet test SimplicityTools.sln --nologo --no-build --verbosity minimal` ✅
- Local `dotnet pack` for all five publishable projects ✅
- Local `dotnet tool install SimplicityTools.Cli --tool-path ... --add-source artifacts/tank-review/packages --version 0.4.0-local` ✅ and `dotnet-simplicity analyze samples/Sample.Simplified/Sample.Simplified.sln` ran successfully
- Analyzer consumer validation ❌: packaged analyzer produced **0 warnings** in a scratch consumer build

### Required revision

- Repack `SimplicityTools.Analyzers` so the analyzer assembly is included in the analyzer package path Roslyn actually consumes.
- Add release-validation coverage that proves a consuming project loads the packaged analyzer and emits at least one expected diagnostic before approving publish readiness.

### 2026-04-30T17:29:31.278-04:00: Analyzer packaging repacked per Tank revision
**By:** Trinity
**What:** `SimplicityTools.Analyzers` must pack as a Roslyn analyzer package by suppressing normal `lib/` output and placing the analyzer assembly under `analyzers/dotnet/cs/`.
**Why:** The rejected package installed cleanly but behaved like a normal library, so downstream consumers emitted zero Simplicity diagnostics. Publish validation now has to prove the actual consumer contract: restore the packed analyzer into a scratch project and confirm `SF0001` fires.
**Validation:** Package validation must inspect the `.nupkg` for `analyzers/dotnet/cs/SimplicityTools.Analyzers.dll` and fail if `lib/net10.0/SimplicityTools.Analyzers.dll` is present. Consumer validation must reference the package normally (no `PrivateAssets="all"`) and build from a repo-root artifact path.

### 2026-04-30T19:09:43.583-04:00: Sprint 5 Launch — Release Packaging (Milestone 5)
**By:** Morpheus
**What:** Sprint 5 launches Milestone 5: Release Packaging — the final upstream gate before the toolkit moves to distribution and global tool delivery. Branch `sprint/5-release-packaging` created from origin/main. Five issues assigned per critical-path wave structure: Wave 1 has Trinity on #35 (Package Metrics) and Switch on #38 (Package Analyzers) in parallel; Wave 2 routes Trinity to #36 (Filters) after #35 completes; Wave 3 routes Trinity to #37 (Tca) after #36 completes; Wave 4 routes Tank to #39 (Integration Validation) after all four packages complete. Critical path: #35 → #36 → #37 → #39. Parallel: #38 with #35.
**Why:** Packages all four core libraries (Metrics, Filters, Tca, Analyzers) with complete metadata, dependency graphs, and integration validation. This milestone establishes the NuGet and IDE distribution foundation. Each issue has one-to-one mapping to NuGet targets. Trinity owns the Metrics → Filters → Tca dependency chain; Switch owns Analyzers in parallel (self-contained); Tank validates all four together. No speculative work—each issue has concrete deliverable with strict DoD.

### 2026-04-30T19:09:43.583-04:00: Milestone 5 release gate rejection
**By:** Tank
**What:** Reject Milestone 5 release approval until `.github/workflows/nuget-publish.yml` is repaired. The analyzer-consumer validation script in that workflow calls `ET.fromstring(...)` but never imports `xml.etree.ElementTree as ET`, so the CI gate fails with `NameError` even though the packed Metrics, Filters, Tca, and Analyzers artifacts themselves validate locally.
**Why:** Release confidence for this milestone depends on the workflow proving the same package contracts we checked by hand. Local package-validation tests and a repo-local all-package consumer build are strong evidence, but they do not override a broken publish gate; if CI cannot execute the analyzer validation step, the branch/tag path is not releasable. Reassign the workflow revision to **Link** under reviewer lockout for the failing artifact.

### 2026-04-30T19:09:43.583-04:00: Analyzer package release contract
**By:** Switch
**What:** `SimplicityTools.Analyzers` should ship as a development-only analyzer package: target `netstandard2.0` for Roslyn host compatibility, pack only under `analyzers/dotnet/cs/`, and suppress nuspec dependency groups so consumers get diagnostics/code fixes without compile-time package assets.
**Why:** Sprint 4 proved the layout contract, but releasable analyzer packages still need the Roslyn host contract and a clean consumer asset graph. The combination of `DevelopmentDependency=true`, `SuppressDependenciesWhenPacking=true`, and consumer validation against `project.assets.json` closes the remaining release gap and removes the NU5128 warning that came from pretending the package had normal framework dependencies.

### 2026-04-30T19:09:43.583-04:00: Metrics package validation shape
**By:** Trinity
**What:** `SimplicityTools.Metrics` package validation should prove three things together: the packed `.nupkg` contains the repo-level README/icon metadata assets, the `lib/net10.0/` payload is limited to the library DLL plus XML docs, and a downstream consumer can restore/build against the packed package from a repo-local folder source.
**Why:** Local `dotnet pack` success alone does not prove the shipped asset set is clean for consumers. Checking the actual `.nupkg` contents and then compiling a fresh consumer against that package catches missing docs, accidental extra assemblies, and package-shape regressions before the Filters/Tca packages copy the same pattern.

### 2026-04-30T19:52:08.101-04:00: Milestone 5 workflow rereview approved
**By:** Tank
**Verdict:** Approved
**What:** Approve `.github/workflows/nuget-publish.yml` for Milestone 5 release gating. The analyzer-consumer validation block now imports `xml.etree.ElementTree as ET` before calling `ET.fromstring(...)`, and it clears `artifacts/analyzer-consumer-validation` before recreating the package source, global package cache, and consumer workspace.
**Why:** The prior blocker was a workflow execution failure, not a package-shape failure. Independent rerun proof now shows the gate can parse the analyzer nuspec, validate the packaged analyzer contract, build a fresh consumer that emits `warning SF0001`, and avoid false passes from stale validation state on repeated executions.
**Evidence:**
- `dotnet restore SimplicityTools.sln --verbosity minimal`
- `dotnet build SimplicityTools.sln --configuration Release --no-restore --verbosity minimal`
- `dotnet test tests/SimplicityTools.Analyzers.Tests/SimplicityTools.Analyzers.Tests.csproj --configuration Release --no-build --verbosity minimal --filter FullyQualifiedName~AnalyzerPackageValidationTests`
- `dotnet pack src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj --configuration Release --no-build --output artifacts/packages -p:Version=0.4.0-ci.tankreview --verbosity minimal`
- Two end-to-end reruns of the workflow's analyzer-consumer validation logic against the packed nupkg both passed, both emitted `warning SF0001`, and the second run removed an injected stale sentinel before rebuilding.

### 2026-04-30T21:27:33.453-04:00: Sprint 6 kickoff and routing
**By:** Morpheus
**What:** Start Milestone 6 on `sprint/6-global-tool-packaging` with Link owning #40 and #42 in Wave 1, Tank owning #41 after local tool install is proven, and Link closing with #43 after validation and docs converge.
**Why:** The global tool package contract is the only true upstream dependency in this milestone. Keeping #40 as the contract task preserves the zero-config first-run promise, lets documentation move in parallel without speculative abstraction, and keeps release dry-run work gated on evidence instead of hope.


### 2026-04-30T21:27:33.453-04:00: GitHub wrap-up for Sprint 5 & 6 complete
**By:** Morpheus
**What:** Executed GitHub wrap-up for Sprint 5 (completed earlier) and Sprint 6 (completed today). Sprint 5 issues #35–#39 were pre-closed; Sprint 6 PR #64 was created and merged, closing issues #40–#43; both Milestone 5 and Milestone 6 are closed.
**Why:** Provides clean GitHub state transition: both sprints are fully closed with no orphaned PRs or open issues. Sprint 5 (NuGet Library Packages) and Sprint 6 (Global Tool Packaging) are complete. Ready to proceed with Milestone 7.


### 2026-04-30T22:22:13-04:00: Sprint 7 Wrapup – Packaging UX & Documentation Complete
**By:** Morpheus
**What:** Sprint 7 completed all six packaging and documentation issues (#44–#49) and closed Milestone 7. PR #65 created with 10 commits (+1934/−847 lines, 16 files). Content includes NuGet badges, quickstart guide, library integration docs, troubleshooting, and CI/CD examples. PR merge is blocked on GitHub CI validation (NuGet packages workflow still running, started 2026-05-01T02:08:28Z, currently at step 7 of 11). PR is technically mergeable but workflow completion is required for safe shipping.
**Why:** Validates that SimplicityTools uses a sprint-branch-to-main model: feature work lives on ephemeral sprint branches (e.g., `sprint/7-packaging-ux-documentation`), branches track main, and each sprint branch merges via PR after completion. Milestone close precedes PR creation; issues close before merge. This pattern simplifies merge semantics for single-contributor or tightly-coordinated teams with strong CI validation.
**Action Items:** (1) Monitor PR #65 validation; merge with squash strategy once workflow succeeds. (2) If validation fails, review logs and determine if fixable or if new sprint work needed. (3) Post-merge, update `.squad/identity/now.md` to reflect completion and plan Milestone 8.
**Decision:** No changes to workflow or branching model. Current sprint-to-main pattern is working as designed. CI validation delays are expected and necessary for package correctness.
