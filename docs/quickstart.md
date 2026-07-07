# Quickstart: Five Essential Commands

After installing `dotnet-simplicity`, try these five commands to understand how SimplicityTools works. This guide uses the Sample.Simplified project to demonstrate each workflow.

## 1. `analyze` — First Look at a Solution

**What it does:** Takes a snapshot of your solution's structure, complexity, and health.

```bash
dotnet simplicity analyze samples/Sample.Simplified/Sample.Simplified.sln
```

**Output:**
```text
Warning: simplicity.json was not found in '/Users/cwoodruff/Git/SimplicityTools/samples/Sample.Simplified'. Using built-in defaults for TCA inputs and filter thresholds.
Simplicity Snapshot (2026-05-01)
----------------------------------------
Projects: 2
Total files: 19
Primary path files: 5
Abstraction layers: 1
Single-impl interfaces: 0
External deps: 0 (0 unused)
Avg complexity: 1.4
Est. onboarding: not computed
```

**What this means:**
- **Projects/Files:** The scope of the solution (2 projects, 19 files).
- **Abstraction layers:** How many levels of indirection exist (1 = good; high numbers signal over-engineering).
- **Single-impl interfaces:** Interfaces with only one implementation (dead abstraction).
- **Avg complexity:** Average cyclomatic complexity per method (1.4 is healthy; >5 signals refactoring needed).
- **Est. onboarding:** Estimated onboarding time for a new team member. Shown as `not computed` until the metric is implemented — no verdict is fabricated from it.

Run this first anytime you want to check the current state of your solution.

---

## 2. `baseline` — Capture a Point in Time

**What it does:** Records the current snapshot so you can later compare drift (e.g., "Did complexity grow in the last sprint?").

```bash
dotnet simplicity baseline samples/Sample.Simplified/Sample.Simplified.sln
```

**Output:**
```text
Warning: simplicity.json was not found in '/Users/cwoodruff/Git/SimplicityTools/samples/Sample.Simplified'. Using built-in defaults for TCA inputs and filter thresholds.
Simplicity Snapshot (2026-05-01)
----------------------------------------
Projects: 2
Total files: 19
Primary path files: 5
Abstraction layers: 1
Single-impl interfaces: 0
External deps: 0 (0 unused)
Avg complexity: 1.4
Est. onboarding: not computed

Baseline written to /Users/cwoodruff/Git/SimplicityTools/samples/Sample.Simplified/.simplicity-baseline.json
```

**What this means:**
- Saves a `.simplicity-baseline.json` file in your solution directory.
- Use this in CI to prevent regression (see `diff` below).
- Establish a baseline after each major refactoring or milestone.

Run this once per planning cycle to establish your complexity budget for the sprint.

---

## 3. `report` — Shareable HTML Dashboard

**What it does:** Generates a self-contained HTML report with metrics, filter verdicts, and trend analysis.

```bash
dotnet simplicity report samples/Sample.Simplified/Sample.Simplified.sln
```

**Output:**
```text
Warning: simplicity.json was not found in '/Users/cwoodruff/Git/SimplicityTools/samples/Sample.Simplified'. Using built-in defaults for TCA inputs and filter thresholds.
Report generated to ./simplicity-report/index.html
```

**What this means:**
- Opens in any browser (no external dependencies).
- Includes metric cards, filter verdicts, complexity breakdown by project, and TCA estimate.
- Share with stakeholders to justify refactoring investment.

Run this weekly and commit the report to CI artifacts for trend tracking.

---

## 4. `diff` — Compare Against Baseline (Regression Gate)

**What it does:** Compares the current snapshot against your baseline and flags regressions.

```bash
dotnet simplicity diff samples/Sample.Simplified/Sample.Simplified.sln
```

**Output:**
```text
Warning: simplicity.json was not found in '/Users/cwoodruff/Git/SimplicityTools/samples/Sample.Simplified'. Using built-in defaults for TCA inputs and filter thresholds.
Simplicity Diff
---------------
Baseline file: /Users/cwoodruff/Git/SimplicityTools/samples/Sample.Simplified/.simplicity-baseline.json
Baseline snapshot: 2026-05-01
Current snapshot: 2026-05-01

Metric delta
- Total projects: 2 -> 2 (0)
- Total files: 19 -> 19 (0)
- Primary path files: 5 -> 5 (0)
- Abstraction layers: 1 -> 1 (0)
- Single-implementation interfaces: 0 -> 0 (0)
- Premature abstraction ratio: 0.00 -> 0.00 (0.00)
- External dependencies: 0 -> 0 (0)
- Unused dependencies: 0 -> 0 (0)
- Average method complexity: 1.35 -> 1.35 (0.00)
- Estimated onboarding time: not computed

Filter score delta
- TwoAmTest: 1.00 -> 1.00 (0.00)
- HalfRule: 1.00 -> 1.00 (0.00)
- PrimaryPathFirst: 0.79 -> 0.79 (0.00)

Regression status: no regressions detected.
```

**What this means:**
- Compares every metric from your baseline to today.
- Shows deltas (→) so you see exactly what changed.
- Zero delta in this case = clean sprint (no regression).
- **Use in CI:** Add `--fail-on-regression` to fail the build if complexity grew.

Run this in pull request CI to prevent complexity creep.

---

## 5. `budget` — Complexity Budget Status

**What it does:** Compares your solution against a complexity budget (cognitive load, operational surface, change safety, discoverability).

```bash
dotnet simplicity budget samples/Sample.Simplified/Sample.Simplified.sln
```

**Output:**
```text
Warning: simplicity.json was not found in '/Users/cwoodruff/Git/SimplicityTools/samples/Sample.Simplified'. Using built-in defaults for TCA inputs and filter thresholds.
Complexity Budget
-----------------
Status: 3/4 dimension(s) within budget.
Bars show configured budget used. Values above 100% are over budget.

Cognitive Load      not scored — onboarding time has not been computed.
Operational Surface [----------]     0%  WITHIN BUDGET
  Premature abstraction ratio: 0.00 (target <= 0.25)
Change Safety       [###-------]    27%  WITHIN BUDGET
  Average method complexity: 1.35 (target <= 5.00)
Discoverability     [##########]   276%  OVER BUDGET
  Primary path ratio: 0.22 (target >= 0.60)

Next move: Discoverability is 176% over budget. Move more of the main business flow onto the primary path so teams can trace it faster.
```

**What this means:**
- Budget is split into four dimensions. Each has a target.
- Green means within budget; red means over.
- Visual bar shows progress at a glance.
- **Cognitive Load:** How much onboarding time does the codebase cost?
- **Operational Surface:** Is there premature abstraction?
- **Change Safety:** Are methods too complex to safely refactor?
- **Discoverability:** Can a new team member trace the business flow?

In this example, the solution is doing well on three fronts but the "Discoverability" dimension is over budget because only 22% of code sits on the primary business path; the team should aim for ≥ 60%.

Run this during sprint planning and code review to make trade-off decisions: "Can we ship this feature *and* keep complexity in budget?"

---

## 6. `watch` — Live Feedback During Development

**What it does:** Monitors your solution and re-analyzes on file changes, showing you what got simpler (or more complex) in real time.

```bash
dotnet simplicity watch samples/Sample.Simplified/Sample.Simplified.sln
```

**Output (partial):**
```text
Watching /Users/cwoodruff/Git/SimplicityTools/samples/Sample.Simplified/Sample.Simplified.sln
Press Ctrl+C to stop.

Warning: simplicity.json was not found in '/Users/cwoodruff/Git/SimplicityTools/samples/Sample.Simplified'. Using built-in defaults for TCA inputs and filter thresholds.
Initial snapshot
----------------
Simplicity Snapshot (2026-05-01)
----------------------------------------
Projects: 2
Total files: 19
Primary path files: 5
Abstraction layers: 1
Single-impl interfaces: 0
External deps: 0 (0 unused)
Avg complexity: 1.4
Est. onboarding: not computed

Filter Verdicts
---------------
TwoAmTest: PASS (1.00)
  TwoAmTest passes with score 1.00 (4/4 checks at or above 0.70).

HalfRule: PASS (1.00)
  HalfRule passes with score 1.00 (3/3 checks at or above 0.70).

PrimaryPathFirst: PASS (0.79)
  PrimaryPathFirst passes with score 0.79 (2/3 checks at or above 0.70).
  - Too little of the codebase sits on the primary path to satisfy this filter.
  Next move: Move more business flow into the primary path and peel supporting concerns away from it.
```

**What this means:**
- Stays running and re-analyzes as you edit files.
- Shows initial snapshot, then updates on each file change.
- Great for refactoring: you see the impact immediately.
- Filter verdicts include actionable next steps.

Use `watch` during refactoring sessions to see your improvements in real time.

---

## Zero-Config Promise

All commands above work **with zero configuration**. SimplicityTools:
- Uses sensible built-in defaults for TCA inputs and filter thresholds.
- Issues warnings, not errors, when config is missing.
- Teaches by default: output always includes "Next move" guidance.

To customize budget thresholds or TCA inputs, add a `simplicity.json` file to your solution root (optional). See [simplicity-schema.json](simplicity-schema.json) for details.

---

## Next Steps

- **Add to CI:** Use `dotnet simplicity diff --fail-on-regression` in pull request checks.
- **Share insights:** Generate a report weekly and track trends.
- **Protect your budget:** Use `dotnet simplicity budget` in sprint planning.
- **IDE feedback:** Install the `SimplicityTools.Analyzers` package to surface issues inline.
- **Deep dive:** Read [Using the SimplicityTools Toolset](using-the-simplicity-tools.md) for full command reference and configuration options.
