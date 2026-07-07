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

These values are validated and reserved for TCA cost estimation with the `SimplicityTools.Tca` library API. No CLI command consumes them yet:

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

Filter thresholds apply to the filter verdicts shown by `report`, `diff`, and `watch`, and to the `budget` dimensions. The `diff` regression deltas themselves (premature abstraction +0.05, average complexity +0.5, filter score −0.10) are fixed and not configurable.

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

> **Closed filter set (by design).** The evaluator surface is intentionally not extensible:
> `FilterName` is a closed enum and the three evaluators are static. The filters encode the
> Simplicity-First methodology rather than a plugin system — a custom "filter" with different
> semantics would silently change what the scores, budget, and TCA output mean. If your
> thresholds differ, tune them via `simplicity.json`; if you need different *measurements*,
> consume `SimplicitySnapshot` directly and score it yourself.

### `TwoAmTest`

Asks whether the solution is understandable and fixable under pressure.

Signals used:

- primary-path files **per project** (target: five)
- average method complexity
- abstraction layers per project
- estimated onboarding time

> **Why per project?** Discoverability asks "can one flow be traced through a handful of files at
> 2 AM," while `PrimaryPathFirst` rewards putting *most* of the codebase on the primary path. An
> absolute file cap made those two goals mathematically incompatible for any solution above ~8
> files — growing the primary path improved one score exactly as it destroyed the other.
> Normalizing by project count measures navigability of a single flow (which happens within a
> project boundary) without penalizing healthy concentration at scale.

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

## Complexity counting rules

The `AverageMethodComplexity` metric (from `SimplicityTools.Metrics`) and the `SF0003` analyzer use the same cyclomatic complexity counter. Two implementations exist (analyzer assemblies cannot reference the metrics assembly), but they follow identical rules and a shared test battery keeps them producing identical numbers.

### Measured units

Each of the following is measured as its own unit, starting at a base complexity of **1**:

- method bodies (block or expression-bodied)
- constructor bodies
- operator and conversion operator bodies
- accessor bodies (`get`, `set`, `init`, `add`, `remove`)
- expression-bodied properties and indexers
- local functions — measured **separately**; their bodies do **not** count toward the enclosing member
- a file's top-level statement block (a branchy top-level `Program.cs` counts as one method-equivalent)

Lambdas and anonymous methods are **not** separate units: their branches count toward the enclosing unit.

### What adds +1

| Construct | Count |
| --- | --- |
| `if` | +1 (the `else` keyword is free; `else if` counts via its `if`) |
| `for`, `foreach`, `while`, `do` | +1 each |
| `catch` clause | +1 each |
| conditional expression `a ? b : c` | +1 |
| conditional access `?.` | +1 per `?.` (opinionated: every null-conditional hop is a hidden branch; most tools do not count this) |
| `case` label in a `switch` statement | +1 each, constant or pattern (`case 1:`, `case string s:`, `case int n when n > 0:`); the `default:` label is free |
| `switch` expression arm | +1 each; the discard arm `_ =>` without a `when` clause is free (it is the `else`-equivalent), but `_ when ... =>` counts |
| `&&`, `\|\|` | +1 each |
| `??`, `??=` | +1 each |
| pattern combinators `and`, `or` | +1 each (in `is` expressions, `case` labels, and switch expression arms alike) |

Not counted: `else` on its own, `default:` labels, bare discard arms, `when` clauses (the label or arm carrying them already counts), `is` checks without combinators, `finally` blocks, and `goto`/`break`/`continue`/`return`/`throw`.

If you change any rule here, change both `SimplicityTools.Metrics/CyclomaticComplexityAnalyzer.cs` and `SimplicityTools.Analyzers/CyclomaticComplexityCalculator.cs`, plus the shared test battery in `tests/Shared/ComplexityCountingTestCases.cs`.

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

### Default severities

- `SF0003`, `SF0004`, `SF0005`, `SF0007` report as **Warning**.
- `SF0001`, `SF0002`, `SF0006` report as **Info** by default (they are advisory half-rules). Raise them with `dotnet_diagnostic.SF0001.severity = warning` (and friends) in `.editorconfig` or `.globalconfig`.
- `SF0001` and `SF0006` skip externally visible (public) interfaces and generic definitions by default, since public API shape is often a deliberate contract. Opt back in with `simplicity_first.include_public_api = true`.

### Default thresholds

- `SF0003`: cyclomatic complexity over `10` per measured unit (methods, constructors, operators, accessors, local functions, expression-bodied properties/indexers, and top-level statement blocks — see [Complexity counting rules](#complexity-counting-rules))
- `SF0004`: call chain depth over `8`
- `SF0005`: constructor parameter count over `7`

### Configuring the analyzers

All knobs are read from analyzer config options (`.editorconfig` or `.globalconfig`). Invalid values silently fall back to the defaults.

| Key | Default | Effect |
| --- | --- | --- |
| `simplicity_first.sf0003_complexity_threshold` | `10` | Cyclomatic complexity limit for SF0003 |
| `simplicity_first.sf0004_layer_threshold` | `8` | Abstraction layer depth limit for SF0004 |
| `simplicity_first.sf0005_parameter_threshold` | `7` | Constructor parameter limit for SF0005 |
| `simplicity_first.sf0002_excluded_packages` | empty | Comma-separated package ids SF0002 never reports |
| `simplicity_first.sf0007_convention_folders` | `Controllers,Endpoints,Handlers,Pages` | Folder names treated as primary path by SF0007 |
| `simplicity_first.include_public_api` | `false` | Analyze externally visible API in SF0001/SF0006 |

Example `.editorconfig`:

```ini
[*.cs]
simplicity_first.sf0003_complexity_threshold = 15
simplicity_first.sf0002_excluded_packages = Serilog, coverlet.collector
simplicity_first.include_public_api = true
```

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

That package is intentionally analyzer-only: it lights up Roslyn diagnostics and code fixes in the IDE/build, but it does not add compile-time library references to the consuming project. The package also ships `build`/`buildTransitive` props that expose the consuming project's `.csproj` to the analyzers as an `AdditionalFiles` item, which is what allows `SF0002` to inspect `PackageReference` items without any file I/O.

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

## Library integration

If you want to build your own tooling around the SimplicityTools packages instead of using the CLI, these sections describe each library and its integration point.

### Using SimplicityTools.Metrics

**Package:** `SimplicityTools.Metrics` on [NuGet.org](https://www.nuget.org/packages/SimplicityTools.Metrics/)

**Purpose:** Snapshot a solution's structural and code complexity metrics. This is the foundation for all other analyses.

**Install:**

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Metrics" Version="x.y.z" />
</ItemGroup>
```

**Basic usage:**

```csharp
using SimplicityTools.Metrics;

var collector = new SimplicityCollector();
var snapshot = await collector.CollectAsync("path/to/YourSolution.sln");

Console.WriteLine($"Projects: {snapshot.ProjectCount}");
Console.WriteLine($"Files: {snapshot.TotalFileCount}");
Console.WriteLine($"Abstraction layers: {snapshot.AbstractionLayerCount}");
Console.WriteLine($"Avg complexity: {snapshot.AverageMethodComplexity:F2}");
Console.WriteLine($"Primary path ratio: {snapshot.PrimaryPathRatio:P0}");
```

**What you get:**

`SimplicitySnapshot` exposes these core measurements:

- `ProjectCount` — Total C# projects in the solution
- `TotalFileCount` — Total source files analyzed
- `PrimaryPathFileCount` — Files on the primary business path
- `AbstractionLayerCount` — Depth of indirection layers
- `SingleImplementationInterfaceCount` — Dead abstractions (interface with one impl)
- `ExternalDependencyCount` — Package references
- `UnusedDependencyCount` — Packages not used in code
- `AverageMethodComplexity` — Average cyclomatic complexity
- `EstimatedOnboardingHours` — Cost estimate for team onboarding

Plus computed ratios:

- `PrimaryPathRatio` — Percentage of code on the main business flow
- `PrematureAbstractionRatio` — Ratio of single-impl interfaces to total interfaces

**When to use:** Build custom analysis dashboards, embed metrics in your build pipeline, or feed metrics into decision support systems.

---

### Using SimplicityTools.Filters

**Package:** `SimplicityTools.Filters` on [NuGet.org](https://www.nuget.org/packages/SimplicityTools.Filters/)

**Purpose:** Evaluate a snapshot against three Simplicity-First health filters that turn raw metrics into pass/fail verdicts.

**Install:**

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Filters" Version="x.y.z" />
</ItemGroup>
```

Note: `SimplicityTools.Filters` includes `SimplicityTools.Metrics` transitively, so you do not need a separate Metrics reference unless you want to call `SimplicityCollector` directly.

**Basic usage:**

```csharp
using SimplicityTools.Filters;

var verdicts = new[]
{
    TwoAmTestEvaluator.Evaluate(snapshot),
    HalfRuleEvaluator.Evaluate(snapshot),
    PrimaryPathFirstEvaluator.Evaluate(snapshot)
};

foreach (var verdict in verdicts)
{
    Console.WriteLine($"{verdict.Filter}:");
    Console.WriteLine($"  Status: {(verdict.Passes ? "PASS" : "FAIL")}");
    Console.WriteLine($"  Score: {verdict.Score:P0}");
    Console.WriteLine($"  Summary: {verdict.Summary}");
    
    if (!verdict.Passes)
    {
        Console.WriteLine($"  Violations:");
        foreach (var violation in verdict.Violations)
        {
            Console.WriteLine($"    - {violation}");
        }
    }
    
    if (verdict.Recommendations.Length > 0)
    {
        Console.WriteLine($"  Next step: {verdict.Recommendations[0]}");
    }
}
```

**The three filters:**

1. **TwoAmTest** — Can the team understand and fix this code under pressure? Checks primary-path clarity, method complexity, abstraction depth, and onboarding time.

2. **HalfRule** — Is the codebase accumulating indirection faster than value? Checks premature abstraction, unused dependencies, and dependency concentration per project.

3. **PrimaryPathFirst** — Is the main business flow still obvious? Checks primary-path concentration, abstraction around the core flow, and project count.

**Verdict structure:**

Each verdict includes:

- `Filter` — Filter name enum (TwoAmTest, HalfRule, or PrimaryPathFirst)
- `Passes` — Boolean pass/fail
- `Score` — Numeric score (0.0 to 1.0)
- `Summary` — One-line interpretation
- `SubScores` — Named sub-scores that contributed to the composite score
- `Violations` — Array of failed checks with explanations
- `Recommendations` — Array of actionable suggestions to improve the score (typically one primary recommendation)

**When to use:** Gate code reviews, build dashboards that surface health verdicts, or create pass/fail checks for CI/CD pipelines.

---

### Using SimplicityTools.Tca

**Package:** `SimplicityTools.Tca` on [NuGet.org](https://www.nuget.org/packages/SimplicityTools.Tca/)

**Purpose:** Estimate the annual cost of complexity *in excess of* the Simplicity-First targets. A codebase that meets every target reports $0 architecture-attributed cost; only the portion of each metric beyond its target is charged.

**Install:**

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Tca" Version="x.y.z" />
</ItemGroup>
```

Note: `SimplicityTools.Tca` includes both `SimplicityTools.Filters` and `SimplicityTools.Metrics` transitively.

**Basic usage:**

```csharp
using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using SimplicityTools.Tca;

var estimate = TcaEstimate.Create(snapshot, verdicts);

Console.WriteLine(estimate.ToExecutiveSummary());
```

For a sample snapshot (6 projects, 4 unused dependencies, average complexity 7.5, 60h onboarding, 25% premature abstraction) this outputs:

```text
Total Cost of Architecture (Annual Estimate)
============================================
Architecture excess over simplicity targets:
Infrastructure:   $2,016 - $3,744
Operational:      $10,080 - $18,720
Coordination:     $100,800 - $187,200
Cognitive:        $85,050 - $157,950
Opportunity:      $92,120 - $171,080
--------------------------------------------
TOTAL EXCESS:     $290,066 - $538,694 per year
Baseline operating cost at target: $158,400 per year (not attributed to architecture)
```

The sample above is the actual output of `TcaEstimate.Create` with default inputs and evaluator-produced verdicts (the CLI does not surface TCA output yet; the `tca` section of `simplicity.json` is validated but reserved).

**The excess-over-target model:**

Every dimension computes an excess factor `max(0, (metric - target) / target)`, capped at `TcaInputs.MaxExcessFactor` (default 3.0) so no dimension scales without bound. A metric exactly at its target contributes $0. The spend a solution would still incur at target — infrastructure for its projects plus coordination for up to the baseline project count — is reported separately as `BaselineOperatingCostPerYear` and is never attributed to architecture.

1. **Infrastructure** — Charges the per-project platform budget (`MonthlyInfrastructureCostPerProjectUsd`) scaled by unused dependencies: each dead dependency adds `InfrastructureExcessPerUnusedDependency` (5%) of the baseline. Zero unused dependencies means zero infrastructure excess. This models restore/scan/upgrade churn from dead dependencies only — it does not measure build time or CI/CD pipeline complexity.
2. **Operational** — Charges the incident bill (on-call rate x monthly incidents x 12 x `IncidentCostMultiplier`) scaled by how far average method complexity exceeds `TargetAverageMethodComplexity` (5). At complexity 5 or below, no incident cost is attributed to architecture.
3. **Coordination** — Charges `MonthlyCoordinationCostPerProjectUsd` for each project beyond `BaselineProjectCount` (3), capped at `MaxExcessFactor` times the baseline. Coordination for the first three projects is baseline operating cost.
4. **Cognitive** — Charges attrition-weighted payroll scaled by how far estimated onboarding time exceeds `TargetOnboardingHours` (40), inflated by `PrematureAbstractionUpliftFactor` per unit of premature-abstraction ratio. When onboarding time has not been measured (`EstimatedOnboardingTime` is null), the dimension reports $0 excess and a note says it was not measured — the model never fabricates a cost.
5. **Opportunity** — Charges `PayrollOpportunityFactor` (40%) of annual payroll scaled by the shortfall of the composite filter score below a perfect 1.0. Perfect filter scores mean zero opportunity cost.

All figures are order-of-magnitude estimates, not measurements. Each dimension is reported as a range (`RangeLowMultiplier` / `RangeHighMultiplier`, default +/-30%). Non-finite metrics (NaN/Infinity) contribute zero excess and add an explanatory note; negative metrics and inputs are clamped to zero so no dimension can produce negative dollars.

**Model constants (all configurable on `TcaInputs`):**

| Property | Default | Rationale |
| --- | --- | --- |
| `MonthlyInfrastructureCostPerProjectUsd` | `200m` | Rough per-project platform spend (build agents, hosting, tooling seats). A placeholder, not a benchmark — replace with your actual spend. |
| `InfrastructureExcessPerUnusedDependency` | `0.05m` | Each unused dependency wastes ~5% of a project's platform budget on restore, scanning, and upgrade churn. |
| `IncidentCostMultiplier` | `4m` | Each on-call hour consumes roughly three more engineer-hours in interruptions, context switching, and follow-up. |
| `MonthlyCoordinationCostPerProjectUsd` | `4000m` | A few engineer-days of cross-team coordination per project per month at typical loaded rates. |
| `BaselineProjectCount` | `3` | Projects up to this count incur normal coordination; only projects beyond it are charged to architecture. |
| `PrematureAbstractionUpliftFactor` | `0.5m` | A fully premature abstraction layer inflates onboarding cost by 50% (newcomers chase indirection). |
| `PayrollOpportunityFactor` | `0.4m` | At most ~40% of engineering time is discretionary feature work that complexity can crowd out. |
| `RangeLowMultiplier` / `RangeHighMultiplier` | `0.7m` / `1.3m` | These are order-of-magnitude estimates; every dimension is reported +/-30%. |
| `MaxExcessFactor` | `3.0m` | Caps every excess factor; beyond 3x its at-target reference cost the linear model stops being credible. |
| `TargetAverageMethodComplexity` | `5m` | Matches the TwoAmTest diagnosability target. |
| `TargetOnboardingHours` | `40m` | One working week; matches the TwoAmTest cognitive-load target. |

**Using custom inputs:**

The team-shaped assumptions are positional constructor parameters; the model constants are init-only properties:

```csharp
var customInputs = new TcaInputs(
    TeamSize: 12,
    AverageEngineerMonthlySalaryUsd: 18000m,
    EstimatedMonthlyIncidentCount: 3,
    OnCallHourlyRateUsd: 175m,
    AttritionCoefficientPercent: 12m)
{
    MonthlyInfrastructureCostPerProjectUsd = 350m,
    MaxExcessFactor = 2.0m
};

var estimate = TcaEstimate.Create(snapshot, verdicts, customInputs);
```

**Configuration via simplicity.json:**

The CLI validates `tca` settings from `simplicity.json` but does not surface a TCA report yet. Library consumers pass inputs directly to `TcaEstimate.Create()`.

**When to use:** Justify refactoring investment to leadership, quantify the business case for code simplification, or track cost trends over time. Present the numbers as directional estimates with your own `TcaInputs` calibrated to your organization.

---


### Using SimplicityTools.Analyzers

**Package:** `SimplicityTools.Analyzers` on [NuGet.org](https://www.nuget.org/packages/SimplicityTools.Analyzers/)

**Purpose:** Seven Roslyn diagnostics that surface simplification opportunities inline in the IDE and during normal builds.

**Install:**

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Analyzers" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>
```

**Important:** Always use `PrivateAssets="all"` for the analyzer package. This prevents the package from leaking into downstream consumers and ensures your project only exposes its own public API.

**Diagnostics:**

| ID | Rule | Category | Code Fix | Default Severity | Threshold (configurable) |
| --- | --- | --- | --- | --- | --- |
| `SF0001` | Interface has single implementation | HalfRule | ✅ Yes | Info | N/A |
| `SF0002` | Package reference has no symbol usage | HalfRule | ✅ Yes | Info | N/A |
| `SF0003` | Method is too complex for fast understanding | TwoAmTest | ❌ No | Warning | CC > 10 |
| `SF0004` | Method call chain is too deep | PrimaryPathFirst | ❌ No | Warning | Depth > 8 |
| `SF0005` | Constructor takes too many parameters | TwoAmTest | ❌ No | Warning | Params > 7 |
| `SF0006` | Generic parameter has only one specialization | HalfRule | ❌ No | Info | N/A |
| `SF0007` | Supporting file is referenced more than primary path | PrimaryPathFirst | ❌ No | Warning | N/A |

**IDE experience:**

- Diagnostics appear as warnings or info suggestions in your editor
- Hover over the warning to see the rule and recommendation
- Click the lightbulb to apply code fixes (for SF0001 and SF0002)

**Customizing detection:**

Mark the primary business path explicitly with `[PrimaryPath]` attributes:

```csharp
using SimplicityTools.Metrics;

[PrimaryPath]
public sealed class CheckoutHandler
{
    // Diagnostics prioritize this class as part of the main flow
}
```

If no explicit annotations exist, the analyzer falls back to convention-based detection:

- Folders named `Controllers`, `Endpoints`, `Handlers`, or `Pages` (override with `simplicity_first.sf0007_convention_folders`)
- Reference-based heuristics (classes that are heavily referenced)

**When to use:** Enable in all projects to surface simplification opportunities during normal development. Combine with the CLI for team-wide dashboards.

---

### Composing the packages

All packages are designed to work together. A typical workflow:

```csharp
using SimplicityTools.Metrics;
using SimplicityTools.Filters;
using SimplicityTools.Tca;

// 1. Collect metrics
var collector = new SimplicityCollector();
var snapshot = await collector.CollectAsync("path/to/Solution.sln");

// 2. Evaluate health
var verdicts = new[]
{
    TwoAmTestEvaluator.Evaluate(snapshot),
    HalfRuleEvaluator.Evaluate(snapshot),
    PrimaryPathFirstEvaluator.Evaluate(snapshot)
};

// 3. Estimate cost
var estimate = TcaEstimate.Create(snapshot, verdicts);

// 4. Report results
Console.WriteLine(snapshot.ToSummary());
Console.WriteLine();
foreach (var verdict in verdicts)
    Console.WriteLine($"{verdict.Filter}: {(verdict.Passes ? "✅ PASS" : "❌ FAIL")} ({verdict.Score:P0})");
Console.WriteLine();
Console.WriteLine(estimate.ToExecutiveSummary());
```

Or use the CLI instead to skip the plumbing:

```bash
dotnet simplicity analyze path/to/Solution.sln
dotnet simplicity budget path/to/Solution.sln
dotnet simplicity report path/to/Solution.sln
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

## CI/CD Integration

SimplicityTools integrates naturally into any CI/CD pipeline. The most common pattern: establish a baseline, protect it in PRs, and fail the build if complexity regresses.

### GitHub Actions

Install the tool and run analysis as part of your workflow:

```yaml
name: Complexity Check
on: [pull_request, push]

jobs:
  simplicity:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Install SimplicityTools
        run: dotnet tool install --global SimplicityTools.Cli
      
      - name: Add tool to PATH
        run: echo "$HOME/.dotnet/tools" >> $GITHUB_PATH
      
      - name: Restore solution
        run: dotnet restore
      
      - name: Run complexity analysis
        run: dotnet simplicity analyze YourSolution.sln
      
      - name: Check regression (PRs only)
        if: github.event_name == 'pull_request'
        run: dotnet simplicity diff YourSolution.sln --fail-on-regression
```

**Key points:**
- `actions/setup-dotnet` installs the .NET SDK on the runner
- Add `~/.dotnet/tools` to `$GITHUB_PATH` so the CLI is discoverable
- Use `--fail-on-regression` to fail the build if complexity increases
- Conditional step: only check regression on PRs, always analyze on main

**With trend tracking:**

To keep a trend report across builds, save historical snapshots:

```yaml
- name: Save snapshot for trends
  run: |
    mkdir -p .simplicity-history
    dotnet simplicity baseline YourSolution.sln
    cp .simplicity-baseline.json .simplicity-history/$(date +%Y-%m-%d).json
  continue-on-error: true

- name: Generate trend report
  run: dotnet simplicity report YourSolution.sln
  continue-on-error: true

- name: Upload report
  uses: actions/upload-artifact@v4
  if: always()
  with:
    name: complexity-report
    path: simplicity-report/
```

### Azure Pipelines

Define stages and use the official .NET task:

```yaml
trigger:
  - main
  - pull_request

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'
    displayName: 'Install .NET SDK'
  
  - script: dotnet tool install --global SimplicityTools.Cli
    displayName: 'Install SimplicityTools'
  
  - script: echo "##vso[task.prependpath]$HOME/.dotnet/tools"
    displayName: 'Add tool to PATH'
  
  - script: dotnet restore
    displayName: 'Restore'
  
  - script: dotnet simplicity analyze $(Build.SourcesDirectory)/YourSolution.sln
    displayName: 'Run analysis'
  
  - script: dotnet simplicity diff $(Build.SourcesDirectory)/YourSolution.sln --fail-on-regression
    displayName: 'Check regression'
    condition: eq(variables['Build.Reason'], 'PullRequest')
```

**Key points:**
- `UseDotNet@2` task handles SDK installation
- Use `$(Build.SourcesDirectory)` for the repo path
- Add tools to PATH using `##vso[task.prependpath]`
- Condition the regression check to PRs only

### GitLab CI

Use a container image that includes the .NET SDK:

```yaml
stages:
  - analyze

complexity-check:
  image: mcr.microsoft.com/dotnet/sdk:10.0
  stage: analyze
  script:
    - dotnet tool install --global SimplicityTools.Cli
    - export PATH="$HOME/.dotnet/tools:$PATH"
    - dotnet restore
    - dotnet simplicity analyze $CI_PROJECT_DIR/YourSolution.sln
    - dotnet simplicity diff $CI_PROJECT_DIR/YourSolution.sln --fail-on-regression || true
  artifacts:
    paths:
      - simplicity-report/
      - .simplicity-baseline.json
    expire_in: 30 days
  only:
    - branches
```

**Key points:**
- `mcr.microsoft.com/dotnet/sdk:10.0` includes the SDK pre-installed
- Export the tool path before running commands
- Use `$CI_PROJECT_DIR` for the repo path
- Add `|| true` after regression check if you want the job to succeed even on regression (optional; remove for strict gating)
- Artifacts are kept for 30 days for trend analysis

### General CI/CD checklist

Regardless of platform, ensure:

1. **SDK is installed:** Use the platform's native setup task (e.g., `actions/setup-dotnet`, `UseDotNet@2`, container image)
2. **Global tool is installed:** Run `dotnet tool install --global SimplicityTools.Cli` in each job
3. **Tool is discoverable:** Add `~/.dotnet/tools` (or `$USERPROFILE\.dotnet\tools` on Windows) to `PATH`
4. **Solution is restored:** Run `dotnet restore` before analysis
5. **Baseline is committed:** Check `.simplicity-baseline.json` into your repo so `diff --fail-on-regression` works
6. **Artifacts are saved:** If using trend reports, commit `.simplicity-history/*.json` files or save them as build artifacts

### Gate PR merges on complexity regression

**Pattern:** Fail the build if complexity increases without explicit approval.

Set up your CI:
```bash
dotnet simplicity baseline YourSolution.sln  # run once locally
git add .simplicity-baseline.json
git commit -m "Add complexity baseline"
```

Then in your CI pipeline, add:
```bash
dotnet simplicity diff YourSolution.sln --fail-on-regression
```

Now any PR that increases complexity will fail the build. Developers either:
- Reduce complexity before merging
- Commit an explicit update to the baseline (with team approval)

## Sample solutions in this repo


The repo includes two teaching samples:

| Sample | What it shows |
| --- | --- |
| `samples/Sample.Simplified` | A compact solution shape with 2 projects, 23 files, 5 primary-path files, and 1 abstraction layer in the checked-in baseline set. |
| `samples/Sample.OverEngineered` | A deliberately over-layered solution with 12 projects, 62 files, 31 primary-path files, and 25 abstraction layers in the checked-in baseline set. |

These are useful for demos, docs screenshots, and CI examples because the test suite already validates the commands against them.

## Troubleshooting

For comprehensive troubleshooting guidance covering installation, PATH issues, analyzer visibility, permissions, CI/CD integration, and advanced diagnostics, see [`docs/troubleshooting.md`](troubleshooting.md).

**Quick reference for common questions:**


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
