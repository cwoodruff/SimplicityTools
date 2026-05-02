---
name: "workflow-dispatch-validation-routing"
description: "Keep mixed validation/release GitHub workflow dispatch inputs from misrouting validation runs"
domain: "release-engineering"
confidence: "high"
source: "morpheus-earned"
---

# Workflow Dispatch Validation Routing

## Pattern

In a GitHub Actions workflow that serves both validation-only packaging and upload-ready release packaging:

1. Route by the selected group first, not by whether the optional `version` field is non-empty.
2. Treat `validation` as a fixed CI-only path that always emits `<release-version>-ci.<run-number>` artifacts.
3. Normalize stale `version` input to empty for `validation` runs, then emit a notice instead of failing.
4. Let release groups (`libraries`, `analyzers`, `cli`) consume the explicit `version` override, or fall back to the canonical version source when blank.

## When to use it

- One workflow handles both branch-style validation and manual release-artifact builds.
- GitHub’s dispatch form exposes optional inputs that operators may leave populated between runs.
- A repo has a single canonical release version but still needs CI-only package suffixes for validation.

## Why it matters

GitHub Actions forms preserve prior input values, so gating first on “version provided” can accidentally turn validation into a release-mode failure. Group-first routing plus normalization protects zero-config validation, keeps release intent explicit, and removes a class of operator error without adding another workflow.
