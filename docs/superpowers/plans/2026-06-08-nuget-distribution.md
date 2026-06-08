# NuGet Distribution & Doc Reconciliation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reconcile SimplicityTools' docs/code with reality (dead analyzer help links, a nonexistent CLI command, pre-launch install claims) and produce a first-public-release runbook for shipping 0.4.0 to NuGet.org plus an ongoing versioning strategy.

**Architecture:** Two deliverables. (1) Targeted drift fixes across seven analyzer source files, one docs file, and README. (2) A new standalone operator doc `docs/distribution-plan.md`. No application logic changes; "tests" here are grep/build/link verification commands. Work happens on branch `docs/nuget-distribution-plan` (already created; the design spec is already committed there).

**Tech Stack:** .NET 10 / Roslyn analyzers (netstandard2.0), Markdown docs, Astro docs-site (`docs-site/src/pages/analyzers/sf000x.astro` are the live help-link targets), GitHub Actions (`.github/workflows/nuget-publish.yml`).

**Verified facts this plan relies on:**
- Real CLI commands: `analyze`, `report`, `baseline`, `diff`, `budget`, `watch`. No `snapshot`.
- `dotnet simplicity baseline <sln>` writes `.simplicity-baseline.json` next to the solution.
- `dotnet simplicity report <sln>` reads `.simplicity-history/*.json` for trends and writes `./simplicity-report/index.html`.
- 7 analyzers set `helpLinkUri: "https://simplicity-first.dev/analyzers/SF000X"` (404). Live routes: `https://simplicitytools.dev/analyzers/sf000x` (lowercase).
- Nothing is on NuGet; no git tags; version `0.4.0` in `Directory.Build.props`.

---

## File Structure

**Modify (code — analyzer help links):**
- `src/SimplicityTools.Analyzers/SingleImplementationInterfaceAnalyzer.cs` (SF0001)
- `src/SimplicityTools.Analyzers/UnusedDependencyAnalyzer.cs` (SF0002)
- `src/SimplicityTools.Analyzers/HighComplexityAnalyzer.cs` (SF0003)
- `src/SimplicityTools.Analyzers/AbstractionLayerDepthAnalyzer.cs` (SF0004)
- `src/SimplicityTools.Analyzers/ConstructorParameterCountAnalyzer.cs` (SF0005)
- `src/SimplicityTools.Analyzers/SingleSpecializationGenericParameterAnalyzer.cs` (SF0006)
- `src/SimplicityTools.Analyzers/NonPrimaryPathOverReferencedAnalyzer.cs` (SF0007)

**Modify (docs):**
- `docs/using-the-simplicity-tools.md` (~line 917 — `snapshot` → `baseline` + copy)
- `README.md` (Quick Install / Get Started — pre-launch honesty + correct snippets)

**Create:**
- `docs/distribution-plan.md` (the new operator runbook, spec §4)

---

## Task 1: Retarget analyzer help links to the live docs site

**Files:**
- Modify: all 7 analyzer `.cs` files listed above

- [ ] **Step 1: Verify the current dead links (baseline)**

Run:
```bash
grep -rn "simplicity-first.dev" src/SimplicityTools.Analyzers/
```
Expected: 7 lines, one per analyzer, each `helpLinkUri: "https://simplicity-first.dev/analyzers/SF000X",`

- [ ] **Step 2: Replace all dead help-link hosts in one sweep**

The path segment case must change from `SF000X` (uppercase) to `sf000x` (lowercase) to match the live Astro routes. Run a per-file `sed` so each ID is lowercased correctly:
```bash
cd src/SimplicityTools.Analyzers
sed -i '' 's#https://simplicity-first.dev/analyzers/SF0001#https://simplicitytools.dev/analyzers/sf0001#' SingleImplementationInterfaceAnalyzer.cs
sed -i '' 's#https://simplicity-first.dev/analyzers/SF0002#https://simplicitytools.dev/analyzers/sf0002#' UnusedDependencyAnalyzer.cs
sed -i '' 's#https://simplicity-first.dev/analyzers/SF0003#https://simplicitytools.dev/analyzers/sf0003#' HighComplexityAnalyzer.cs
sed -i '' 's#https://simplicity-first.dev/analyzers/SF0004#https://simplicitytools.dev/analyzers/sf0004#' AbstractionLayerDepthAnalyzer.cs
sed -i '' 's#https://simplicity-first.dev/analyzers/SF0005#https://simplicitytools.dev/analyzers/sf0005#' ConstructorParameterCountAnalyzer.cs
sed -i '' 's#https://simplicity-first.dev/analyzers/SF0006#https://simplicitytools.dev/analyzers/sf0006#' SingleSpecializationGenericParameterAnalyzer.cs
sed -i '' 's#https://simplicity-first.dev/analyzers/SF0007#https://simplicitytools.dev/analyzers/sf0007#' NonPrimaryPathOverReferencedAnalyzer.cs
cd ../..
```
(Note: `sed -i ''` is the BSD/macOS form. On Linux use `sed -i`.)

- [ ] **Step 3: Verify no dead host remains and routes are lowercase**

Run:
```bash
grep -rn "simplicity-first.dev" src/SimplicityTools.Analyzers/   # expect: no output
grep -rn "simplicitytools.dev/analyzers/sf000" src/SimplicityTools.Analyzers/ | wc -l   # expect: 7
```
Confirm each target route file exists in the site:
```bash
for n in 1 2 3 4 5 6 7; do test -f docs-site/src/pages/analyzers/sf000$n.astro && echo "sf000$n OK"; done
```
Expected: `sf0001 OK` … `sf0007 OK`.

- [ ] **Step 4: Build the analyzers project to confirm the edits compile**

Run:
```bash
dotnet build src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj --nologo --verbosity quiet
```
Expected: Build succeeded. (Pre-existing CS8604 warnings from the review may still appear; they are gated separately and are not introduced by this task.)

- [ ] **Step 5: Commit**

```bash
git add src/SimplicityTools.Analyzers/*.cs
git commit -m "fix(analyzers): retarget SF0001-SF0007 help links to simplicitytools.dev"
```

---

## Task 2: Remove the nonexistent `dotnet simplicity snapshot` command from docs

**Files:**
- Modify: `docs/using-the-simplicity-tools.md` (~line 917)

- [ ] **Step 1: Confirm the single offending reference**

Run:
```bash
grep -rn "simplicity snapshot" docs/ README.md | grep -v CODEBASE_REVIEW
```
Expected: exactly one hit — `docs/using-the-simplicity-tools.md:917`.

- [ ] **Step 2: Replace the snapshot step with the real baseline+copy flow**

In `docs/using-the-simplicity-tools.md`, find this block:
```yaml
- name: Save snapshot for trends
  run: |
    mkdir -p .simplicity-history
    cp $(dotnet simplicity snapshot YourSolution.sln) .simplicity-history/$(date +%Y-%m-%d).json
  continue-on-error: true
```
Replace it with (uses the real `baseline` command, which writes `.simplicity-baseline.json` next to the solution; `report` then reads `.simplicity-history/*.json` for trends):
```yaml
- name: Save snapshot for trends
  run: |
    mkdir -p .simplicity-history
    dotnet simplicity baseline YourSolution.sln
    cp .simplicity-baseline.json .simplicity-history/$(date +%Y-%m-%d).json
  continue-on-error: true
```

- [ ] **Step 3: Verify no `snapshot` command reference survives**

Run:
```bash
grep -rn "simplicity snapshot" docs/ README.md | grep -v CODEBASE_REVIEW
```
Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add docs/using-the-simplicity-tools.md
git commit -m "docs: replace nonexistent 'simplicity snapshot' with real baseline flow"
```

---

## Task 3: Make README install honest pre-launch and correct the snippets

**Files:**
- Modify: `README.md` (Quick Install table region ~lines 74-104)

- [ ] **Step 1: Add a pre-launch availability note above the Quick Install table**

In `README.md`, locate the `### ⚡ Quick Install` heading (line ~74). Immediately after that heading line and before the package table, insert:
```markdown
> **Availability:** SimplicityTools is not on NuGet.org yet. The install commands and version badges below become valid once the matching release tag (`libraries/vX.Y.Z`, `analyzers/vX.Y.Z`, `cli/vX.Y.Z`) is published. Until then, [build from source](#get-started) or follow the [distribution plan](docs/distribution-plan.md). The badges self-update on first publish.
```

- [ ] **Step 2: Remove the now-redundant "(when published)" hedge in Get Started**

In `README.md`, find:
```markdown
1. **Install the global tool** (when published):
```
Replace with:
```markdown
1. **Install the global tool** (once published — see [availability](#-quick-install)):
```

- [ ] **Step 3: Verify the three install snippets match real package IDs / tool name**

Run:
```bash
grep -n "dotnet tool install --global SimplicityTools.Cli" README.md   # global tool
grep -n "dotnet add package SimplicityTools.Metrics" README.md         # libraries
grep -n 'PrivateAssets="all"' README.md                                # analyzers
grep -n "dotnet simplicity analyze" README.md                          # invoked command name
```
Expected: each grep returns at least one line. (These already exist and are correct; this step confirms the honesty edits did not break them.)

- [ ] **Step 4: Confirm no stray availability contradictions remain**

Run:
```bash
grep -n "when published" README.md
```
Expected: only the single corrected reference from Step 2 (`once published — see`). No bare "(when published)" remains.

- [ ] **Step 5: Commit**

```bash
git add README.md
git commit -m "docs(readme): state pre-launch NuGet availability, link distribution plan"
```

---

## Task 4: Sweep docs for command/flag accuracy against the real 6-command surface

**Files:**
- Modify (only if drift found): `docs/quickstart.md`, `docs/using-the-simplicity-tools.md`, `docs/troubleshooting.md`

- [ ] **Step 1: Enumerate every `dotnet simplicity <word>` invocation in docs**

Run:
```bash
grep -rhoE "dotnet simplicity [a-z]+" docs/*.md README.md | sort -u
```
Expected (allowed) set: `analyze`, `baseline`, `budget`, `diff`, `report`, `watch`. Any other verb (e.g. `snapshot`, `init`, `check`) is drift.

- [ ] **Step 2: Fix any non-allowed verb found**

For each disallowed verb, open the file at the reported location and rewrite the example using the correct real command (see this plan's "Verified facts" for behavior). If Step 1 returns only the six allowed verbs, make no change and note "no command drift found" — skip to Step 4.

- [ ] **Step 3: Spot-check the headline flag `--fail-on-regression` is attached to `diff`**

Run:
```bash
grep -rn "fail-on-regression" docs/ README.md
```
Expected: every occurrence is on a `dotnet simplicity diff ...` line (this is the gating flag from the README). If any occurrence is attached to a different command, correct it to `diff`.

- [ ] **Step 4: Commit (only if changes were made)**

```bash
git add -A docs/ README.md
git commit -m "docs: align command/flag examples with the real CLI surface"
```
If Step 2 and Step 3 produced no edits, skip the commit and record "no drift" in the task notes.

---

## Task 5: Write the NuGet distribution plan (`docs/distribution-plan.md`)

**Files:**
- Create: `docs/distribution-plan.md`

- [ ] **Step 1: Create the file with the full runbook content**

Write `docs/distribution-plan.md` with exactly this content:

````markdown
# SimplicityTools Distribution Plan

**Status:** First public release of `0.4.0` (never shipped) + ongoing strategy.
**Audience:** Maintainers/operators cutting releases.
**Companion:** Pre-publish blockers are tracked in [`CODEBASE_REVIEW_2026-05-28.md`](CODEBASE_REVIEW_2026-05-28.md); release mechanics in [`../CONTRIBUTING.md`](../CONTRIBUTING.md).

SimplicityTools ships five NuGet packages in three independently tagged release groups:

| Release group | Tag | Packages |
| --- | --- | --- |
| Shared libraries | `libraries/vX.Y.Z` | `SimplicityTools.Metrics`, `SimplicityTools.Filters`, `SimplicityTools.Tca` |
| Analyzers | `analyzers/vX.Y.Z` | `SimplicityTools.Analyzers` |
| CLI | `cli/vX.Y.Z` | `SimplicityTools.Cli` (`dotnet-simplicity` global tool) |

The canonical version baseline is `SimplicityToolsReleaseVersion` in `Directory.Build.props` (currently `0.4.0`).

---

## 1. Pre-publish gate (must be GREEN before any real tag)

Treat the 2026-05-28 review's blockers as open. Do not push a stable tag until all pass:

- [ ] Clean build, zero warnings (including CS8604 in Analyzers):
  `dotnet build SimplicityTools.sln --nologo --verbosity minimal`
- [ ] Full test suite green (Sample.Simplified baseline, CLI P95 perf gate):
  `dotnet test SimplicityTools.sln --nologo --no-build --verbosity minimal`
- [ ] Analyzer package consumer validation: pack, reference from a scratch project with `PrivateAssets="all"`, confirm at least one `SF000x` diagnostic loads and **no** `lib/` compile assets or NuGet dependencies leak downstream.
- [ ] CLI packaged smoke test: install from a local feed, then
  `dotnet simplicity analyze ./samples/Sample.Simplified/Sample.Simplified.sln` succeeds.
- [ ] Docs reconciled: no `simplicity-first.dev` links in `src/` or `docs/`; no `dotnet simplicity snapshot`; README states pre-launch availability.

Go/No-Go: any unchecked box = NO-GO.

## 2. Publishing mechanics

- **Secret:** `NUGET_API_KEY` stored in repo Actions secrets. Use a key scoped to the `SimplicityTools.*` package glob with a finite expiry; rotate after first release.
- **Version source:** `Directory.Build.props` sets the baseline; the pushed tag's SemVer is authoritative at publish time. Local `dotnet pack` defaults to `<version>-local`.
- **Tag → publish:** `nuget-publish.yml` reads the SemVer from the tag, validates the matching package group, and publishes `.nupkg` + `.snupkg` to NuGet.org. Branch pushes only produce `-ci.<run>` validation artifacts.
- **Validation dry-run:** Actions → NuGet release pipeline → Run workflow with `release_group=validation` to exercise pack/validate without publishing.
- **Prerelease first:** because this is the first-ever publish, ship `0.4.0-preview.1` (or `-rc.1`) per group first to prove the live pipeline and install UX, then the bare `0.4.0` stable.

## 3. User install paths + post-publish smoke tests

### 3.1 Global CLI tool
```bash
dotnet tool install --global SimplicityTools.Cli
dotnet simplicity analyze path/to/YourSolution.sln
```
Smoke test after publish: install on a clean machine/container, run `analyze` on `Sample.Simplified`, confirm exit 0 and a metrics summary.

### 3.2 Libraries
```bash
dotnet add package SimplicityTools.Metrics   # + Filters / Tca as needed (version together)
```
Smoke test: a scratch console app calls `await new SimplicityCollector().CollectAsync("...sln")` and prints `snapshot.ToSummary()`.

### 3.3 Analyzers
```xml
<PackageReference Include="SimplicityTools.Analyzers" Version="x.y.z" PrivateAssets="all" />
```
Smoke test: build a scratch project that references it and confirm an `SF000x` warning appears in build output, with no added compile/runtime assets in the consumer graph.

## 4. First-release runbook (ordered)

1. Confirm the §1 gate is fully GREEN.
2. Confirm `SimplicityToolsReleaseVersion` is `0.4.0`.
3. (Optional) Run the `validation` workflow dispatch; confirm artifacts build.
4. Push **prerelease** tags, libraries first (dependency root):
   `git tag libraries/v0.4.0-preview.1 && git push origin libraries/v0.4.0-preview.1`
   then `analyzers/v0.4.0-preview.1`, then `cli/v0.4.0-preview.1`.
5. After each publish, verify on NuGet.org and run the matching §3 smoke test.
6. When prerelease is validated, push **stable** tags in the same order:
   `libraries/v0.4.0` → `analyzers/v0.4.0` → `cli/v0.4.0`.
7. Re-run all three smoke tests against the stable packages.
8. Announce (README badges go live automatically; update CHANGELOG).

## 5. Ongoing strategy

- **SemVer policy:** patch (`x.y.Z`) = additive/safe; minor (`x.Y.z`) = new metrics/rules — evaluate before upgrading; major (`X.y.z`) = breaking API. `Metrics`/`Filters`/`Tca` always version together; `Analyzers` and `Cli` move on their own cadence.
- **Prerelease channel:** publish `-preview.N`/`-rc.N` for any change touching the published API surface or the analyzer package layout before the stable tag.
- **Changelog:** maintain `CHANGELOG.md` with one section per release group and version.
- **Bad publish:** NuGet packages are immutable — never attempt deletion. Unlist the broken version and publish a fixed patch.
````

- [ ] **Step 2: Verify the file exists and has all five sections**

Run:
```bash
grep -nE "^## [1-5]\." docs/distribution-plan.md
```
Expected: five headings — `## 1. Pre-publish gate`, `## 2. Publishing mechanics`, `## 3. User install paths`, `## 4. First-release runbook`, `## 5. Ongoing strategy`.

- [ ] **Step 3: Verify the README link target now resolves**

Run:
```bash
test -f docs/distribution-plan.md && echo "link target OK"
```
Expected: `link target OK` (this is the file `README.md` links to from Task 3 Step 1).

- [ ] **Step 4: Commit**

```bash
git add docs/distribution-plan.md
git commit -m "docs: add NuGet/dotnet-tool/CLI distribution plan and first-release runbook"
```

---

## Task 6: Final acceptance verification (spec §5)

**Files:** none (verification only; fix-and-recommit if a check fails)

- [ ] **Step 1: No dead host in code or docs (excluding history + dated review)**

Run:
```bash
grep -rn "simplicity-first.dev" src/ docs/ README.md | grep -v "CODEBASE_REVIEW_2026-05-28"
```
Expected: no output.

- [ ] **Step 2: No `snapshot` command reference**

Run:
```bash
grep -rn "simplicity snapshot" docs/ README.md | grep -v "CODEBASE_REVIEW_2026-05-28"
```
Expected: no output.

- [ ] **Step 3: Distribution plan present with runbook + gate**

Run:
```bash
grep -c "Go/No-Go" docs/distribution-plan.md          # expect: 1
grep -c "First-release runbook" docs/distribution-plan.md   # expect: 1
```

- [ ] **Step 4: Solution still builds (no regression from edits)**

Run:
```bash
dotnet build SimplicityTools.sln --nologo --verbosity minimal
```
Expected: Build succeeded (pre-existing gated warnings allowed; no NEW errors from doc/link edits).

- [ ] **Step 5: Review the full diff and confirm scope**

Run:
```bash
git log --oneline main..HEAD
git diff --stat main..HEAD
```
Expected: commits for Tasks 1–5 (and 4 only if drift was found); changed files limited to the 7 analyzers, `docs/using-the-simplicity-tools.md`, `README.md`, `docs/distribution-plan.md`, and the already-committed spec. Nothing outside the plan's File Structure.

- [ ] **Step 6: Finalize**

Branch `docs/nuget-distribution-plan` is ready. Use the superpowers:finishing-a-development-branch skill to choose merge/PR.
```

---

## Self-Review Notes

- **Spec coverage:** §3.1 → Task 1; §3.2 → Task 2; §3.3 → Task 3; §3.4 → Task 4; §4.1–4.5 → Task 5; §5 acceptance → Task 6. All spec sections mapped.
- **Placeholder scan:** No TBD/TODO; every edit step shows exact strings/commands and the full content of the new doc.
- **Consistency:** README link `docs/distribution-plan.md` (Task 3 Step 1) matches the file created in Task 5 and verified in Task 6 Step 3. Analyzer route casing (`sf000x`, lowercase) is consistent across Task 1 and the verified `docs-site` page files. The `baseline`→`.simplicity-baseline.json`→`.simplicity-history/` flow in Task 2 matches the verified CLI behavior.
