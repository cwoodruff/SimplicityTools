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
- **2026-05-02T06:08:59.230-04:00 — Central version contract:** `Directory.Build.props` now owns the repo-wide release baseline in `SimplicityToolsReleaseVersion`, with local package defaults and validation CI versions derived from it rather than hardcoded per project or workflow.
- **2026-05-02T06:08:59.230-04:00 — Docs-site sync pattern:** `docs-site/scripts/extract-version.mjs` generates `docs-site/src/data/version.ts` from `Directory.Build.props`, and `docs-site/src/components/SiteFooter.astro` renders that release line in the public footer.
- **2026-05-02T06:08:59.230-04:00 — Key paths:** Version source of truth lives in `Directory.Build.props`; release-generation behavior lives in `.github/workflows/nuget-publish.yml`; public display contract lives in `docs-site/src/components/SiteFooter.astro` and `docs-site/src/data/version.ts`.
- **2026-05-02T06:43:28.375-04:00 — Workflow dispatch routing:** GitHub run `25250085225` failed because `workflow_dispatch` paired `release_group=validation` with a non-empty `version`, and the old resolver treated any version input as a release build. The stable contract is simpler: validation always emits `<release-version>-ci.<run-number>` artifacts and ignores stale version input.
- **2026-05-02T06:43:28.375-04:00 — Review outcome:** Trinity’s revised release resolver is acceptable when it keeps release groups (`libraries`, `analyzers`, `cli`) on the versioned path but routes validation independently and documents that behavior in `CONTRIBUTING.md`.
- **2026-05-02T06:43:28.375-04:00 — Key paths:** Failure evidence lives in GitHub Actions run `25250085225`; dispatch routing lives in `.github/workflows/nuget-publish.yml`; operator guidance lives in `CONTRIBUTING.md`.
- **2026-05-02T06:43:28.375-04:00 — Validation dispatch hardening:** The safest shell contract is to normalize `workflow_dispatch` validation runs to an empty effective version before any SemVer or release-group checks. That preserves release behavior for `libraries`, `analyzers`, and `cli` while making stale UI input harmless.
- **2026-05-02T06:43:28.375-04:00 — Validation proof:** Reproducing the resolver locally as an input matrix is enough to prove the workflow fix before burning a full GitHub run; include validation default, validation with stale version, release groups with and without overrides, and a tag path.

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

---

## 2026-05-02T10:08:59Z — Central Release Version Contract Approved

**Squad Orchestration Input:** Morpheus background task approved shared version contract.

**Contract Locked:**
- `Directory.Build.props` is the single editable source of truth for the repo-wide release baseline via `SimplicityToolsReleaseVersion`
- Package defaults derive `-local`, CI validation builds derive `-ci.<run-number>`, workflow dispatch uses the same baseline unless an explicit override is supplied
- The Astro footer reads that property at build time

**Rationale:** MSBuild is the native packaging boundary for every publishable project in this repo, so anchoring the version there keeps the contract behind the packaging surface instead of inventing another config file.

**Trinity Implemented:** Workflow now reads `SimplicityToolsReleaseVersion` from `Directory.Build.props` and applies version derivation rules  
**Link Implemented:** Website footer displays version automatically via `docs-site/scripts/extract-version.mjs`  
**Tank Validated:** All package types and docs-site rendering verified against contract  

**Decision Propagated to:** `.squad/decisions.md`  
**Orchestration Logs:** `.squad/orchestration-log/2026-05-02T10-08-59Z-*`

---

## 2026-05-02T10:43:28Z — Orchestration: NuGet Workflow Validation Fix Complete

**Role in orchestration:** Review + replacement author

### Review (2026-05-02T06:43:28.375-04:00)
Reviewed Tank's rejection against GitHub Actions run 25250085225. Confirmed that old workflow resolver keyed validation routing off the stale, non-empty `version` field rather than `release_group` first. Root cause identified: dispatcher must be release_group-first to avoid stale UI state corrupting routing.

### Replacement Author (2026-05-02T06:43:28.375-04:00)
Authored new dispatch-resolution logic in `.github/workflows/nuget-publish.yml`:
- Check `release_group` before applying version constraints
- Validation dispatch ignores non-empty version and emits CI-only packages
- Libraries, analyzers, CLI groups preserve explicit SemVer override and Directory.Build.props fallback
- Updated CONTRIBUTING.md and decision/history files

**Tank re-reviewed and approved.** Fix ready for deployment.

---

