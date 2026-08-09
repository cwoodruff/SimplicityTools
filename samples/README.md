# SimplicityTools samples

Three solutions, one purpose: give every tool in the toolkit something real to measure.

| Sample | Projects | Files | What it demonstrates |
| --- | --- | --- | --- |
| [**Sample.Simplified**](Sample.Simplified/) | 2 | 19 | The target state. One app, one test project, no premature abstraction. Every metric comfortably inside budget. |
| [**Sample.OverEngineered**](Sample.OverEngineered/) | 12 | 65 | The failure state. Twelve projects, 25 abstraction layers, 24 single-implementation interfaces for one order-placement flow. |
| [**Sample.ClaimsPortal**](Sample.ClaimsPortal/) | 8 | 49 | **The realistic middle, and the full-coverage sample.** Builds clean, tests pass, and still trips all seven analyzers, fails one filter, and carries a six-figure TCA estimate. |

## Which one do I want?

- **Learning what "simple" means** → `Sample.Simplified`. Read `Orders/OrderService.cs` and you have
  the whole business flow.
- **Seeing the tools scream** → `Sample.OverEngineered`. Every metric is red. Good for demos.
- **Seeing every tool work end to end** → `Sample.ClaimsPortal`. It is the only sample that exercises
  the CLI, the HTML report, all seven analyzers, all three filters, and the TCA calculator, and the
  only one that ships a `simplicity.json` and a documented `.editorconfig`. Start with its
  [README](Sample.ClaimsPortal/README.md).

## Running the tools against a sample

```bash
dotnet build src/SimplicityTools.Cli/SimplicityTools.Cli.csproj
CLI="dotnet src/SimplicityTools.Cli/bin/Debug/net10.0/SimplicityTools.Cli.dll"

$CLI analyze  samples/Sample.ClaimsPortal/Sample.ClaimsPortal.sln
$CLI budget   samples/Sample.ClaimsPortal/Sample.ClaimsPortal.sln
$CLI report   samples/Sample.ClaimsPortal/Sample.ClaimsPortal.sln
$CLI baseline samples/Sample.ClaimsPortal/Sample.ClaimsPortal.sln
$CLI diff     samples/Sample.ClaimsPortal/Sample.ClaimsPortal.sln --fail-on-regression
```

Swap in any other sample's `.sln`. `Sample.Simplified` and `Sample.OverEngineered` have no
`simplicity.json`, so they run on built-in defaults and the CLI says so.

## Notes for contributors

- All three samples are pinned by
  [`tests/SimplicitySampleBaselines.json`](../tests/SimplicitySampleBaselines.json); changing a
  sample's code means re-measuring and updating that file, the CLI test assertions, `README.md`, and
  `docs/quickstart.md` together. These drift as a set.
- Sample stderr expectations are derived from disk, not hard-coded: a sample that ships a
  `simplicity.json` must run silently, and one that does not must emit the defaults notice. Adding
  or removing a sample's config file needs no test change.
- `Sample.ClaimsPortal` also has its own 20 xUnit tests in the CI matrix.
- Generated artifacts (`simplicity-report/`, `.simplicity-history/`, `.simplicity-baseline.json`) are
  never committed.
