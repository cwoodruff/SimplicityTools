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

## Recent Work (Sprint 5–6)

📌 **Sprint 5 Launch (2026-04-30T19:09:43.583-04:00):** Milestone 5 = Release Packaging. Created `sprint/5-release-packaging` branch. Wave structure: Trinity (#35 Metrics, #36 Filters, #37 Tca), Switch (#38 Analyzers) parallel, Tank (#39 Integration) final gate. Critical path: #35 → #36 → #37 → #39. All issues assigned; DoD criteria defined.

📌 **Sprint 5 Release Workflow Rereview (2026-04-30T19:52:08.101-04:00):** Approved Link's fix to `.github/workflows/nuget-publish.yml`. Both Metrics, Filters, Tca, and Analyzers packages validated locally; workflow rereview passed with adversarial stale-state testing.

📌 **Sprint 6 GitHub Wrap-Up & Kickoff (2026-04-30T21:27:33.453-04:00):** 
- Sprint 5 issues #35–#39: Verified closed. Milestone 5 closed.
- Sprint 6 PR #64 created and merged. Issues #40–#43 closed. Milestone 6 closed.
- Sprint 6 routing: Link owns #40 (contract) & #42 (docs) Wave 1; Tank owns #41 (validation) Wave 2; Link closes #43 (dry-run) Wave 3.

## Key Decisions

- Zero-config first-run is non-negotiable validation gate.
- Release proof = local pack + local consumer + CI workflow verification.
- Sprint structure enforces critical path dependencies; no speculative work.

## Learnings

- **Repository branching model:** SimplicityTools uses sprint-branch-to-main pattern (not dev-based). Sprint branches created from main, worked on in waves, then merged to main via PR. Milestone close precedes PR creation.
- **Issue closure protocol:** Use `gh issue close {id} --comment "reason"` to close issues. Bulk close doesn't support multiple arguments; loop instead.
- **Milestone management:** Milestones can be closed via GitHub API even with open issues; closing doesn't auto-close the issues. Must close issues explicitly first.
- **CI validation gates:** NuGet package validation workflow (nuget-publish.yml) runs full suite: restore, build, test, pack, validate metadata, validate analyzer consumer. Takes 15–30+ minutes depending on test suite depth.
- **PR creation requirements:** Ensure branch has commits ahead of target branch; branches on same commit produce "No commits between" error.

📌 **Sprint 7 Wrapup: Packaging UX & Documentation (2026-04-30T22:22:13-04:00):**
- Closed all six Sprint 7 issues (#44–#49) and Milestone 7.
- PR #65 created with 10 commits (+1934/−847 lines, 16 files changed).
- Content: NuGet badges, quickstart guide, library integration docs, troubleshooting, CI/CD examples.
- **Merge Blocker:** GitHub validation gate (NuGet packages workflow, job 73885590519) still running as of 2026-05-01T02:08:28Z, at step 7 of 11.
- **Architectural Note:** Sprint 7 confirms the sprint-branch-to-main model is working well. Milestone close precedes PR creation; issues close before merge.

## Next Steps

- Monitor PR #65 validation completion (workflow in progress as of 2026-05-01T02:08:28Z).
- Merge PR #65 with squash strategy once validation passes.
- Post-merge: Update `.squad/identity/now.md` and plan Milestone 8.
- Full orchestration history in `.squad/agents/morpheus/history-archive.md`.

