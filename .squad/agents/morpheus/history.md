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

## Learnings

- **2026-05-01T19:30:22.856-04:00 — NuGet release workflow:** `.github/workflows/nuget-publish.yml` now separates validation-only CI packaging from upload-ready manual dispatch builds by requiring an explicit SemVer for `libraries`, `analyzers`, or `cli` on workflow dispatch, while keeping tag pushes as the only automated publish gate.
- **2026-05-01T19:30:22.856-04:00 — Release safety pattern:** The publish path validates the exact artifact set and version before pushing, and release artifacts must include both `.nupkg` and matching `.snupkg` files. Keep release proof centered on the workflow plus `CONTRIBUTING.md` guidance rather than ad hoc scripts.
- **2026-05-01T19:30:22.856-04:00 — Key paths:** Release orchestration lives in `.github/workflows/nuget-publish.yml`; operator guidance lives in `CONTRIBUTING.md`.

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

## 2026-05-01T23:30:22Z: NuGet Release Workflow Updates

**Session:** nuget-release-workflow  
**Co-agent:** Tank

Updated the NuGet workflow to support manual `workflow_dispatch` runs with explicit `release_group` and `version` parameters, enabling operators to build release-ready artifacts without automatic NuGet.org publishing. Tag pushes remain the only automated publish gate. Decisions recorded in `.squad/decisions.md`.

**Key Changes:**
- Workflow now accepts `release_group` and `version` inputs for manual release artifact builds
- Validation confirms exact package/version match before any publish attempt
- `.snupkg` files no longer treated as primary packages in publish set

**Next:** Tag-based automation ready for production use.
