# Squad Decisions Archive

## Active Decisions (Archived 2026-05-01, Entries from 2026-04-29)

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
### 2026-05-02T06:43:28.375-04:00: Tank rereview — NuGet workflow dispatch validation routing
**By:** Tank
**Revision author:** Morpheus
**Verdict:** Approved

**What I validated:**
- Replayed the current `.github/workflows/nuget-publish.yml` release-shape step locally across the workflow-dispatch matrix.
- Confirmed `release_group=validation` now emits `0.4.0-ci.<run-number>` even when `version` is non-empty or stale.
- Confirmed `libraries`, `analyzers`, and `cli` still accept explicit SemVer overrides, still fall back to `Directory.Build.props` when blank, and keep invalid explicit versions blocked.
- Built and packed the affected package sets locally; package versions and `.snupkg` pairing matched expectations, and the CLI package installed successfully from the locally packed feed.
- Ran the existing package validation suites in `tests/SimplicityTools.{Metrics,Filters,Tca,Analyzers}.Tests` successfully.

**GitHub status note:**
- The currently deployed workflow on `main` is still not the approved replacement. Run `25250085225` reproduces the old `workflow_dispatch` validation-group failure.
- Push run `25240574498` on the same deployed SHA fails later on `Sample.Simplified` startup coverage, which is unrelated to the dispatch-routing fix.

**Why approved:**
The replacement fix addresses the reported bug and preserves the release-group behaviors that matter. The remaining lack of a green GitHub run is a deployment/proof gap, not a defect in Morpheus's rewritten dispatch logic.

---

### 2026-05-02T06:43:28.375-04:00: NuGet workflow validation dispatch revision
**By:** Morpheus

**Decision:** In `.github/workflows/nuget-publish.yml`, resolve `workflow_dispatch` intent by `release_group` first and normalize `validation` runs to an empty effective version before any release-version guard logic executes.

**Why:** GitHub keeps prior dispatch input values in the UI. If validation routing keys off a non-empty `version` field, a stale value can incorrectly force the release-only path and fail the run before build validation starts. Normalizing validation input preserves zero-config validation while keeping `libraries`, `analyzers`, `cli`, and tag-triggered releases on their existing contracts.

**Implications:**

- `release_group=validation` always emits `<SimplicityToolsReleaseVersion>-ci.<run-number>` packages.
- A populated validation `version` input is ignored and surfaced as a notice, not an error.
- `libraries`, `analyzers`, and `cli` still accept an explicit SemVer override or fall back to `SimplicityToolsReleaseVersion`.
- Tag releases remain authoritative for publish behavior.

---

### 2026-05-02T06:43:28.375-04:00: Validation dispatch ignores optional version
**By:** Morpheus
**What:** The NuGet release pipeline must treat `release_group=validation` as a validation-only path even when the GitHub Actions form still contains a `version` value. Only `libraries`, `analyzers`, and `cli` may consume the version input for upload-ready builds; when those groups omit a version, the workflow falls back to `SimplicityToolsReleaseVersion` from `Directory.Build.props`.
**Why:** The failing GitHub run proved the old resolver let a stale UI field push validation into the versioned-build branch, which breaks the zero-config validation contract for no release value. Ignoring that field on validation keeps the contract obvious, preserves CI-only package suffixes, and avoids making operators debug workflow form state.

---

### 2026-05-02T06:43:28.375-04:00: NuGet validation dispatches ignore stale version input
**By:** Trinity
**What:** The `NuGet release pipeline` workflow should resolve `release_group` before applying workflow-dispatch version rules so `validation` runs always emit the CI-only package version and ignore any optional `version` value still present in the GitHub Actions form.
**Why:** GitHub can retain the prior `version` field between manual dispatches. Without this guard, a user can choose `validation` and still trip the versioned-release gate, which blocks the intended validation path even though no publishable release group was requested.

---

### 2026-05-02T06:43:28.375-04:00: Tank — NuGet workflow validation

**By:** Tank
**Decision:** Reject the current NuGet workflow revision as a fix for the reported validation-group failure.

**Evidence**
- GitHub Actions run `25250085225` on `main` failed in `Resolve release shape` with `requested_group="validation"` and `requested_version="0.4.0"`.
- The current local workflow rewrite still rejects that same tuple with `Workflow dispatch versioned builds must target libraries, analyzers, or cli.`
- The only behavior change I could validate is different blank-version handling for `libraries`, `analyzers`, and `cli`: those now fall back to `SimplicityToolsReleaseVersion` from `Directory.Build.props`.

**Required revision**

1. Decide the contract for `validation` dispatches with a populated `version` field:
   - either accept it and normalize to `<version>-ci.<run-number>`, or
   - make the UI/path impossible or unmistakable by ignoring/clearing `version` when `validation` is selected and tightening the message/docs.
2. Add regression coverage for the release-shape input matrix so these combinations are proven explicitly:
   - `validation` + blank version
   - `validation` + explicit version
   - `libraries|analyzers|cli` + blank version
   - `libraries|analyzers|cli` + explicit version

**Reviewer handoff**

- **Requested revision owner:** Morpheus

---

### 2026-05-01T12:58:06.465-04:00: Sample.Simplified startup should avoid native apphost
**By:** Morpheus
**What:** For `samples/Sample.Simplified/App/App.csproj`, disable native apphost generation and rely on the managed host path (`dotnet` launching the DLL) for local startup.
**Why:** In this repo worktree on macOS, the generated apphost is ad-hoc signed and rejected at launch under Apple integrity enforcement, which kills the sample before `Main()` runs. The DLL executes correctly via `dotnet exec`, so this is a host packaging issue, not an application logic issue. For sample apps, zero-config first run matters more than producing a native launcher.

---

### 2026-05-01T12:58:06.465-04:00: Sample.Simplified startup proof must exercise the real launcher
**By:** Tank
**What:** Treat the Sample.Simplified startup fix as valid only when the executable assembly name avoids the `.App` suffix and both the generated apphost plus `dotnet run --no-build` start cleanly.
**Why:** In-process tests did not cover the failure path. The regression only shows up when the sample is launched the way a developer actually starts it, so the proof has to include a real process launch.

---

### 2026-05-01T12:58:06.465-04:00: Avoid `.App` executable names for macOS-run samples
**By:** Trinity
**What:** Renamed the Sample.Simplified executable assembly from `Sample.Simplified.App` to `Sample.Simplified.Demo` and added a `dotnet run` smoke test so the sample startup path is exercised through the real CLI entry point.
**Why:** On macOS, `dotnet run` was exiting with code 137 during startup while the sample logic itself was healthy. The failure traced to the generated executable name ending in `.App`, which is an unsafe launch target on that platform; using a non-`.App` assembly name keeps the sample runnable and gives us regression coverage.

---

### 2026-05-01T08:00:28.862-04:00: Milestone 8 final deploy closeout boundary
**By:** Morpheus
**What:** Treat issue #61 as the only remaining Milestone 8 item and keep it open until a push to `main` creates `gh-pages` and DNS for `tools.simplicity-first.dev` resolves. There is no separate Wave 5 beyond this production handoff.
**Why:** The repo-side contract is already defined by the custom-domain files, validation gate, and deploy workflow, but GitHub Pages publication and DNS resolution are external production events. Closing early would confuse "merge-ready" with "live and verified."

---

### 2026-05-01T07:37:47.635-04:00: Custom Domain as Canonical Origin & Deploy Gate
**By:** Morpheus  
**Status:** ✅ COMPLETE

The docs site now treats `https://tools.simplicity-first.dev` as the canonical production origin, not the repository subpath. Wave 4 moved the site from bootstrap-on-project-pages mode into custom-domain deployment, requiring canonical URLs, Open Graph metadata, sitemap entries, robots directives, and CNAME all tied to one stable origin. Build validation must pass before any deploy workflow publishes `docs-site/dist/` to `gh-pages`.

Implications:
- Keep site URLs root-relative in the Astro app
- Treat `npm run build:validate` as the release gate for docs-site changes
- Do not mark the custom-domain issue done until `gh-pages` exists and external DNS resolves the domain

**Issue:** #61 (partial — external blockers on DNS/Pages)

---

### 2026-05-01T06:37:49.140-04:00: Site Validation Checklist Pattern Established
**By:** Tank  
**Status:** ✅ COMPLETE

Established a 3-phase site validation checklist for docs-site pull requests to ensure consistent quality as Wave 2 site delivery wraps and Wave 3 additions proceed. Pattern covers: (1) **Build Validation** – `npm run build` zero errors/warnings, dist output, <500ms time; (2) **Structure Validation** – spot-check templates for correct title, header nav, main content, footer grid, breadcrumbs; (3) **Responsive Validation** – hamburger visibility at <960px, full menu at ≥960px, media queries at 720/960px. All Wave 2 acceptance criteria verified: 7 hub pages build cleanly, navigation consistent, responsive design confirmed, footer/CTA structure intact, 3 reusable templates, dark theme + #E31B23 accent applied. Applies to future pages/template changes. Link to pattern added to docs-site CONTRIBUTING section.

**Issue:** #51, #52  

---

### 2026-05-01T06:12:43.398-04:00: PR #65 Merged — Perf-Gate Calibration Complete
**By:** Morpheus (Lead)  
**Status:** ✅ Merged

PR #65 (Sprint 7: Packaging UX & Documentation) merged with perf-gate calibration. Tank determined the original 5-second p95 threshold was too tight for GitHub-hosted ubuntu-latest runners, which average 8.354–9.394s. Fixed with dynamic threshold: 5s local, 10s on GitHub Actions CI. Test method renamed; workflow filter updated. Commit `cec4e47` (2026-05-01T03:15:50Z). Workflow run #25200555636 passed. Milestone 7 complete; closes issues #44–#49.

---

### 2026-05-01T06:12:43.398-04:00: Issue #50 Closeout – Astro Project Setup & GitHub Pages Configuration
**By:** Morpheus  
**Status:** ✅ COMPLETE and CLOSED

Astro project in `docs-site/` is fully bootstrapped. Build scripts (`npm run dev/build/preview`) functional. GitHub Pages configured: `astro.config.mjs` with site URL (cwoodruff.github.io), base-path (/SimplicityTools/), trailing slash enforcement. Directory structure initialized: `src/layouts/`, `src/pages/`, `src/components/`, `src/assets/`. Initial homepage renders; `.nojekyll` tracked for Pages compatibility. Wave 1 complete; Wave 2 now active (issues #51, #52).

---

### 2026-05-01T06:12:43.398-04:00: Wave 2 Site Information Architecture Locks Hub-First Migration Path
**By:** Link  
**What:** Astro site centered on shared base layout plus landing/docs/reference templates; hub pages bridge to existing repository markdown until Wave 3 migrates deeper content into Astro.
**Why:** Wave 2 needed polished first-run experience without blocking full content migration. Hub-first structure gives users coherent homepage, nav, and landing-page story now, while Wave 3 moves command and analyzer content into stable routes instead of revisiting site structure again.
**Outcome:** Added reusable base layout, responsive nav/footer, breadcrumbs, seven top-level Astro pages (home, getting-started, features, pricing, docs, reference, samples). Content adapted from README/docs. Build passed; Wave 3 unblocked.

---

### 2026-04-30T22:09:34.021-04:00: PR #65 CI hang mitigation isolates the CLI performance gate
**By:** Tank
**What:** Split the NuGet publish workflow test phase so the solution-wide `dotnet test` run excludes `SimplicityTools.Cli.Tests`, then run the CLI functional tests and the CLI performance gate in their own named steps with detailed console logging.
**Why:** The repeated "hang" was the workflow going silent inside `SimplicityTools.Cli.Tests` while those tests were also spawning their own `dotnet build`/`analyze` work and competing with the rest of the solution test graph. Isolating the CLI suite keeps the coverage intact, makes the performance gate more honest, and gives Actions logs clear, visible progress instead of a dead-looking test phase.

### 2026-04-30T22:09:34.021-04:00: PR #65 Blocked — CLI Performance Gate Timeout

**By:** Morpheus  
**Status:** BLOCKED

---

## Situation

- Tank's workflow hang fix has been successfully applied to `sprint/7-packaging-ux-documentation` branch
- The fix correctly isolates CLI tests, preventing workflow resource contention
- PR #65 CI now runs cleanly through both test isolation steps
- **BLOCKER:** The CLI performance gate test is failing:
  - Test: `SimplicityTools.Cli.Tests.AnalyzeCommandPerformanceTests.AnalyzeCommand_OverEngineeredSample_CompletesWithinFiveSecondsAtP95`
  - Expected: p95 ≤ 5 seconds
  - Observed: p95 = 9.335 seconds (15 sample runs)
  - Margin: exceeds threshold by 1.87x

---

## What's Working

✓ Workflow no longer hangs (Tank's fix is correct)  
✓ All library tests pass (6 projects, ~45 tests, 2m28s)  
✓ All CLI functional tests pass (16/16, 1m41s)  
✓ Performance gate test runs reliably with full console visibility  
✓ Workflow has clear, honest logs and progress reporting  

---

## Root Cause Analysis

The blocker is **NOT caused by Tank's test isolation fix**. Rather:

- Tank's fix reveals a latent performance issue by giving proper visibility
- The performance gate was not visible in prior workflow runs (hung or cancelled)
- Gate now runs cleanly, surfacing the p95 regression

## Investigation Needed

Determine if the 9.3s vs 5s gap is:
1. **Real code regression** (analyzers or CLI got slower)
2. **CI runner capacity issue** (GitHub runner overloaded during test run)
3. **Threshold miscalibration** (gate was never realistic for CI environment)

---

## Next Steps (Required to Merge)

1. Specialist (recommend Tank) investigates performance baseline vs current
2. Run profiling to identify bottleneck if code regression is confirmed
3. Either:
   - **Option A:** Fix code performance regression, re-run tests → gate passes
   - **Option B:** Adjust threshold with explicit justification and trade-off analysis → gate passes
4. Re-run PR #65 CI validation after resolution

---

## Why This Matters

Milestone 7 DoD includes "zero-config promise maintained" — the performance gate is part of that contract. We cannot merge a PR that fails its defined acceptance criteria without understanding and resolving why.

---

## Decision

**PR #65 remains open and blocked pending performance investigation.**

Recommend: Spawn specialist task to profile CLI performance and determine root cause of gate failure. Escalate to next decision round with findings.

### 2026-04-30T22:09:34.021-04:00: PR #65 CLI performance gate calibration

**By:** Tank
**What:** Keep the CLI analyze p95 gate, but calibrate it by environment: 5s outside GitHub Actions and 10s on GitHub-hosted CI. Update the workflow filter to the renamed threshold-aware test.
**Why:** The PR does not modify CLI or CLI test code; only the workflow changed. Historical GitHub-hosted Ubuntu runs on main and sprint branches consistently report p95 between 8.354s and 9.394s, while local measurement on the same sample is 3.615s p95. That makes the 5s CI threshold a false gate caused by runner constraints, not a PR regression.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

### 2026-04-29T07:32:23.826-04:00: CLI sample baselines stay date-agnostic
**By:** Tank
**What:** Keep `tests/SimplicitySampleBaselines.json` limited to numeric snapshot metrics plus solution-relative paths. CLI analyze tests should derive the expected summary date from actual output instead of storing `CollectedAt` in the baseline file.
**Why:** `CollectedAt` is runtime state, not a product baseline. Keeping the baselines date-agnostic lets the suite catch real metric drift in the samples without false failures every day the CLI runs.

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


### 2026-05-01T05:50:05.727-04:00: Sprint 8 Kickoff — Astro Website
**By:** Morpheus
**What:** Milestone 8 is active on `sprint/8-astro-website` to build the public SimplicityTools website in Astro for GitHub Pages and `tools.simplicity-first.dev`. The work is sequenced as Wave 1 `#50`, Wave 2 `#51` and `#52`, Wave 3 `#55`, `#58`, and `#59`, then Wave 4 `#57`, `#60`, and `#61`.

**Why:** Website delivery is mostly sequential for a single contributor. Locking `#50` as the gate keeps project scaffolding, routing assumptions, and Pages deployment constraints stable before navigation, content, SEO, and deployment polish work begin.

### 2026-05-01T05:50:05.727-04:00: Sprint 8 Wave 1 Complete — Astro Project Setup & Pages Bootstrap
**By:** Link
**What:** Completed issue `#50` on branch `sprint/8-astro-website` by bootstrapping `docs-site/` with a GitHub Pages-ready Astro setup: `astro.config.mjs`, build/dev/preview scripts, starter layouts/pages/components/assets structure, `public/.nojekyll`, and a small local README. Validation covered `npm run build`, `npm run dev`, `npm run preview`, plus the repository `.NET` build/tests.

**Why:** Keeping Astro aligned to the repository Pages path (`/SimplicityTools/`) from day one avoids later routing rework and preserves a clean first-run experience for contributors. This foundation unblocks Wave 2 navigation, layouts, and landing pages without reopening project setup decisions.

### 2026-05-01T07:09:22.214-04:00: Wave 3 Information Architecture Established
**By:** Morpheus  
**Status:** ✅ COMPLETE

Established information architecture for Wave 3 docs-site delivery. Top-level landing pages (`/`, `/getting-started/`, `/features/`, `/pricing/`, `/docs/`, `/reference/`) focus on routing and adoption context. Deep reference material organized into stable, task-shaped sections:
- `/docs/commands/` for CLI command contracts
- `/docs/filters/` for filter interpretation
- `/docs/configuration/` for `simplicity.json`
- `/docs/library-usage/` for programmatic package composition
- `/analyzers/` for SF0001-SF0007
- `/integration/` for IDE, CI/CD, and csproj guidance

**Rationale:** Keeps homepage and hubs readable while making deep docs easy to style, link, and extend in later waves. Matches existing package and command boundaries instead of inventing a second documentation taxonomy.

**Issues:** #55, #58, #59
### 2026-05-01T13:31:28.564-04:00: Preserve Sample.Simplified demo assembly name during project rename
**By:** Tank (Trinity cross-validated)
**What:** Renamed the Sample.Simplified solution projects to `Sample.Simplified.App` and `Sample.Simplified.Tests`, but kept the runnable app assembly name as `Sample.Simplified.Demo` while updating project/file names, namespaces, solution wiring, and launch tests to the new paths.
**Why:** The sample's developer-facing identity should match the solution naming convention, but the macOS startup regression is tied to executable assembly names ending in `.App`. Keeping the namespace/project rename separate from the output assembly name preserves the user-visible rename without reintroducing a launch failure.

---


### 2026-05-01T13:51:44.498-04:00: Docs-site custom domain source of truth and validation gate
**By:** Link & Tank
**What:** Use the repository-root `CNAME` file as the authoritative custom-domain artifact; sync it into `docs-site/public/CNAME` during the deploy workflow; fail docs-site validation if canonical metadata, robots.txt, sitemap.xml, or built HTML still mention the legacy domain `tools.simplicity-first.dev`.
**Why:** The domain cutover to `simplicitytools.dev` is user-facing. Without a single source of truth, deployed artifacts can drift from workflow config and local builds. Validation must catch stale domain references so GitHub Pages publishes only sites that consistently advertise the correct origin.
**Consequences:**
- Astro local builds and GitHub Pages deploys both emit the same CNAME value
- Contributors only update one root CNAME artifact for future domain changes
- The workflow fails fast if configuration drifts from the canonical domain

### 2026-05-01T19:30:22.856-04:00: Release-ready NuGet artifacts without weakening publish gates
**By:** Morpheus

**What:** Updated the NuGet workflow to support **manual workflow_dispatch** runs that build upload-ready artifacts without requiring an automated publish to NuGet.org. Operators can now supply explicit `release_group` (`libraries`, `analyzers`, or `cli`) and SemVer `version` to generate release-ready packages.

**Why:** The repository needed a credible release path for operators to inspect and manually validate artifacts before tagging, without treating every manual workflow run as an automatic publish. This removes the misleading "dry-run" framing while keeping tag pushes as the only automated publish gate.

**Consequences:**
- Tag pushes remain the only automated publish gate to NuGet.org
- Manual workflow_dispatch requires both `release_group` and `version` parameters
- Invalid manual runs (missing version or mismatched group) fail explicitly
- Release artifacts are validated to include matching `.snupkg` files before any push

---

### 2026-05-01T19:30:22.856-04:00: NuGet release workflow publish safety validation
**By:** Tank

**What:** Added explicit artifact validation to the publish job: every downloaded `.nupkg` must match the tagged version, package IDs must match the selected release group exactly, and CI/local placeholder versions are rejected before any push to NuGet.org.

**Why:** The old workflow did not gate publish on artifact identity. Adding publishable-artifact checks turns the workflow into a real release path while keeping NuGet.org safe from accidental CI-version or wrong-group uploads.

**Notes:**
- Push only `.nupkg` files; matching `.snupkg` files are expected alongside them but are not pushed as primary packages
- Validation runs stay allowed on branch pushes via CI-only versions, but release tags are the only publish gate

---

### 2026-05-02T06:08:59.230-04:00: Central release version contract
**By:** Morpheus
**What:** `Directory.Build.props` is now the single editable source of truth for the repo-wide release baseline via `SimplicityToolsReleaseVersion`. Package defaults derive `-local`, branch-validation workflow builds derive `-ci.<run-number>`, workflow dispatch uses the same baseline unless an explicit override is supplied, and the Astro footer reads that property at build time.
**Why:** MSBuild is the native packaging boundary for every publishable project in this repo, so anchoring the version there keeps the contract behind the packaging surface instead of inventing another config file. The website should display the public release line, not a local placeholder, and release prep needs one place to bump before generating artifacts.

---

### 2026-05-02T06:08:59.230-04:00: Shared release version implementation and validation
**By:** Trinity
**What:** Updated `.github/workflows/nuget-publish.yml` to read `SimplicityToolsReleaseVersion` from `Directory.Build.props` as the default for all package builds. Local package defaults emit `-local`, CI validation builds derive `-ci.<run-number>`, and manual workflow_dispatch runs use the baseline unless an explicit override is supplied. The Astro site footer receives the same version via `docs-site/scripts/extract-version.mjs` build-time extraction.
**Why:** One canonical version property eliminates sync burden across three independent package types (libraries, CLI, website) and turns version updates into a single edit point in MSBuild configuration.

---

### 2026-05-02T06:08:59.230-04:00: Shared version source validation approved
**By:** Tank
**What:** Approved the shared version-source implementation after running targeted package validation tests and docs-site build validation. Confirmed that `Directory.Build.props` is read correctly, package defaults emit `-local` versions, and the Astro footer renders the canonical version correctly in the build output.
**Why:** The implementation must not break existing package validation, CLI output, or site rendering before it ships. Targeted tests confirm the contract holds.
**Notes:** A full `dotnet test SimplicityTools.sln` still hits pre-existing `AnalyzeCommandTests` sample-count failures unrelated to the shared version contract.

---

### 2026-05-02T06:08:59.230-04:00: Central Version Source & Website Footer Display
**By:** Link
**What:** Established a single source of truth for SimplicityTools version that serves both release workflows and the public website footer. `Directory.Build.props` contains the canonical version (currently `0.4.0-local`); `docs-site/scripts/extract-version.mjs` reads it at build time; `npm run prebuild` ensures the version is always current before site builds.
**Why:** Eliminates version drift between NuGet packages, CLI, and website. Release workflows can now trust that the website always displays the correct version, and contributors never manually sync version strings.
**Implementation:** Added `prebuild` script to `docs-site/package.json`; created `extract-version.mjs` to parse `Directory.Build.props` and generate `docs-site/src/data/version.ts`; updated `SiteFooter.astro` to import and display the version.
**Testing:** Full build validated via `npm run build:validate`; version appears in footer HTML; all 32 pages built successfully; link checker passed.

