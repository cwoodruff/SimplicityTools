# SimplicityTools

**A .NET toolkit that measures solution complexity and surfaces simplification opportunities in your IDE and CI/CD pipeline.**

SimplicityTools makes it obvious when a codebase becomes harder to understand, change, and operate. It runs on your first build with zero config, surfaces Roslyn diagnostics inline in your editor, and produces a shareable HTML report for teams and stakeholders.

## The Problem

As solutions grow, complexity compounds. Extra layers creep in. Unused dependencies accumulate. Method complexity drifts. Your team spends more time understanding the codebase than changing it. But most complexity metrics stop at counting lines and cyclomatic complexity. They don't explain *what* to fix or *why* it matters to the business.

**SimplicityTools answers the hard questions:**
- Is the primary business flow still obvious to a new team member?
- Are we adding more abstraction layers than value?
- How much is complexity costing us in team onboarding, incidents, and attrition?

## What You Get

### 🎯 Five Complementary Tools

| Tool | What it Does | Best For |
| --- | --- | --- |
| **`dotnet-simplicity` CLI** | Snapshot your solution, establish baselines, compare drift, check a complexity budget, and watch for changes | Quick reads, CI/CD gates, automation |
| **HTML Report** | Self-contained, shareable report with metric cards, filter verdicts, complexity budget, and trend analysis over time | Executive summaries, trend tracking, post-review artifacts |
| **Roslyn Analyzers** | Seven diagnostics that surface premature abstraction, unused dependencies, method complexity, and structural drift | Real-time IDE feedback, code-fix suggestions |
| **Filter Evaluators** | Three teaching filters (TwoAmTest, HalfRule, PrimaryPathFirst) that turn raw metrics into health verdicts | Decision support, team communication |
| **TCA Calculator** | Estimates annual cost of excess complexity in team onboarding, incidents, and turnover | Executive buy-in, justifying refactoring effort |

### 🚀 Zero-Config First Run

No configuration needed to get started. All commands work immediately:

```bash
dotnet simplicity analyze path/to/YourSolution.sln
dotnet simplicity report path/to/YourSolution.sln
dotnet simplicity watch path/to/YourSolution.sln
```

Warnings, not errors. Sensible defaults. Teach-first output.

### 🛠️ Build Into Your Workflow

Establish baselines and protect them in CI:

```bash
dotnet simplicity baseline path/to/YourSolution.sln          # capture today
dotnet simplicity diff path/to/YourSolution.sln --fail-on-regression  # gate PRs
```

Get live feedback during refactoring:

```bash
dotnet simplicity watch path/to/YourSolution.sln
# Stays open, re-analyzes on file changes, shows you what got simpler
```

### 📊 What Gets Measured

For each snapshot, SimplicityTools collects:
- **Structural metrics:** project count, file count, abstraction layer depth, unused dependencies
- **Code metrics:** average method complexity, primary-path concentration
- **Filter verdicts:** pass/fail health scores for understandability, abstraction discipline, and business-flow clarity
- **Cost estimate:** annual impact on team velocity, incidents, and retention

### 🎓 Analyzer Code Fixes

Two of the seven diagnostics come with IDE code fixes:
- **SF0001:** Interface has single implementation → auto-rewrites to the concrete type, removes the interface
- **SF0002:** Unused package reference → removes the package from `.csproj` with one click

## For Developers

### Get Started

1. **Install the global tool** (when published):
   ```bash
   dotnet tool install --global SimplicityTools.Cli
   dotnet simplicity analyze path/to/YourSolution.sln
   ```

2. **Or build from source:**
   ```bash
   dotnet build src/SimplicityTools.Cli/SimplicityTools.Cli.csproj --nologo --verbosity quiet
   dotnet src/SimplicityTools.Cli/bin/Debug/net10.0/SimplicityTools.Cli.dll analyze samples/Sample.Simplified/Sample.Simplified.sln
   ```

3. **Read the full guide:**  
    [Using the SimplicityTools Toolset](docs/using-the-simplicity-tools.md) — six commands, configuration reference, filter explanations, and practical workflows.

4. **Need package or release details?**  
   [Contributing guide](CONTRIBUTING.md) — release tags, package versioning rules, local test-publish flow, and NuGet release steps.

### Add to Your Project

Consume the Roslyn analyzers in your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Analyzers" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>
```

The analyzer package is development-only: it contributes IDE/build diagnostics and code fixes, but it intentionally does not expose a compile-time API surface to your project.

Or use the libraries directly:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Tca" Version="x.y.z" />
</ItemGroup>
```

`SimplicityTools.Tca` restores both `SimplicityTools.Filters` and `SimplicityTools.Metrics` transitively. Add direct `SimplicityTools.Filters` or `SimplicityTools.Metrics` package references only when you want to pin those surfaces explicitly in your project file.

```csharp
using SimplicityTools.Metrics;
using SimplicityTools.Filters;
using SimplicityTools.Tca;

var snapshot = await new SimplicityCollector().CollectAsync("path/to/Solution.sln");
var verdicts = new[] {
    TwoAmTestEvaluator.Evaluate(snapshot),
    HalfRuleEvaluator.Evaluate(snapshot),
    PrimaryPathFirstEvaluator.Evaluate(snapshot)
};
var estimate = TcaEstimate.Create(snapshot, verdicts);
```

### Package versioning at a glance

- `SimplicityTools.Metrics`, `SimplicityTools.Filters`, and `SimplicityTools.Tca` version together.
- `SimplicityTools.Analyzers` can ship on its own cadence.
- `SimplicityTools.Cli` can ship on its own cadence.
- Git tags drive published package versions: `libraries/vX.Y.Z`, `analyzers/vX.Y.Z`, and `cli/vX.Y.Z`.

## For Stakeholders

### Why This Matters

**Complexity is expensive.** Every layer of indirection, every unused dependency, every high-complexity method is a tax on your team's ability to deliver fast. SimplicityTools quantifies that tax:

- **Onboarding cost:** How many hours does a new team member need to understand the core flow?
- **Incident response:** How much of an incident's resolution time is spent navigating the codebase instead of fixing the bug?
- **Retention risk:** How much does excessive complexity contribute to engineer burnout and attrition?

### Use Cases

**Before a major refactoring:** Capture a baseline, quantify the cost of the current shape, use that to justify engineering investment.

**During code review:** Surface structural problems early instead of discovering them after merge.

**In sprint planning:** Use the complexity budget to make trade-off decisions: "Can we ship this feature *and* keep complexity under control?"

**For team onboarding:** Generate a report once a month and track whether the codebase is getting easier or harder to understand.

## Project Structure

```
SimplicityTools/
├── src/
│   ├── SimplicityTools.Metrics/        # Core snapshot and collection logic
│   ├── SimplicityTools.Filters/        # Filter evaluators (TwoAmTest, HalfRule, PrimaryPathFirst)
│   ├── SimplicityTools.Tca/            # Cost-of-complexity estimates
│   ├── SimplicityTools.Analyzers/      # Roslyn diagnostics and code fixes
│   └── SimplicityTools.Cli/            # dotnet-simplicity command-line tool
├── samples/
│   ├── Sample.Simplified/              # Good-shape reference: 2 projects, 23 files, 1 abstraction layer
│   └── Sample.OverEngineered/          # Anti-pattern reference: 12 projects, 62 files, 25 abstraction layers
├── docs/
│   ├── using-the-simplicity-tools.md   # Complete command reference and workflows
│   └── simplicity-schema.json           # Configuration schema reference
└── tests/
    ├── SimplicityTools.Metrics.Tests/
    ├── SimplicityTools.Filters.Tests/
    ├── SimplicityTools.Analyzers.Tests/
    └── SimplicityTools.Cli.Tests/
```

## Key Design Decisions

- **Zero dependencies at runtime** — SimplicityTools uses only .NET built-ins and Roslyn analyzers.
- **Zero-config first run** — The toolkit warns, never errors, when configuration is missing. Defaults are sensible.
- **Self-contained reports** — HTML reports include all CSS inline. No external assets. Works offline, in air-gapped CI/CD, anywhere.
- **Primary-path teaching** — The toolkit teaches "what's your core business flow?" through explicit `[PrimaryPath]` annotations or convention-based detection (Controllers, Endpoints, Handlers, Pages).
- **Roslyn round-trip validation** — Code fixes are tested end-to-end: analyze → fix → reanalyze to verify the fix actually resolved the issue.

## Next Steps

- **New to SimplicityTools?** Start with [Using the SimplicityTools Toolset](docs/using-the-simplicity-tools.md).
- **Want to run it now?** Try `dotnet simplicity analyze samples/Sample.Simplified/Sample.Simplified.sln`.
- **Configuring for your team?** See the [`simplicity.json` schema](docs/simplicity-schema.json).
- **Cutting a package release?** Follow [CONTRIBUTING.md](CONTRIBUTING.md) for the tag and NuGet workflow.
- **Contributing?** Check out the issues and sprint milestones. The team operates in public.

---

**Built for teams who care about code quality, not just code volume.**
