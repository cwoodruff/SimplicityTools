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
