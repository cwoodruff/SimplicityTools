---
name: "workflow-dispatch-input-matrix"
description: "Validate every meaningful workflow_dispatch input combination before approving release-pipeline changes"
domain: "release-engineering"
confidence: "high"
source: "tank-earned"
---

# Workflow Dispatch Input Matrix

## Pattern

When a GitHub Actions workflow branches on `workflow_dispatch` inputs, validate the full input matrix instead of only the expected happy path.

Minimum matrix:

1. Default group + blank optional inputs
2. Default group + populated optional inputs
3. Default group + populated **invalid/stale** optional inputs
4. Non-default groups + blank optional inputs
5. Non-default groups + populated optional inputs
6. Tag-trigger path if the workflow also publishes from tags

## Why it matters

Release workflows often hide bugs in guard clauses, especially when a single optional input changes the entire decision tree. A matrix check catches “works for release, breaks for validation” regressions before the failure shows up in GitHub.

## Evidence to collect

- Actual run logs for the failing tuple
- Local or scripted replay of the decision tree for each tuple
- One end-to-end build/test pass after the logic change

## Approval rule

Do not approve a release-workflow fix unless the reported failing tuple is shown to pass or is intentionally blocked with clearer, documented behavior.
