# Contributing to SimplicityTools

Thanks for helping ship SimplicityTools. The repository is organized around five publishable packages:

- `SimplicityTools.Metrics`
- `SimplicityTools.Filters`
- `SimplicityTools.Tca`
- `SimplicityTools.Analyzers`
- `SimplicityTools.Cli`

## Versioning strategy

SimplicityTools uses SemVer tags to drive package versions in CI.

### Release groups

| Release group | Packages | Tag format | Notes |
| --- | --- | --- | --- |
| Shared libraries | `SimplicityTools.Metrics`, `SimplicityTools.Filters`, `SimplicityTools.Tca` | `libraries/vX.Y.Z` | These three move together because they form the reusable API surface. |
| Analyzer package | `SimplicityTools.Analyzers` | `analyzers/vX.Y.Z` | Can advance independently from the shared libraries. |
| Global tool | `SimplicityTools.Cli` | `cli/vX.Y.Z` | Can advance independently from the shared libraries and analyzers. |

### What CI does with those tags

- Normal branch pushes run build, test, and package validation artifacts with a CI-only version (`0.4.0-ci.<run-number>`).
- Manual workflow dispatch can build upload-ready NuGet artifacts for `libraries`, `analyzers`, or `cli` when you supply an explicit SemVer.
- Tag pushes remain the publish gate: CI reads the SemVer from the tag, validates the matching package group, and publishes the generated `.nupkg` and `.snupkg` files to NuGet.org.

### Local default versions

If you run `dotnet pack` locally without passing `-p:Version=...`, the package projects default to `0.4.0-local`. That keeps manual validation obvious and avoids pretending a local build is a real release.

## Release process

### 1. Validate locally

```bash
dotnet build SimplicityTools.sln --nologo --verbosity minimal
dotnet test SimplicityTools.sln --nologo --no-build --verbosity minimal
```

### 2. Package the release candidates you plan to ship

Shared libraries:

```bash
dotnet pack src/SimplicityTools.Metrics/SimplicityTools.Metrics.csproj -c Release --no-build -o artifacts/packages -p:Version=0.4.0-local --nologo
dotnet pack src/SimplicityTools.Filters/SimplicityTools.Filters.csproj -c Release --no-build -o artifacts/packages -p:Version=0.4.0-local --nologo
dotnet pack src/SimplicityTools.Tca/SimplicityTools.Tca.csproj -c Release --no-build -o artifacts/packages -p:Version=0.4.0-local --nologo
```

Analyzer package:

```bash
dotnet pack src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj -c Release --no-build -o artifacts/packages -p:Version=0.4.0-local --nologo
```

CLI tool:

```bash
dotnet pack src/SimplicityTools.Cli/SimplicityTools.Cli.csproj -c Release --no-build -o artifacts/packages -p:Version=0.4.0-local --nologo
```

### 3. Test-publish to a local folder feed

Create a repo-local feed and push the generated packages into it:

```bash
mkdir -p artifacts/local-feed
for package in artifacts/packages/*.nupkg; do
  case "$package" in
    *.snupkg) continue ;;
  esac

  dotnet nuget push "$package" --source artifacts/local-feed --skip-duplicate
done
```

Then validate the install flow you changed:

- Libraries: add the local folder source in a sample or scratch consumer project.
- Analyzer package: reference it with `PrivateAssets="all"`.
- CLI tool: install from the local folder source.

The analyzer package should stay analyzer-only when consumed: diagnostics must load, but the package must not add `lib/` compile assets or NuGet dependencies to the consumer graph.

Example CLI install:

```bash
dotnet tool install --global SimplicityTools.Cli --add-source "$(pwd)/artifacts/local-feed" --version 0.4.0-local
```

Example analyzer reference:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Analyzers" Version="0.4.0-local" PrivateAssets="all" />
</ItemGroup>
```

### 4. Optional: build upload-ready artifacts in GitHub Actions

Use **Actions → NuGet release pipeline → Run workflow** when you want GitHub to build the exact packages before you cut a tag:

- Set `release_group` to `libraries`, `analyzers`, or `cli`
- Set `version` to the exact SemVer you plan to release

That run will validate the solution, produce upload-ready `.nupkg` and `.snupkg` artifacts, and stop short of publishing.

### 5. Cut the real release tag

When the release candidate is good, create the tag that matches the package group:

```bash
git tag libraries/v0.4.0
git push origin libraries/v0.4.0
```

Or:

```bash
git tag analyzers/v0.4.0
git push origin analyzers/v0.4.0
```

```bash
git tag cli/v0.4.0
git push origin cli/v0.4.0
```

GitHub Actions will read the version from the tag, rebuild, repack, and publish only the matching package set to NuGet.org.
