# M2: Truthful Features & Measurement Trust (0.5.0) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close all 12 issues in milestone M2 (#78–#89) as five area PRs, in dependency order: collector core → CLI trust → watch → analyzers → CI hardening.

**Architecture:** The collector pipeline is consolidated to a single MSBuildWorkspace shared across passes; measurement metrics (TotalFiles/PrimaryPath, unused deps, multi-TFM) are recomputed from one consistent population; the CLI grows a versioned persistence envelope, history writing, and real config threading; the watch command gets shutdown/cancellation/overflow robustness; analyzers become configurable and crash-proof; the publish workflow gets supply-chain hardening.

**Tech Stack:** .NET 10, Roslyn (Microsoft.CodeAnalysis.Workspaces.MSBuild 4.14), xUnit, System.Text.Json, GitHub Actions.

## Global Constraints

- All branches base on `origin/main` (M1 merged as 261d7ac). One branch + PR per area: `m2-collector`, `m2-cli-trust`, `m2-watch`, `m2-analyzers`, `m2-ci-hardening`. Later branches that depend on earlier ones stack on the earlier branch until it merges.
- `TreatWarningsAsErrors` is active for Release builds — run `dotnet build SimplicityTools.sln -c Release` before every push; SF0003 (complexity ≤ 10) and SF0004 (layers ≤ 8) are dogfooded and WILL fail the build.
- Test protocol (memory: simplicitytools-test-gotchas): `dotnet build SimplicityTools.sln` once, then per-project `dotnet test --no-build`. Never bare `dotnet test` on the sln. CLI tests: exclude `AnalyzeCommandPerformanceTests`.
- `tests/SimplicitySampleBaselines.json` + CLI assertions + `README.md` + `docs/quickstart.md` drift as a set — every metric-semantics change updates all four.
- `SimplicitySnapshotTests` locks the snapshot ctor signature via reflection — update `ExpectedConstructorTypes` when the record changes.
- TDD per issue: failing test first, then fix. Commits reference issue numbers.

---

## PR 1 — `m2-collector`: measurement core (#85, #83, #84, #82, #78)

Work these five in this order — they all touch `SimplicityCollector.cs` / `SemanticCollectionPass.cs` / `HeuristicCollectionPass.cs`.

### Task 1.1: Single workspace + workspace-failure surfacing (#85)

**Files:**
- Modify: `src/SimplicityTools.Metrics/SimplicityCollector.cs` (open ONE `MSBuildWorkspace`, do restore once, pass `Solution` to both passes)
- Modify: `src/SimplicityTools.Metrics/SemanticCollectionPass.cs:10-17` → `CollectAsync(Solution solution, CancellationToken)`
- Modify: `src/SimplicityTools.Metrics/HeuristicCollectionPass.cs:10-17` → `CollectPrimaryPathFileCountAsync(Solution solution, CancellationToken)`
- Modify: `src/SimplicityTools.Metrics/MSBuildLocatorRegistration.cs:12-17` (don't mark initialized when RegisterDefaults throws; use lock, not Interlocked-then-throw)
- Modify: `src/SimplicityTools.Metrics/ISimplicityCollector.cs` + collector ctor: add `Action<string>? onDiagnostic` (collector ctor param, default null) invoked for each `WorkspaceFailed` event and for project-count mismatch (workspace projects by distinct FilePath vs solution-file C# project count).
- Test: `tests/SimplicityTools.Metrics.Tests/SimplicityCollectorTests.cs` — add: diagnostic callback receives message when a solution references a nonexistent csproj (new tiny fixture `TestData/BrokenReferenceFixture` whose .sln lists a missing project).

**Interfaces:**
- Produces: `SimplicityCollector(Action<string>? onDiagnostic = null)` public ctor overload; passes now take `Solution`, are called with the same instance.
- Restore runs once (`SolutionRestoreCoordinator.RestoreIfNeededAsync`) before workspace open; passes no longer call it.

Steps: failing diagnostic-callback test → refactor → existing Metrics tests green → commit `feat: single workspace per collection; surface workspace load failures (#85)`.

### Task 1.2: Multi-TFM dedup (#83)

**Files:**
- Modify: `SemanticCollectionPass.cs:25` and `HeuristicCollectionPass.cs:20`: `solution.Projects.Where(ShouldAnalyzeProject).GroupBy(p => p.FilePath, StringComparer.OrdinalIgnoreCase).Select(g => g.First())`
- Modify: `SemanticCollectionPass.cs:36-68`: key interface→implementation matching on compilation-independent identity `(string FullyQualifiedName, string AssemblyName)` instead of `SymbolEqualityComparer.Default` symbol instances.
- Create: `tests/SimplicityTools.Metrics.Tests/TestData/MultiTargetFixture/` — one project with `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>`, one interface + one implementation + one method with known complexity; .sln; restored assets.
- Test: `SimplicityCollectorTests` — assert MultiTargetFixture counts each metric ONCE (e.g. AbstractionLayerCount == 1, single-impl == 1, avg complexity equals the single-TFM value).

Steps: fixture + failing test (expect double-counts today) → dedup fix → identity-key fix → commit `fix: analyze one TFM per project; stop double-counting metrics (#83)`.

### Task 1.3: Consistent PrimaryPathRatio population (#84)

**Decision:** both numerator and denominator come from HeuristicCollectionPass's Roslyn document population (countable source files, test projects excluded). `HeuristicCollectionPass` returns `(int PrimaryPathFileCount, int AnalyzedFileCount)`; snapshot `TotalFiles` = `AnalyzedFileCount`. StructuralCollectionPass keeps TotalProjects (and its file scan stays only as input to nothing — delete the file-counting path). Sub-fixes:
- `HeuristicCollectionPass.cs:31-35`: annotation mode becomes UNION of annotated + convention matches (no more all-or-nothing collapse).
- `HeuristicCollectionPass.cs:161-174`: top-quartile fallback returns 0 extra files when all inbound counts are equal (degenerate distribution) instead of counting everything.
- `StructuralCollectionPass.cs:177-188` glob: `**/` must match root-level files (translate `**/` to `(?:.*/)?`).

**Files:** the two passes, `SimplicityCollector.cs`, `SimplicitySnapshot.cs` doc comment; `tests/SimplicitySampleBaselines.json`, `README.md:246`, `docs/quickstart.md` (TotalFiles semantics change: Sample.Simplified drops its 5 test files → re-measure and update all four); fixture expectations in `SimplicityCollectorTests`.

Steps: failing tests for union-annotation + degenerate-quartile + root-glob → implement → re-measure samples with the CLI, update baselines/docs → commit `fix: PrimaryPathRatio numerator and denominator share one population (#84)`.

### Task 1.4: assets.json-based unused-dependency detection (#82)

**Decision:** new `internal static class PackageAssetsReader` parses `obj/project.assets.json`: declared direct deps from `project.frameworks.<tfm>.dependencies` (skip `"autoReferenced": true`), package→compile/runtime assembly file names from `targets` section. Packages with NO compile/runtime assets (analyzers, build-only, meta-packages with only dependencies) are never flagged unused. "Used" = referenced symbol's containing assembly name ∈ package's assembly set (keep `MarkUsedBySymbol`; DELETE bidirectional namespace-prefix matching `SemanticCollectionPass.cs:268-278`). Stale-restore: `SolutionRestoreCoordinator` also restores when assets.json is older than the csproj (compare LastWriteTimeUtc).

**Files:**
- Create: `src/SimplicityTools.Metrics/PackageAssetsReader.cs`
- Modify: `SemanticCollectionPass.cs:141-300` (replace GetDeclaredPackageReferences + path-sniffing lookup), `SolutionRestoreCoordinator.cs:51-79`
- Test: `tests/SimplicityTools.Metrics.Tests/PackageAssetsReaderTests.cs` (parse a checked-in minimal assets.json: direct dep, autoReferenced dep, analyzer-only package, meta-package), plus SemanticFixture expectations.

Steps: reader tests first → reader → integrate → fixture updates → commit `fix: unused-dependency detection reads project.assets.json (#82)`.

### Task 1.5: EstimatedOnboardingTime honesty (#78)

**Decision:** `SimplicitySnapshot.EstimatedOnboardingTime` becomes `TimeSpan?`; null = not computed (collector passes null). All renderers omit/announce: `ToSummary()` prints `Est. onboarding: not computed`; budget Cognitive Load dimension prints `not computed` (no % / no WITHIN BUDGET verdict); HTML report tile shows `Not yet measured` (no "Efficient" badge); diff skips the onboarding delta row; `TwoAmTestEvaluator.cs:22` drops the onboarding sub-score when null (average remaining sub-scores). Baselines get `"estimatedOnboardingTime": null`.

**Files:** `SimplicitySnapshot.cs`, `SimplicityCollector.cs:55`, `ComplexityBudgetReport.cs:16-22`, `ReportGenerator.cs:505-510`, `SnapshotDiffReportBuilder`, `TwoAmTestEvaluator.cs`, `SimplicitySnapshotTests.ExpectedConstructorTypes`, `tests/SimplicitySampleBaselines.json`, CLI test expected summaries, `docs/quickstart.md` sample output.

Steps: failing tests (summary text, budget text, snapshot contract) → change type + renderers → update baselines/docs → full suite → commit `fix: EstimatedOnboardingTime is nullable; no fabricated verdicts (#78)`.

**PR 1 close-out:** Release build green, all tests green, `dotnet run -- analyze` both samples and eyeball output, PR `M2: measurement trust — collector core` closing #85 #83 #84 #82 #78.

---

## PR 2 — `m2-cli-trust` (stacks on PR 1): #81, #80, #79

### Task 2.1: Versioned persistence envelope (#81)

**Decision:** `internal sealed record SnapshotEnvelope(int Version, string ToolVersion, SimplicitySnapshot Snapshot)`, Version const = 1. `BaselineSnapshotFile.WriteAsync` writes envelope; `ReadAsync`: if root object has `version` property → envelope path, reject unknown versions (`InvalidOperationException` naming found vs supported); else legacy raw snapshot path. BOTH deserialize with `JsonUnmappedMemberHandling.Disallow` + a required-property check so schema drift errors instead of zero-filling. Same for `SnapshotHistory.ReadAsync` (accept envelope + legacy) — and corrupt/unreadable history files log `Skipping unreadable history file '<name>': <reason>` to a `TextWriter` param (stderr) instead of silent catch (`SnapshotHistory.cs:45-53`).

**Files:** Create `src/SimplicityTools.Cli/SnapshotEnvelope.cs`; modify `BaselineSnapshotFile.cs`, `SnapshotHistory.cs`, `Program.cs` (pass Console.Error); tests: envelope round-trip, legacy-file read, unknown-version rejection, missing-property rejection, corrupt-history stderr note, missing-baseline error/exit-code (issue asks for it explicitly).

### Task 2.2: History writing + retention (#80)

**Decision:** `SnapshotHistory.AppendAsync(string solutionPath, SimplicitySnapshot snapshot, int retentionLimit = 30)` writes `<CollectedAt:yyyy-MM-ddTHHmmssZ>.json` (envelope format), prunes oldest-by-filename beyond 30. Called by `report` AND `baseline`. `ReportGenerator.BuildTrendPoints` (`ReportGenerator.cs:594-603`): current snapshot marked by reference identity (exclude any history entry whose CollectedAt == current's, then append current with IsCurrent=true — no tick-equality dedupe as identity).

**Files:** `SnapshotHistory.cs`, `Program.cs` (report/baseline handlers), `ReportGenerator.cs`; tests: append creates file + envelope, retention prunes, report command produces trend after two runs end-to-end (this is the acceptance for "trend analysis reachable").

### Task 2.3: Config threading (#79)

**Decision:** new `public sealed record FilterThresholds(double PrimaryPathRatioTarget, double PrematureAbstractionRatioTarget, double MaxMethodComplexity, double MaxOnboardingHours, double PassingScore)` in `SimplicityTools.Filters` with `static FilterThresholds Default` (0.60/0.25/5/40/0.70 — matches current hardcoded values). All three evaluators + `FilterScoring` + `SnapshotFilterEvaluation.Evaluate` gain `(snapshot, thresholds)` overloads; parameterless overloads delegate with Default (back-compat for Filters.Tests). CLI maps `FilterThresholdConfiguration` → `FilterThresholds` and threads through analyze/report/baseline/diff/watch (watch loads config too). Diff regression thresholds stay const but documented (docs corrected: they are not config keys). Add `RunWithSnapshotAsync(string solutionPath, Func<SimplicitySnapshot, SimplicityConfiguration, Task<int>> action)` helper in Program.cs collapsing the 6× boilerplate — watch SF0003/SF0004 dogfood limits.

**Files:** Create `src/SimplicityTools.Filters/FilterThresholds.cs`; modify `FilterScoring.cs`, `TwoAmTestEvaluator.cs`, `HalfRuleEvaluator.cs`, `PrimaryPathFirstEvaluator.cs`, `SnapshotFilterEvaluation.cs` (Cli), `Program.cs`, `SnapshotDiffReportBuilder`, `ReportGenerator.cs`, `WatchCommand.cs` (accept thresholds), `docs/using-the-simplicity-tools.md:142-150`; tests: evaluator honors custom thresholds; end-to-end: `simplicity.json` with `passingScore: 0.9` flips a filter verdict in `analyze` output.

**PR 2 close-out:** same verification protocol; PR closes #81 #80 #79.

---

## PR 3 — `m2-watch` (stacks on PR 2 because of config threading in watch): #88

### Task 3.1: Watch robustness

All in `src/SimplicityTools.Cli/WatchCommand.cs`:
1. Shutdown race: debouncer callback receives the linked shutdown token (`WatchCommandRunner` ctor line 45-48: pass `_shutdownSource.Token` instead of `CancellationToken.None`); `RunAsync` finally awaits in-flight analysis (`await _analysisGate.WaitAsync(); ... Release()` after cancel, before dispose) so disposal happens at quiescence.
2. FSW: `InternalBufferSize = 65536` in `CreateWatcher`; `OnWatcherError` re-arms (dispose + recreate watcher) and signals the debouncer for a forced full re-analysis with a "watcher overflow — re-scanning" note.
3. Debounce max-latency: `WatchChangeDebouncer` tracks first-signal timestamp; if now - first > 5s force-fire instead of re-postponing (`MaxLatency = TimeSpan.FromSeconds(5)` ctor param).
4. Cancellation plumbing: collector delegate becomes `Func<string, CancellationToken, Task<SimplicitySnapshot>>`; default impl passes token into `CollectAsync`; Program.cs passes command-level tokens into `CollectAsync` for all commands.
5. `WatchAnalysisReportBuilder.Evaluate` (323-331) deleted; use `SnapshotFilterEvaluation.Evaluate` + `GetFilterOrder`.

Tests (new `WatchCommandTests.cs`): debouncer max-latency force-fire; debouncer cancels cleanly on shutdown token (no ODE); collector delegate receives token; existing two watch tests keep passing.

---

## PR 4 — `m2-analyzers` (independent of PRs 1-3, base main): #86, #87

### Task 4.1: Crash-proofing (#87)
- `PackageReferenceAnalysis.cs:142-165`: DELETE the `File.Exists`/`File.ReadAllText` fallback; AdditionalFiles is the only source.
- `ParsePackageReferences` line ~169: wrap `XDocument.Parse` in `try/catch (XmlException) → return empty`.
- `UnusedDependencyAnalyzer.cs:55-59`: only report when the location came from an AdditionalFile; otherwise `Location.None`.
- Ship `buildTransitive/SimplicityTools.Analyzers.props` in the package: `<ItemGroup><AdditionalFiles Include="$(MSBuildProjectFullPath)" Visible="false" /></ItemGroup>` — pack via csproj `<None Pack="true" PackagePath="buildTransitive/">`; update `AnalyzerPackageValidationTests` to assert the props file ships and SF0002 fires in the consumer.
- Tests: malformed csproj AdditionalFile → no crash, no diagnostic; no AdditionalFile → no diagnostic (fallback removed).

### Task 4.2: Configurability + severity (#86)
- SF0001/SF0002/SF0006: `DiagnosticSeverity.Info` defaults; skip externally-visible symbols (`symbol.DeclaredAccessibility == Accessibility.Public` and effectively-visible) in SF0001/SF0006 unless `.editorconfig` `simplicity_first.include_public_api = true`.
- Threshold options read via `AnalyzerConfigOptionsProvider` (compilation-level, `GlobalOptions` + per-tree): keys `simplicity_first.sf0003_complexity_threshold`, `simplicity_first.sf0005_parameter_threshold`, `simplicity_first.sf0004_layer_threshold`, `simplicity_first.sf0002_excluded_packages` (comma-separated), `simplicity_first.sf0007_convention_folders`. Shared helper `AnalyzerOptionReader.cs`. Message formats become parameterized (`exceeds the limit of {2}`) — SF0003 message change ripples to `AnalyzerPackageValidationTests`/docs.
- SF0007: report at first type-declaration identifier location instead of `root.GetLocation()`.
- Tests per knob (in-memory `.editorconfig` via `AnalyzerTestInfrastructure` analyzer-config support), public-API skip tests for SF0001/SF0006.

---

## PR 5 — `m2-ci-hardening` (independent, base main): #89

### Task 5.1: nuget-publish.yml
- `concurrency: nuget-publish` (group, cancel-in-progress false) at workflow level.
- `publish` job: `environment: nuget` (create the environment via `gh api repos/:owner/:repo/environments/nuget` — required reviewers need the user, note in PR).
- `${{ github.event.inputs.version }}` and `release_group` moved into `env:` vars in the "Resolve release shape" step.

### Task 5.2: pins + Dependabot + heartbeat
- SHA-pin `peaceiris/actions-gh-pages` in deploy-site.yml (resolve current v4 SHA via `gh api`); pin actions/* to full SHAs across all workflows (keep version comment).
- Create `.github/dependabot.yml` with `package-ecosystem: github-actions`, weekly.
- `squad-heartbeat.yml`: `actions/checkout` with `ref: main` for the script execution.
- Verification: `actionlint` if available, else YAML parse + a dry-run `workflow_dispatch` where safe.

---

## Execution order & tracking

1. PR 1 tasks 1.1→1.5 (biggest, riskiest — do first, inline)
2. PR 2 tasks 2.1→2.3 (stacks on PR 1 branch)
3. PR 3 (stacks on PR 2)
4. PR 4 (parallel-safe, base main)
5. PR 5 (parallel-safe, base main)

PRs 4 and 5 may be delegated to subagents in worktrees while PRs 1-3 proceed inline, since they share no files with the collector/CLI work.
