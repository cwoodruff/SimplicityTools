# Using the SimplicityTools Toolset

SimplicityTools is a small .NET toolkit built around one job: show teams where a solution has become harder to understand, change, and operate.

This repository currently ships five user-facing packages:

| Package | What it is for |
| --- | --- |
| `SimplicityTools.Cli` | The command-line entry point for analyzing a `.sln`, generating reports, capturing a baseline, comparing drift, checking a budget, and watching for change. |
| `SimplicityTools.Metrics` | The core collector and data model. It opens a solution, counts files/projects/abstractions, calculates complexity, and returns a `SimplicitySnapshot`. |
| `SimplicityTools.Filters` | Evaluates a snapshot against the three Simplicity-First filters: `TwoAmTest`, `HalfRule`, and `PrimaryPathFirst`. |
| `SimplicityTools.Tca` | Converts a snapshot plus filter verdicts into an annual Total Cost of Architecture estimate. |
| `SimplicityTools.Analyzers` | Roslyn diagnostics and code fixes that surface simplification opportunities inside the IDE and normal builds. |

If you only use one thing first, use the CLI.

## Package install surfaces

The published packages are grouped on purpose:

- `SimplicityTools.Metrics`, `SimplicityTools.Filters`, and `SimplicityTools.Tca` ship as one version line.
- `SimplicityTools.Analyzers` can release on its own cadence.
- `SimplicityTools.Cli` can release on its own cadence as the global tool.

Git tags drive published versions:

- `libraries/vX.Y.Z`
- `analyzers/vX.Y.Z`
- `cli/vX.Y.Z`

The release checklist and local test-publish flow live in [`CONTRIBUTING.md`](../CONTRIBUTING.md).

## Prerequisites

- .NET 10 SDK installed (`net10.0` is the current target in this repo)
- A solution file (`*.sln`) to analyze
- A normal restore/build environment for the solution you point the tools at

From this checkout, the fastest way to get started is:

```bash
dotnet build src/SimplicityTools.Cli/SimplicityTools.Cli.csproj --nologo --verbosity quiet
```

That produces the CLI assembly at:

```text
src/SimplicityTools.Cli/bin/Debug/net10.0/SimplicityTools.Cli.dll
```

You can then run commands directly with `dotnet <path-to-dll> ...` from the repo.

## Command naming

The CLI project is packed as the `SimplicityTools.Cli` .NET tool with `ToolCommandName` set to `dotnet-simplicity`.

In practice, the command surface in this repo is documented as:

```bash
dotnet simplicity <command> <solution.sln>
```

If you are running from source instead of an installed tool, use:

```bash
dotnet src/SimplicityTools.Cli/bin/Debug/net10.0/SimplicityTools.Cli.dll <command> <solution.sln>
```

## First run from this repository

Try the simplified sample first:

```bash
dotnet build src/SimplicityTools.Cli/SimplicityTools.Cli.csproj --nologo --verbosity quiet
dotnet src/SimplicityTools.Cli/bin/Debug/net10.0/SimplicityTools.Cli.dll analyze samples/Sample.Simplified/Sample.Simplified.sln
```

You should get a snapshot summary like:

```text
Simplicity Snapshot (YYYY-MM-DD)
----------------------------------------
Projects: ...
Total files: ...
Primary path files: ...
Abstraction layers: ...
Single-impl interfaces: ...
External deps: ... (... unused)
Avg complexity: ...
Est. onboarding: ...h
```

If there is no configuration file beside the solution, the tool warns once per run:

```text
Warning: simplicity.json was not found in '<solution-directory>'. Using built-in defaults for TCA inputs and filter thresholds.
```

That warning is expected. Missing config is not an error.

## Configuration: `simplicity.json`

Place `simplicity.json` beside the solution file you analyze. The loader looks in the solution directory only.

Reference schema:

- `docs/simplicity-schema.json`

Current full configuration shape, including built-in defaults:

```json
{
  "tca": {
    "teamSize": 8,
    "averageEngineerMonthlySalaryUsd": 15000,
    "estimatedMonthlyIncidentCount": 4,
    "onCallHourlyRateUsd": 150,
    "attritionCoefficientPercent": 15
  },
  "filters": {
    "primaryPathRatioTarget": 0.6,
    "prematureAbstractionRatioTarget": 0.25,
    "maxMethodComplexity": 5,
    "maxOnboardingHours": 40,
    "passingScore": 0.7
  }
}
```

### What each setting changes

#### `tca`

These values affect TCA calculations in `SimplicityTools.Tca`:

- `teamSize`: size of the engineering team
- `averageEngineerMonthlySalaryUsd`: monthly salary used in cost estimates
- `estimatedMonthlyIncidentCount`: incidents per month used for operational cost
- `onCallHourlyRateUsd`: hourly on-call rate used for incident cost
- `attritionCoefficientPercent`: turnover pressure factored into cognitive cost

#### `filters`

These values affect pass/fail thresholds and budget output:

- `primaryPathRatioTarget`: minimum acceptable primary-path coverage
- `prematureAbstractionRatioTarget`: maximum acceptable single-implementation interface ratio
- `maxMethodComplexity`: maximum acceptable average method complexity
- `maxOnboardingHours`: maximum acceptable onboarding time
- `passingScore`: pass threshold for filter verdicts

### Important configuration behavior

- Partial files are supported; missing values fall back to defaults.
- Unsupported properties fail fast.
- Invalid ranges fail fast.
- A bad config stops the command instead of silently continuing.

A practical minimal override looks like this:

```json
{
  "filters": {
    "primaryPathRatioTarget": 0.75,
    "maxMethodComplexity": 4
  }
}
```

## CLI commands

The CLI currently supports six commands.

### `analyze`

Analyze a solution and print a compact snapshot summary.

```bash
dotnet simplicity analyze path/to/YourSolution.sln
```

Use this when you want a quick read of solution shape without producing files.

What it measures:

- total projects
- total countable source files
- primary-path file count
- abstraction layer count
- interfaces with a single implementation
- external dependency count
- unused dependency count
- average method complexity
- estimated onboarding time

Notes:

- The structural project count still reflects all C# projects in the solution, but the semantic and primary-path passes skip test projects when calculating things like abstractions, dependencies, and primary-path heuristics.
- Primary-path detection uses explicit `[PrimaryPath]` annotations first; if none exist, it falls back to `Controllers`, `Endpoints`, `Handlers`, and `Pages`, plus a reference-based heuristic.

### `report`

Generate a self-contained HTML report.

```bash
dotnet simplicity report path/to/YourSolution.sln
```

Output:

```text
./simplicity-report/index.html
```

What the report contains right now:

- Executive Summary
- Filter Verdicts
- Metric Detail
- Complexity Budget
- Trend Analysis
- Appendix

Useful details:

- The report is fully self-contained: inline styles, no external scripts, no external assets.
- The output path is always `./simplicity-report/index.html` relative to the current working directory.
- The watch command ignores the `simplicity-report` directory so report generation does not create watch loops.

Expected success message:

```text
Report generated to ./simplicity-report/index.html
```

### Trend history in reports

Trend history is file-based.

The report loader scans:

```text
.simplicity-history/*.json
```

Requirements:

- files must deserialize as `SimplicitySnapshot`
- at least two historical snapshots must exist for the full trend wave to render
- invalid or unreadable JSON files are skipped

If there is not enough history, the report shows an on-ramp message instead of a chart.

Important: there is currently **no dedicated CLI command that archives history for you**. If you want trend charts, you need to save snapshot JSON files into `.simplicity-history/` yourself.

A simple workflow is:

1. capture a baseline with `baseline`
2. copy or serialize snapshots into `.simplicity-history/`
3. run `report` again

### `baseline`

Capture the current solution state as the comparison file for future drift checks.

```bash
dotnet simplicity baseline path/to/YourSolution.sln
```

Output file:

```text
<solution-directory>/.simplicity-baseline.json
```

Behavior:

- overwrites any existing baseline file
- writes indented camelCase JSON
- prints the snapshot summary first, then the file path

Expected confirmation line:

```text
Baseline written to <solution-directory>/.simplicity-baseline.json
```

Use this when you want to say, “this is the shape we are willing to compare against.”

### `diff`

Compare the current solution to the saved baseline.

```bash
dotnet simplicity diff path/to/YourSolution.sln
```

Optional CI gate:

```bash
dotnet simplicity diff path/to/YourSolution.sln --fail-on-regression
```

What it prints:

- baseline file path
- baseline and current snapshot dates
- metric deltas
- filter score deltas
- regression summary

Regression rules currently checked:

- `PrematureAbstractionRatio` increase greater than `+0.05`
- `AverageMethodComplexity` increase greater than `+0.50`
- any increase in unused dependency count when the current count is non-zero
- any filter score drop greater than `0.10`

Expected footer pattern:

```text
Regression status: no regressions detected.
```

or:

```text
Regression status: N regression(s) detected.
```

If the baseline file is missing, the command fails with a next-step message telling you to run `dotnet simplicity baseline <solution.sln>` first.

### `budget`

Render a four-line complexity budget scorecard using the current snapshot and `simplicity.json` thresholds.

```bash
dotnet simplicity budget path/to/YourSolution.sln
```

Current budget dimensions:

| Dimension | Metric | Threshold source |
| --- | --- | --- |
| Cognitive Load | onboarding time | `filters.maxOnboardingHours` |
| Operational Surface | premature abstraction ratio | `filters.prematureAbstractionRatioTarget` |
| Change Safety | average method complexity | `filters.maxMethodComplexity` |
| Discoverability | primary path ratio | `filters.primaryPathRatioTarget` |

Output includes:

- status line (`X/4 dimension(s) within budget`)
- ASCII bars
- actual values
- configured targets
- next-move guidance

Example shape:

```text
Complexity Budget
-----------------
Status: ...
Bars show configured budget used. Values above 100% are over budget.
```

This command is the quickest way to show whether a team’s current thresholds are realistic.

Important current behavior: the collector currently emits `0h` for `EstimatedOnboardingTime`, so the Cognitive Load line will read `0.0h` unless that metric is supplied by another snapshot source in your own code.

### `watch`

Run analysis continuously while files change.

```bash
dotnet simplicity watch path/to/YourSolution.sln
```

What happens:

- prints `Watching <full-solution-path>`
- prints `Press Ctrl+C to stop.`
- runs an immediate `Initial snapshot`
- re-runs after file changes using a 500 ms debounce
- prints `Updated snapshot` plus filter verdicts after each refresh

The watcher ignores these paths to avoid self-triggering noise:

- `bin`
- `obj`
- `.git`
- `.vs`
- `simplicity-report`

Config behavior in watch mode:

- `simplicity.json` is reloaded on every pass
- missing-config warnings are suppressed after the first warning until the file appears again

This is the right command for refactoring sessions where you want immediate “did this get simpler?” feedback.

## Filter verdicts and what they mean

The repository currently evaluates three filters.

### `TwoAmTest`

Asks whether the solution is understandable and fixable under pressure.

Signals used:

- primary-path file count
- average method complexity
- abstraction layers per project
- estimated onboarding time

### `HalfRule`

Asks whether the solution is accumulating indirection faster than value.

Signals used:

- premature abstraction ratio
- unused dependency accumulation
- dependency count per project

### `PrimaryPathFirst`

Asks whether the main business flow is still obvious.

Signals used:

- primary-path concentration
- abstraction dilution around the primary path
- project count

Filter verdicts carry:

- pass/fail
- score
- summary sentence
- violation list
- one next recommendation

The CLI watch output and HTML report both surface these verdicts directly.

## Analyzer package and code-fix usage

`SimplicityTools.Analyzers` is the Roslyn package in this repo. It currently ships seven diagnostics.

| ID | Rule | Category | Code fix |
| --- | --- | --- | --- |
| `SF0001` | Interface has single implementation | HalfRule | Yes |
| `SF0002` | Package reference has no symbol usage | HalfRule | Yes |
| `SF0003` | Method is too complex for fast understanding | TwoAmTest | No |
| `SF0004` | Method call chain is too deep | PrimaryPathFirst | No |
| `SF0005` | Constructor takes too many parameters | TwoAmTest | No |
| `SF0006` | Generic parameter has only one specialization | HalfRule | No |
| `SF0007` | Supporting file is referenced more than the primary path | PrimaryPathFirst | No |

### Current thresholds baked into analyzers

- `SF0003`: cyclomatic complexity over `10`
- `SF0004`: call chain depth over `8`
- `SF0005`: constructor parameter count over `7`

### Current code fixes

#### `SF0001`

Rewrites interface references to the single concrete implementation and removes the interface where possible.

Use this when the interface exists only as ceremony.

#### `SF0002`

Removes the targeted `<PackageReference>` from the project file without rewriting unrelated XML.

Use this when the package is referenced but contributes no used symbols.

### How to consume the analyzer package

Inside this repo, the CLI project already references the analyzer project as an analyzer.

For another solution, the normal consumer shape is a package reference such as:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Analyzers" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>
```

That package is intentionally analyzer-only: it lights up Roslyn diagnostics and code fixes in the IDE/build, but it does not add compile-time library references to the consuming project.

If you also want explicit primary-path annotations in application code, reference `SimplicityTools.Metrics` and use `[PrimaryPath]` on a class or method:

```csharp
using SimplicityTools.Metrics;

[PrimaryPath]
public sealed class CheckoutHandler
{
}
```

If you do not annotate anything, the tooling falls back to path conventions:

- `Controllers`
- `Endpoints`
- `Handlers`
- `Pages`

## Library usage

If you want to build your own tooling around the repo packages instead of the CLI, these are the main entry points.

### `SimplicityTools.Metrics`

```csharp
using SimplicityTools.Metrics;

var collector = new SimplicityCollector();
var snapshot = await collector.CollectAsync("path/to/YourSolution.sln");
Console.WriteLine(snapshot.ToSummary());
```

`SimplicitySnapshot` exposes the raw measures plus computed ratios like:

- `PrimaryPathRatio`
- `PrematureAbstractionRatio`

### `SimplicityTools.Filters`

Install the package directly when you want the filter evaluators without taking a project reference to this repo:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Filters" Version="x.y.z" />
</ItemGroup>
```

`SimplicityTools.Filters` brings in `SimplicityTools.Metrics` transitively, so a separate metrics package reference is only needed when you also want `SimplicityCollector` or other metrics-first APIs directly.

```csharp
using SimplicityTools.Filters;

var verdicts = new[]
{
    TwoAmTestEvaluator.Evaluate(snapshot),
    HalfRuleEvaluator.Evaluate(snapshot),
    PrimaryPathFirstEvaluator.Evaluate(snapshot)
};
```

Each verdict includes `Passes`, `Score`, `Summary`, `Violations`, and `Recommendations`.

### `SimplicityTools.Tca`

Install the package directly when you want the annual cost model without a project reference to this repo:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Tca" Version="x.y.z" />
</ItemGroup>
```

`SimplicityTools.Tca` brings in both `SimplicityTools.Filters` and `SimplicityTools.Metrics` transitively, so a separate package reference is only needed when you want to pin one of those library surfaces explicitly.

```csharp
using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using SimplicityTools.Tca;

var estimate = TcaEstimate.Create(snapshot, verdicts);
Console.WriteLine(estimate.ToExecutiveSummary());
```

Important current behavior: the CLI validates and loads the `tca` section in `simplicity.json`, but there is not yet a dedicated CLI command that prints the TCA estimate. Today, the TCA package is primarily useful when you are calling the libraries directly.

You can also provide explicit inputs instead of defaults:

```csharp
var estimate = TcaEstimate.Create(
    snapshot,
    verdicts,
    new TcaInputs(
        TeamSize: 12,
        AverageEngineerMonthlySalaryUsd: 18000m,
        EstimatedMonthlyIncidentCount: 3,
        OnCallHourlyRateUsd: 175m,
        AttritionCoefficientPercent: 12m));
```

## Practical workflows

### Local first-pass on a solution

```bash
dotnet simplicity analyze path/to/YourSolution.sln
dotnet simplicity budget path/to/YourSolution.sln
dotnet simplicity report path/to/YourSolution.sln
```

Use this flow when you want a fast read, a target check, and something shareable.

### Establish a baseline and protect it in CI

```bash
dotnet simplicity baseline path/to/YourSolution.sln
dotnet simplicity diff path/to/YourSolution.sln --fail-on-regression
```

Typical usage:

- commit `.simplicity-baseline.json`
- run `diff --fail-on-regression` in pull requests
- fail the build when simplicity regresses

### Live refactoring loop

Terminal 1:

```bash
dotnet simplicity watch path/to/YourSolution.sln
```

Then refactor until:

- complexity budget improves
- filter verdicts flip from fail to pass
- diff output stops reporting regression

### Building a shareable trend report

1. save historical snapshot JSON files into `.simplicity-history/`
2. run `dotnet simplicity report ...`
3. publish `simplicity-report/index.html` as a build artifact

## Sample solutions in this repo

The repo includes two teaching samples:

| Sample | What it shows |
| --- | --- |
| `samples/Sample.Simplified` | A compact solution shape with 2 projects, 23 files, 5 primary-path files, and 1 abstraction layer in the checked-in baseline set. |
| `samples/Sample.OverEngineered` | A deliberately over-layered solution with 12 projects, 62 files, 31 primary-path files, and 25 abstraction layers in the checked-in baseline set. |

These are useful for demos, docs screenshots, and CI examples because the test suite already validates the commands against them.

## Troubleshooting

### “`simplicity.json` was not found”

That is a warning, not a failure. The command continues with defaults.

### “Invalid simplicity.json”

The file failed schema-like validation in the loader. Check for:

- unsupported property names
- non-numeric values where numbers are expected
- values outside allowed ranges (for example, `passingScore` above `1`)

### “Baseline file was not found”

Run:

```bash
dotnet simplicity baseline path/to/YourSolution.sln
```

before using `diff`.

### Report has no trend wave

That means `.simplicity-history/` does not yet contain at least two readable `SimplicitySnapshot` JSON files.

### Watch mode feels noisy

The debounce window is already built in, and common generated directories are ignored. If it is still noisy, check whether your edits are touching files outside the ignored paths.

### Primary path detection looks wrong

Add explicit `[PrimaryPath]` annotations through `SimplicityTools.Metrics`, or move the core flow into convention-matching folders such as `Controllers`, `Endpoints`, `Handlers`, or `Pages`.

### I expected an analyzer code fix but do not see one

Right now, only `SF0001` and `SF0002` have code fixes in this repository. The other diagnostics are advisory only.

## What to do next

If you are introducing the toolkit to a team, the most useful sequence is:

1. `analyze` for the quick read
2. `budget` for target visibility
3. `baseline` to capture today
4. `diff --fail-on-regression` in CI
5. `report` when you need a shareable artifact
6. `watch` during simplification work

That sequence matches the current codebase well and keeps the first run focused on “what changed, what matters, and what should I do next?”
