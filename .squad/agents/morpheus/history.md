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
