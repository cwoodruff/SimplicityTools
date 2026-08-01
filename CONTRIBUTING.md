# Contributing to SimplicityTools

Thanks for helping ship SimplicityTools. The repository is organized around five publishable packages:

- `SimplicityTools.Metrics`
- `SimplicityTools.Filters`
- `SimplicityTools.Tca`
- `SimplicityTools.Analyzers`
- `SimplicityTools.Cli`

## Versioning strategy

SimplicityTools uses SemVer tags to drive package versions in CI.

The canonical release line lives in `Directory.Build.props` as `<SimplicityToolsReleaseVersion>`. Update that property when you prepare the next release train so local package defaults, workflow-dispatch packaging, and the docs-site footer all stay aligned.

### Release groups

| Release group | Packages | Tag format | Notes |
| --- | --- | --- | --- |
| Shared libraries | `SimplicityTools.Metrics`, `SimplicityTools.Filters`, `SimplicityTools.Tca` | `libraries/vX.Y.Z` | These three move together because they form the reusable API surface. |
| Analyzer package | `SimplicityTools.Analyzers` | `analyzers/vX.Y.Z` | Can advance independently from the shared libraries. |
| Global tool | `SimplicityTools.Cli` | `cli/vX.Y.Z` | Can advance independently from the shared libraries and analyzers. |

### What CI does with those tags

- Normal branch pushes run build, test, and package validation artifacts with a CI-only version based on `SimplicityToolsReleaseVersion` (`<release-version>-ci.<run-number>`).
- Manual workflow dispatch can build upload-ready NuGet artifacts for `libraries`, `analyzers`, or `cli`. If you leave the version input blank, the workflow uses `SimplicityToolsReleaseVersion` from `Directory.Build.props`.
- Manual workflow dispatch also supports the default `validation` group for CI-only package validation; that path always emits `<release-version>-ci.<run-number>` packages, ignores any stale version input left in the GitHub UI, and logs a notice instead of failing.
- Tag pushes remain the publish gate: CI reads the SemVer from the tag, validates the matching package group, and publishes the generated `.nupkg` and `.snupkg` files to NuGet.org.

### Local default versions

If you run `dotnet pack` locally without passing `-p:Version=...`, the package projects default to `$(SimplicityToolsReleaseVersion)-local` from `Directory.Build.props`. That keeps manual validation obvious and avoids pretending a local build is a real release.

## Release process

### 1. Validate locally

```bash
dotnet build SimplicityTools.sln --nologo --verbosity minimal
dotnet test SimplicityTools.sln --nologo --no-build --verbosity minimal
```

### 2. Package the release candidates you plan to ship

Shared libraries:

```bash
dotnet pack src/SimplicityTools.Metrics/SimplicityTools.Metrics.csproj -c Release --no-build -o artifacts/packages --nologo
dotnet pack src/SimplicityTools.Filters/SimplicityTools.Filters.csproj -c Release --no-build -o artifacts/packages --nologo
dotnet pack src/SimplicityTools.Tca/SimplicityTools.Tca.csproj -c Release --no-build -o artifacts/packages --nologo
```

Analyzer package:

```bash
dotnet pack src/SimplicityTools.Analyzers/SimplicityTools.Analyzers.csproj -c Release --no-build -o artifacts/packages --nologo
```

CLI tool:

```bash
dotnet pack src/SimplicityTools.Cli/SimplicityTools.Cli.csproj -c Release --no-build -o artifacts/packages --nologo
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

Replace `0.4.0-local` with the current `SimplicityToolsReleaseVersion-local` value when you validate a different release line.

Example analyzer reference:

```xml
<ItemGroup>
  <PackageReference Include="SimplicityTools.Analyzers" Version="0.4.0-local" PrivateAssets="all" />
</ItemGroup>
```

### 4. Publish via workflow dispatch

Use **Actions → NuGet release pipeline → Run workflow** to build, validate, and publish packages directly from the UI:

- Leave `release_group=validation` (the default) for a dry-run that stamps CI-only versions and **never publishes**. The optional `version` field is ignored in this mode.
- Set `release_group` to `libraries`, `analyzers`, or `cli` to build and **publish** the matching package group to NuGet.org. The workflow uses `SimplicityToolsReleaseVersion` from `Directory.Build.props` unless you supply a `version` override.

> **Note:** Tag pushes (`libraries/vX.Y.Z`, `analyzers/vX.Y.Z`, `cli/vX.Y.Z`) are still supported as an alternative release path. Both paths run the full test and validation gates before publishing.
