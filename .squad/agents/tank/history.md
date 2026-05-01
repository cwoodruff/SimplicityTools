# Tank: Release Engineering & Validation

- **Owner:** Tank
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CI/CD, performance testing
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- Tank owns package validation, integration testing, performance gating, and release verification.
- Strong release proof requires consumer install validation for each delivery surface.
- Zero-config first-run validation is a non-negotiable gate.

## Recent Work (Sprint 5–6)

📌 **Sprint 4–5 Analyzer Package Review (2026-04-30T22:15:00Z):** Approved Trinity's analyzer-packaging revision. Confirmed `analyzers/dotnet/cs/` layout (not `lib/`), consumer validation gate working (SF0001 warning emitted). Workflow validation also now required as part of release proof.

📌 **Sprint 5 Release Workflow Rereview (2026-04-30T19:52:08.101-04:00):** Approved Link's fix to `.github/workflows/nuget-publish.yml` (added missing Python import). Validated with adversarial rerun: injected stale sentinel in validation workspace, confirmed cleanup and rebuild succeeded on second pass. Release blocker cleared.

📌 **Sprint 6 Assignment (2026-04-30T21:27:33.453-04:00):** Own #41 (Validate global tool zero-config first-run) in Wave 2 of Milestone 6. Unblocks after Link completes #40 (package contract proof).

📌 **PR #65 Performance Gate Calibration (2026-04-30T22:09:34.021-04:00):** Profiled CLI performance baseline and determined the 5s performance gate was unrealistic for GitHub Actions CI (9.3s historical p95). Calibrated gate to 5s local, 10s GitHub-hosted CI. Resolved blocker documented by Morpheus. Decisions merged to decisions.md.

## Next Steps

- Wave 2 of Milestone 6: Validate `dotnet tool install dotnet-simplicity --global` on both sample solutions; confirm zero-config first-run behavior.
- Full orchestration history in `.squad/agents/tank/history-archive.md`.

