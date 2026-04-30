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
