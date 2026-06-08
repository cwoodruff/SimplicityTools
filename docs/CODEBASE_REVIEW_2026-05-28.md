# SimplicityTools Codebase Review & Release Readiness Plan
**Lead:** Morpheus  
**Date:** 2026-05-28  
**Status:** Pre-release checkpoint — NOT production-ready; critical blockers and broken features identified

**VERDICT: ⚠️ NOT RELEASE-READY.** Repo requires focused work on test failures, null-safety warnings, documentation drift, and analyzer package validation before 0.4.0 can ship.

---

## Executive Summary

SimplicityTools has **solid architecture** but **multiple release blockers** prevent shipping 0.4.0 today:

1. **Main test suite is failing:** Sample.Simplified baseline says 23 files but now measures 24; test expectations don't match reality
2. **Null-safety warnings block build:** CS8604 warnings in analyzers package prevent professional release
3. **CLI performance gate is red:** Sample.OverEngineered timeout gate failing
4. **Analyzer package is broken in release:** NuGet package layout wrong; Roslyn won't discover diagnostics; no consumer validation gate
5. **Documentation drifts from code:** Help URLs point to dead sites; snapshot command doesn't exist; reported features (TCA breakdown, project filtering) not implemented
6. **Analyzer logic mismatches docs:** SF0001, SF0004, and other diagnostics don't behave as documented

**Timeline to release:** ~2 weeks (48–80 focused hours) to close blockers, then follow-up hardening over 2–3 sprints.

**Owners:** Tank & Trinity (code blockers), Link (docs/UX), Switch (analyzer contracts).

---

## What's Solid ✅

### Architecture & Design
- **Package boundaries clear:** Five independently-releasable packages with correct dependency graph
- **Zero-config promise enforced:** CLI, Metrics, Filters work without simplicity.json (though TCA settings and filter config are not wired end-to-end)
- **Release orchestration complete:** Central version control, tag-driven CI/CD, local pack→feed→consume testing documented
- **Sprint-branch model proven:** Asynchronous feature work on sprint/* branches, merged to main via PR

### Implementation Status
- **Five packages implemented:** Metrics, Filters, Tca, Analyzers, Cli all build and ship
- **Seven analyzers coded:** SF0001–SF0007 exist as diagnostics and code fixes
- **CLI feature-complete:** Analyze, baseline, report, diff, budget, watch commands exist
- **Website/Astro:** Hub structure built, 32+ pages generated, GitHub Pages configured
- **Test infrastructure:** xunit, coverlet, GitHub Actions workflows in place
- **Roslyn integration:** Workspace collection, semantic analysis, round-trip validation patterns established

---

## Critical Blockers (MUST FIX BEFORE 0.4.0) 🔴

### B1: Sample.Simplified Baseline Mismatch
**Issue:** Test baseline says Sample.Simplified has 23 files; current codebase measures 24.  
**Impact:** Main test suite fails; blocks release tag push.  
**Current State:**  
- Test expectation: 23 files  
- Actual measurement: 24 files  
- Cause: Sample.Simplified codebase changed but baseline not updated  

**Action Required:**
1. Run: `dotnet build && dotnet test tests/SimplicityTools.Metrics.Tests/SimplicityTools.Metrics.Tests.csproj --nologo`
2. Identify failing test (likely in `StructuralMetricsTests` or fixture snapshot)
3. Update baseline fixture/assertion to reflect 24 files
4. Verify: Re-run full test suite, all tests pass
5. Commit with message: "Fix: Update Sample.Simplified baseline to 24 files"

**Owner:** Tank  
**Effort:** 30–45 min  
**Blocking:** Yes — tag push blocked until fixed

---

### B2: Null-Safety Warnings (CS8604)
**Issue:** Build succeeds but with 4 nullable-reference warnings in Analyzers package.  
**Impact:** Professional release requires clean build; ship gate rejects warnings.  
**Warnings:**  
- `UnusedDependencyCodeFixProvider.cs:29` — packageId nullable but typed non-null  
- `PackageReferenceAnalysis.cs:34, :187` — FilePath can be null  
- `PrimaryPathConventions.cs:29` — filePath passed without null check  

**Action Required:**
1. Review each warning location (use `dotnet build --verbosity diagnostic | grep CS8604`)
2. Apply null-coalescing (`??`), null checks, or nullable annotations as appropriate
3. Run: `dotnet build src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj --nologo`
4. Verify: 0 warnings
5. Re-run full test suite to ensure no regressions
6. Commit with message: "Fix: Resolve CS8604 nullable-reference warnings in Analyzers"

**Owner:** Tank or Trinity  
**Effort:** 1–2 hours  
**Blocking:** Yes — ship gate rejects build warnings

---

### B3: CLI Performance Gate Timeout
**Issue:** `AnalyzeCommand_OverEngineeredSample_CompletesWithinExpectedThresholdAtP95` test is failing.  
**Impact:** Performance regression gate red; blocks release validation.  
**Current State:**  
- Sample.OverEngineered: 12 projects, 62 files, 25 abstraction layers (worst-case)
- P95 threshold: Currently exceeded
- Gate runs in CI to catch regressions before shipping

**Action Required:**
1. Run locally: `dotnet test tests/SimplicityTools.Cli.Tests/SimplicityTools.Cli.Tests.csproj --nologo --filter "FullyQualifiedName=SimplicityTools.Cli.Tests.AnalyzeCommandPerformanceTests.AnalyzeCommand_OverEngineeredSample_CompletesWithinExpectedThresholdAtP95"`
2. Profile execution: measure time for Sample.OverEngineered analysis
3. If > threshold:
   - Check for O(n²) loops in metrics collection (likely culprit)
   - Check Roslyn workspace initialization overhead
   - Verify fixture projects compile correctly (compilation time inflates analysis time)
4. Optimize and re-test
5. If threshold needs adjustment, update test constant with justification
6. Commit with message: "Perf: Fix AnalyzeCommand P95 threshold on Sample.OverEngineered"

**Owner:** Tank or Trinity  
**Effort:** 2–4 hours  
**Blocking:** Yes — performance gate runs in CI release validation

---

### B4: Analyzer Package Layout (Release-Blocking Consumer Validation)
**Issue:** NuGet package ships analyzer DLL in wrong directory; Roslyn never loads it.  
**Impact:** Consumers install package, diagnostics don't show up; perceived as broken tool.  
**Current State:**  
- Package layout: `lib/net10.0/SimplicityTools.Analyzers.dll` (WRONG)
- Correct layout: `analyzers/dotnet/cs/SimplicityTools.Analyzers.dll`
- No CI gate validates correct layout after packing
- No consumer smoke test proves diagnostics load

**Action Required:**
1. Review: `src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj` — ensure `<AnalyzerLanguage>cs</AnalyzerLanguage>` and output paths are correct
2. Test locally:
   ```bash
   dotnet pack src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj -c Release -o test-pkg --nologo
   unzip -l test-pkg/SimplicityTools.Analyzers.*.nupkg | grep analyzers/
   ```
   Should show: `analyzers/dotnet/cs/SimplicityTools.Analyzers.dll`
3. Add consumer validation test to `.github/workflows/nuget-publish.yml`:
   - Create scratch .csproj
   - `dotnet add package SimplicityTools.Analyzers` from local feed
   - Build scratch project
   - Verify: stdout contains at least one SF000x diagnostic warning
4. Update workflow to run validation after pack step; fail if no diagnostics found
5. Commit with message: "Fix: Ensure analyzer package layout is correct and add consumer validation gate"

**Owner:** Trinity (package config) + Tank (validation gate)  
**Effort:** 2–3 hours  
**Blocking:** Yes — must validate before first tag push; prevents broken analyzer release

---

### B5: Documentation Dead Links & Missing Features
**Issue:** Help URLs point to `simplicity-first.dev` (dead); docs claim features that don't exist.  
**Current State:**  
- Analyzer diagnostics link to dead URLs → users can't get help
- Docs say `dotnet simplicity snapshot` exists → it doesn't
- Docs say ReportGenerator outputs project breakdown and TCA estimates → it doesn't
- SimplicitySnapshot property names wrong in documentation

**Action Required:**
1. Search & replace dead URLs:
   ```bash
   grep -r "simplicity-first.dev" src/tests/docs/
   ```
2. Replace with: `https://simplicitytools.dev/docs/analyzers/{diagnostic-id}`
3. Verify links exist in docs-site (check `.squad/decisions.md` for site structure)
4. Review `docs/using-the-simplicity-tools.md` and `README.md`:
   - Remove `dotnet simplicity snapshot` references (command doesn't exist)
   - Remove TCA/project-breakdown claims from report section (not implemented)
   - Fix SimplicitySnapshot property names against actual source
5. Add link validation to CI: `.github/workflows/deploy-site.yml` already validates; ensure non-existent command references are caught
6. Commit with message: "Fix: Remove dead URLs, correct documentation vs. code mismatch"

**Owner:** Link (DX/docs)  
**Effort:** 1–2 hours  
**Blocking:** Yes — dead links hurt credibility before launch

---

## Important Follow-Up Work (MUST FIX BEFORE ANNOUNCEMENT) 🟠

### F1: Analyzer Logic vs. Documentation Mismatches

**SF0001 (Single Implementation Interface):**
- **Issue:** Code fix is too permissive; doesn't handle dependent-interface chains safely
- **Example:** `IPricer → ICheckoutPricer → DefaultPricer`. Removing IPricer breaks callers typed to ICheckoutPricer
- **Action:** Refactor fixer to refuse fix when dependent interfaces exist OR rewrite entire chain; add 3-level regression test
- **Owner:** Switch (analyzer contract review) + Tank  
- **Effort:** 2–3 hours

**SF0004 (Abstraction Depth):**
- **Issue:** Diagnostic doesn't match documented "abstraction-layer promise"; implementation is narrower
- **Action:** Audit code vs. docs; either expand implementation or narrow documentation; add test coverage for true scope
- **Owner:** Switch  
- **Effort:** 1–2 hours

**Other analyzers (SF0002, SF0006, SF0007):**
- **Issue:** Limited test coverage; edge cases like suppression directives, sample-solution signals, repeated-reference counting not validated
- **Action:** Expand test suite; add suppression tests, multi-reference scenarios
- **Owner:** Switch or Tank  
- **Effort:** 2–3 hours per analyzer

---

### F2: Configuration & Filter Settings Not Wired End-to-End
**Issue:** `simplicity.json` config loads but TCA settings (EstimatedOnboardingTime) and filter settings (passingScore) don't affect CLI output.  
**Impact:** Users think feature exists; actually no-op.  
**Action Required:**
1. Add integration tests: Load simplicity.json with custom TCA settings, verify CLI uses them
2. Add integration tests: Load custom filter passingScore, verify report reflects it
3. Trace data flow from config loading → CLI commands → output; identify where settings are dropped
4. Wire settings through; re-run integration tests
5. Commit with message: "Fix: Wire TCA and filter settings from config end-to-end"

**Owner:** Trinity  
**Effort:** 2–3 hours  
**Blocking:** Medium (config exists but is silent no-op; users need working config before encouraging adoption)

---

### F3: Missing Feature Documentation
**What's missing:**
- `--help` and `--version` flags not documented (users have to discover via trial)
- Quickstart guide conflates source-build vs. NuGet install paths (confusing for first-time users)
- Sample projects under-explained as teaching artifacts (no guide on "which sample shows what?")
- simplicity.json schema under-linked in docs (hard to find when users want to configure)
- README lacks role-based entry points (different for CLI users vs. library users vs. contributors)
- `report` output path unclear; `diff` and `baseline` workflows under-documented
- `watch` command has no usage guide
- No changelog exists; users don't know what's new in 0.4.0
- CONTRIBUTING.md lacks DX validation checklist (what to test before PR)

**Action Required:**
1. Add `--help` and `--version` output to quickstart guide
2. Separate quickstart into "Install from NuGet" vs. "Build from Source"
3. Add sample reference guide: "Sample.Simplified shows X, Sample.OverEngineered shows Y"
4. Link simplicity.json schema prominently in README "Configuration" section
5. Restructure README with role-based navigation (CLI → Library → Contributing)
6. Document `report` output path behavior and diff/baseline workflows
7. Add `watch` command usage section
8. Create CHANGELOG.md for 0.4.0 release
9. Add DX checklist to CONTRIBUTING.md (build, test, pack, validate locally before PR)

**Owner:** Link (DX)  
**Effort:** 4–6 hours  
**Blocking:** Medium (not blocking build/test, but critical for user adoption)

---

### F4: Analyzer Package & Release Validation Missing
**Issue:** No smoke test for packaged CLI installation; no proof that packaged analyzer works in consumer projects.  
**Action Required:**
1. Add to `.github/workflows/nuget-publish.yml` after pack step:
   ```bash
   # Install CLI from local feed
   dotnet tool install --global SimplicityTools.Cli --add-source ./artifacts/packages --version 0.4.0-{ci,local}
   # Run smoke test
   dotnet-simplicity analyze ./samples/Sample.Simplified/Sample.Simplified.sln
   ```
2. Add analyzer consumer validation (see B4)
3. Document in CONTRIBUTING.md the validation checklist before manual tag push
4. Commit with message: "Test: Add packaged CLI and analyzer consumer validation gates"

**Owner:** Tank or Trinity  
**Effort:** 1–2 hours  
**Blocking:** Medium (not blocking tag push, but important pre-release validation)

---

## Nice-to-Have (POST-RELEASE ITERATION) 🟡

| Item | Owner | Effort | Notes |
|------|-------|--------|-------|
| Performance benchmarking CI gate | Tank | 3–4 hours | Not run in CI today; detect regressions early |
| ReportGenerator complexity refactor (SF0003 self-violation) | Trinity | 4–6 hours | Tool violates own rule; extract loops into methods |
| Extended SF0001 regression tests (3-level hierarchy) | Switch | 1–2 hours | Current tests cover 2-level; expand coverage |
| Dependency advisory: Update Microsoft.Build.Tasks.Core | Trinity | 2–3 hours | Known high-severity vulnerability; update + validate |
| Symbol package (.snupkg) validation | Trinity | 2–3 hours | Generated but not validated for debugger usability |
| Config integration tests | Tank | 2–3 hours | No end-to-end test of simplicity.json loading |
| Release notes template & changelog automation | Morpheus | 2–3 hours | Manual today; automate for future releases |

---

## Execution Sequence (Next 2 Weeks)

### Phase 1: Close Blockers (48–72 hours, parallel tracks)

**Track A — Test Failures (Tank):**
1. Fix Sample.Simplified baseline (30 min)
2. Fix CLI performance gate (2–4 hours)
3. Run full test suite; verify passing

**Track B — Build Warnings (Tank or Trinity):**
1. Fix null-safety warnings (1–2 hours)
2. Verify clean build

**Track C — Package Validation (Trinity + Tank):**
1. Fix analyzer package layout (1 hour)
2. Add consumer validation gate to CI (1–2 hours)
3. Test locally; verify gate catches broken releases

**Track D — Documentation (Link):**
1. Fix dead URLs (30 min)
2. Remove non-existent feature claims (30 min)
3. Verify links in docs-site

**After Phase 1:** Create PR with all blocker fixes; merge to main once CI passes.

---

### Phase 2: Important Follow-Up (1 week)

**Before public announcement:**
1. Audit SF0001, SF0004, other analyzer logic vs. docs (Switch + Tank)
2. Wire TCA and filter settings end-to-end (Trinity)
3. Expand analyzer edge-case test coverage (Switch)

**In parallel:**
1. Improve documentation: help flags, role-based README, sample guide, watch/diff/baseline workflows (Link)
2. Create changelog (Morpheus)

**After Phase 2:** Publish tag; announce 0.4.0 release.

---

### Phase 3: Post-Release Hardening (Sprints 9–10)

- Performance benchmarking CI gate
- ReportGenerator complexity refactor
- Extended test coverage
- Dependency updates
- Release process automation

---

## Detailed Action Plan for Release

### For Tank

**Blocker-Priority Tasks:**
1. `dotnet build && dotnet test` — identify Sample.Simplified baseline failure
2. Fix baseline mismatch (update fixture or assertion)
3. Run CLI performance gate locally; profile Sample.OverEngineered
4. Optimize if needed; adjust threshold with justification
5. Verify all tests pass before Phase 1 PR merge

**Follow-Up Tasks:**
1. Add analyzer consumer validation gate to CI
2. Add packaged CLI smoke test
3. Expand SF0001 regression coverage (3-level hierarchy)
4. Performance benchmarking integration (Sprint 9)

---

### For Trinity

**Blocker-Priority Tasks:**
1. Fix analyzer package layout (.csproj config)
2. Test pack locally; verify `analyzers/dotnet/cs/` layout
3. Add consumer validation to CI workflow
4. Run full test suite

**Follow-Up Tasks:**
1. Wire TCA and filter settings end-to-end
2. ReportGenerator complexity refactor
3. Update Microsoft.Build.Tasks.Core dependency
4. Symbol package validation

---

### For Link (DX)

**Blocker-Priority Tasks:**
1. Search & replace dead `simplicity-first.dev` URLs
2. Remove `dotnet simplicity snapshot` references
3. Fix SimplicitySnapshot property names
4. Verify links in docs-site before Phase 1 PR merge

**Follow-Up Tasks:**
1. Separate quickstart: NuGet vs. source build
2. Add sample reference guide
3. Role-based README restructuring
4. Document --help, --version, watch, diff, baseline workflows
5. Link simplicity.json schema
6. Create CHANGELOG.md

---

### For Switch (Analyzer Contracts)

**Follow-Up Tasks:**
1. Audit SF0001, SF0004 logic vs. docs
2. Expand analyzer edge-case tests (suppression, samples, repeated references)
3. Add SF0001 3-level hierarchy regression test

---

### For Morpheus (Release Coordination)

**Phase 1:**
- Coordinate parallel blocker fixes
- Ensure CI passes before Phase 1 PR merge
- Review & approve PRs

**Phase 2:**
- Review follow-up work
- Prepare release notes
- Coordinate Phase 2 PR merge

**Phase 3:**
- Create release tag (after Phase 1 + Phase 2 complete)
- Monitor NuGet publish workflow
- Post release announcement

---

## Release Readiness Verdict

**Current Status:** 🔴 **NOT RELEASE-READY**

**Blockers preventing 0.4.0 ship:**
1. Main test suite failing (Sample.Simplified baseline)
2. Null-safety warnings blocking build gate
3. CLI performance gate timeout
4. Analyzer package layout broken (consumer validation missing)
5. Dead documentation links & missing features

**Timeline to release:**
- **Phase 1 (Blockers):** 2–3 days (48–72 hours focused work)
- **Phase 1 PR merge + CI validation:** 1 day
- **Phase 2 (Follow-up):** 3–5 days (wire settings, audit analyzer logic, improve docs)
- **Phase 2 PR merge + CI validation:** 1 day
- **Total:** ~2 weeks to 0.4.0 ship

**Go/No-Go:** **NO-GO** until all Phase 1 blockers are fixed and tests pass. **CONDITIONAL GO** after Phase 1 if Phase 2 follow-up is accepted as "good enough for launch" (vs. waiting for full polish).

**Recommended approach:** Complete Phase 1 + Phase 2 before tag push. Post-launch hardening (Phase 3) can follow in subsequent sprints without delaying launch.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|-----------|
| Analyzer package ships broken (wrong layout) | **High if unfixed** | **CRITICAL** (diagnostics don't load) | Add consumer validation gate (B4) |
| Test suite still failing on tag push | **High if unfixed** | **CRITICAL** (CI blocks publish) | Fix baselines + perf gate (B1, B3) |
| Null-safety warnings in production | **High if unfixed** | **HIGH** (unprofessional build) | Fix CS8604 warnings (B2) |
| Documentation contradicts code | **High (currently true)** | **MEDIUM** (user confusion) | Fix dead URLs and claims (B5) |
| Analyzer logic mismatches docs | **Medium** | **MEDIUM** (trust issues) | Audit and fix (F1) |
| Config settings are no-ops | **Medium** | **MEDIUM** (silent failure) | Wire end-to-end (F2) |

---

## Summary: What Must Be Done Before 0.4.0

**MANDATORY (Release Blockers):**
- [ ] Fix Sample.Simplified baseline mismatch
- [ ] Fix null-safety warnings (CS8604)
- [ ] Fix CLI performance gate timeout
- [ ] Fix analyzer package layout + add consumer validation
- [ ] Remove dead documentation URLs and non-existent feature claims

**STRONGLY RECOMMENDED (Before Announcement):**
- [ ] Audit analyzer logic vs. documentation
- [ ] Wire TCA and filter settings end-to-end
- [ ] Improve documentation (role-based README, samples guide, command reference)

**TRACKING:**
- Use `.squad/decisions.md` for release gate decisions
- Document any threshold adjustments (performance P95) with justification
- Create PR template for Phase 1 and Phase 2 to enforce checklist

---

**Prepared by:** Morpheus (Lead) + Team feedback  
**Review Date:** 2026-05-28  
**Next Review:** After Phase 1 PR merge  
**Classification:** Team Internal
