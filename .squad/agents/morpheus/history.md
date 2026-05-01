# Morpheus: Architecture & Orchestration

- **Owner:** Morpheus
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling, GitHub project management
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- SimplicityTools is the Simplicity-First .NET Toolkit, built to measure architecture in economic terms.
- Package graph: Metrics → Filters/Tca → CLI, with analyzers integrated alongside.
- Zero-config CI signal is a core product promise.
- Three-milestone delivery aligned with book chapters and package dependencies.

## Key Decisions

- Zero-config first-run is non-negotiable validation gate.
- Release proof = local pack + local consumer + CI workflow verification.
- Sprint structure enforces critical path dependencies; no speculative work.

## Recent Learnings

- **Repository branching model:** SimplicityTools uses sprint-branch-to-main pattern (not dev-based). Sprint branches created from main, worked on in waves, then merged to main via PR. Milestone close precedes PR creation.
- **Issue closure protocol:** Use `gh issue close {id} --comment "reason"` to close issues. Bulk close doesn't support multiple arguments; loop instead.
- **Milestone management:** Milestones can be closed via GitHub API even with open issues; closing doesn't auto-close the issues. Must close issues explicitly first.
- **CI validation gates:** NuGet package validation workflow (nuget-publish.yml) runs full suite: restore, build, test, pack, validate metadata, validate analyzer consumer. Takes 15–30+ minutes depending on test suite depth.
- **PR creation requirements:** Ensure branch has commits ahead of target branch; branches on same commit produce "No commits between" error.

## Active Status (Milestone 8 Closed, Sample.Simplified Startup Addressed)

**2026-05-01T12:58:06.465-04:00 — Milestone 8 Closure Complete:**
- Repo-side engineering for the Astro site and GitHub Pages deployment is **complete and verified**.
- Deployment workflow, CNAME file, SEO artifacts, and all documentation content staged and integrated.
- Explicit boundary: Repo-complete ✅ vs production-complete 🔄 (DNS/Pages are external).
- Issue #61 and Milestone 8 closed on repo-complete grounds.

**2026-05-01T12:58:06.465-04:00 — Sample.Simplified Startup Fix:**
- Led root-cause analysis: macOS apphost rejection under Apple integrity enforcement.
- Architectural decision: `samples/Sample.Simplified/App/App.csproj` should disable native apphost generation.
- Trinity implemented fix (renamed assembly to `Sample.Simplified.Demo`), Tank validated with real process launch regression coverage.
- All three decisions merged into `.squad/decisions.md`.

## Archived History

- Full orchestration history in `.squad/agents/morpheus/history-archive.md`.
