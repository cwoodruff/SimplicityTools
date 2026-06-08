# Simplicity-First .NET Toolkit: AI Implementation Instructions

> **Source of truth:** `SimplicityFirst_DotNet_Toolkit_Plan.docx`
> Every requirement in this document derives from that plan. No scope expansion, no inferred features. If a question is not answered here, it is not in scope.

---

## 0. Read This First

You are an AI agent (Claude Code, Cursor, or equivalent) implementing the Simplicity-First .NET Toolkit. Your output is a working multi-package solution that ships as five NuGet packages and a global CLI tool.

### Operating Constraints

- **Target framework:** .NET 10
- **Language version:** C# 14
- **Style:** Plain prose in comments and docs. No em-dashes. No banned vocabulary (leverage, robust, comprehensive, streamline, foster, utilize, cutting-edge, game-changer, deep dive). Use commas, periods, or colons instead of em-dashes.
- **Brand colors** (used in the HTML report only): background `#0D0D0D`, red accent `#E31B23`.
- **Philosophy alignment:** Every metric you implement must trace back to one of the three filters (2 AM Test, Half-Rule, Primary Path First) or to one of the five TCA categories (Infrastructure, Operational, Coordination, Cognitive, Opportunity).

### What You Must Not Do

- Do not invent metrics that are not in section 3.1 of the plan.
- Do not add analyzers beyond SF0001 through SF0007.
- Do not change the SimplicitySnapshot record shape. It is a public contract.
- Do not introduce circular package references. The dependency graph in section 2 is strictly unidirectional.
- Do not add external dependencies beyond `Microsoft.CodeAnalysis.CSharp`, `Microsoft.Build.Locator`, and standard test/benchmark libraries.

---

## 1. Solution Layout

Create this exact structure at the repository root:

```
SimplicityTools/
  src/
    SimplicityTools.Metrics/         # Core data model + collection
    SimplicityTools.Analyzers/       # Roslyn diagnostic analyzers
    SimplicityTools.Filters/         # Three-filter evaluation engine
    SimplicityTools.Tca/             # TCA cost-translation layer
    SimplicityTools.Cli/             # dotnet-simplicity global tool
  tests/
    SimplicityTools.Metrics.Tests/
    SimplicityTools.Analyzers.Tests/
    SimplicityTools.Filters.Tests/
    SimplicityTools.Tca.Tests/
  samples/
    Sample.OverEngineered/           # Deliberately complex reference solution
    Sample.Simplified/               # Simplicity-First refactored equivalent
  docs/
  SimplicityTools.sln
```

### Package Dependency Graph

```
Metrics  ──┬──>  Filters  ──┐
           │                ├──>  Cli
           ├──>  Tca  ──────┘
           │
Analyzers ─┘   (Analyzers depends on Metrics for the data model only;
                ships standalone as analyzer-only NuGet package)
```

`SimplicityTools.Metrics` has no internal dependencies. Treat this constraint as a build-time invariant.

---

## 2. Implementation Order

Implement in this order. Each step has a hard prerequisite on the step before it.

| # | Task | Output | Prerequisite |
|---|------|--------|--------------|
| 1 | SimplicitySnapshot record + ToSummary | Compilable `SimplicityTools.Metrics` package | None |
| 2 | Sample.OverEngineered scaffold | Solution that produces predictable bad metrics | 1 |
| 3 | Sample.Simplified scaffold | Solution that produces predictable good metrics | 1 |
| 4 | Structural pass (MSBuild walk) | TotalProjects, TotalFiles populated | 1 |
| 5 | Semantic pass (Roslyn) | All other snapshot properties populated | 4 |
| 6 | PrimaryPathAttribute + heuristic pass | PrimaryPathFileCount populated | 5 |
| 7 | CLI `analyze` command | Working `dotnet simplicity analyze` | 6 |
| 8 | CLI `report` command (HTML) | Working `dotnet simplicity report` | 7 |
| 9 | Filter evaluators + FilterVerdict | Working `SimplicityTools.Filters` package | 6 |
| 10 | TCA calculator | Working `SimplicityTools.Tca` package | 9 |
| 11 | CLI `baseline`, `diff`, `budget` commands | Full CLI surface | 9, 10 |
| 12 | Roslyn analyzers SF0001 to SF0007 | Working `SimplicityTools.Analyzers` package | 5 |
| 13 | Code fix providers for SF0001 and SF0002 | Functional refactorings | 12 |

Steps 1 through 8 constitute Milestone 1 (book launch). Steps 9 through 11 are Milestone 2. Steps 12 through 13 are Milestone 3.

---

## 3. SimplicityTools.Metrics

### 3.1 The SimplicitySnapshot Record

Create `src/SimplicityTools.Metrics/SimplicitySnapshot.cs` with this exact shape. The properties are public contract. Do not rename, reorder, or change types.

```csharp
namespace SimplicityTools.Metrics;

public sealed record SimplicitySnapshot(
    int TotalProjects,
    int TotalFiles,
    int PrimaryPathFileCount,
    int AbstractionLayerCount,
    int ExternalDependencyCount,
    int UnusedDependencyCount,
    int InterfacesWithSingleImplementation,
    double AverageMethodComplexity,
    TimeSpan EstimatedOnboardingTime,
    DateTimeOffset CollectedAt)
{
    public double PrimaryPathRatio =>
        TotalFiles > 0 ? (double)PrimaryPathFileCount / TotalFiles : 0;

    public double PrematureAbstractionRatio =>
        AbstractionLayerCount > 0
            ? (double)InterfacesWithSingleImplementation / AbstractionLayerCount
            : 0;

    public string ToSummary() => /* exact format from plan section 3.1 */;
}
```

The `ToSummary()` output format is fixed and used in book chapter listings. Reproduce it exactly:

```
Simplicity Snapshot (yyyy-MM-dd)
----------------------------------------
Projects: {TotalProjects}
Total files: {TotalFiles}
Primary path files: {PrimaryPathFileCount}
Abstraction layers: {AbstractionLayerCount}
Single-impl interfaces: {InterfacesWithSingleImplementation}
External deps: {ExternalDependencyCount} ({UnusedDependencyCount} unused)
Avg complexity: {AverageMethodComplexity:F1}
Est. onboarding: {EstimatedOnboardingTime.TotalHours:F0}h
```

### 3.2 Property Definitions (Authoritative)

Implementations must match these definitions. A property that returns a different number from a different definition is a bug regardless of how reasonable the alternative reading is.

| Property | Definition |
|---|---|
| `TotalProjects` | Count of `.csproj` files in the solution, excluding test projects (any project name ending in `.Tests` or under a `tests/` folder). |
| `TotalFiles` | Count of `.cs` files. Exclude generated files (anything matching `*.Designer.cs`, `*.g.cs`, or marked `<auto-generated>`), `obj/`, `bin/`. |
| `PrimaryPathFileCount` | Files participating in the primary request path. Identification rules in section 3.5. |
| `AbstractionLayerCount` | Total number of `interface` declarations in non-test projects. Generic interface definitions count as one. |
| `ExternalDependencyCount` | Count of unique NuGet package IDs referenced across all non-test projects. A package referenced from three projects counts as one. |
| `UnusedDependencyCount` | Subset of `ExternalDependencyCount` for which no symbol from the package is referenced in any `.cs` file. Resolve via Roslyn `Compilation.References` and namespace usage. |
| `InterfacesWithSingleImplementation` | Interfaces for which exactly one non-abstract implementing type exists across the solution. Use `SymbolFinder.FindImplementationsAsync`. |
| `AverageMethodComplexity` | Arithmetic mean of cyclomatic complexity across all non-generated method bodies. Property accessors and constructors count. Calculation rules in section 3.4. |
| `EstimatedOnboardingTime` | Result of the formula in section 3.6. |
| `CollectedAt` | `DateTimeOffset.UtcNow` at the start of the collection run. |

### 3.3 Collection Pipeline (Three Passes)

The collector runs in three sequential passes. Implement them as separate classes behind a single facade:

```csharp
public interface ISimplicityCollector
{
    Task<SimplicitySnapshot> CollectAsync(string solutionPath, CancellationToken ct);
}
```

#### Pass 1: Structural

- Use `Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults()` once per process.
- Walk the `.sln` file. For each project: count source files, parse `<PackageReference>` entries.
- Output: `TotalProjects`, `TotalFiles`, `ExternalDependencyCount` (preliminary count of declared references).
- **Performance budget:** under 200ms for solutions with up to 50 projects.

#### Pass 2: Semantic

- Build a `Microsoft.CodeAnalysis.Compilation` per project. Use `MSBuildWorkspace.OpenSolutionAsync`.
- For each compilation:
  - Walk all `INamedTypeSymbol` instances. Count interfaces. Resolve implementations using `SymbolFinder.FindImplementationsAsync`.
  - For each method body, build a `ControlFlowGraph` and compute cyclomatic complexity per the McCabe rules in section 3.4.
  - Resolve declared `<PackageReference>` entries against actual symbol usage. A package is "used" if any namespace from its assemblies appears in a `using` directive or a fully qualified name reference.
- Output: `AbstractionLayerCount`, `InterfacesWithSingleImplementation`, `AverageMethodComplexity`, `UnusedDependencyCount`, finalized `ExternalDependencyCount`.

#### Pass 3: Heuristic

- Apply primary path identification (see section 3.5).
- Output: `PrimaryPathFileCount`, `EstimatedOnboardingTime`.

### 3.4 Cyclomatic Complexity Rules (McCabe)

Base complexity per method = 1. Add 1 for each of the following:

- `if`, `else if`
- `for`, `foreach`, `while`, `do`
- `case` label (each case in a switch)
- `catch` clause
- `&&` operator
- `||` operator
- `?` (ternary conditional)
- `??` (null coalescing)
- Pattern matching switch arm
- Conditional access `?.` (each one)

Do not count `else` (it is the inverse of an `if` already counted). Do not count `try` or `finally`. Do not count return statements.

### 3.5 Primary Path Identification

Implement these signals in priority order. The first signal that produces a result wins for a given file.

1. **Explicit annotation** (highest priority): The class or any method in the file is annotated with `[PrimaryPath]`. The attribute lives in `SimplicityTools.Metrics` and looks like this:

   ```csharp
   [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
                   AllowMultiple = false, Inherited = false)]
   public sealed class PrimaryPathAttribute : Attribute { }
   ```

2. **Folder convention**: The file lives in a folder named `Controllers/`, `Endpoints/`, `Handlers/`, or `Pages/` (case-insensitive).

3. **Inbound reference percentile**: Count inbound symbol references for the type defined in each file (using `SymbolFinder.FindReferencesAsync`). Files in the top quartile (75th percentile or above) of inbound references qualify as primary path.

If a solution contains any `[PrimaryPath]` annotation, **disable signals 2 and 3 entirely**. The presence of explicit annotation indicates the team has made a deliberate choice; heuristics must not override that choice.

### 3.6 Onboarding Time Formula

Implement exactly:

```csharp
double baseHours       = TotalFiles * 0.5;
double layerTax        = AbstractionLayerCount * 2.0;
double dependencyTax   = ExternalDependencyCount * 0.75;
double rawHours        = baseHours + layerTax + dependencyTax;
EstimatedOnboardingTime = TimeSpan.FromHours(rawHours);
```

The coefficients 0.5, 2.0, and 0.75 are calibrated against the Architecture Tax essay reference points. Do not change them without an explicit test recalibration.

Expose calibration through this interface so teams can override:

```csharp
public interface IOnboardingCalibration
{
    double FilesCoefficient { get; }
    double LayersCoefficient { get; }
    double DependenciesCoefficient { get; }
}
```

Default implementation returns 0.5, 2.0, 0.75.

---

## 4. SimplicityTools.Analyzers

### 4.1 Analyzer Inventory

Implement all seven. Each goes in its own file under `src/SimplicityTools.Analyzers/`.

| ID | Filter | Trigger | Default Severity |
|---|---|---|---|
| SF0001 | Half-Rule | Interface with exactly one non-abstract implementation | Warning |
| SF0002 | Half-Rule | `<PackageReference>` with no symbol usage in any `.cs` file | Warning |
| SF0003 | 2 AM Test | Method with cyclomatic complexity greater than 10 | Warning |
| SF0004 | Primary Path First | Method whose call chain passes through more than 8 abstraction layers | Warning |
| SF0005 | 2 AM Test | Class with more than 7 constructor parameters | Warning |
| SF0006 | Half-Rule | Generic type parameter used in only one specialization | Warning |
| SF0007 | Primary Path First | Non-primary-path file with higher inbound reference count than primary-path files | Warning |

### 4.2 Implementation Template

Every analyzer follows this shape. Do not deviate.

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleImplementationInterfaceAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new(
        id:                 "SF0001",
        title:              "Interface has single implementation",
        messageFormat:      "Interface {0} has exactly one implementation. " +
                            "Remove the interface and use the concrete type directly.",
        category:           "SimplicityFirst.HalfRule",
        defaultSeverity:    DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri:        "https://simplicitytools.dev/analyzers/sf0001");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext ctx)
    {
        // Implementation here.
    }
}
```

### 4.3 Code Fix Providers

Two are mandatory in Milestone 3:

- **SF0001 fix:** "Remove interface and replace with concrete type." Walks all references to the interface, rewrites them to the concrete type, deletes the interface declaration.
- **SF0002 fix:** "Remove PackageReference from project file." Edits the `.csproj` file in place. Show a preview before applying.

Each fix provider lives in `src/SimplicityTools.Analyzers/CodeFixes/` and follows the standard `CodeFixProvider` pattern.

### 4.4 Help Link URLs

All analyzers point to `https://simplicitytools.dev/analyzers/sf000x` where x is the analyzer number (lowercase). These pages are live on the docs site.

---

## 5. SimplicityTools.Filters

### 5.1 The FilterVerdict Model

```csharp
public sealed record FilterVerdict(
    FilterName Filter,
    bool       Passes,
    double     Score,           // 0.0 (failing) to 1.0 (perfect)
    string     Summary,
    string[]   Violations,
    string[]   Recommendations);

public enum FilterName { TwoAmTest, HalfRule, PrimaryPathFirst }
```

`Score` is bounded `[0.0, 1.0]`. `Passes` is true when `Score >= 0.7` (this threshold is configurable in `simplicity.json`).

### 5.2 Two-AM Test Evaluator

Computes four sub-scores, each in `[0.0, 1.0]`. The composite `Score` is the arithmetic mean of the four.

| Dimension | Metric | Target | Sub-score Calculation |
|---|---|---|---|
| Discoverability | Primary path hop count | <= 5 files | `min(1.0, 5.0 / hops)` |
| Diagnosability | AverageMethodComplexity | <= 5 | `min(1.0, 5.0 / complexity)` |
| Fixability | AbstractionLayerCount / TotalProjects | <= 3 per project | `min(1.0, 3.0 / ratio)` |
| Cognitive Load | EstimatedOnboardingTime | <= 40 hours | `min(1.0, 40.0 / hours)` |

If a metric is zero, sub-score is 1.0.

### 5.3 Half-Rule Evaluator

Three sub-checks. Composite `Score` is the arithmetic mean.

| Sub-Check | Metric | Target | Sub-score |
|---|---|---|---|
| Premature abstraction | PrematureAbstractionRatio | <= 0.25 | `max(0.0, 1.0 - (ratio / 0.25 - 1.0))` clipped to `[0,1]` |
| Dependency accumulation | UnusedDependencyCount | 0 | `1.0` if 0; else `max(0.0, 1.0 - count * 0.1)` |
| Dependency sprawl | ExternalDependencyCount / TotalProjects | <= 8 per project | `min(1.0, 8.0 / ratio)` |

### 5.4 Primary Path First Evaluator

| Sub-Check | Metric | Target | Sub-score |
|---|---|---|---|
| Primary path concentration | PrimaryPathRatio | >= 0.60 | `min(1.0, ratio / 0.60)` |
| Abstraction dilution | AbstractionLayerCount / PrimaryPathFileCount | <= 1 layer per 3 files (i.e., ratio <= 0.333) | `min(1.0, 0.333 / ratio)` |
| Project count | TotalProjects | <= 5 for solutions under 100k LOC | `min(1.0, 5.0 / projects)` |

### 5.5 Violations and Recommendations

For each filter, populate `Violations[]` with one string per failing sub-check, and `Recommendations[]` with the single highest-impact action sorted by estimated improvement to the composite score. Cap each list at 5 items.

---

## 6. SimplicityTools.Tca

### 6.1 The TcaEstimate Model

```csharp
public sealed record TcaEstimate(
    MoneyRange InfrastructureCostPerYear,
    MoneyRange OperationalCostPerYear,
    MoneyRange CoordinationCostPerYear,
    MoneyRange CognitiveCostPerYear,
    MoneyRange OpportunityCostPerYear)
{
    public MoneyRange TotalPerYear =>
        InfrastructureCostPerYear + OperationalCostPerYear +
        CoordinationCostPerYear  + CognitiveCostPerYear +
        OpportunityCostPerYear;

    public string ToExecutiveSummary() => /* see section 6.4 */;
}

public readonly record struct MoneyRange(decimal Low, decimal High)
{
    public static MoneyRange operator +(MoneyRange a, MoneyRange b) =>
        new(a.Low + b.Low, a.High + b.High);

    public override string ToString() => $"${Low:N0} - ${High:N0}";
}
```

### 6.2 Cost Derivation Rules

Each rule produces a `MoneyRange` (Low and High). All formulas read from `simplicity.json` (see section 6.3) for team-specific inputs. All formulas produce annual figures.

#### Infrastructure
- Baseline: `$200/month per project`.
- Complexity coefficient: `1.0 + (UnusedDependencyCount * 0.05)`, capped at 2.0.
- `Annual = TotalProjects * 200 * 12 * coefficient`.
- Range: Low = result * 0.8, High = result * 1.2.

#### Operational
- `complexityFactor = AverageMethodComplexity / 5.0`.
- `Annual = complexityFactor * onCallHourlyRateUsd * estimatedMonthlyIncidentCount * 12 * 4`. (The factor of 4 represents the team-hours per incident, per the DORA elite-performer benchmark.)
- Range: Low = result * 0.7, High = result * 1.3.

#### Coordination
- `excessProjects = max(0, TotalProjects - 3)`.
- `Annual = excessProjects * 4000 * 12`.
- Range: Low = result * 0.75, High = result * 1.25.

#### Cognitive
- `onboardingFactor = EstimatedOnboardingTime.TotalHours / 40.0`.
- `attritionMultiplier = 1.0 + (PrematureAbstractionRatio * 0.5)` (premature abstraction increases attrition pressure).
- `Annual = onboardingFactor * averageEngineerMonthlySalaryUsd * 12 * (attritionCoefficientPercent / 100.0) * teamSize * attritionMultiplier`.
- Range: Low = result * 0.7, High = result * 1.3.

#### Opportunity
- `compositeScore = (twoAmScore + halfRuleScore + primaryPathScore) / 3.0`.
- `Annual = teamSize * averageEngineerMonthlySalaryUsd * 12 * (1.0 - compositeScore) * 0.4`.
- Range: Low = result * 0.5, High = result * 1.5 (opportunity cost has the widest variance).

### 6.3 simplicity.json Schema

The file lives at the repository root next to the `.sln` file. Provide a JSON schema document at `docs/simplicity-schema.json` and validate input against it.

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
    "primaryPathRatioTarget": 0.60,
    "prematureAbstractionRatioTarget": 0.25,
    "maxMethodComplexity": 5,
    "maxOnboardingHours": 40,
    "passingScore": 0.7
  }
}
```

If the file is absent, use these values as defaults and log a warning.

### 6.4 Executive Summary Format

`ToExecutiveSummary()` produces this exact format:

```
Total Cost of Architecture (Annual Estimate)
============================================
Infrastructure:   ${low} - ${high}
Operational:      ${low} - ${high}
Coordination:     ${low} - ${high}
Cognitive:        ${low} - ${high}
Opportunity:      ${low} - ${high}
--------------------------------------------
TOTAL:            ${low} - ${high} per year
```

---

## 7. dotnet-simplicity CLI

### 7.1 Tool Configuration

In `src/SimplicityTools.Cli/SimplicityTools.Cli.csproj`:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>simplicity</ToolCommandName>
  <PackageId>SimplicityTools.Cli</PackageId>
</PropertyGroup>
```

Resulting install: `dotnet tool install --global SimplicityTools.Cli`. Invocation: `dotnet simplicity <command>`.

### 7.2 Command Surface

| Command | Arguments | Behavior |
|---|---|---|
| `analyze` | `<solution.sln>` | Run all three passes, print SimplicitySnapshot.ToSummary() + FilterVerdicts + TcaEstimate to stdout. Exit code 0. |
| `report` | `<solution.sln> [--format html\|json\|md]` | Generate full report into `./simplicity-report/`. Default format html. |
| `watch` | `<solution.sln>` | Re-run analyze on file change events under the solution root. Use `FileSystemWatcher` with debounce of 500ms. |
| `baseline` | `<solution.sln>` | Run analyze, write the snapshot to `.simplicity-baseline.json` in the solution directory. Overwrites existing baseline. |
| `diff` | `<solution.sln> [--fail-on-regression]` | Run analyze, compare against `.simplicity-baseline.json`, print delta. With the flag, exit with code 1 if any regression rule is hit (see 7.3). |
| `budget` | `<solution.sln>` | Print Complexity Budget consumption across the four dimensions: Cognitive Load, Operational Surface, Change Safety, Discoverability. |

### 7.3 Regression Rules for `diff --fail-on-regression`

Exit 1 if any of these are true:

- `PrematureAbstractionRatio` increased by more than `0.05`.
- `AverageMethodComplexity` increased by more than `0.5`.
- `UnusedDependencyCount` increased above `0`.
- Any filter `Score` decreased by more than `0.1`.

Exit 0 in all other cases.

### 7.4 HTML Report Structure

Generate a single self-contained HTML file with all CSS inlined. No external assets, no CDN links. Use brand colors `#0D0D0D` background and `#E31B23` red accent. The report has six sections in this order:

1. **Executive Summary**: TCA headline number, composite filter score (mean of three filter scores), top three actions ordered by estimated improvement.
2. **Filter Verdicts**: One section per filter with score, violations, recommendations.
3. **Metric Detail**: Full SimplicitySnapshot with comparison to baseline (if present) and to benchmark thresholds.
4. **Complexity Budget**: Four-dimension visualization showing budget consumption as horizontal bars.
5. **Trend Analysis**: Chart of key metrics over the last 10 snapshots from `.simplicity-history/`. Use inline SVG (no JavaScript chart libraries).
6. **Appendix**: Methodology notes, calibration values used, citation references.

### 7.5 CI/CD Integration

Ship a GitHub Actions template at `docs/github-action-template.yml`:

```yaml
- name: Install SimplicityTools
  run: dotnet tool install --global SimplicityTools.Cli

- name: Analyze and diff against baseline
  run: dotnet simplicity diff src/MyApp.sln --fail-on-regression
```

---

## 8. Sample Projects

These two projects are book artifacts. They are not throwaway test fixtures. Every metric in every chapter exercise is reproducible by running the toolkit against them, so their numbers are part of the published contract.

### 8.1 Sample.OverEngineered

Target metrics (the toolkit must report these against this sample):

| Metric | Target Value |
|---|---|
| TotalProjects | 12 |
| TotalFiles | ~120 |
| AbstractionLayerCount | ~40 |
| InterfacesWithSingleImplementation | ~33 (PrematureAbstractionRatio ~0.82) |
| ExternalDependencyCount | 23 |
| UnusedDependencyCount | 7 |
| AverageMethodComplexity | ~8.5 |
| EstimatedOnboardingTime | ~87 hours |
| PrimaryPathRatio | ~0.31 |

Structural composition: split a simple order-placement domain across 12 projects (Domain, Application, Infrastructure, Persistence, ReadModel, WriteModel, Messaging, Cache, Validation, Authorization, Telemetry, Web). Use mediator-pattern with one handler interface per command. Add 7 NuGet packages that are referenced but never imported.

### 8.2 Sample.Simplified

Target metrics:

| Metric | Target Value |
|---|---|
| TotalProjects | 2 |
| TotalFiles | ~40 |
| AbstractionLayerCount | ~6 |
| InterfacesWithSingleImplementation | ~1 (PrematureAbstractionRatio ~0.12) |
| ExternalDependencyCount | 9 |
| UnusedDependencyCount | 0 |
| AverageMethodComplexity | ~3.2 |
| EstimatedOnboardingTime | ~31 hours |
| PrimaryPathRatio | ~0.74 |

Structural composition: same domain, 2 projects (App and App.Tests). Modular monolith. Direct service classes instead of interface-per-handler. Only the abstractions that have multiple implementations remain interfaces.

### 8.3 Verification Harness

Add `tests/SimplicitySampleBaselines.json` containing the expected snapshot values for both samples. The integration test suite asserts the actual snapshot matches the baseline within tolerance:

- Integer metrics: exact match required.
- Floating point metrics: within +/- 5%.
- TimeSpan metrics: within +/- 10%.

Any change to collection logic that alters a baseline value requires:
1. An explicit decision documented in the test commit message.
2. A rebaseline of `SimplicitySampleBaselines.json`.
3. A note in `docs/CHANGELOG.md` flagging the recalibration.

---

## 9. Testing Strategy

### 9.1 Unit Test Coverage Targets

- `SimplicityTools.Metrics`: at least 90% line coverage.
- `SimplicityTools.Filters`: at least 90% line coverage.
- `SimplicityTools.Tca`: at least 90% line coverage.
- `SimplicityTools.Analyzers`: every analyzer has at least three tests (positive case, negative case, suppression case).
- `SimplicityTools.Cli`: integration tests via the `Process` API against the sample projects.

### 9.2 Required Unit Tests

These tests are mandatory, not advisory:

- `SimplicitySnapshot`: every property and derived property tested with known inputs.
- `PrimaryPath` heuristic: 12 representative cases including naming variants, annotation variants, and the conflict case (annotation present, heuristic disagrees, annotation wins).
- Cyclomatic complexity: McCabe example suite. Verify base = 1, single `if` = 2, single `if/else` = 2, nested `if/if` = 3, `&&` = +1, `||` = +1, ternary = +1, switch with N cases = N+1 (the +1 is the base).
- `PrematureAbstractionRatio`: cases include zero interfaces, all single-implementation, all multi-implementation, mixed.
- Onboarding time formula: three reference points: (TotalFiles=40, Layers=6, Deps=9) -> approximately 31h; (TotalFiles=120, Layers=40, Deps=23) -> approximately 87h; an empty solution -> 0h.

### 9.3 Roslyn Analyzer Test Harness

Use `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit` (or NUnit equivalent). For each analyzer test:

1. Diagnostic fires on the expected line and column.
2. Code fix produces compilable C#.
3. `#pragma warning disable SF000X` and `.editorconfig` `dotnet_diagnostic.SF000X.severity = none` both suppress the diagnostic.
4. Running the analyzer against `Sample.Simplified` produces zero false positives.

### 9.4 Performance Tests

Use `BenchmarkDotNet`. The CLI `analyze` command must complete in under 5 seconds (P95) on `Sample.OverEngineered` running on standard CI hardware (GitHub-hosted `ubuntu-latest` runners). Failing this threshold fails the CI build.

---

## 10. Milestone Definition of Done

### Milestone 1 (Book Launch)

You may declare Milestone 1 complete when all of the following are true:

- [ ] `dotnet tool install --global SimplicityTools.Cli` succeeds from a local NuGet feed.
- [ ] `dotnet simplicity analyze samples/Sample.OverEngineered/Sample.OverEngineered.sln` produces output matching `SampleBaselines.json` for that solution.
- [ ] `dotnet simplicity analyze samples/Sample.Simplified/Sample.Simplified.sln` produces output matching `SampleBaselines.json` for that solution.
- [ ] `dotnet simplicity report` generates a valid HTML file that opens in a browser without console errors.
- [ ] `SimplicitySnapshot.ToSummary()` output exactly matches the format in section 3.1 (validated by string comparison test).
- [ ] All Milestone 1 unit and integration tests pass on CI.
- [ ] Performance benchmark passes (under 5s P95 on the OverEngineered sample).

### Milestone 2 (Filters + TCA)

- [ ] All three FilterVerdicts populate correctly against both sample solutions.
- [ ] TcaEstimate produces non-zero ranges for all five categories.
- [ ] `simplicity.json` schema validation works (valid file accepted, malformed file rejected with clear error).
- [ ] `dotnet simplicity baseline` produces a valid `.simplicity-baseline.json`.
- [ ] `dotnet simplicity diff --fail-on-regression` exits 0 when no regression and 1 when regression detected (verified via test cases that mutate snapshots).
- [ ] `dotnet simplicity budget` reports all four dimensions.
- [ ] HTML report includes the full TCA executive summary section.

### Milestone 3 (Analyzers + IDE)

- [ ] All seven analyzers fire on `Sample.OverEngineered` and produce zero diagnostics on `Sample.Simplified`.
- [ ] Code fixes for SF0001 and SF0002 produce compilable output validated by Roslyn round-trip.
- [ ] Trend analysis chart renders in the HTML report when `.simplicity-history/` contains 2 or more snapshots.
- [ ] Documentation site source published in `docs/`.

---

## 11. Out-of-Scope Reminders

If you find yourself wanting to add any of the following, stop. They are explicitly out of scope for v1:

- Languages other than C# (no F#, no VB.NET analyzer support).
- Frameworks other than .NET 10 (no .NET Framework, no Mono, no Unity).
- IDE extensions beyond Roslyn analyzers (no full VS extension package, no JetBrains plugin in Milestone 1).
- A web dashboard, hosted service, or telemetry reporting.
- Custom user-defined analyzer rules (the seven SF000X are the contract).
- Metrics beyond the SimplicitySnapshot record fields.
- Cost categories beyond the five TCA dimensions.

The toolkit ships free and open-source. The Complexity Audit is the paid engagement that the toolkit drives demand for. Keep this positioning in every design choice: when in doubt, choose the option that makes the team's complexity problem more visible without trying to solve it for them.

---

## 12. Glossary (Authoritative)

- **2 AM Test**: First Simplicity-First filter. Could a tired, stressed engineer debug this at 2 AM without help?
- **Half-Rule**: Second Simplicity-First filter. Can we build half of what we think we need and still deliver real value?
- **Primary Path First**: Third Simplicity-First filter. Are we designing for the 95% case or drowning in edge cases?
- **TCA**: Total Cost of Architecture. Five categories: Infrastructure, Operational, Coordination, Cognitive, Opportunity.
- **Complexity Budget**: Team-level constraint with four dimensions: Cognitive Load, Operational Surface, Change Safety, Discoverability.
- **Architecture Tax**: The continuing cost of architectural complexity, measured across the five TCA categories.
- **Primary Path**: The 95% use case that the system actually serves most often.

End of instructions. Build to the spec, not beyond it.
