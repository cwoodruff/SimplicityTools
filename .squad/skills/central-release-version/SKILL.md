---
name: "central-release-version"
description: "Drive package defaults, release packaging, and website version display from one MSBuild property"
domain: "release-engineering"
confidence: "high"
source: "trinity-earned"
---

# Central Release Version

## Pattern

Define one canonical release property in `Directory.Build.props` (for example `SimplicityToolsReleaseVersion`) and derive all secondary version shapes from it:

1. Local/default NuGet and tool packages use `<release-version>-local`.
2. CI validation artifacts use `<release-version>-ci.<run-number>`.
3. Workflow-dispatch packaging branches on `release_group` before applying version logic: validation runs ignore any optional version input and still emit the CI-only version, while `libraries`/`analyzers`/`cli` runs use either the explicit SemVer override or the canonical release version.
4. Website version UI reads the same property during build and emits generated frontend data.

## When to use it

- Monorepos that ship multiple .NET packages plus a website or docs surface.
- Repos where release preparation currently requires editing the same version string in multiple places.
- Pipelines that distinguish between local, CI validation, and real published versions.
- GitHub Actions workflows whose dispatch UI can retain stale input values between runs.

## Why it matters

This keeps version changes deterministic and reviewable. One edit updates package defaults, workflow-generated artifacts, and user-facing version display without drift between MSBuild, GitHub Actions, and frontend code.
Branching on release intent before version handling also prevents validation runs from failing because the GitHub Actions form kept an old release version in the optional input field.
