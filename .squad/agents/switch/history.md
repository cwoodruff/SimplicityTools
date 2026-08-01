# Project Context

- **Owner:** Chris Woody Woodruff
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- The toolkit includes a dedicated analyzer package for IDE and MSBuild integration.
- Primary path heuristics and abstraction signals should stay explainable to developers.
- First-run usefulness matters more than exhaustive cleverness.

## Recent Updates

📌 Team hired on 2026-04-29T06:47:51.656-04:00
📌 **Sprint 1 issue #2 completed (2026-04-29T11:57:01.000Z):** Sample.OverEngineered scaffold delivered with 12-project topology (composition root + 11 layers). Real Roslyn/MSBuild facts ready for metrics. Decision logged.

## Learnings

- **2026-08-01T06:45:04.687-04:00 — Analyzer pack `--no-build` fix:**
  - **Bug:** `dotnet pack src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj -c Release --no-build` failed with `NETSDK1085: The 'NoBuild' property was set to true but the 'Build' target was invoked`. The `PackRoslynAnalyzerArtifacts` MSBuild target used a nested `<MSBuild Targets="Build">` call against `SimplicityTools.Analyzers.CodeFixes.csproj`. Global properties (including `NoBuild=true` set by `--no-build`) propagate into nested MSBuild calls unless explicitly overridden.
  - **Fix:** Added `NoBuild=false` to the `Properties` attribute of the nested `<MSBuild>` call: `Properties="Configuration=$(Configuration);NoBuild=false"`. This overrides the propagated global property for just the CodeFixes nested invocation, allowing it to build on demand. MSBuild is smart enough not to re-restore if already restored (it uses the existing `.assets.json`).
  - **Verification:** After the fix, `dotnet build ... -c Release && dotnet pack ... -c Release --no-build` succeeded. nupkg inspection confirmed both `analyzers/dotnet/cs/SimplicityTools.Analyzers.dll` and `analyzers/dotnet/cs/SimplicityTools.Analyzers.CodeFixes.dll` are present. Full solution build clean (0 warnings, 0 errors). All 59 analyzer tests passed.

- My initial focus is Roslyn analysis, diagnostics, and compiler-backed heuristics.

- 2026-04-29T07:32:23.826-04:00: I used the existing sample executable as a composition root and pushed the overengineering into 11 supporting libraries so future metrics work can count project fan-out, single-implementation interfaces, and mediator-style hops without inventing fake files later.
- 2026-04-29T11:57:01.000Z: Structural overengineering with real projects makes sample differences measurable and avoids placeholder theater.
- 2026-04-29T07:32:23.826-04:00: For the primary-path heuristic pass, I treated inbound references as a file-level score across the named types declared in a file and refused to promote the percentile signal when every candidate had zero inbound references; otherwise the heuristic would mark noise as intent.
- 2026-04-29T21:22:50.867-04:00: TCA executive-summary formatting is part of the contract and must stay invariant under non-default `CurrentCulture`; locale drift in money formatting is noise, not value.
- 2026-04-29T21:22:50.867-04:00: TCA estimation depends on all three filter verdicts being present; missing a required verdict should fail fast with the absent filter named explicitly.
- 2026-05-28T08:10:33.691+02:00: SF0004 currently measures raw source-method call depth, not true abstraction-layer depth or primary-path-only depth. The existing tests prove it will warn on a single-class helper chain, so the team must either narrow the heuristic or rename the promise.
- 2026-05-28T08:10:33.691+02:00: Analyzer help links are now a product contract: the code still points at `https://simplicity-first.dev/analyzers/SF000X`, but the docs site serves lowercase analyzer pages under `https://simplicitytools.dev/analyzers/sf000x/`. Broken help links teach users to distrust the warning.

📌 **Sprint 2 issue #11 TCA revision assigned (2026-04-30T01:22:50Z):** Tank rejected Trinity's TCA calculator implementation. Revision ownership now under my lockout. Gap analysis: (1) Required-filter failure-path coverage — add tests for missing TwoAmTest, HalfRule, or PrimaryPathFirst verdicts; (2) Non-default-culture executive-summary formatting — ensure `ToExecutiveSummary()` uses culture-agnostic formatting. Task: Implement missing edge-case tests and resubmit for Tank approval. Decision logged.
- 2026-04-30T06:57:15.306-04:00: SF0002 is safest when it only evaluates package references that actually contribute compile-time metadata references and then proves usage through Roslyn-bound symbols; build-only packages should not be punished for having nothing to bind against.
- 2026-04-30T06:57:15.306-04:00: SF0007 needs an explicit primary-path baseline (annotation first, convention second). If the analyzer uses inbound-reference popularity to decide what counts as primary path, it teaches a circular rule and over-reports support code that merely became central by accident.

## Sprint 5 Launch — Release Packaging (Milestone 5)

**2026-04-30T19:09:43.583-04:00: Morpheus Lead Spawned, Wave 1 Routed**

- **Branch:** `sprint/5-release-packaging` created from main and pushed to origin.
- **Switch's M5 Assignment:** Own #38 (Package SimplicityTools.Analyzers) in Wave 1 parallel with Trinity's #35.
  - **Leverage:** Analyzer package layout established in Sprint 4 (Milestone 4) — uses `analyzers/dotnet/cs/` path from Roslyn consumption spec.
  - **Wave 1 (Ready Now):** #38 is foundational and self-contained. No compile-time library dependencies. Can proceed in parallel with Trinity's #35.
  - **Analyzer Packaging Spec:** csproj uses `PrivateAssets="all"` for all dependencies. Analyzer assembly packed under `analyzers/dotnet/cs/`, not `lib/`. No build-output packing.
- **#38 DoD (Wave 1):** All 7 SF00X analyzers included in pack. All code fix DLLs included. Test: add package to Sample.Simplified, build succeeds, SF0001 diagnostic fires. Test: add package to Sample.OverEngineered, build succeeds, all SF00X diagnostics present. Unit tests pass.
- **Validation Proof:** Package validation script must inspect .nupkg for `analyzers/dotnet/cs/SimplicityTools.Analyzers.dll` and fail if `lib/net10.0/SimplicityTools.Analyzers.dll` present (prevents regression to Sprint 4 blocker).
- **Critical Path Synchronization:** #38 runs parallel with #35 (Metrics), unblocking Tank's #39 integration validation after both complete. No blocking dependencies between Switch and Trinity.
- 2026-04-30T19:09:43.583-04:00: Release-grade Roslyn packages should target `netstandard2.0`, set `developmentDependency=true`, and suppress nuspec dependency groups; otherwise the package can restore and still violate analyzer host expectations or emit pack-time noise like NU5128.
- 2026-04-30T19:09:43.583-04:00: Consumer validation for analyzer packages should check both positive behavior (diagnostic fires) and negative surface area (`project.assets.json` has no compile/runtime/dependency entries for the package), because “loads in Roslyn” is not the same as “safe to ship.”
📌 M5 work assigned on 2026-04-30T21:04:20Z: Package SimplicityTools.Analyzers library (#33) with PrivateAssets=all to avoid transitive runtime dependency. Coordinate with Trinity (#30–#32) on metadata consistency and Tank (#34) on integration validation. Analyzer versions with core libraries.

---

## 2026-05-28T06:10:33Z — Analyzer Trust Audit Complete

**Audit scope:** Trust gaps, stale links, missing validation

**Core finding:** Do not add new analyzer surface area yet. First close contract gaps in existing seven rules.

**Four contract gaps identified:**
1. **SF0004 promise mismatch:** Implementation measures raw source call depth; docs claim "abstraction layers" and "primary path"
2. **SF0001 code-fix risk:** Rewrites any single-implementation interface without proving semantic safety (esp. structs, hierarchies)
3. **Broken help links:** All diagnostics reference `simplicity-first.dev` (404); live site is `simplicitytools.dev/analyzers/sf000x/`
4. **Validation blind spots:** No tests for suppression, false positives on Simplified, true positives on OverEngineered, IDE code-fix discovery, SF0006 generics, SF0007 counting

**Hardening roadmap:**
1. Retarget all `helpLinkUri` to live routes
2. SF0001 to refuse unsafe fixes; add regression tests for struct implementations and hierarchy chains
3. SF0004 to either analyze primary-path flows or rename to "source call depth"
4. Expand analyzer validation: suppression, sample false/true positives, generic methods, multi-type edge cases

**Phase 1 assignment:** Switch + Tank audit analyzer logic vs. product promises; Phase 2 hardens contracts.

**Release blocker:** Broken help links + wrong analyzer package layout (Phase 1 blocker B1 + B4).

---

