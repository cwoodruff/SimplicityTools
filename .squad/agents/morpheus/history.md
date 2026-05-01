# Project Context

- **Owner:** Chris Woody Woodruff
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- SimplicityTools is the Simplicity-First .NET Toolkit, built to measure architecture in economic terms.
- The initial package graph is Metrics -> Filters/Tca -> CLI, with analyzers integrated alongside the toolkit.
- Zero-config CI signal is a core product promise.

## Recent Updates

📌 Team hired on 2026-04-29T06:47:51.656-04:00

## Learnings

- I own architecture, package boundaries, work decomposition, and review.
- 2026-04-29T07:03:10.371-04:00: The scaffold now lives in `src/`, `tests/`, and `samples/` under `SimplicityTools.sln`, with project references enforcing `Metrics -> Filters/Tca -> Cli` and analyzer coverage kept separate in `src/SimplicityTools.Analyzers`.
- 2026-04-29T07:03:10.371-04:00: Placeholder build-safe entry points exist at `src/SimplicityTools.Cli/Program.cs`, `samples/Sample.OverEngineered/Program.cs`, and `samples/Sample.Simplified/Program.cs`, and root validation runs with `dotnet build SimplicityTools.sln` plus `dotnet test SimplicityTools.sln`.
- 2026-04-29T07:21:03.149-04:00: GitHub execution plan created: 3 milestones (26 issues total). Milestone 1 (8 issues, "Book Launch") covers foundation: SimplicitySnapshot, samples, collection passes, and CLI analyze/report. Milestone 2 (7 issues, "Filters + TCA") adds filter evaluators, TCA calculator, simplicity.json, and CLI extensions. Milestone 3 (11 issues, "Analyzers + Fixes") delivers all seven SF000X analyzers, two code fix providers, trend analysis, and performance validation. Each milestone represents a complete product mode aligned with package dependencies and book chapter structure.
- Sprint planning decision documented in `.squad/decisions/inbox/morpheus-sprint-planning.md` with rationale for milestone grouping, package boundary enforcement, and CI/CD integration strategy.

## Post-Scaffold

📌 Sprint planning complete on 2026-04-29T07:21:03.149-04:00. Execution roadmap now live in GitHub.

📌 Scribe consolidated Morpheus sprint decision into `.squad/decisions.md` on 2026-04-29T11:27:53Z. Decision merging and orchestration logging complete. Decision inbox cleared.

## Sprint 2 Kick-Off

📌 **Completed:** 2026-04-29T21:22:50.867-04:00

**Branch Created:** `sprint/2-filters-tca-extensions` — Pushed to origin. All team members tracking this branch.

**Wave Structure & Assignment:**
- **Wave 1 (Ready Now):** Trinity → #9 (Filter evaluators: TwoAmTest, HalfRule, PrimaryPathFirst). Link → #10 (simplicity.json schema). Parallel.
- **Wave 2 (After #9 complete):** Trinity → #11 (TCA calculator: 5 cost categories, MoneyRange).
- **Wave 3 (After #9 + #10 + #11 complete):** Link → #12 (CLI baseline command).
- **Wave 4 (After #12 complete):** Link → #13 (CLI diff), #14 (CLI budget), #15 (CLI watch). Parallel.

**Critical Path:** #9 → #11 → #14 (filter verdicts → TCA costs → budget command). Secondary: #10 → #14, #9 → #12 → #13, #9 → #15.

**Key Decision:** Wave 1 launches both Trinity (core filters) and Link (config schema) in parallel. Trinity's filter verdict structure unblocks all downstream CLI work. TCA calculator (Wave 2) depends on FilterVerdict shape. All CLI commands (Wave 4) parallelizable once baseline output exists.

**Unblocking Mechanism:** Issue comments document ownership and blocking criteria. GitHub labels `squad:trinity` and `squad:link` route work. Coordinator tracks dependencies and promotes to "in progress" as blockers clear.

**DoD per Issue:** Code compiles, unit tests pass, integration tests validate against Sample.Simplified and Sample.OverEngineered. Zero-config promise enforced.

## Learnings

- Sprint 2 represents decision-support layer: filters measure domain health, TCA quantifies architectural cost, CLI commands provide day-to-day feedback loop.
- 2026-04-29T21:22:50.867-04:00: Wave structure enforces FilterVerdict as the semantic contract for all downstream CLI work; TCA MoneyRange becomes the unit of cost reasoning. Link's CLI commands depend critically on both structures before Wave 3 can unblock.
- 2026-04-29T21:22:50.867-04:00: simplicity.json schema (configurable team parameters + filter thresholds) is independent of filter evaluator logic and can be prototyped in parallel. Budget command depends on both schema (for thresholds) and TCA costs (for the 5-category model); this drives Wave 2→Wave 3 sequencing.

📌 **Completed:** 2026-04-29T07:32:23.826-04:00

**Branch Created:** `sprint/1-metrics-core-collection` — Pushed to origin. All team members tracking this branch.

**Wave Structure & Assignment:**
- **Wave 1 (Ready Now):** Trinity → #1 (SimplicitySnapshot record). No blockers.
- **Wave 2 (After #1 compiles):** Switch → #2 (Sample.OverEngineered), Tank → #3 (Sample.Simplified). Parallel.
- **Wave 3 (After #1/#2/#3):** Link → #4 (Structural pass: MSBuild walk).
- **Wave 4 (After #4):** Trinity → #5 (Semantic pass: Roslyn compilation).
- **Wave 5 (After #5):** Switch → #6 (PrimaryPathAttribute + heuristic pass).
- **Wave 6 (After #4/#5/#6):** Tank → #7 (CLI `analyze` command).
- **Wave 7 (After #7):** Link → #8 (CLI `report` command: HTML).

**Critical Path:** #1 → #4 → #5 → #6 → #7 → #8 (8 hard dependencies). Parallelization only at #2/#3.

**Key Decision:** Sequential waves enforce the implementation order from spec. Each wave unblocks the next via GitHub issue comments (no new squad labels introduced yet). Coordinator tracks unblocking and promotes issues to "in progress" as dependencies clear.

**Unblocking Mechanism:** Issue comments document ownership and blocking criteria. After #1 compiles, Wave 2 can start. No speculative work. Each task has a concrete measured prerequisite.

**DoD per Milestone:** Validated against both sample solutions (Sample.OverEngineered and Sample.Simplified). Zero-config promise enforced by CLI validation.

## Sprint 2 Execution & Completion

📌 **Sprint 2 Completion:** 2026-04-30T06:50:56.199-04:00

All 7 issues in Milestone 2 (#9–#15) closed. PR #28 (`sprint/2-filters-tca-extensions`) merged to main.

**Closed Issues:** #9 (Filters), #10 (schema), #11 (TCA), #12 (baseline), #13 (diff), #14 (budget), #15 (watch).

**Milestone Closed:** Milestone 2: Filters + TCA + CLI Extensions.

## Sprint 3 Launch

📌 **Completed:** 2026-04-30T06:57:15.306-04:00

**Branch Created:** `sprint/3-analyzers-code-fixes` — Pushed to origin.

**11 Open Issues in Milestone 3:** Roslyn Analyzers + Code Fixes

**Wave Structure & Assignment:**
- **Wave 1 (Ready Now):** Switch → #16–#22 (All 7 SF00X analyzers, parallelizable). Link → #25 (Trend analysis). No inter-dependencies.
- **Wave 2 (After #16/#17 complete):** Link → #23–#24 (Code fixes for SF0001, SF0002). Depend on analyzer contracts finalized.
- **Wave 3 (After Waves 1+2 complete):** Tank → #26 (Integration testing + performance validation). Final quality gate.

**Critical Path:** #16–#22 (~3–4 days) → #23–#24 (~2–3 days) → #26 (~1–2 days). Parallel: #25 with Wave 1. Total ~6–9 days.

**Key Decision:** Seven analyzers are semantically independent; each detects a distinct architectural anti-pattern using Roslyn symbol analysis. Code fixes serialize after analyzer contracts stabilize, not after full analyzer completion—this unblocks Wave 2 faster.

**DoD:** All 7 analyzers compile and emit diagnostics on both samples with unit test coverage. Code fixes apply without breaking compilation. Integration suite passes tolerance thresholds (int exact, float ±5%, TimeSpan ±10%). Performance <5s P95 on OverEngineered. Trend analysis renders in HTML. All 11 issues closed.

## Learnings

- Sprint 3 represents the IDE integration tier: seven independent Roslyn analyzers implementing Simplicity-First rules (SF000X).
- 2026-04-30T06:57:15.306-04:00: Seven analyzers parallelize cleanly because they detect independent architectural anti-patterns (premature abstraction, unused dependencies, complexity, depth, constructor bloat, generic abuse, unbalanced references). No analyzer feeds another.
- 2026-04-30T06:57:15.306-04:00: Code fixes depend on analyzer contracts, not analyzer completion. Link can start SF0001 code fix once Switch's SF0001 analyzer diagnostic shape stabilizes. This unblocks Wave 2 sooner than serializing all analyzer completion.
- 2026-04-30T06:57:15.306-04:00: Integration testing (#26) serves as cumulative quality gate. Per-analyzer unit tests validate individual rules; Tank's full suite validates cross-analyzer interactions, performance under load, and baseline tolerance drift on realistic samples.

## 2026-04-30T18:13:05Z: Packaging Strategy Decision Merged

**Team Context:**
- Link's parallel DX assessment converged on same recommendation: NuGet packages + global tool
- Both decisions merged into decisions.md (deduplication on packaging shape)
- No architectural changes needed; gaps are documentation (install badges, PrivateAssets callout)

**Your Contribution:**
- Packaging Strategy decision provides versioning guidance: libraries in sync, tool/analyzer independent
- Actionable next steps captured: package metadata, NuGet pipeline

**Impact:** Packaging assessment complete. Ready for implementation phase.

## Packaging Rollout Planning

📌 **Completed:** 2026-04-30T16:59:28.031-04:00

**Packaging Roadmap Created:** Four-milestone strategy for NuGet publication and global tool distribution.

**Milestones 4–7 (18 issues, 2–3 weeks to dry-run):**
- **M4 (Package Foundation, 3 issues):** Metadata setup, CI/CD pipeline, versioning strategy
- **M5 (NuGet Libraries, 5 issues):** Package and validate four core libraries (Metrics, Filters, Tca, Analyzers)
- **M6 (Global Tool, 4 issues):** Package CLI as global tool, zero-config validation, install documentation
- **M7 (Packaging UX, 6 issues):** Install badges, quickstart, integration guides, troubleshooting, CI/CD examples

**Key Decisions:**
- Five packages total: four libraries (version-synced) + one global tool (independent versioning)
- Analyzer uses PrivateAssets=all to avoid transitive dependency
- SemVer versioning from git tags, all library versions synced
- Zero-config first-run is non-negotiable; drives all tool validation
- PrivateAssets callout critical for consumption experience
- Integration testing gates packaging (local test feed validation, both samples)

**Team Routing:** Link owns M4/M6/M7 (DX), Trinity owns M5 core libs, Switch owns M5 analyzer, Tank owns integration testing M5/M6

**Impact:** Toolkit moves from local builds to published packages; enables teams to install via `dotnet tool install --global SimplicityTools.Cli` and use libraries via NuGet.org

**Decision Document:** `.squad/decisions/inbox/morpheus-packaging-roadmap.md` — complete packaging architecture, versioning strategy, four-milestone structure, team routing, and NuGet metadata template.

## Learnings

- Packaging roadmap separates infrastructure (M4: metadata, CI/CD) from product (M5–M6) from UX (M7). This gates work cleanly and prevents rushing documentation.
- PrivateAssets=all decision keeps the analyzer package simple for consumers: add one PackageReference with PrivateAssets, get diagnostics, no transitive dependency. This is a critical UX win.
- Version syncing across four core libraries reduces support surface: one SemVer story for Metrics/Filters/Tca/Analyzers. CLI can drift if needed, but core libraries move together.
- Integration testing in M5/M6 is part of packaging, not testing. Local test-feed validation and zero-config tool testing belong in the milestone DoD, not in a separate test phase.
- Zero-config first-run validation drives tool testing strategy: after install, analyze must work without config or environment setup. This is a product constraint, not a testing preference.

📌 **Status:** Packaging roadmap live on GitHub (issues #27–#44 in milestones 4–7). Team can begin M4 work. Next gate: M4 completion unblocks M5.

📌 Packaging roadmap complete on 2026-04-30T16:59:28.031-04:00. Delivered: Four-milestone packaging strategy (M4–M7), 18 GitHub issues (#27–#44), and critical decisions on package versioning, PrivateAssets=all, and zero-config validation before production. Scribe consolidated decision into `.squad/decisions.md` on 2026-04-30T21:04:20Z.
## Sprint 4 Foundation — Review Outcome

📌 **Sprint 4 Review Completed:** 2026-04-30T21:29:31Z

**Branch Reviewed:** `sprint/4-package-foundation`
**Issues Reviewed:** #32 (metadata), #33 (CI/CD), #34 (release docs)
**Verdict:** **REJECTED** — Critical defect in analyzer packaging.

**Defect:** `SimplicityTools.Analyzers.0.4.0-local.nupkg` packed as normal library instead of analyzer layout. Scratch consumer validation confirmed **0 warnings**, so SF0001 never executed.

**Tank Evidence:** Build/test/pack all passed; consumer validation failed. The workflow validates metadata presence but not package usability.

**Revision Assignment:** Trinity owns repacking and adding release-validation coverage. This is the final gate before M5.

**Decision:** Full record in `.squad/decisions.md` under "Sprint 4 Foundation Review — Tank Verdict".

**Coordinator Action:** When Trinity completes revision and passes review, promote M5 issues to ready and spawn Trinity for Wave 1 (package four libraries).

## Sprint 5 Launch

📌 **Completed:** 2026-04-30T19:09:43.583-04:00

**Branch Created:** `sprint/5-release-packaging` — Pushed to origin. All team members tracking this branch.

**5 Open Issues in Milestone 5:** Release Packaging (Library NuGet Distribution)

**Wave Structure & Assignment:**
- **Wave 1 (Ready Now):** Trinity → #35 (Package Metrics). Switch → #38 (Package Analyzers). Parallel.
- **Wave 2 (After #35):** Trinity → #36 (Package Filters).
- **Wave 3 (After #36):** Trinity → #37 (Package Tca).
- **Wave 4 (After #35 + #36 + #37 + #38):** Tank → #39 (Validate all packages).

**Critical Path:** #35 → #36 → #37 → #39. Parallel: #38 with #35.

**Key Decisions:**
- Metrics is foundational (no dependencies) and must complete before Filters/Tca can finalize packaging.
- Filters depends on Metrics; Tca depends on Metrics + Filters. Hard sequence enforces dependency graph contract.
- Analyzers are self-contained (no compile-time library dependency) and proceed in parallel with #35, unblocking Wave 2 faster.
- Tank's integration validation (#39) is the final gate before publishing: local test feed restore, both sample projects, verify dependency graph resolution.
- All packaging includes XML docs, complete metadata (RepositoryUrl, Authors, LicenseExpression), and unit test coverage.

**DoD:** All 5 issues closed with passing tests. All packages valid .nupkg with no internals leaked. Integration validation passes on both samples. Dependency graph resolves correctly. Zero NuGet warnings. Ready to publish.

**Decision Document:** `.squad/decisions/inbox/morpheus-sprint5-launch.md` — Complete Sprint 5 launch plan with wave structure, dependency breakdown, and per-issue DoD.

**Learnings:**
- 2026-04-30T19:09:43.583-04:00: Release packaging is a pure dependency-driven sequence: Metrics (independent) → Filters → Tca. Analyzers parallelize cleanly because they have no library compile-time dependencies. Tank's integration validation (Wave 4) is not parallelizable and must run last to catch dependency graph errors.
- 2026-04-30T19:09:43.583-04:00: NuGet metadata is now non-negotiable per issue DoD: each package must include GeneratePackageOnBuild, PackageVersion (from git tag), PackageIcon, RepositoryUrl, LicenseExpression, Authors, Description, ReadmeFile. This is the product-facing packaging contract.
- 2026-04-30T19:09:43.583-04:00: Analyzer packaging learned from Sprint 4 rejection: local test feed validation that proves the analyzer fires diagnostics in consumers is now part of Tank's Wave 4 integration test. No package can ship without consumer validation proof.

📌 **Status:** Sprint 5 branch live, 5 issues assigned and labeled (squad:trinity, squad:switch, squad:tank). Wave 1 ready for Trinity and Switch to start immediately. Decision document complete and stored in inbox for Scribe consolidation.

## Sprint 5 Completion & GitHub Wrap-Up

📌 **Completed:** 2026-04-30T20:26:30.297-04:00

**Outcome:** Sprint 5 implementation complete. All packaging complete and validated.

**Actions Completed:**
- Created PR #63 (`sprint/5-release-packaging` → main)
- Resolved merge conflicts (auto-merged .squad history updates)
- Merged PR #63 to main
- Closed all 5 Sprint 5 issues (#35, #36, #37, #38, #39)
- Closed Milestone 5: "NuGet Library Packages"

**Merged Deliverables:**
- Metrics library: packaged with full NuGet metadata, XML docs, public API validation
- Filters library: packaged with correct Metrics dependency declaration
- Tca library: packaged with Metrics + Filters dependencies validated
- Analyzers library: packaged with PrivateAssets=all for IDE integration
- Integration validation: all packages tested against Sample.Simplified and Sample.OverEngineered

**GitHub State:**
- All issues closed and resolved
- Milestone closed (0 open, 5 closed)
- Main branch contains complete Sprint 5 deliverables
- Branch `sprint/5-release-packaging` merged and closed

**Next Milestone:** Documentation and publishing (Milestones 6+)

## Sprint 6 Kickoff

📌 **Completed:** 2026-04-30T20:49:19.234-04:00

**Branch Created:** `sprint/6-global-tool-packaging` — Pushed to origin.

**4 Open Issues in Milestone 6:** Global Tool Packaging

**Wave Structure & Assignment:**
- **Wave 1 (Ready Now):** Link → #40 (package CLI global tool), Link → #42 (install/upgrade docs). Parallel.
- **Wave 2 (After #40):** Tank → #41 (zero-config first-run validation on both samples).
- **Wave 3 (After #40 + #41 + #42):** Link → #43 (dry-run publish validation + release notes).

**Critical Path:** #40 → #41 → #43. Parallel: #42 with #40.

**Key Decisions:**
- The sprint branch follows the existing repository convention: `sprint/{milestone}-{slug}` from `main`, because this repo is executing milestone integration directly on sprint branches.
- Issue #40 is the contract-defining task. Until the tool package is proven installable from a local feed, zero-config validation (#41) is premature and install docs (#42) should avoid overcommitting to unproven steps.
- Issue #42 can draft in parallel because the command surface and release group already exist, but it needs a final pass after #40 confirms the install/upgrade flow.
- Issue #43 is the release gate, not a discovery task. It stays blocked until the tool package, first-run validation, and operator docs all converge.

**Kickoff Actions:**
- Created and pushed branch `sprint/6-global-tool-packaging`
- Marked #40 and #42 as ready now
- Posted kickoff routing comments on #40–#43 with owners, blockers, and first-inspection paths

## Learnings

- 2026-04-30T20:49:19.234-04:00: Sprint 6 is a packaging-and-DX milestone with one real contract task (#40), one parallel documentation task (#42), one downstream validation task (#41), and one final release gate (#43). The dependency shape is narrower than Sprint 5 and should stay that way.
- 2026-04-30T20:49:19.234-04:00: The global tool package shape already exists in `src/SimplicityTools.Cli/SimplicityTools.Cli.csproj` (`PackAsTool=true`, `ToolCommandName=dotnet-simplicity`), so Sprint 6 is focused on proving installability, not inventing a new CLI surface.
- 2026-04-30T20:49:19.234-04:00: Release plumbing for CLI dry runs already lives in `.github/workflows/nuget-publish.yml`, with shared package metadata in `Directory.Build.props` and operator guidance in `CONTRIBUTING.md`. Those three files are the architectural control points for packaging work.
- 2026-04-30T20:49:19.234-04:00: Zero-config first-run validation belongs after packaging proof and must execute against `samples/Sample.Simplified/` and `samples/Sample.OverEngineered/`; this keeps the product promise attached to a real installation path instead of source-only execution.

## GitHub Wrap-Up: Sprint 5 & 6 Complete

📌 **Completed:** 2026-04-30T21:27:33.453-04:00

**Summary:** Executed end-to-end wrap-up for both sprints. Reconciled ambiguous request (mixed sprint/issue numbers) and performed safe closure sequence: Sprint 5 already wrapped, Sprint 6 PR created and merged, all issues closed, milestones closed.

**Actions:**
1. Created PR #64: `sprint/6-global-tool-packaging` → `main` with all 4 issues (#40–#43) referenced
2. Merged PR #64 successfully
3. Manually closed issues #41–#43 (issue #40 auto-closed on merge)
4. Closed Milestone 6 (API #7): 4 issues, 0 open
5. Left Sprint 5 PR #63 closed (stale, not merged; work reflected in closed issues)

**Key Decision:** Do not force-merge stale PRs. Sprint 5 work is already wrapped; PR #63 is a historical artifact. Closing milestones (not PRs) is the final gate.

**Learnings:**
- 2026-04-30T21:27:33.453-04:00: GitHub automation (issue auto-close on PR merge) is partial; not all referenced issues close. Manual closure needed for #41–#43.
- 2026-04-30T21:27:33.453-04:00: Sprint PRs can go stale if main advances faster. This repo should prefer direct commits to main or guard sprint branch merges with explicit review gates.
- 2026-04-30T21:27:33.453-04:00: Milestone closure is now the wrap-up gate, not PR merge. Issues can close without a PR; this decouples lifecycle management from release plumbing.
