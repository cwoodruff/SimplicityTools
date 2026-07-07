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

The canonical version baseline is `SimplicityToolsReleaseVersion` in `Directory.Build.props` (currently `0.5.0`).

---

## 1. Pre-publish gate (must be GREEN before any real tag)

Treat the 2026-05-28 review's blockers as open. Do not push a stable tag until all pass:

- [ ] Clean build, zero warnings (including CS8604 in Analyzers):
  `dotnet build SimplicityTools.sln --nologo --verbosity minimal`
- [ ] Full test suite green (Sample.Simplified baseline, CLI P95 perf gate):
  `dotnet test SimplicityTools.sln --nologo --no-build --verbosity minimal`
- [ ] Analyzer package consumer validation: pack, reference from a scratch project with `PrivateAssets="all"`, confirm at least one `SF000x` diagnostic loads and **no** `lib/` compile assets or NuGet dependencies leak downstream. The package must set `<developmentDependency>true</developmentDependency>` (already set in the analyzer .csproj). See the local-feed steps in [../CONTRIBUTING.md](../CONTRIBUTING.md).
- [ ] CLI packaged smoke test: pack and install from a local folder feed (`dotnet tool install --global SimplicityTools.Cli --add-source ./artifacts/local-feed --version <version>-local`; see [`../CONTRIBUTING.md`](../CONTRIBUTING.md)), then `dotnet simplicity analyze ./samples/Sample.Simplified/Sample.Simplified.sln` succeeds.
- [ ] Docs reconciled: no `simplicity-first.dev` links in `src/` or `docs/`; no `dotnet simplicity snapshot`; README states pre-launch availability.

Go/No-Go: any unchecked box = NO-GO.

## 2. Publishing mechanics

- **Secret:** `NUGET_API_KEY` stored in repo Actions secrets. Use a key scoped to the `SimplicityTools.*` package glob with a finite expiry; rotate after first release.
- **Version source:** `Directory.Build.props` sets the baseline; the pushed tag's SemVer is authoritative at publish time. Local `dotnet pack` defaults to `<version>-local`.
- **Tag -> publish:** `nuget-publish.yml` reads the SemVer from the tag, validates the matching package group, and publishes `.nupkg` + `.snupkg` to NuGet.org. Branch pushes only produce `-ci.<run>` validation artifacts.
- **Validation dry-run:** Actions -> NuGet release pipeline -> Run workflow with `release_group=validation` to exercise pack/validate without publishing.
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

1. Confirm the section 1 gate is fully GREEN.
2. Confirm `SimplicityToolsReleaseVersion` is `0.5.0`.
3. (Optional) Run the `validation` workflow dispatch; confirm artifacts build.
4. Push **prerelease** tags, libraries first (dependency root):
   `git tag libraries/v0.4.0-preview.1 && git push origin libraries/v0.4.0-preview.1`
   then `analyzers/v0.4.0-preview.1`, then `cli/v0.4.0-preview.1`.
5. After each publish, verify on NuGet.org and run the matching section 3 smoke test.
6. When prerelease is validated, push **stable** tags in the same order:
   `libraries/v0.4.0` -> `analyzers/v0.4.0` -> `cli/v0.4.0`.
7. Re-run all three smoke tests against the stable packages.
8. Announce (README badges go live automatically; update CHANGELOG).

## 5. Ongoing strategy

- **SemVer policy:** patch (`x.y.Z`) = additive/safe; minor (`x.Y.z`) = new metrics/rules — evaluate before upgrading; major (`X.y.z`) = breaking API. `Metrics`/`Filters`/`Tca` always version together; `Analyzers` and `Cli` move on their own cadence.
- **Prerelease channel:** publish `-preview.N`/`-rc.N` for any change touching the published API surface or the analyzer package layout before the stable tag.
- **Changelog:** maintain `CHANGELOG.md` with one section per release group and version.
- **Bad publish:** NuGet packages are immutable — never attempt deletion. Unlist the broken version and publish a fixed patch.
