# SimplicityTools — Documentation Reconciliation & NuGet Distribution Design

**Date:** 2026-06-08
**Author:** Chris Woodruff (with Claude Code)
**Status:** Design — pending implementation plan
**Supersedes:** Nothing. Complements `docs/CODEBASE_REVIEW_2026-05-28.md` (the still-open blocker record).

---

## 1. Purpose of the Solution (Context)

SimplicityTools is a .NET toolkit that measures solution complexity and surfaces simplification opportunities in the IDE and CI/CD. It ships as five independently versioned NuGet packages:

| Package | Role |
| --- | --- |
| `SimplicityTools.Metrics` | Core snapshot/collection API (`SimplicityCollector.CollectAsync`) |
| `SimplicityTools.Filters` | Health-verdict evaluators (TwoAmTest, HalfRule, PrimaryPathFirst) |
| `SimplicityTools.Tca` | Total-cost-of-complexity estimate |
| `SimplicityTools.Analyzers` | Seven Roslyn diagnostics (SF0001–SF0007) + two code fixes |
| `SimplicityTools.Cli` | `dotnet-simplicity` global tool |

Distribution is designed around **tag-driven CI publishing** to NuGet.org, with three release groups: `libraries/vX.Y.Z` (Metrics+Filters+Tca together), `analyzers/vX.Y.Z`, and `cli/vX.Y.Z`. The canonical version baseline lives in `Directory.Build.props` (`SimplicityToolsReleaseVersion`, currently `0.4.0`).

### Verified current state (2026-06-08)

- **Nothing has shipped.** No git tags exist; neither `SimplicityTools.Cli` nor `SimplicityTools.Metrics` resolve on NuGet.org (`BlobNotFound`). Version is pinned at `0.4.0`, never released.
- **Real CLI command surface:** `analyze`, `report`, `baseline`, `diff`, `budget`, `watch` (6 commands). There is **no** `snapshot` command.
- **Dead analyzer help links:** All seven analyzers set `helpLinkUri` to `https://simplicity-first.dev/analyzers/SF000X` (404). The live docs site serves `https://simplicitytools.dev/analyzers/sf000x/` (lowercase).
- **Doc drift:** `docs/using-the-simplicity-tools.md:917` uses the nonexistent `dotnet simplicity snapshot`. README "Quick Install" presents NuGet badges and install commands as if live, hedged only with "(when published)".
- **Packaging mechanics look correct:** `SimplicityTools.Analyzers.csproj` now packs the DLL into `analyzers/dotnet/cs` via the `PackRoslynAnalyzerArtifacts` target (the review's B4 layout concern appears addressed in csproj; consumer validation in CI still to be confirmed). `nuget-publish.yml` and the tag-driven contract exist but have never been triggered.
- **Per user direction:** the 2026-05-28 review's blockers are assumed **still open** and are treated as a pre-publish gate, not re-audited here.

---

## 2. Goals

1. **Reconcile docs with reality** (fix drift) and **strengthen distribution-facing docs** (NuGet + dotnet tooling + CLI install/usage).
2. **Produce a first-public-release runbook** that takes `0.4.0` from never-shipped to live on NuGet.org, plus an **ongoing versioning/cadence strategy**.

Non-goals: re-auditing analyzer logic correctness; redesigning the CLI; broad README restructure beyond the install/distribution surface; fixing the code-side review blockers themselves (they are gated, not solved here).

---

## 3. Deliverable 1 — Documentation & Drift Fixes

### 3.1 Analyzer help links (code)
Retarget all seven `helpLinkUri` values from `https://simplicity-first.dev/analyzers/SF000X` to `https://simplicitytools.dev/analyzers/sf000x/` (lowercase, trailing slash to match the live routes). Files:
`SingleImplementationInterfaceAnalyzer.cs` (SF0001), `UnusedDependencyAnalyzer.cs` (SF0002), `HighComplexityAnalyzer.cs` (SF0003), `AbstractionLayerDepthAnalyzer.cs` (SF0004), `ConstructorParameterCountAnalyzer.cs` (SF0005), `SingleSpecializationGenericParameterAnalyzer.cs` (SF0006), `NonPrimaryPathOverReferencedAnalyzer.cs` (SF0007).

Verification: each SF000x route resolves on the live site (or is confirmed to exist in `docs-site`).

### 3.2 Remove the nonexistent `snapshot` command
`docs/using-the-simplicity-tools.md` (~line 917) shows `cp $(dotnet simplicity snapshot ...)`. Replace with a workflow using the real commands. `baseline` writes the snapshot JSON to a deterministic path (`BaselineSnapshotFile`); rewrite the history-capture example around `baseline` (and/or `report`'s JSON output) so it reflects shipping behavior. Sweep the rest of the doc for any other `snapshot`-as-command usages.

### 3.3 README install honesty + distribution surface
- Add a clear, prominent status note near "Quick Install": packages are **not yet on NuGet**; install commands become valid once the corresponding `vX.Y.Z` tag is published. Keep the badges (they self-update once live) but ensure surrounding prose does not imply current availability.
- Tighten the three install paths so each is copy-paste correct against the real package IDs and the `dotnet-simplicity` tool command name:
  - Global tool: `dotnet tool install --global SimplicityTools.Cli` → invoked as `dotnet simplicity ...`
  - Libraries: `dotnet add package SimplicityTools.Metrics|Filters|Tca`
  - Analyzers: `PackageReference ... PrivateAssets="all"`
- Ensure the command list reflects the real 6 commands (no `snapshot`).

### 3.4 Accurate command reference
Confirm `docs/using-the-simplicity-tools.md` and `docs/quickstart.md` document exactly the 6 real commands with correct flags (e.g., `diff --fail-on-regression`). Remove or correct any command/flag not present in `Program.cs`. (Scope-limited: fix incorrect/nonexistent items; not a full rewrite.)

---

## 4. Deliverable 2 — NuGet Distribution Plan (new doc)

A standalone runbook written to `docs/distribution-plan.md` (user-facing/operator doc, distinct from this spec). Structure:

### 4.1 Pre-publish gate (blockers must pass)
A checklist derived from `CODEBASE_REVIEW_2026-05-28.md`, treated as still-open:
- [ ] `dotnet build SimplicityTools.sln` — 0 warnings (incl. CS8604 in Analyzers)
- [ ] `dotnet test SimplicityTools.sln` — all green (Sample.Simplified baseline, CLI P95 perf gate)
- [ ] Analyzer package consumer validation: pack → reference from scratch project → confirm an `SF000x` diagnostic loads and no `lib/` compile assets / NuGet deps leak
- [ ] CLI packaged smoke test: install from local feed → `dotnet simplicity analyze` on `Sample.Simplified`
- [ ] Doc drift fixed (Deliverable 1 merged; no dead `simplicity-first.dev` links, no `snapshot` command)

The gate is a hard go/no-go before any real tag is pushed.

### 4.2 Publishing mechanics
- **Secrets:** `NUGET_API_KEY` configured in repo secrets; scope and expiry noted.
- **Release groups & tags:** `libraries/vX.Y.Z`, `analyzers/vX.Y.Z`, `cli/vX.Y.Z` — what each publishes and why libraries move together.
- **Version source:** `Directory.Build.props` `SimplicityToolsReleaseVersion`; tag SemVer authoritative at publish.
- **Prerelease vs stable:** recommend a `0.4.0-preview.N` (or `-rc.N`) first push to validate the full pipeline against NuGet.org before the bare `0.4.0` stable, since this is the first-ever publish.
- **Validation-only dispatch:** `release_group=validation` workflow path for dry runs.

### 4.3 Three user install paths (with post-publish smoke tests)
For each: install command, minimal usage, and a verification step that proves the published artifact works:
1. **Global tool** — `dotnet tool install --global SimplicityTools.Cli`; verify `dotnet simplicity analyze`.
2. **Libraries** — `dotnet add package`; verify `SimplicityCollector.CollectAsync` compiles/runs in a scratch consumer.
3. **Analyzers** — `PackageReference PrivateAssets="all"`; verify a diagnostic surfaces and the package contributes no compile/runtime assets downstream.

### 4.4 First-release runbook (ordered)
Exact steps: confirm gate green → bump/confirm version → optional GitHub Actions validation dispatch → push prerelease tag(s) → verify on NuGet.org + run the three smoke tests → push stable tag(s) → re-verify → announce. Recommended order: `libraries` first (dependency root), then `analyzers` and `cli`.

### 4.5 Ongoing strategy
- **Cadence & SemVer policy:** patch = additive/safe; minor = new metrics/rules (evaluate); major = breaking API. Restate the libraries-version-together rule.
- **Prerelease channel:** when to ship `-preview`/`-rc`.
- **Changelog:** introduce `CHANGELOG.md` keyed to release groups.
- **Yank/deprecate policy:** how to handle a bad publish (NuGet unlist, not delete).

---

## 5. Acceptance Criteria

- No reference to `simplicity-first.dev` remains in `src/` or `docs/` (excluding `.squad/` history and the dated review).
- No reference to a `dotnet simplicity snapshot` command remains in `docs/` or `README.md`.
- README clearly states pre-launch availability and all three install snippets are copy-paste correct.
- `docs/distribution-plan.md` exists with all five sections (4.1–4.5), including a green/red pre-publish gate and an ordered first-release runbook with verification commands.
- This spec, the doc fixes, and the new plan are committed.

## 6. Out of Scope
- Fixing the code-side review blockers (tests, warnings, perf gate) — gated, executed separately.
- Re-auditing analyzer logic vs. docs (review item F1).
- Wiring `simplicity.json` config end-to-end (review item F2).
- Full README restructure / role-based navigation beyond the install surface.
