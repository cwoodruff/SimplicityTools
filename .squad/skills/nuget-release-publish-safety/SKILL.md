---
name: "nuget-release-publish-safety"
description: "Validate NuGet release artifacts before publish and ensure both package and symbol payloads are release-correct"
domain: "release-engineering"
confidence: "high"
source: "morpheus-release-update"
---

# NuGet Release Publish Safety

## Pattern

Before a GitHub Actions job publishes NuGet packages, validate the downloaded artifacts against the intended release contract:

1. Resolve the expected release group and SemVer.
2. Inspect each `.nupkg` nuspec and confirm the package version exactly matches that SemVer.
3. Reject CI/local placeholder versions such as `*.ci.*` and `*-local`.
4. Confirm the package ID set exactly matches the release group (`libraries`, `analyzers`, or `cli`).
5. Confirm each publishable package has a matching `.snupkg`.
6. Push both the `.nupkg` and `.snupkg` payloads to NuGet.org after validation.

## When to use it

- GitHub Actions workflows that publish grouped NuGet packages from downloaded artifacts.
- Release pipelines that create validation artifacts on branch builds but publish only from tags.
- Reviews where a workflow claims release readiness and you need proof it cannot accidentally publish the wrong package set.

## Why it matters

This closes the gap between “pack succeeded” and “publish is safe.” It catches wrong-group artifacts, stale CI versions, missing symbol packages, and mismatched publish payloads before NuGet.org sees them.
