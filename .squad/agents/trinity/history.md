# Project Context

- **Owner:** Chris Woody Woodruff
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- The toolkit turns architectural measurements into TCA-aligned cost signals.
- The core library surface spans Metrics, Filters, and Tca packages.
- The toolkit should be useful on the first CI run without configuration.

## Recent Updates

📌 Team hired on 2026-04-29T06:47:51.656-04:00
📌 **Sprint 1 issue #1 completed (2026-04-29T11:44:51.000Z):** SimplicitySnapshot record + ToSummary() delivered with spec-aligned 10-property contract. Contract finalized: legacy instance members excluded. Tank's contract tests validate regression signal. Notes: stale CLI reference to `snapshot.SolutionName` flagged for future cross-team remediation.

## Learnings

- My initial focus is the core measurement and cost-translation packages.
 - 2026-04-29T07:32:23.826-04:00: For contract-first sprint work, the exact public surface must win over scaffold compatibility, because downstream callers are cheaper to migrate than a public record shape that drifts from spec.
 - 2026-04-29T11:44:51.000Z: Wave 2 (sample scaffolds #2, #3) and Wave 3+ (structural, semantic, heuristic passes) now unblocked after issue #1 completion.
 - 2026-04-29T07:32:23.826-04:00: Pass 1 can stay fast and deterministic by using `SolutionFile` plus raw project-file parsing for declared `Compile` and `PackageReference` items, leaving full MSBuild/Roslyn evaluation to later semantic work.
 - 2026-04-29T07:32:23.826-04:00: For package-usage metrics, matching declared package IDs to Roslyn metadata reference paths and then confirming usage through semantic symbols keeps the collector incremental while still grounding unused-dependency counts in actual compilations.
 - 2026-04-29T21:22:50.867-04:00: Filter verdicts are easier to reuse downstream when they carry named sub-scores alongside the composite score, because CLI/TCA layers can explain failures without re-deriving the math from raw snapshot metrics.
 - 2026-04-29T21:22:50.867-04:00: When snapshot inputs do not expose a richer metric yet, mapping evaluators to the closest existing deterministic signal is better than adding ambient heuristics mid-sprint; note the gap explicitly and keep the evaluator formulas stable.
  - 2026-04-29T21:22:50.867-04:00: TCA stays easier to validate when the library owns only deterministic cost formulas and receives configuration assumptions as explicit inputs, instead of reaching into repo-local config files.
  - 2026-04-29T21:22:50.867-04:00: Using filter composite scores directly for opportunity-cost math gives the calculator a stable input contract while preserving one place to evolve scoring behavior.
- 2026-04-30T06:57:15.306-04:00: For SF0007, explicit `[PrimaryPath]` annotations must fully define the comparison set when present; convention-only files in `Controllers`, `Endpoints`, `Handlers`, or `Pages` revert to supporting files and can still be diagnosed.
- 2026-04-30T06:57:15.306-04:00: SF0005 should stay scoped to classes only; broadening constructor-count warnings to structs turns data-carrier shapes into false positives instead of surfacing service objects doing too much.
- 2026-04-30T06:57:15.306-04:00: For SF0001, removing a base interface safely requires inlining its members into direct dependent interfaces before dropping the inheritance edge; otherwise consumers typed to the surviving interface lose inherited members and the fix breaks compilation.
- 2026-04-30T06:57:15.306-04:00: Explicit interface implementations need the same revision pass to check the original specifier symbol before rewriting, because Roslyn semantic lookups on already-rewritten nodes are no longer anchored to the original syntax tree.

---

## 2026-04-30T10:57:15Z — Orchestration Snapshot
**From:** Scribe cross-agent sync
- **Analyzer Revision Assigned:** Trinity owns SF0005 + SF0007 fixes under reviewer lockout.
- **Scope:** Narrow SF0005 to classes, fix SF0007 mixed-mode behavior, add regression tests.
- **Reviewer Lockout:** Tank cannot review this revision; resubmit to Morpheus or use decision consensus.
- **Critical Path:** This is blocking issue #23 (Code Fixes) from Wave 2.

---

## 2026-04-30T10:57:15Z — Scribe cross-agent sync
**Code-Fix Review Rejection & Trinity Assignment:**
- **Issue:** Sprint 3 issues #23-#24 code-fix review completed. SF0001 dependent-interface-chain bug found and rejected.
- **Evidence:** `SingleImplementationInterfaceCodeFixProvider` removes base `IPricer` while `ICheckoutPricer : IPricer` remains, breaking callers typed to dependent interface.
- **Verdict:** Rejected. Trinity assigned for revision (reviewer lockout: Tank locked out).
- **Revision Scope:** SF0001 must either refuse the fix when dependencies exist or rewrite chain safely with regression coverage.
- **Next:** Await Trinity revision. Decision recorded in `.squad/decisions.md`.
- **Status:** Tank available for #26 and other Sprint 3 tasks post-review.

---

## 2026-04-30T21:29:31Z — Sprint 4 Foundation Review Rejection & New Revision
**From:** Scribe cross-agent sync (Tank Sprint 4 review outcome)
- **Issue:** Sprint 4 foundation review completed on `sprint/4-package-foundation` (Milestone 4 issues #32, #33, #34).
- **Verdict:** **REJECTED** — Analyzer package structure defective.
- **Critical Defect:** `SimplicityTools.Analyzers.0.4.0-local.nupkg` packed as normal library (`lib/net10.0/...`) instead of analyzer layout (`analyzers/dotnet/cs/...`). Verified: scratch consumer build emitted **0 warnings**, so SF0001 never executed.
- **Evidence:** `dotnet build`, `dotnet test`, `dotnet pack`, and `dotnet tool install` all passed; consumer validation failed.
- **Trinity Assignment:** Repack analyzer with correct layout; add release-validation coverage to prove consumers load the analyzer and emit expected diagnostics before publish approval.
- **Reviewer Lockout:** None; normal review cycle on next submission.
- **Critical Path:** M4 completion blocks M5 (library packaging). This revision is the final gate before packaging pipeline advances.
- **Decision:** Full record in `.squad/decisions.md` under "Sprint 4 Foundation Review — Tank Verdict".
- 2026-04-30T17:29:31.278-04:00: Roslyn consumer validation is only trustworthy when the packed analyzer is restored into a real downstream project from a repo-root artifact path; consumer fixtures under `bin/` can mask diagnostics, and `PrivateAssets="all"` on the validation `PackageReference` suppresses the analyzer path we need to prove.

---

## 2026-04-30T21:29:31Z — Decision Archived
**From:** Scribe session (post-Tank verdict)
- **Action:** Trinity's analyzer packaging revision decision merged from inbox into shared decision log.
- **Decision Point:** 2026-04-30T17:29:31.278-04:00 — Analyzer packaging repacked per Tank revision
  - Scope: `SimplicityTools.Analyzers` must pack under `analyzers/dotnet/cs/` with normal `lib/` output suppressed
  - Validation: Must inspect `.nupkg` for correct analyzer path; must fail if legacy `lib/net10.0/` path exists
  - Consumer validation: Must prove packaging restores into downstream project with expected SF0001 diagnostics firing
- **Status:** Decision now in team memory, ready for implementation and validation tracking.

---

## 2026-04-30T22:15:00Z — Sprint 4 Milestone 4 Analyzer Rereview Approved
**From:** Scribe session (Tank rereview outcome)
- **Issue:** Sprint 4 Milestone 4 analyzer-package rereview completed.
- **Trinity's Revision:** Modified `SimplicityTools.Analyzers.csproj` to:
  - Set `PackageType` to `Analyzer`
  - Suppress normal build-output packing
  - Pack analyzer DLL/PDB under `analyzers/dotnet/cs/`
- **Validation Evidence:** Tank verified:
  - Packed nupkg contains `analyzers/dotnet/cs/SimplicityTools.Analyzers.dll`
  - No `lib/net10.0/SimplicityTools.Analyzers.dll` entry (legacy path suppressed)
  - Downstream consumer restore and build emitted `warning SF0001` (analyzer loaded by Roslyn)
- **Test Coverage Added:** `AnalyzerPackageValidationTests.PackedAnalyzerPackage_UsesAnalyzerLayout_AndReportsDiagnosticsInConsumer` ✅
- **Release Gate Automated:** `.github/workflows/nuget-publish.yml` now enforces both checks:
  - Fails if analyzer asset missing from `analyzers/dotnet/cs/`
  - Fails if analyzer still ships under `lib/net10.0/`
  - Fails if downstream consumer build does not emit `warning SF0001`
- **Verdict:** ✅ **APPROVED** — Publish blocker closed for Sprint 4 Milestone 4.
- **Status:** Analyzer package release-ready. No further revisions needed.
