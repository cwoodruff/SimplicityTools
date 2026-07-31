---
updated_at: 2026-07-31T08:37:38.619-04:00
focus_area: Post-0.5.0 — first public release shipped; next focus TBD
active_issues: []
---

# Current State

**Release version:** `0.5.0` (confirmed in `Directory.Build.props`, `CHANGELOG.md`, commit `3545d45`)

**0.5.0 shipped on 2026-07-08** as the first public release. All five packages published together:
`SimplicityTools.Metrics`, `SimplicityTools.Filters`, `SimplicityTools.Tca` (libraries, `net8.0`/`net10.0`),
`SimplicityTools.Analyzers` + `SimplicityTools.Analyzers.CodeFixes` (Roslyn, min Roslyn 4.4/4.6),
`SimplicityTools.Cli` (`net10.0`).

## What Shipped Since the May 28 Review

The three NO-GO blockers identified in the 2026-05-28 codebase review were all resolved before 0.5.0:

1. **CS8604 null-safety** — Fixed in `M1: Stabilize main (0.4.0 blockers)` (commit `261d7ac`,
   2026-07-06). Explicit null guards added; `netstandard2.0` target is the cause of the narrowing
   gap and the workaround is documented in that commit.

2. **ReportGenerator complexity / SF0003** — Fixed in `fix: keep CommandLineEntryPoint.RunAsync
   within SF0003/SF0004 limits` (commit `84c549a`). Command routing moved to a dictionary to keep
   the entry point within its own complexity budget.

3. **Analyzer package validation gate** — Added to CI via `M1: Stabilize main` which wired up
   `ci.yml`; consumer validation also confirmed in commit `b25f100` (`ci: analyzer consumer
   validation opts into SF0001 like the package tests`).

## Additional Work That Landed Post-Review

Organized across three internal milestones (M1 → M3):

- **M1:** Null-safety, baseline file count fix (23→24), unknown-command exit-1, CI `ci.yml` added
- **M2:** Measurement trust (single Roslyn workspace, deduped multi-targeting, real unused-dep
  detection, null `EstimatedOnboardingTime`), watch robustness, versioned persistence, live config
- **M3:** Single-pass reference counting (O(n) not O(n²)), TCA overhaul (excess-over-target model,
  all constants in `TcaInputs`), analyzer split into `Analyzers` + `Analyzers.CodeFixes` packages,
  safe SF0001 code fix, complexity counting unified for modern C#, CLI JSON output (`--format json`),
  real argument parsing, actionable errors, CPM, `net8.0` multi-targeting, packaging polish
- **Post-M3 / 0.5.0:** Perf gate now prints P95 distribution on success; actions pinned/bumped

## Repo Health

- **Build:** Clean — zero warnings or errors (`dotnet build SimplicityTools.sln`)
- **Open TODOs in src/:** 1 (a generated comment in `SingleImplementationInterfaceCodeFixProvider.cs`;
  intentional — it's emitted as user-visible reviewer guidance, not a code debt marker)
- **Stale local branches:** `docs/nuget-distribution-plan`, `fix-104-perf-gate`, `release-0.5.0`
  — all merged or superseded; can be pruned
- **Remote branches:** M1–M3 sprint branches exist on origin; all merged to `main`

## Next Focus

**No active sprint or open issues are tracked here.** The repo is in a clean post-release state.

Suggested next areas (evidence-based, not speculative):
- `EstimatedOnboardingTime` is explicitly `null` in 0.5.0 — the metric is unimplemented and
  documented as such. This is the most prominent known gap in the feature surface.
- Stale branches on origin (m1-*/m2-*/m3-* series) can be pruned for hygiene.
- Docs-site (`simplicitytools.dev`) reflects 0.4.0 content; a pass to align with 0.5.0 feature set
  (JSON output, analyzer split, TCA overhaul, trend analysis) would be worthwhile.

**Coordinator should ask the user** before spinning up new sprint work — no explicit next milestone
is recorded in any tracked file.
