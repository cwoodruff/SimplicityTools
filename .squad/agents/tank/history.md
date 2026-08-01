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


## 2026-05-28T05:40:02.687Z — Copilot Instructions Command Validation

**Task:** Validate that all build/test/docs-site commands documented in `.github/copilot-instructions.md` are executable and accurate.

**Validation Coverage:**
- CLI test filtering patterns (exclude AnalyzeCommandPerformanceTests, single performance gate)
- Build/test/pack commands for all package projects
- Performance gate test isolation (P95 threshold validation)
- Docs-site Node.js >= 20.0.0 requirement verification
- Documentation site build passes with `npm run build:validate`

**Status:** ✅ Complete. All commands verified and integrated into Copilot instructions.

## Learnings
- 2026-05-28: Current repo audit found Sample.Simplified teaching metrics drifted to 24 files while baselines/docs/tests still expect 23, analyzer HelpLinkUri values still point at 404ing simplicity-first.dev pages, and release automation still lacks a packed CLI install smoke test.

---

## 2026-05-28T06:10:33Z — Release Validation Audit Complete

**Audit scope:** Broken flows, release readiness, test coverage gaps

**Three-part recovery plan:**
1. Restore truth for teaching artifacts (Sample.Simplified baseline = source of truth; update CLI assertions, customer docs, quickstart output together)
2. Repair broken help-link journeys (retarget all analyzer `helpLinkUri` from simplicity-first.dev to live simplicitytools.dev routes)
3. Prove packaged CLI (add release gate: pack → install from feed → run zero-config flow on Sample.Simplified)

**Critical findings:**
- Sample baseline stale (23 vs. 24 files) breaks full-solution validation
- CLI performance gate red (P95 measured ~5.2s vs. < 5s limit); hotspot in HeuristicCollectionPass
- Analyzer help links all dead (simplicity-first.dev → 404)
- No CLI package-install validation in release pipeline

**Evidence:**
- `dotnet test` fails on stale baseline
- P95 gate failure
- Analyzer consumer produces 0 warnings (wrong package layout)
- No dotnet tool install smoke test

**Impact:** Shipping without these fixes violates teaching-artifact and zero-config first-run promises. Users will hit dead links immediately.

**Phase 1 assignment:** Tank fixes test baseline + profiles perf gate + adds CLI validation gate (parallel with Trinity's null-safety and complexity).

---


## Learnings

### 2026-07-31T09:38:54-04:00 — Concurrent Pack Race & Stale SHA Assertion Fixes

**Bug A: File-lock race in package-validation tests (Metrics, Filters, Tca)**

Root cause: `MetricsPackageValidationTests`, `FiltersPackageValidationTests`, and `TcaPackageValidationTests` each invoke `dotnet pack ... --configuration Release` against the shared source projects (particularly `SimplicityTools.Metrics.csproj`). When `dotnet test` on the solution runs these test assemblies as concurrent vstest processes, the concurrent invocations race to write to the same `obj/Release/net8.0/ref/SimplicityTools.Metrics.dll` (Roslyn CopyRefAssembly task), causing intermittent `IOException: The process cannot access the file`.

A second contributing race: each test set `NUGET_PACKAGES` to a different isolated global-packages directory, causing concurrent writes of incompatible `project.assets.json` content to `src/SimplicityTools.Metrics/obj/project.assets.json`.

**Fixes applied (3 pack test files):**
1. Added `-p:BaseOutputPath={workingDir}/build-output/{projectSlug}/` per pack invocation → moves compiled `bin/Release/` output to an isolated directory per test run, preventing the originally-reported `deps.json` lock.
2. Added `-p:ProduceReferenceAssembly=false` per pack invocation → disables the Roslyn `CopyRefAssembly` task entirely, eliminating the `obj/Release/net8.0/ref/SimplicityTools.Metrics.dll` race. Reference assemblies are only needed for incremental build optimization and are never included in the NuGet package, so disabling them is safe for the pack-test context.
3. Changed `nugetPackagesDirectory` to nullable `string?`; pack invocations pass `null` (use `~/.nuget/packages/`) while consumer builds still use the isolated `globalPackagesDirectory`. This eliminates the concurrent `project.assets.json` content conflict.

**What NOT to use (lesson learned the hard way):** Passing `-p:BaseIntermediateOutputPath=...` to redirect `obj/` breaks NuGet dependency discovery. When `BaseIntermediateOutputPath` is a global MSBuild property, ALL projects in the multi-project build share it. This causes intra-build collisions (Metrics, Filters, and Tca all write to the same `build-obj/tca/Release/net8.0/` directory), and NuGet loses track of project reference → PackageId mappings, producing nuspec files with MISSING DEPENDENCIES (Filters disappeared from Tca's nuspec). `ProduceReferenceAssembly=false` is the correct minimal fix.

**Also fixed: `AnalyzeCommandTests.BuildCliAsync()` race**  
This helper built with `--configuration Release`, which also races with pack tests on shared project Release `obj/` files. Changed to Debug (no `--configuration` flag) and updated `GetCliAssemblyPath()` from `bin/Release/` to `bin/Debug/`. Performance tests intentionally remain on Release.

**Verification:** Ran all 4 affected test projects in parallel via background `dotnet test` processes simultaneously — all 4 passed. Full solution test run (242 tests, all packages) also passed with zero failures.

---

**Bug B: `VersionFlag_PrintsInformationalVersionAndReturnsZero` hardcoded stale commit SHA**

Root cause: The test originally called `CliHelp.GetInformationalVersion()` to get the expected version string from the IN-PROCESS test assembly. But the subprocess runs a freshly-built CLI binary, which carries the current `SourceRevisionId` from HEAD at build time. When the test-runner and the CLI binary were built at different times (e.g., test DLL stale from a previous build), their `InformationalVersion` SHA suffixes could diverge.

**Fix:** Replaced `Assert.Equal(CliHelp.GetInformationalVersion(), actualOutput)` with `Assert.Matches(@"^0\.5\.0-local\+[0-9a-f]+$", actualOutput)`. The regex validates:
- Version prefix matches the `SimplicityToolsLocalPackageVersion` property from `Directory.Build.props` (`0.5.0-local`)
- `+` separator per SemVer informational version convention
- Hex SHA suffix of any length (`[0-9a-f]+`)

This will not break on the next commit or on any future version bump (once the prefix constant is updated to match `Directory.Build.props`).

---

### 2026-08-01T08:08:11-04:00 — Second-order bug in the version regex fix above

**What went wrong:** The `^0\.5\.0-local\+[0-9a-f]+$` regex introduced in the previous fix was itself a partial hardcode: it assumed both a specific version number (`0.5.0`) and a specific prerelease label (`-local`). In CI, the `test-cli-functional` job (part of Morpheus's now-parallelized nuget-publish.yml pipeline) builds the CLI with a different `-p:Version=` override — in this case `0.8.0` with no `-local` suffix — producing `InformationalVersion = 0.8.0+2b37f69ecd7e9e15e3eaad7f8f7fa0899c5253a3` (full 40-char SHA). The regex failed to match on both counts.

**Root cause of the secondary hardcode:** The `0.5.0-local` portion was copied directly from the `SimplicityToolsLocalPackageVersion` property in `Directory.Build.props`. That value is only produced locally (via the `Condition="'$(Version)' == ''"` fallback). CI always overrides `$(Version)` via the workflow's `pack_version` output, so the local-only suffix must be treated as *optional*, and the version number itself is fully environment-dependent.

**Corrected regex:**
```csharp
@"^\d+\.\d+\.\d+(-[a-zA-Z0-9][a-zA-Z0-9.-]*)?\+[0-9a-f]{7,40}$"
```
This validates:
- Any `major.minor.patch` semver core (no hardcoded numbers)
- Optional prerelease label starting with an alphanumeric character (covers `-local`, `-ci.42`, `-beta.1`, etc.)
- Required `+` separator
- Hex commit hash 7–40 chars (covers git short SHA ~7-12 chars locally and full 40-char SHA in CI)

**Verification:**
1. Regex validated in Node.js REPL against CI failure string (`0.8.0+2b37f69ecd7e9e15e3eaad7f8f7fa0899c5253a3`) and two local shapes (`0.5.0-local+fc6ad7c7a032e8a8b59c1cd5f5a6f`, `0.5.0-local+fc6ad7c`) — all three returned `true`.
2. `VersionFlag_PrintsInformationalVersionAndReturnsZero` ran against the local Debug build and passed (1 test, 0 failures).

**Lesson:** Regex assertions on version strings need to encode the FORMAT CONTRACT, not a specific expected value. Any time a version string is expected to vary across build environments, the assertion should be format-only. Never propagate `SimplicityToolsLocalPackageVersion` directly into a test regex; that value is local-only by design.

