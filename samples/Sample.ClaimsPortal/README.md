# Sample.ClaimsPortal

**The realistic middle.** An eight-project insurance claims intake service that is neither a toy nor a
cautionary tale — it is the shape most working .NET solutions actually have. It builds clean, its
tests pass, and it is still measurably harder to understand than it needs to be.

`Sample.Simplified` shows what good looks like. `Sample.OverEngineered` shows what collapse looks
like. This sample exists because neither of those exercises the whole toolkit at once. Every tool in
the repository — the CLI, the HTML report, all seven analyzers, the three filters, and the TCA
calculator — has a working example here.

---

## Contents

- [The scenario](#the-scenario)
- [Solution layout](#solution-layout)
- [Run it](#run-it)
- [Tool 1 — the `dotnet-simplicity` CLI](#tool-1--the-dotnet-simplicity-cli)
- [Tool 2 — the HTML report](#tool-2--the-html-report)
- [Tool 3 — the Roslyn analyzers](#tool-3--the-roslyn-analyzers)
- [Tool 4 — the filter evaluators](#tool-4--the-filter-evaluators)
- [Tool 5 — the TCA calculator](#tool-5--the-tca-calculator)
- [Configuration files](#configuration-files)
- [Fixing the sample](#fixing-the-sample)

---

## The scenario

A claim arrives. The portal has to decide, quickly and defensibly, whether to pay it:

1. Validate the submission.
2. Look up the policy and the coverage that applies to the claim category.
3. Screen it for fraud.
4. Adjudicate: deny, approve in full, approve with adjustments, or send to manual review.
5. Journal the decision, settle the payout, and notify the claimant.

That is the primary path. Everything a new team member needs to learn on day one lives inside those
five steps. The sample's problem is that reading the code does not lead you through them.

---

## Solution layout

```
Sample.ClaimsPortal/
  Sample.ClaimsPortal.Api/            # Exe. Endpoints/ (primary path), Support/, Composition/
  Sample.ClaimsPortal.Claims/         # Intake chain, adjudication rules, decision journal
  Sample.ClaimsPortal.Policies/       # Policy and coverage lookup
  Sample.ClaimsPortal.Fraud/          # Fraud signals and scoring
  Sample.ClaimsPortal.Payments/       # Payout ledger and settlement
  Sample.ClaimsPortal.Notifications/  # Claimant messaging
  Sample.ClaimsPortal.Platform/       # Clock, ids, telemetry, Money
  Sample.ClaimsPortal.Tests/          # 20 xUnit tests, all passing
  tools/SimplicityReport/             # Library example (NOT part of the sample solution)
  simplicity.json                     # TCA inputs + filter thresholds
  .editorconfig                       # Analyzer thresholds, documented
```

`tools/SimplicityReport` is deliberately excluded from `Sample.ClaimsPortal.sln`. Keeping the
measuring tool out of the solution means the numbers below describe the claims portal, not the
tooling that measures it.

---

## Run it

```bash
# From the repository root.
dotnet build   samples/Sample.ClaimsPortal/Sample.ClaimsPortal.sln
dotnet test    samples/Sample.ClaimsPortal/Sample.ClaimsPortal.Tests
dotnet run --project samples/Sample.ClaimsPortal/Sample.ClaimsPortal.Api
```

The demo drives three claims through the full intake chain:

```
Sample.ClaimsPortal — claim intake walkthrough
------------------------------------------------
clean auto claim       200 AUT-000001 Approved $3,700.00 (clean auto claim#…)
                       200 AUT-000001 paid $3,700.00 (clean auto claim payout#…)
thin documentation     200 PRO-000002 PartiallyApproved $13,125.00 (thin documentation#…)
                       200 PRO-000002 paid $13,125.00 (thin documentation payout#…)
fraud screen trips     202 MED-000003 ManualReview (fraud screen trips#…)
                       200 MED-000003 paid $0.00 (fraud screen trips payout#…)

Journal entries: 3
Notifications sent: 3
Telemetry events: 5
Endpoint log lines: 12
```

Seeded policy windows are anchored to the current date, so the output stays the same whenever you
run it.

---

## Tool 1 — the `dotnet-simplicity` CLI

Build the CLI from source (or install the global tool once it is published):

```bash
dotnet build src/SimplicityTools.Cli/SimplicityTools.Cli.csproj
CLI="dotnet src/SimplicityTools.Cli/bin/Debug/net10.0/SimplicityTools.Cli.dll"
SLN=samples/Sample.ClaimsPortal/Sample.ClaimsPortal.sln
```

### `analyze` — the snapshot

```
$ $CLI analyze $SLN

Simplicity Snapshot (2026-08-09)
----------------------------------------
Projects: 8
Total files: 49
Primary path files: 20
Abstraction layers: 7
Single-impl interfaces: 6
External deps: 2 (1 unused)
Avg complexity: 1.6
Est. onboarding: not computed
```

Read it as: six of seven interfaces in the solution have exactly one implementation, and one of two
package references is dead. Average complexity is fine — the problem is not spread evenly, it is
concentrated in one method (see SF0003 below).

### `budget` — where the headroom went

```
$ $CLI budget $SLN

Complexity Budget
-----------------
Status: 1/3 dimension(s) within budget.
Bars show configured budget used. Values above 100% are over budget.

Cognitive Load      not scored — onboarding time has not been computed.
Operational Surface [##########]   343%  OVER BUDGET
  Premature abstraction ratio: 0.86 (target <= 0.25)
Change Safety       [###-------]    31%  WITHIN BUDGET
  Average method complexity: 1.57 (target <= 5.00)
Discoverability     [##########]   147%  OVER BUDGET
  Primary path ratio: 0.41 (target >= 0.60)

Next move: Operational Surface is 243% over budget. Remove single-use abstractions so the
solution exposes fewer moving parts.
```

This is the sample's headline result: **1 of 3 dimensions within budget**, driven by
single-implementation interfaces and by only 41% of the code sitting on the primary path.

### `baseline` and `diff` — the CI gate

```bash
$CLI baseline $SLN          # writes .simplicity-baseline.json next to the .sln
# ... make a change ...
$CLI diff $SLN --fail-on-regression   # exit code 1 on regression
```

Neither `.simplicity-baseline.json` nor `.simplicity-history/` is committed with the sample; create
them locally. A clean `diff` right after `baseline` looks like this:

```
Metric delta
- Total projects: 8 -> 8 (0)
- Single-implementation interfaces: 6 -> 6 (0)
- Premature abstraction ratio: 0.86 -> 0.86 (0.00)
- Unused dependencies: 1 -> 1 (0)
- Average method complexity: 1.57 -> 1.57 (0.00)

Filter score delta
- TwoAmTest: 1.00 -> 1.00 (0.00)
- HalfRule: 0.63 -> 0.63 (0.00)
- PrimaryPathFirst: 0.75 -> 0.75 (0.00)

Regression status: no regressions detected.
```

Try it: delete `IClock` and use `SystemClock` directly, re-run `diff`, and watch the
premature-abstraction ratio and the HalfRule score improve.

### `watch` — live feedback while refactoring

```bash
$CLI watch $SLN
```

Stays open, re-analyzes on file change, and prints the filter verdicts each time. Useful when you
are working through the fixes in the last section of this document.

---

## Tool 2 — the HTML report

```bash
$CLI report $SLN
open samples/Sample.ClaimsPortal/simplicity-report/index.html
```

Writes a self-contained page (metric cards, filter verdicts, complexity budget, trend chart) and
appends a snapshot to `.simplicity-history/`. Run `report` a few times as you refactor to get a
real trend line instead of a single point. Both output locations are gitignored.

---

## Tool 3 — the Roslyn analyzers

All seven diagnostics fire in this sample. The analyzer wiring is opt-in so an ordinary build stays
quiet:

```bash
dotnet build samples/Sample.ClaimsPortal/Sample.ClaimsPortal.sln -p:UseSimplicityAnalyzers=true
```

| Rule | Severity | Count | Where it fires | Why |
| --- | --- | --- | --- | --- |
| **SF0001** Interface has single implementation | Info | 6 | `IClock`, `IClaimNumberGenerator`, `ITelemetrySink`, `IPolicyDirectory`, `IPayoutLedger`, `INotifier` | Each was added "for testability" or "for when we swap the backend". Neither happened. |
| **SF0002** Package reference has no symbol usage | Info | 1 | `Sample.ClaimsPortal.Notifications.csproj` → `Humanizer.Core` | The templates were rewritten with interpolated strings; nobody removed the package. |
| **SF0003** Method is too complex | Warning | 1 | `ClaimAdjudicator.Adjudicate` — complexity **21** vs limit 10 | Three years of business rules in one block. |
| **SF0004** Call chain is too deep | Warning | 2 | `ClaimWorkflow.Submit` (9 layers), `ClaimIntakeService.Intake` (10 layers) vs limit 8 | Submit → Coordinate → Validate → Route → Dispatch → Handle → Map → Save → Append → Write. |
| **SF0005** Constructor takes too many parameters | Warning | 2 | `ClaimIntakeService` (8), `ClaimSubmission` (9) vs limit 7 | One class owns intake, routing, payout, notification, and telemetry. |
| **SF0006** Generic parameter has one specialization | Info | 1 | `Envelope<T>` — only ever `Envelope<ClaimSubmission>` | Indirection without flexibility. |
| **SF0007** Supporting file out-references the primary path | Warning | 9 | `RequestContext.cs` (45 inbound vs 7 for the busiest endpoint), `ApiResult.cs` (39), `ClaimDecision.cs` (97) | Readers meet the plumbing before they meet the product. |

**Deliberate non-hit:** `IFraudSignal` has two implementations (`VelocitySignal`,
`AmountAnomalySignal`), so SF0001 stays quiet. An abstraction that is genuinely polymorphic earns
its keep — the analyzer is not against interfaces, it is against interfaces with one implementation.

**Code fixes.** SF0001 and SF0002 ship IDE code fixes. Open `IClock.cs` or the Notifications
`.csproj` in Visual Studio or Rider with the analyzers wired up and the lightbulb rewrites the
interface away or deletes the package reference.

### Seeing the Info-level rules

SF0001, SF0002, and SF0006 are `Info` severity: they show in the IDE, but not in ordinary
`dotnet build` console output. Two ways to see them from the command line:

```bash
# Promote them — uncomment the dotnet_diagnostic lines in .editorconfig, then build.
# Or capture everything in a SARIF log:
dotnet build samples/Sample.ClaimsPortal/Sample.ClaimsPortal.Notifications/Sample.ClaimsPortal.Notifications.csproj \
  -p:UseSimplicityAnalyzers=true -t:Rebuild -p:ErrorLog=/tmp/notifications.sarif
```

SF0002 reports on the `.csproj` line that declares the package, so its severity has to be set from a
`[*.csproj]` section — a `[*.cs]` section will not reach it. The sample's `.editorconfig` documents
this.

---

## Tool 4 — the filter evaluators

The three teaching filters turn the raw snapshot into verdicts. `tools/SimplicityReport` calls them
directly:

```bash
dotnet run --project samples/Sample.ClaimsPortal/tools/SimplicityReport
```

```
Filter Verdicts
----------------------------------------
TwoAmTest         PASS  score 1.00
  TwoAmTest passes with score 1.00 (3/3 checks at or above 0.70).
HalfRule          FAIL  score 0.63
  HalfRule fails with score 0.63 (2/3 checks at or above 0.70).
  - Single-implementation interfaces exceed the Half-Rule tolerance.
  -> Remove or inline single-implementation interfaces before adding more abstractions.
PrimaryPathFirst  PASS  score 0.75
  PrimaryPathFirst passes with score 0.75 (1/3 checks at or above 0.70).
  - Too little of the codebase sits on the primary path to satisfy this filter.
  - Project count is above the Primary Path First target.
  -> Merge or remove low-value projects until the solution shape is easier to follow.

Simplicity score: 57/100
```

A mixed result is the point. **TwoAmTest passes** — average complexity is low, so most of the code
really is readable at 2 AM. **HalfRule fails** — the abstraction budget is spent on interfaces that
buy nothing. **PrimaryPathFirst passes but barely**, and it names both reasons: too little primary
path, too many projects. A single red/green light would have hidden all of that.

The relevant API surface:

```csharp
var thresholds = new FilterThresholds(
    PrimaryPathRatioTarget: 0.60,
    PrematureAbstractionRatioTarget: 0.25,
    MaxMethodComplexity: 5.0,
    MaxOnboardingHours: 40.0,
    PassingScore: 0.70);

FilterVerdict[] verdicts =
[
    TwoAmTestEvaluator.Evaluate(snapshot, thresholds),
    HalfRuleEvaluator.Evaluate(snapshot, thresholds),
    PrimaryPathFirstEvaluator.Evaluate(snapshot, thresholds)
];

var score = SimplicityScoring.CalculateScore(snapshot, thresholds);
```

---

## Tool 5 — the TCA calculator

The same tool prices the excess, using the team assumptions from `simplicity.json`:

```csharp
var inputs = new TcaInputs(
    TeamSize: 12,
    AverageEngineerMonthlySalaryUsd: 14_500m,
    EstimatedMonthlyIncidentCount: 6,
    OnCallHourlyRateUsd: 165m,
    AttritionCoefficientPercent: 18m);

var estimate = TcaEstimate.Create(snapshot, verdicts, inputs);
Console.WriteLine(estimate.ToExecutiveSummary());
```

```
Total Cost of Architecture (Annual Estimate)
============================================
Architecture excess over simplicity targets:
Infrastructure:   $672 - $1,248
Operational:      $0 - $0
Coordination:     $168,000 - $312,000
Cognitive:        $0 - $0
Opportunity:      $119,679 - $222,261
--------------------------------------------
TOTAL EXCESS:     $288,351 - $535,509 per year
Baseline operating cost at target: $163,200 per year (not attributed to architecture)
Note: Onboarding time was not measured; the cognitive category reports $0 excess.
```

Read the categories, not just the total. **Coordination** dominates because eight projects for one
claims workflow means eight build graphs, eight review surfaces, and eight places to look. The
model's per-project and per-incident assumptions are documented defaults on `TcaInputs` — replace
them with your organization's real numbers before quoting a figure to anyone.

---

## Configuration files

### `simplicity.json`

Read by every CLI command; must sit next to the `.sln`.

```json
{
  "$schema": "../../docs/simplicity-schema.json",
  "tca": {
    "teamSize": 12,
    "averageEngineerMonthlySalaryUsd": 14500,
    "estimatedMonthlyIncidentCount": 6,
    "onCallHourlyRateUsd": 165,
    "attritionCoefficientPercent": 18
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

The schema is at [`docs/simplicity-schema.json`](../../docs/simplicity-schema.json). The `$schema`
pointer is optional — the CLI accepts and ignores it — and gives you completion and validation in
editors. Every other unknown root property is rejected with a pointed error message.

### `.editorconfig`

Every analyzer knob, written out at its default value so the file doubles as a copy-paste starting
point:

| Key | Default | Rule |
| --- | --- | --- |
| `simplicity_first.sf0003_complexity_threshold` | 10 | SF0003 |
| `simplicity_first.sf0004_layer_threshold` | 8 | SF0004 |
| `simplicity_first.sf0005_parameter_threshold` | 7 | SF0005 |
| `simplicity_first.sf0002_excluded_packages` | *(empty)* | SF0002 |
| `simplicity_first.sf0007_convention_folders` | `Controllers, Endpoints, Handlers, Pages` | SF0007 |
| `simplicity_first.include_public_api` | `false` | SF0001, SF0006 |

The sample sets `include_public_api = true`. The default of `false` is right for a shipped library —
you cannot delete a public interface just because it has one implementation today — but this is an
application, so nothing outside the solution binds to these types. Without that setting SF0001 and
SF0006 stay silent here, because every type in the sample is public.

Thresholds are also relaxed for the test project, because wide, repetitive test code is not the
lesson this sample teaches.

---

## Fixing the sample

The sample is a fixture, so it is left broken on purpose. If you want to feel the tools work, fix it
on a branch and re-run `analyze`, `budget`, and `diff` after each step:

1. **Delete the five dead interfaces.** `IClock`, `IClaimNumberGenerator`, `ITelemetrySink`,
   `IPolicyDirectory`, `INotifier` each have one implementation and no test double. SF0001's code fix
   does it for you. Watch the premature-abstraction ratio fall from 0.86 and HalfRule go green.
2. **Remove the `Humanizer.Core` reference.** SF0002's code fix, one click.
3. **Collapse the intake chain.** `IntakeCoordinator`, `SubmissionValidator`, `TriageRouter`, and
   `AdjudicationGateway` add no behavior between the endpoint and `AdjudicationHandler`. Deleting
   them takes SF0004 from 10 layers to 4.
4. **Split `ClaimAdjudicator.Adjudicate`.** Eligibility, deductible, and category adjustments are
   three independent rule sets. Complexity 21 becomes three methods well under the threshold.
5. **Narrow `ClaimIntakeService`.** With the interfaces gone and payout/notification moved to the
   handler, the constructor drops under seven parameters.
6. **Merge projects.** `Platform`, `Policies`, `Fraud`, `Payments`, and `Notifications` are five
   projects for one workflow. Folders inside `Claims` would carry the same boundaries at a fraction
   of the coordination cost — this is the change the TCA estimate is pricing.

Steps 1–5 are mechanical. Step 6 is the one worth arguing about, which is exactly why the tools
report it as a number instead of an opinion.
