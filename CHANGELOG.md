# Changelog

All notable changes to the Simplicity-First .NET Toolkit are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow
[SemVer](https://semver.org/) with the pre-1.0 policy described in
[docs/distribution-plan.md](docs/distribution-plan.md).

## [0.5.0] — 2026-07-08

First public release. Five packages ship together: `SimplicityTools.Metrics`,
`SimplicityTools.Filters`, `SimplicityTools.Tca` (libraries, `net8.0`/`net10.0`),
`SimplicityTools.Analyzers` (Roslyn analyzers + code fixes, minimum Roslyn 4.4/4.6), and
`SimplicityTools.Cli` (the `dotnet simplicity` tool, `net10.0`).

Everything below describes the changes since the `0.4.0` internal baseline for those following
the repository; for new users, it doubles as a tour of the design decisions.

### Measurement trust

- The collector opens **one** Roslyn workspace per run (previously three), restores once, and
  surfaces workspace load failures through a diagnostics callback instead of silently dropping
  projects from the metrics.
- Multi-targeted projects are measured **once**, not once per target framework.
- `TotalFiles` and `PrimaryPathFileCount` are drawn from the same population (countable source
  files in non-test, solution-declared projects), so `PrimaryPathRatio` is no longer skewed by
  test projects or by projects pulled in from outside the solution.
- Unused-dependency detection reads `project.assets.json` (the authoritative package graph)
  instead of sniffing file paths; analyzer/build-only/meta-packages are never flagged.
- `EstimatedOnboardingTime` is `TimeSpan?` and currently **null**: no output fabricates a
  verdict from an unimplemented metric (previously it reported "0.0h / Efficient").
- Empty or failed collections **fail every filter** with an explicit message instead of scoring
  a perfect 1.0.
- Complexity counting handles modern C# (top-level statements, local functions, pattern
  labels, `??=`, `and`/`or` combinators) identically in the library and the SF0003 analyzer,
  with the counting rules documented.

### Methodology coherence

- TwoAmTest Discoverability measures primary-path files **per project**, resolving its
  mathematical contradiction with PrimaryPathFirst concentration on solutions above ~8 files.
- One threshold-derived scoring model backs the terminal budget, the HTML report bands, and
  the simplicity score — configuring `simplicity.json` moves every surface together.
- TCA reports **excess over target**: a codebase meeting every goal is attributed $0 of
  architecture cost (previously ~$177k–$327k/yr), the at-target baseline is labeled
  separately, and every constant lives in `TcaInputs` with documented rationale.
- A verdict with a collapsed dimension (sub-score < 0.1) cannot pass on the strength of its
  average.

### Features

- **Trend analysis works out of the box**: `report` and `baseline` append snapshots to
  `.simplicity-history/` (30-file retention); the trend section unlocks on the second run.
- **Versioned persistence**: baseline and history files carry
  `{ version, toolVersion, snapshot }` and are validated property-by-property — schema drift
  fails loudly instead of zero-filling into fake regressions. Legacy raw-snapshot files are
  still read.
- **`simplicity.json` is honored everywhere**: filter thresholds (including `passingScore`)
  flow through `analyze`, `report`, `diff`, `budget`, and `watch`.
- **CLI**: `--format json` on `analyze`/`diff`/`budget` (machine-readable, stdout-only);
  per-command `--help` and `--version`; options accepted anywhere in argv; `report --output`
  (default is now the solution directory, not the CWD); actionable messages for missing
  MSBuild/malformed solutions with `--verbose` for detail; documented exit codes.
- **`watch` robustness**: clean Ctrl+C shutdown (no disposed-semaphore race), 64 KB watcher
  buffer with automatic re-arm and catch-up analysis after overflow, a 5-second debounce
  latency cap under continuous churn, and cancellation plumbed through collection.
- **Library hosting**: `SimplicityCollectorOptions` lets hosts opt out of the library's
  process-global side effects (`dotnet restore` and MSBuildLocator registration).

### Analyzers

- SF0001–SF0007 ship with release tracking; package splits into analyzer + code-fix assemblies
  (standard layout), minimum Roslyn lowered to 4.4 (analyzers) / 4.6 (code fixes).
- SF0001/SF0002/SF0006 default to **Info** severity and skip externally-visible symbols;
  `.editorconfig` knobs cover thresholds, excluded packages, convention folders, and
  `simplicity_first.include_public_api`.
- The SF0001 code fix no longer changes semantics silently: `nameof` references suppress the
  fix, DI-style registrations get a review comment, accessibility mismatches bail out, hoisted
  members are rendered symbol-qualified, and cross-project references are matched by identity.
- No file I/O in analyzers; the packaged props wire the project file as an `AdditionalFiles`
  item so SF0002 works in real consumers.
- Analyzers run on targeted syntax/operation/symbol actions with per-compilation state — no
  more repeated whole-compilation walks on the IDE hot path.

### Performance

- The primary-path heuristic no longer runs a whole-solution `FindReferencesAsync` per type
  (quadratic on real solutions); inbound references are counted in a single pass per document
  with byte-identical results.
- End-to-end collection on the bundled samples dropped from minutes to seconds; the CLI test
  suite went from ~16 minutes to under a minute.

### Breaking changes (vs. the 0.4.0 baseline)

- `SimplicitySnapshot` is a `required`/`init` record (no positional constructor);
  `EstimatedOnboardingTime` is `TimeSpan?`; `Empty()` lost its unused parameter.
- `FilterVerdict` collections are `IReadOnlyList<T>`; the vestigial `FilterEvaluation` record
  is gone; violations describe sub-scores below the passing bar only.
- `TcaEstimate` output shape and constants moved to an excess-over-target model; executive
  summary format changed.
- `FilterScoring` thresholds and evaluator signatures now take `FilterThresholds`.
- Baseline/history files are written in the envelope format (old files remain readable).
- `report` writes to `<solution directory>/simplicity-report` by default.

[0.5.0]: https://github.com/cwoodruff/SimplicityTools/releases/tag/libraries%2Fv0.5.0
