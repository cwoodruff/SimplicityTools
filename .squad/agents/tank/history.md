# Tank: Release Engineering & Validation

- **Owner:** Tank
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CI/CD, performance testing
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- Tank owns package validation, integration testing, performance gating, and release verification.
- Strong release proof requires consumer install validation for each delivery surface.
- Zero-config first-run validation is a non-negotiable gate.

## Current Focus (Sprint 7–9)

### 2026-05-02T10:43:28Z — NuGet Workflow Validation Fix: First Review & Re-Review

**First Review (2026-05-02T06:43:28.375-04:00):** Rejected initial workflow revision targeting GitHub Actions run 25250085225 failure. Validated dispatch with stale version field still failed normalization. Requested design clarification from Morpheus: either normalize version input on validation or make it unmistakable by ignoring/clearing it.

**Re-Review (2026-05-02T06:43:28.375-04:00):** After Morpheus authored replacement fix (dispatch resolver release_group-first), replayed workflow-dispatch matrix locally. Validated:
- `release_group=validation` emits CI-only version even with stale version input
- `libraries`, `analyzers`, `cli` preserve explicit SemVer and fallback behavior
- Local build/pack validated; CLI package installs from feed
- Existing validation test suites pass

**Verdict:** Approved. Fix addresses reported bug and preserves release-group behaviors.

---

### 2026-05-01T06:37:49Z — Site Validation Checklist Pattern Established

Established 3-phase site validation checklist for docs-site PRs: (1) Build Validation – `npm run build` zero errors/warnings, <500ms; (2) Structure Validation – spot-check templates for correct title, header nav, main content, footer, breadcrumbs; (3) Responsive Validation – hamburger visibility <960px, full menu ≥960px, media queries at 720/960px.

**Issues:** #51, #52  

---

### Prior Work (2026-04-29 to 2026-05-01)

- **Analyzer Package Review (Sprint 4–5):** Approved Trinity's analyzer-packaging revision. Confirmed `analyzers/dotnet/cs/` layout, consumer validation gate working (SF0001 warning emitted).
- **Release Workflow Rereview (Sprint 5):** Approved Link's fix to `.github/workflows/nuget-publish.yml` (missing Python import). Validated with adversarial rerun.
- **Performance Gate Calibration (Sprint 6):** Profiled CLI baseline; recalibrated gate from 5s local to 5s local + 10s GitHub CI based on 9.3s p95 historical.
- **Preflight Validation #51 & #52 (Sprint 6–7):** Approved site structure, navigation, responsiveness, and homepage for Wave 2 delivery. All acceptance checklists passed.

---

## Recent Approvals & Decisions

- ✅ NuGet workflow dispatch validation fix (release_group-first routing)
- ✅ Site validation checklist pattern
- ✅ Performance gate calibration

**Ref:** `.squad/decisions.md` and `.squad/orchestration-log/`

