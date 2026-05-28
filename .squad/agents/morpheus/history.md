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

## Recent Learnings (2026-05-28)

- **Repository branching model:** SimplicityTools uses sprint-branch-to-main pattern (not dev-based).
- **Release strategy:** Three release groups (libraries/v, analyzers/v, cli/v); Metrics/Filters/Tca version together.
- **Central version contract:** `Directory.Build.props` owns `SimplicityToolsReleaseVersion`; all packages derive versions from this.
- **Zero-config promise:** Non-negotiable validation gate; all features must degrade gracefully without config.
- **Validation dispatch hardening:** Workflow dispatch should key on `release_group` first, ignore stale `version` input.
- **Docs-site sync:** Version extraction script generates version.ts at build time from MSBuild property.
- **macOS apphost constraint:** Sample projects must disable apphost generation or use non-`.App` naming.

## Archived History

**Pre-2026-05-28 work:** See `.squad/agents/morpheus/history-archive.md`
- Milestone 1–3 scaffold and core delivery
- Milestone 4–5 NuGet packaging and release orchestration
- Milestone 6–8 Astro website and GitHub Pages deployment
- NuGet workflow dispatch routing fix (2026-05-02)

---

## 2026-05-28T08:10:33Z — Codebase Review & Release Verdict Finalized

**Status:** Release readiness audit complete; verdict revised to **NO-GO until Phase 1 fixed**.

**Five-agent parallel audit identified:**
- ✅ Structurally solid architecture, feature-complete CLI, comprehensive docs, website deployed
- ⚠️ Three critical-path blockers: null-safety (CS8604), complexity refactor (ReportGenerator), analyzer validation gate
- ⚠️ Trust gaps: stale test baseline (23 vs. 24 files), broken help links (simplicity-first.dev → dead), analyzer package layout wrong (blocks Roslyn discovery), onboarding-time stubbed

**Phase 1 (48–72h parallel):**
- Track A: Fix null-safety warnings (Tank/Trinity, 2–4h)
- Track B: Refactor ReportGenerator complexity (Trinity, 4–6h)
- Track C: Add analyzer validation gate to CI (Trinity, 1–2h)
- Track D: Fix dead URLs, false claims in docs (Link)

**Phase 2 (1 week):** Audit analyzer logic, wire TCA/filter settings, docs improvements
**Phase 3 (post-release):** Performance benchmarking, extended tests

**Decision artifacts:** `.squad/decisions.md` merged 4 new entries; 5 orchestration logs written; `docs/CODEBASE_REVIEW_2026-05-28.md` consolidated all findings.


