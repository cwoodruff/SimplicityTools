# SimplicityTools

**A .NET toolkit that measures solution complexity and surfaces simplification opportunities in your IDE and CI/CD pipeline.**

SimplicityTools makes it obvious when a codebase becomes harder to understand, change, and operate. It runs on your first build with zero config, surfaces Roslyn diagnostics inline in your editor, and produces a shareable HTML report for teams and stakeholders.

**Website:** https://tools.simplicity-first.dev

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

### ⚡ Quick Install

| Package | Latest | Install |
| --- | --- | --- |
| **Cli** (global tool) | [![NuGet](https://img.shields.io/nuget/v/SimplicityTools.Cli.svg?logo=nuget)](https://www.nuget.org/packages/SimplicityTools.Cli/) | `dotnet tool install --global SimplicityTools.Cli` |
| **Metrics** (core library) | [![NuGet](https://img.shields.io/nuget/v/SimplicityTools.Metrics.svg?logo=nuget)](https://www.nuget.org/packages/SimplicityTools.Metrics/) | `dotnet add package SimplicityTools.Metrics` |
| **Filters** (evaluators) | [![NuGet](https://img.shields.io/nuget/v/SimplicityTools.Filters.svg?logo=nuget)](https://www.nuget.org/packages/SimplicityTools.Filters/) | `dotnet add package SimplicityTools.Filters` |
| **Tca** (cost calculator) | [![NuGet](https://img.shields.io/nuget/v/SimplicityTools.Tca.svg?logo=nuget)](https://www.nuget.org/packages/SimplicityTools.Tca/) | `dotnet add package SimplicityTools.Tca` |
| **Analyzers** (IDE warnings) | [![NuGet](https://img.shields.io/nuget/v/SimplicityTools.Analyzers.svg?logo=nuget)](https://www.nuget.org/packages/SimplicityTools.Analyzers/) | `dotnet add package SimplicityTools.Analyzers --prerelease` |

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

3. **First run?** Try the [Quickstart](docs/quickstart.md) — five essential commands with real output from Sample.Simplified.

4. **Read the full guide:**  
     [Using the SimplicityTools Toolset](docs/using-the-simplicity-tools.md) — six commands, configuration reference, filter explanations, and practical workflows.

5. **Need package or release details?**  
    [Contributing guide](CONTRIBUTING.md) — release tags, package versioning rules, local test-publish flow, and NuGet release steps.

### Add to Your Project

Choose one or more based on your needs:

**For comprehensive integration guides, API details, and composition examples, see [Library Integration](docs/using-the-simplicity-tools.md#library-integration) in the full documentation.**

#### 1. **Analyzers only** (IDE diagnostics and code fixes)

Add to your project file:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Analyzers" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>
```

`PrivateAssets="all"` ensures the analyzer package does not leak into downstream consumers. The package contributes IDE/build diagnostics and code fixes, but no compile-time API surface.

#### 2. **Metrics library** (Core complexity snapshot API)

For custom tooling or integration into your own analysis pipeline:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Metrics" Version="x.y.z" />
</ItemGroup>
```

```csharp
using SimplicityTools.Metrics;

var collector = new SimplicityCollector();
var snapshot = await collector.CollectAsync("path/to/Solution.sln");
Console.WriteLine(snapshot.ToSummary());
```

See [Using SimplicityTools.Metrics](#using-simplicitytoolsmetrics) for API details.

#### 3. **Filters library** (Health verdicts)

Evaluate a snapshot against three Simplicity-First filters:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Filters" Version="x.y.z" />
</ItemGroup>
```

`SimplicityTools.Filters` includes `SimplicityTools.Metrics` as a transitive dependency.

```csharp
using SimplicityTools.Filters;

var verdicts = new[] {
    TwoAmTestEvaluator.Evaluate(snapshot),
    HalfRuleEvaluator.Evaluate(snapshot),
    PrimaryPathFirstEvaluator.Evaluate(snapshot)
};

foreach (var verdict in verdicts)
{
    Console.WriteLine($"{verdict.Filter}: {(verdict.Passes ? "PASS" : "FAIL")} ({verdict.Score:P0})");
}
```

See [Using SimplicityTools.Filters](#using-simplicitytoolsfilters) for verdict details.

#### 4. **TCA library** (Cost of complexity estimate)

Convert metrics and filter verdicts into an annual complexity cost:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Tca" Version="x.y.z" />
</ItemGroup>
```

`SimplicityTools.Tca` includes both `SimplicityTools.Filters` and `SimplicityTools.Metrics` transitively.

```csharp
using SimplicityTools.Filters;
using SimplicityTools.Metrics;
using SimplicityTools.Tca;

var estimate = TcaEstimate.Create(snapshot, verdicts);
Console.WriteLine(estimate.ToExecutiveSummary());
```

See [Using SimplicityTools.Tca](#using-simplicitytoolstca) for cost model details.

#### Version constraints and breaking changes

- `SimplicityTools.Metrics`, `SimplicityTools.Filters`, and `SimplicityTools.Tca` **version together** — use the same `x.y.z` for all three.
- `SimplicityTools.Analyzers` and `SimplicityTools.Cli` **release independently** — pin only the versions you need.
- Patch versions (`x.y.Z`) are additive and safe to upgrade.
- Minor versions (`x.Y.z`) may include new metrics or filter rules — evaluate impact before upgrading.
- Major versions (`X.y.z`) indicate breaking API changes — review [release notes](CONTRIBUTING.md#release-tags) before upgrading.

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
├── docs-site/                          # Astro site for https://tools.simplicity-first.dev
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

## Website Development

- Live site: https://tools.simplicity-first.dev
- Local docs workflow:
  ```bash
  cd docs-site
  npm install
  npm run dev
  ```
- Validation before deploy:
  ```bash
  cd docs-site
  npm run build:validate
  ```
- Deployment runs from `.github/workflows/deploy-site.yml` on pushes to `main` and publishes `docs-site/dist/` to `gh-pages`.
- Operator checklist after merge to `main`:
  1. Let the first successful `Deploy docs site` workflow create the `gh-pages` branch.
  2. In GitHub repository settings, set Pages to deploy from `gh-pages` / root.
  3. Set the custom domain to `tools.simplicity-first.dev` and confirm `gh-pages/CNAME` still contains that value.
  4. Create or verify the DNS `CNAME` record: `tools.simplicity-first.dev` → `cwoodruff.github.io`.
  5. Wait for DNS/Pages propagation, then verify `https://tools.simplicity-first.dev`, `/robots.txt`, and `/sitemap.xml`.

## Next Steps

- **New to SimplicityTools?** Start with [Using the SimplicityTools Toolset](docs/using-the-simplicity-tools.md).
- **Want to run it now?** Try `dotnet simplicity analyze samples/Sample.Simplified/Sample.Simplified.sln`.
- **Configuring for your team?** See the [`simplicity.json` schema](docs/simplicity-schema.json).
- **Cutting a package release?** Follow [CONTRIBUTING.md](CONTRIBUTING.md) for the tag and NuGet workflow.
- **Contributing?** Check out the issues and sprint milestones. The team operates in public.

---

**Built for teams who care about code quality, not just code volume.**
