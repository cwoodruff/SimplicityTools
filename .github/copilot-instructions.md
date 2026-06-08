# Copilot Instructions for SimplicityTools Repository

## Overview

**SimplicityTools** is a .NET 10 toolkit that measures solution complexity and surfaces simplification opportunities. It ships as five NuGet packages with independent release schedules but shared architectural boundaries.

- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, CLI
- **Release strategy:** SemVer with three independent package groups
- **Zero-config promise:** All commands work on first run with sensible defaults
- **Repository branching:** Sprint-branch-to-main pattern (not dev-based)

---

## High-Level Architecture

### The Five Packages

| Package | Purpose | Dependencies | Release Group | Published By |
|---------|---------|--------------|---------------|------|
| **SimplicityTools.Metrics** | Core data model and snapshot collection primitives | Microsoft.Build, Roslyn | `libraries` | Tag: `libraries/vX.Y.Z` |
| **SimplicityTools.Filters** | Three-filter evaluation engine (TwoAmTest, HalfRule, PrimaryPathFirst) | Metrics (project ref) | `libraries` | Tag: `libraries/vX.Y.Z` |
| **SimplicityTools.Tca** | Annual cost-of-complexity estimates | Metrics, Filters (transitive) | `libraries` | Tag: `libraries/vX.Y.Z` |
| **SimplicityTools.Analyzers** | Roslyn diagnostics and code fixes (SF0001, SF0002, etc) | None; analyzer-only | `analyzers` | Tag: `analyzers/vX.Y.Z` |
| **SimplicityTools.Cli** | Global dotnet tool for `dotnet simplicity` commands | Metrics, Filters, Tca | `cli` | Tag: `cli/vX.Y.Z` |

### Package Dependency Graph

```
Metrics (no external package deps)
  ├→ Filters (depends on Metrics)
  │  └→ Tca (depends on Filters, Metrics)
  └→ Cli (depends on Metrics, Filters, Tca)

Analyzers (no deps; ships independently)
```

**Key constraint:** Metrics, Filters, and Tca **version together** and must use identical `x.y.z` versions. Analyzers and Cli can advance independently.

### Test Structure

- `tests/SimplicityTools.Metrics.Tests/` — Unit tests for snapshot collection, semantic analysis, structural metrics
- `tests/SimplicityTools.Filters.Tests/` — Verdict logic tests (TwoAmTest, HalfRule, PrimaryPathFirst)
- `tests/SimplicityTools.Tca.Tests/` — Cost estimation model tests
- `tests/SimplicityTools.Analyzers.Tests/` — Roslyn diagnostics and code-fix round-trip validation
- `tests/SimplicityTools.Cli.Tests/` — CLI command integration tests
- `artifacts/{*}-package-validation-tests/` — Consumer validation suites; run after packing to prove analyzer/package contracts

### Sample Projects

- `samples/Sample.Simplified/` — Reference architecture (good shape): 2 projects, 23 files, 1 abstraction layer
- `samples/Sample.OverEngineered/` — Anti-pattern (high complexity): 12 projects, 62 files, 25 abstraction layers

---

## Build, Test, and Lint Commands

### Local Development

**Build the entire solution:**
```bash
dotnet build SimplicityTools.sln --nologo --verbosity minimal
```

**Build a specific package:**
```bash
dotnet build src/SimplicityTools.Cli/SimplicityTools.Cli.csproj --nologo --verbosity minimal
```

**Run all tests:**
```bash
dotnet test SimplicityTools.sln --nologo --no-build --verbosity minimal
```

**Run tests for one package suite:**
```bash
dotnet test tests/SimplicityTools.Metrics.Tests/SimplicityTools.Metrics.Tests.csproj --nologo --no-build
```

**Run CLI tests (excluding performance gate; slower, exercises integration):**
```bash
dotnet test tests/SimplicityTools.Cli.Tests/SimplicityTools.Cli.Tests.csproj --nologo --no-build --filter "FullyQualifiedName!~AnalyzeCommandPerformanceTests"
```

**Run the CLI performance gate test only (validates P95 threshold on Sample.OverEngineered):**
```bash
dotnet test tests/SimplicityTools.Cli.Tests/SimplicityTools.Cli.Tests.csproj --nologo --no-build --filter "FullyQualifiedName=SimplicityTools.Cli.Tests.AnalyzeCommandPerformanceTests.AnalyzeCommand_OverEngineeredSample_CompletesWithinExpectedThresholdAtP95"
```

**Pack for local validation (creates `.nupkg` and `.snupkg`):**
```bash
dotnet pack src/SimplicityTools.Metrics/SimplicityTools.Metrics.csproj -c Release --no-build -o artifacts/packages --nologo
dotnet pack src/SimplicityTools.Filters/SimplicityTools.Filters.csproj -c Release --no-build -o artifacts/packages --nologo
dotnet pack src/SimplicityTools.Tca/SimplicityTools.Tca.csproj -c Release --no-build -o artifacts/packages --nologo
dotnet pack src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj -c Release --no-build -o artifacts/packages --nologo
dotnet pack src/SimplicityTools.Cli/SimplicityTools.Cli.csproj -c Release --no-build -o artifacts/packages --nologo
```

**Install CLI from local package feed:**
```bash
dotnet tool install --global SimplicityTools.Cli --add-source "$(pwd)/artifacts/packages" --version 0.4.0-local
```

### Testing the Toolkit Itself

**Analyze Sample.Simplified (good reference):**
```bash
dotnet build src/SimplicityTools.Cli/SimplicityTools.Cli.csproj --nologo --verbosity quiet
dotnet src/SimplicityTools.Cli/bin/Debug/net10.0/SimplicityTools.Cli.dll analyze samples/Sample.Simplified/Sample.Simplified.sln
```

**Analyze Sample.OverEngineered (anti-pattern reference):**
```bash
dotnet src/SimplicityTools.Cli/bin/Debug/net10.0/SimplicityTools.Cli.dll analyze samples/Sample.OverEngineered/Sample.OverEngineered.sln
```

### CI/CD Workflows

**NuGet validation and release pipeline:** `.github/workflows/nuget-publish.yml`
- **Triggers:** Push to `main`, `dev`, `sprint/**`; tag pushes (`libraries/vX.Y.Z`, `analyzers/vX.Y.Z`, `cli/vX.Y.Z`); manual `workflow_dispatch`
- **What it does:**
  - Restores and builds (`dotnet build ... --configuration Release`)
  - Runs full test suite excluding CLI tests (`dotnet test SimplicityTools.sln --filter "FullyQualifiedName!~SimplicityTools.Cli.Tests"`)
  - Runs CLI integration tests (all except performance gate): `--filter "FullyQualifiedName!~AnalyzeCommandPerformanceTests"`
  - Runs the CLI performance gate test in isolation: `AnalyzeCommand_OverEngineeredSample_CompletesWithinExpectedThresholdAtP95` (validates P95 threshold on Sample.OverEngineered)
  - Packs each group (`dotnet pack ... -c Release`)
  - Validates package metadata and analyzer consumer contract
  - Publishes to NuGet.org only on tag pushes
- **Performance test strategy:** Excluded from main CLI test run due to resource intensity; runs separately as a dedicated gate. This isolates performance regression detection without slowing down functional test feedback.
- **Validation mode:** Leave `release_group=validation` in manual dispatch; ignores any stale `version` field and emits `<release-version>-ci.<run-number>` artifacts
- **Release modes:** Set `release_group` to `libraries`, `analyzers`, or `cli` for upload-ready builds; optionally override `version` (defaults to `SimplicityToolsReleaseVersion` from `Directory.Build.props`)

**Docs-site deployment:** `.github/workflows/deploy-site.yml`
- **Trigger:** Push to `main` (also supports manual dispatch)
- **What it does:** Builds Astro site from `docs-site/`, publishes to `gh-pages` branch, GitHub Pages serves to `simplicitytools.dev`

---

## Key Conventions and Patterns

### Version Source of Truth

**File:** `Directory.Build.props`
- **Property:** `SimplicityToolsReleaseVersion` (currently `0.4.0`)
- **What it controls:**
  - Local package defaults: `0.4.0-local` (set via `SimplicityToolsLocalPackageVersion`)
  - CI validation builds: `0.4.0-ci.<run-number>` (set by workflow)
  - Release baseline for manual workflow_dispatch
  - Docs-site footer version display (via `docs-site/scripts/extract-version.mjs` → `docs-site/src/data/version.ts`)

**Never hardcode package versions in individual project files.** Always derive from `Directory.Build.props`.

### Release Tag Conventions

**Tag format:** `<group>/v<X.Y.Z>`
- `libraries/v0.4.0` → publishes Metrics, Filters, Tca
- `analyzers/v0.4.0` → publishes Analyzers only
- `cli/v0.4.0` → publishes Cli only

**Process:**
1. Validate locally: `dotnet build ... && dotnet test ...`
2. Pack release candidates: `dotnet pack ... -c Release --no-build`
3. Test-publish to local feed: `dotnet nuget push ... --source artifacts/local-feed`
4. Consume and validate the install flow (see CONTRIBUTING.md)
5. Create and push the tag (CI handles the rest)

### Package Metadata

**All publishable packages:**
- Include icon: `assets/nuget/simplicitytools-icon.png`
- Include README.md (from repo root)
- License: MIT
- Tags: `dotnet;complexity;architecture;roslyn;cli;analyzers;simplicity`

**Analyzer packages specifically:**
- Use `PrivateAssets="all"` in consumer `.csproj` files
- Must not leak compile-time dependencies to consumers
- Validation suites in `artifacts/analyzer-package-validation-tests/` and `artifacts/analyzer-consumer-validation/` prove this contract

### Test Organization and Coverage

**Roslyn analyzer tests** (`SimplicityTools.Analyzers.Tests`):
- End-to-end validation: trigger diagnostic → apply code fix → re-analyze to verify fix
- Each diagnostic gets before/after test cases
- SF0001 and SF0002 include code-fix round-trip proofs

**Metrics collection tests** (`SimplicityTools.Metrics.Tests`):
- Use fixture test projects in `TestData/` subdirectories (e.g., `StructuralFixture/`, `SemanticFixture/`, `PrimaryPathAnnotationFixture/`)
- Fixture projects must build and snapshot correctly
- Primary-path heuristic covers Controllers, Endpoints, Handlers, Pages

**CLI integration tests** (`SimplicityTools.Cli.Tests`):
- Test all commands: `analyze`, `report`, `watch`, `baseline`, `diff`
- Validate against Sample.Simplified and Sample.OverEngineered
- Slower than unit tests; run last

**Consumer validation suites:**
- Created during build in `artifacts/{package}-package-validation-tests/`
- Proves that package can be restored, referenced, and consumed
- Analyzer suites specifically validate that diagnostics load and `PrivateAssets="all"` is respected

### Docs-Site Synchronization

**File:** `docs-site/` (Astro site)
- **Requirements:** Node.js >= 20.0.0 (check `docs-site/package.json` for current engine requirement)
- **Local development:** `cd docs-site && npm install && npm run dev` (runs Astro dev server with auto-reload)
- **Version extraction:** `docs-site/scripts/extract-version.mjs` reads `Directory.Build.props`, emits `docs-site/src/data/version.ts` (runs automatically as `prebuild` script)
- **Footer:** `docs-site/src/components/SiteFooter.astro` displays `data.version` automatically
- **Build validation:** `npm run build:validate` runs `npm run build && npm run check-links` before deploy; validates all internal links and catches broken references
- **Deployment:** `.github/workflows/deploy-site.yml` runs on pushes to `main`; publishes to `gh-pages` branch

**Important:** Version data is generated at build time from the repo's MSBuild properties, not hardcoded. The prebuild step ensures version.ts is always up-to-date.

### Primary-Path Convention

The toolkit teaches "what's your core business flow?" through:
1. **Explicit annotation:** `[PrimaryPath]` attribute on entry points
2. **Heuristic detection** (when annotation is absent):
   - Controllers (ASP.NET MVC)
   - Endpoints (FastEndpoints, Minimal APIs)
   - Handlers (MediatR, CQRS patterns)
   - Pages (Razor, Blazor)

**Copilot note:** When building test fixtures or samples, mark primary paths explicitly to keep metrics validation deterministic.

### Zero-Config First-Run Promise

**Core principle:** All SimplicityTools commands work immediately without configuration.
- Defaults are sensible (e.g., abstract-layer thresholds, method-complexity limits)
- Missing config emits warnings, never errors
- `simplicity.json` is optional; see `docs/simplicity-schema.json` for reference

**Implication:** When adding new metrics or filters, ensure they degrade gracefully if config is absent.

---

## Repository-Specific Constraints

### macOS Apphost Handling

**Context:** In this worktree, generated .NET apphosts with `.App` suffixes are rejected by Apple integrity enforcement on startup.

**Mitigation:**
- `samples/Sample.Simplified/App/App.csproj` uses `<UseAppHost>false</UseAppHost>`
- Sample assembly renamed to avoid `.App` suffix
- Regression coverage: `dotnet run` smoke test included

**If you add new samples:** Disable apphost generation or use non-`.App` names to avoid macOS startup failures.

### Sprint Branch Organization

**Branching model:** Feature work happens on `sprint/{name}` branches, merged to `main` via PR, not via a persistent `dev` branch.

**Implication for Copilot:**
- Expect `sprint/` prefix in branch names
- PRs target `main`, not `dev`
- Milestone work runs in focused sprints, then closes

---

## Key Files and Their Purposes

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Repo-wide MSBuild configuration; owns `SimplicityToolsReleaseVersion` |
| `SimplicityTools.sln` | Main solution file; all five packages, all tests |
| `CONTRIBUTING.md` | Release process walkthrough; must stay in sync with workflow behavior |
| `README.md` | Marketing + quick-start; customer-facing |
| `.github/workflows/nuget-publish.yml` | Build, test, pack, validate, publish pipeline |
| `.github/workflows/deploy-site.yml` | Astro site build and publish to gh-pages |
| `docs-site/scripts/extract-version.mjs` | Generates version.ts from Directory.Build.props |
| `docs-site/src/components/SiteFooter.astro` | Displays current version in site footer |
| `docs/using-the-simplicity-tools.md` | Complete feature walkthrough and API reference |
| `docs/simplicity-schema.json` | Configuration schema for `simplicity.json` files |
| `.squad/decisions.md` | Team architecture decisions (not exhaustive) |
| `.squad/agents/morpheus/history.md` | Lead's project knowledge and learnings |

---

## Common Workflows

### Adding a New Metric

1. Implement collection logic in `SimplicityTools.Metrics`
2. Add tests in `tests/SimplicityTools.Metrics.Tests` with fixture data if needed
3. Expose the metric in the `ISimplicitySnapshot` contract
4. If metric affects filter verdicts, update `TwoAmTestEvaluator`, `HalfRuleEvaluator`, or `PrimaryPathFirstEvaluator` in Filters
5. Update `docs/using-the-simplicity-tools.md` with metric description
6. Update sample snapshots or expectations in tests
7. Build, test, pack locally: `dotnet build && dotnet test && dotnet pack`

### Adding a New Analyzer Diagnostic

1. Create diagnostic class in `SimplicityTools.Analyzers` (e.g., `SF0003Analyzer`)
2. Create corresponding code-fix class (e.g., `SF0003CodeFix`)
3. Add test cases in `SimplicityTools.Analyzers.Tests` with before/after code snippets
4. Implement end-to-end round-trip validation (trigger → fix → re-analyze)
5. Update README.md table of diagnostics
6. Build, test, pack locally
7. Validate consumer contract: `dotnet add package SimplicityTools.Analyzers --version 0.4.0-local` in a scratch project, ensure diagnostics load in IDE

### Releasing a Package Group

1. Update `SimplicityToolsReleaseVersion` in `Directory.Build.props` if this is a version bump
2. Review `CONTRIBUTING.md` release process
3. Run local validation: `dotnet build && dotnet test && dotnet pack -c Release`
4. Create local feed and test-publish each package
5. Validate install flow (libraries via `dotnet add`, Analyzers via IDE, Cli via `dotnet tool install`)
6. Create SemVer tag (`libraries/vX.Y.Z`, `analyzers/vX.Y.Z`, or `cli/vX.Y.Z`)
7. Push tag; CI handles publishing to NuGet.org
8. Verify on NuGet.org within 5–15 minutes

---

## Troubleshooting

### Build Fails: "MSBuild not found"

**Cause:** Roslyn workspaces API requires MSBuild discovery.  
**Fix:** `dotnet build` runs MSBuild.Locator initialization; restart the shell session if you switch .NET versions mid-session.

### Tests Timeout or Hang

**Cause:** Fixture projects may not compile cleanly, or performance tests may exceed local thresholds.  
**Fix:** Check `tests/SimplicityTools.Metrics.Tests/TestData/` subdirectories; ensure fixture `.csproj` files have valid references. For CLI performance test failures, run locally with `dotnet test ... --filter "AnalyzeCommandPerformanceTests"` to profile Sample.OverEngineered and verify it completes within P95 threshold.

### CLI Performance Test Fails in CI

**Cause:** `AnalyzeCommand_OverEngineeredSample_CompletesWithinExpectedThresholdAtP95` validates that analysis of Sample.OverEngineered (worst-case 12 projects, 62 files, 25 layers) completes within expected time.  
**Fix:** Profile locally: `dotnet run --project src/SimplicityTools.Cli/SimplicityTools.Cli.csproj -- analyze samples/Sample.OverEngineered/Sample.OverEngineered.sln`. If slower than expected, check for O(n²) logic in metrics collection, Roslyn workspace initialization, or fixture compilation issues. Performance test runs last in CI to catch regressions early without blocking functional tests.

### Local Package Install Fails

**Cause:** Analyzer package still has compile-time exports.  
**Fix:** Check `SimplicityTools.Analyzers.csproj` for stray `<ItemGroup>` entries that export compile assets; use `PrivateAssets="all"` in consumer projects.

### Docs-Site Footer Shows Stale Version

**Cause:** `docs-site/src/data/version.ts` wasn't regenerated.  
**Fix:** Run `docs-site/scripts/extract-version.mjs` locally or rebuild the site (`npm run build`).

---

## Related Documentation

- **Full feature guide:** `docs/using-the-simplicity-tools.md`
- **Configuration schema:** `docs/simplicity-schema.json`
- **Quickstart:** `docs/quickstart.md`
- **Release process:** `CONTRIBUTING.md`
- **Team decisions:** `.squad/decisions.md`

---

**Last updated:** 2026-05-28  
**Version baseline:** 0.4.0  
**Maintainer:** Morpheus (Lead, Architecture & Orchestration)
