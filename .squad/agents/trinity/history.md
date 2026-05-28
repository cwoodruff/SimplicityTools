# Trinity — PackageImpl & Release Packaging Lead

**Project Context**
- Owner: Chris Woody Woodruff
- Project: SimplicityTools (C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI)
- Focus: Core measurement & cost-translation packages (Metrics, Filters, Tca)

**Completed Work (Pre-2026-05-02)**
- Sprint 1 (Issue #1): SimplicitySnapshot record + ToSummary() contract delivered
- Sprint 3 (Issues #23-#24): SF0005/SF0007 code-fix revisions with test coverage
- Sprint 4 (M4): Analyzer packaging rework — moved from `lib/` to `analyzers/dotnet/cs/` layout with consumer validation
- Sprint 5 (M5): Metrics → Filters → Tca library packaging chain completed with XML docs, dependency validation
- Sprint 8 (M8 Waves 1-3): Astro website bootstrap, navigation, docs content, analyzer documentation
- Sample.Simplified: Resolved macOS `.App` suffix startup issue, renamed projects, preserved assembly name, added smoke tests

**Key Learnings**
- Contract-first sprint work: exact public surface must win over scaffold compatibility
- For package-usage metrics: match declared package IDs to Roslyn metadata, confirm via semantic symbols
- Filter verdicts easier to reuse when they carry named sub-scores alongside composite score
- TCA stays easier to validate when library owns deterministic cost formulas and receives config as explicit inputs
- SF0007 explicit `[PrimaryPath]` annotations must fully define comparison set
- SF0005 scoped to classes only; structs are false positives
- SF0001 dependent-interface-chain requires safe rewriting or refusal when dependencies exist
- Explicit interface implementations need revision pass to check original specifier symbol
- On macOS, executable names ending in `.App` break `dotnet run` startup
- Canonical release line in `Directory.Build.props` keeps packaging deterministic

---
## 2026-05-02T10:08:59Z — Shared Release Version Implementation Complete

**Squad Orchestration Input:** Trinity background task implemented shared release version source.

**Implementation Delivered:**
- Updated `.github/workflows/nuget-publish.yml` to read `SimplicityToolsReleaseVersion` from `Directory.Build.props`
- Implemented version derivation: `-local` for packages, `-ci.<run-number>` for CI validation, exact SemVer for tagged releases
- Wired version property into all three package types (libraries, analyzers, CLI)
- Tag-triggered publishes continue to use exact SemVer from tag name
- Manual workflow_dispatch runs use baseline unless operator supplies override

**Validation Passed:**
- Workflow reads version correctly
- Package defaults emit `-local` versions
- CI validation version derivation works
- Targeted build/test/pack/docs flows validated

**Morpheus Approved:** Central release version contract ✅  
**Link Integrated:** Version ready for website footer display ✅  
**Tank Validated:** All package types verified ✅  

**Decision Propagated to:** `.squad/decisions.md`  
**Orchestration Logs:** `.squad/orchestration-log/2026-05-02T10-08-59Z-trinity.md`

## Learnings

- The current repo-level validation failures are dominated by contract drift: Sample.Simplified now analyzes to 24 total files, but tests/docs still assert 23.
- The CLI configuration surface currently over-promises: tca inputs and filters.passingScore are parsed, but the shipped CLI only consumes filter thresholds for the budget report.
- The docs currently advertise workflows and APIs that are not on the live surface, including a nonexistent `dotnet simplicity snapshot` command and outdated SimplicitySnapshot property names.

- GitHub Actions workflow-dispatch forms can retain a previously entered version value, so NuGet validation runs must branch on `release_group` first and ignore stale `version` input when `release_group=validation`.
- `.github/workflows/nuget-publish.yml` owns the release-shape gating for validation, libraries, analyzers, and cli dispatches; `Directory.Build.props` remains the canonical source for the shared release line.
- `CONTRIBUTING.md` documents the operator contract for the NuGet release pipeline, including when validation runs use CI-only versions versus when upload-ready package groups use the canonical or explicit SemVer.

---

## 2026-05-02T10:43:28Z — Orchestration: NuGet Workflow Validation Fix Complete

**Role in orchestration:** Design contributor (validation dispatch spec)

Contributed design clarification: NuGet release pipeline should resolve `release_group` before applying version rules so validation runs always emit CI-only package version and ignore optional `version` value still present in GitHub Actions form. Root cause: GitHub retains prior dispatch inputs between manual dispatches; without release_group-first routing, a user can select validation and still trip the versioned-release gate due to stale form state.

**Outcome:** Morpheus authored replacement fix implementing this design. Tank approved after comprehensive local validation.

---

## 2026-05-28T06:10:33Z — Codebase Review: Core Libraries Audit Complete

**Audit scope:** Metrics, Filters, Tca, CLI; test coverage and docs drift

**Key findings (11 items):**
1. Sample baseline stale (tests expect 23, actual 24) — breaks validation
2. CLI performance gate red (P95 ~5.2s vs. <5s) — hotspot: HeuristicCollectionPass
3. Onboarding-time metric stubbed to TimeSpan.Zero — weakens budget/TCA
4. Config advertises unimplemented behavior (filter pass threshold, TCA inputs)
5. Docs promise snapshot/history workflows that don't exist
6. Library API examples have name mismatches (ProjectCount vs. TotalProjects)
7. Invalid CLI commands exit with code 0 (should fail)
8. Structural dependency counting simplistic for conditional refs
9. Primary-path heuristic needs tighter semantics
10. TCA input validation narrow
11. Transitive vulnerability warning (Microsoft.Build.Tasks.Core)

**Critical path P1–P3:**
- P1: CS8604 null-safety (2–4h)
- P2: ReportGenerator complexity (4–6h)
- P5a: Analyzer package validation (1–2h)

**Recommended implementation order:**
1. Repair validation contract (baseline, tests, docs)
2. Close docs/product gaps (remove/implement snapshot, fix API names)
3. Make config honest (wire filter threshold, TCA into CLI)
4. Finish missing metric (implement onboarding-time estimation)
5. Performance + hardening (profile, add edge-case tests)

**Release verdict:** NOT ready until Phase 1 fixed. Null-safety, complexity, and analyzer package layout are non-negotiable before tag push.

---

