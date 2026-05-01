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

## Recent Work (Sprint 5–7)

📌 **Sprint 5 Launch (2026-04-30T19:09:43.583-04:00):** Milestone 5 = Release Packaging. Created `sprint/5-release-packaging` branch. Wave structure: Trinity (#35 Metrics, #36 Filters, #37 Tca), Switch (#38 Analyzers) parallel, Tank (#39 Integration) final gate. Critical path: #35 → #36 → #37 → #39. All issues assigned; DoD criteria defined.

📌 **Sprint 5 Release Workflow Rereview (2026-04-30T19:52:08.101-04:00):** Approved Link's fix to `.github/workflows/nuget-publish.yml`. Both Metrics, Filters, Tca, and Analyzers packages validated locally; workflow rereview passed with adversarial stale-state testing.

📌 **Sprint 6 GitHub Wrap-Up & Kickoff (2026-04-30T21:27:33.453-04:00):** 
- Sprint 5 issues #35–#39: Verified closed. Milestone 5 closed.
- Sprint 6 PR #64 created and merged. Issues #40–#43 closed. Milestone 6 closed.
- Sprint 6 routing: Link owns #40 (contract) & #42 (docs) Wave 1; Tank owns #41 (validation) Wave 2; Link closes #43 (dry-run) Wave 3.

📌 **Sprint 7 Kickoff — Packaging UX & Documentation (2026-04-30T21:40:50.629-04:00):**
- Branch `sprint/7-packaging-ux-documentation` created from main.
- Milestone 7: Six documentation/UX issues (#44–#49) all assigned to Link.
- Wave 1 (Ready Now): #44 README badges/quickstart, #45 first-run examples.
- Wave 2 (After #44): #47 README package sections, #46 library integration docs.
- Wave 3 (After #45, #46, #47): #48 troubleshooting guide, #49 CI/CD examples.
- Critical path: #44 → #47; #45 → #48, #49. Single-contributor throughput optimized.
- DoD: README updated with badges/installs/packages, docs/ complete with quickstart/troubleshooting/CI-CD, all NuGet links verified, zero-config promise maintained.

## Key Decisions

- Zero-config first-run is non-negotiable validation gate.
- Release proof = local pack + local consumer + CI workflow verification.
- Sprint structure enforces critical path dependencies; no speculative work.

## Next Steps

- Execute Milestone 6 wave assignments on `sprint/6-global-tool-packaging`.
- Milestone 7 planning (Packaging UX & Documentation).
- Full orchestration history in `.squad/agents/morpheus/history-archive.md`.

