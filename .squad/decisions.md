# Squad Decisions

## Active Decisions

### 2026-04-29T06:47:51.656-04:00: Project scope anchor
**By:** Chris Woody Woodruff
**What:** The squad is being set up around the Simplicity-First .NET Toolkit plan in `docs/SimplicityFirst_DotNet_Toolkit_Plan.docx`. Initial delivery centers on five packages: Metrics, Analyzers, Filters, Tca, and the `dotnet-simplicity` CLI, supported by sample solutions.
**Why:** This is the authoritative product brief the squad should use for routing and implementation planning.

### 2026-04-29T06:47:51.656-04:00: Zero-config first run is mandatory
**By:** Chris Woody Woodruff
**What:** The toolkit must run in CI/CD with zero configuration and provide useful signal on the first run.
**Why:** This is the key product constraint from the project plan and should influence CLI, analyzer, and documentation decisions.

### 2026-04-29T06:47:51.656-04:00: Initial squad composition
**By:** Squad
**What:** The project starts with a five-member Matrix cast: Morpheus, Trinity, Switch, Tank, and Link, supported by Scribe and Ralph.
**Why:** The work naturally splits into architecture, core .NET implementation, analyzer/compiler work, testing, and developer experience.

### 2026-04-29T07:03:10.371-04:00: Analyzer package stays isolated in the scaffold
**By:** Morpheus
**What:** The initial scaffold keeps `SimplicityTools.Analyzers` as its own package and wires `SimplicityTools.Cli` to it as an analyzer asset reference, while executable and library code dependencies remain `Metrics -> Filters/Tca -> Cli`.
**Why:** This preserves the plan's package direction without forcing the CLI to take a normal compile-time dependency on analyzer internals, keeping the analyzer package separable for IDE/MSBuild use from the first scaffold.

### 2026-04-29T07:21:03.149-04:00: Sprint structure decomposition
**By:** Morpheus
**What:** Mapped the 13-step implementation order into three milestones: Metrics & Core Collection (M1, steps 1–8), Filters + TCA + CLI Extensions (M2, steps 9–11), and Roslyn Analyzers + Code Fixes (M3, steps 12–13). Each milestone represents a meaningful delivery gate aligned with the book's chapter structure and the three-tier package architecture (Metrics → Filters/Tca → Cli).
**Why:** Grouping steps into three sprints provides narrative clarity (measurement → decision support → IDE feedback), aligns with package dependency boundaries, and syncs with the book chapters. All 26 resulting issues have been created in GitHub with milestone assignments and prerequisite tracking. DoD criteria defined per milestone.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
