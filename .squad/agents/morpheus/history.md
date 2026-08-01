# Morpheus: Architecture & Orchestration

- **Owner:** Morpheus
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling, GitHub project management
- **Created:** 2026-04-29T06:47:51.656-04:00

## Learnings (2026-08-01 — CLI separate release group + version source of truth)

**Item 1 — CLI not publishing alongside libraries: expectation gap, not a bug.**
`release_group=libraries` is intentionally Metrics/Filters/Tca only. CLI has its own release cadence and must be dispatched separately with `release_group=cli` (or a `cli/vX.Y.Z` tag push). There is no workflow mechanism to auto-follow a libraries release with a CLI release. No code change required; CONTRIBUTING.md updated to document the two-dispatch pattern explicitly.

**Item 2 — Version mismatch (0.5.0 published, 0.8.0 expected): root cause is an uncommitted Directory.Build.props.**
The workflow's `resolve` job reads `SimplicityToolsReleaseVersion` from `Directory.Build.props` in the checked-out commit, then passes it as `-p:Version=<pack_version>` to every `dotnet pack` call. MSBuild command-line `-p:` properties have the highest precedence — they override anything in individual `.csproj` files unconditionally.

Commit `df90533` ("Parallelize CI test jobs and bump versions to 0.8.0") added `<Version>0.8.0</Version>` to each `.csproj` file but did NOT update `Directory.Build.props`. `Directory.Build.props` was edited locally to `0.8.0` but never committed (confirmed: `git status` shows `M Directory.Build.props`, and `git show HEAD:Directory.Build.props` shows `0.5.0`). CI always checks out the COMMITTED state, so every run since `df90533` has read `0.5.0` as the canonical version and overridden the per-project values silently.

**Correct fix (for Chris to apply):**
1. Commit `Directory.Build.props` — the local 0.8.0 edit is already correct, it just needs `git add Directory.Build.props && git commit`.
2. Remove the hardcoded `<Version>0.8.0</Version>` entries from all individual `.csproj` files — they violate the repo convention ("Never hardcode package versions in individual project files — always derive from Directory.Build.props") and will become stale on every future version bump.

CONTRIBUTING.md updated with a "Version bump checklist" that documents the correct workflow explicitly.

## Core Context

- SimplicityTools is the Simplicity-First .NET Toolkit, built to measure architecture in economic terms.

## Learnings (2026-08-01 — nuget-publish.yml publish gate fix)

**Root cause:** The `publish` job in `nuget-publish.yml` had `if: startsWith(github.ref, 'refs/tags/')` (line 532). This gated publishing exclusively to tag-push triggers. Every run in the repo history has been either a branch push or a `workflow_dispatch` — the repo has zero release tags. So the publish job has **never run**. The design previously required a manual tag push after `workflow_dispatch`, but that second step was never executed.

**Fix applied:** Widened the `publish` job `if:` condition to also fire when the trigger is `workflow_dispatch` and `release_group != 'validation'`:
```yaml
if: >-
  startsWith(github.ref, 'refs/tags/') ||
  (github.event_name == 'workflow_dispatch' &&
   needs.validate.outputs.release_group != 'validation')
```
Branch pushes still skip publish entirely — validation-only by design. `workflow_dispatch` with `release_group=validation` (the default) still never publishes. A `workflow_dispatch` with `release_group=libraries/analyzers/cli` now publishes after passing all test and validation gates.

**CONTRIBUTING.md updated:** Steps 4 and 5 rewritten — workflow_dispatch with a real release_group now both validates AND publishes in a single run. Tag pushes remain a supported alternative path but are no longer the only path.

**Key insight:** There was no secondary guard to prevent validation-mode packages (stamped `-ci.<run>`) from being published — the `publish/validate-publishable-artifacts` step already rejects them with `if ".ci." in package_version`. So the widened condition is safe.
- Package graph: Metrics → Filters/Tca → CLI, with analyzers integrated alongside.
- Zero-config CI signal is a core product promise.
- Three-milestone delivery aligned with book chapters and package dependencies.

## Key Decisions

- Zero-config first-run is non-negotiable validation gate.
- Release proof = local pack + local consumer + CI workflow verification.
- Sprint structure enforces critical path dependencies; no speculative work.

## Recent Learnings (2026-05-28)

- **Repository branching model:** SimplicityTools uses sprint-branch-to-main pattern (not dev-based).
- **Release strategy:** Three release groups (libraries/v, analyzers/v, cli/v); Metrics/Filters/Tca version together.
- **Central version contract:** `Directory.Build.props` owns `SimplicityToolsReleaseVersion`; all packages derive versions from this.
- **Zero-config promise:** Non-negotiable validation gate; all features must degrade gracefully without config.
- **Validation dispatch hardening:** Workflow dispatch should key on `release_group` first, ignore stale `version` input.
- **Docs-site sync:** Version extraction script generates version.ts at build time from MSBuild property.
- **macOS apphost constraint:** Sample projects must disable apphost generation or use non-`.App` naming.

## Learnings (2026-08-01 — Analyzers snupkg exemption)

**Analyzers package intentionally embeds PDB; snupkg validation exempted.**

`SimplicityTools.Analyzers.csproj` sets `<DebugType>embedded</DebugType>` and `<IncludeSymbols>false</IncludeSymbols>`, so `dotnet pack` produces only a `.nupkg` — no `.snupkg` is generated. This is deliberate: Roslyn analyzer assemblies ship with embedded symbols rather than a separate symbol package.

The blanket "every `.nupkg` must have a matching `.snupkg`" check in `nuget-publish.yml` failed for this package. Fixed both occurrences:
- **Line ~358** — `validate` job, "Validate packed metadata" step
- **Line ~629** — `publish` job, artifact-set validation step

Each check now consults a `PACKAGES_WITHOUT_SNUPKG` set (currently `{"SimplicityTools.Analyzers"}`) before requiring a `.snupkg`. A comment explains the intent. Metrics, Filters, Tca, and Cli still fail hard if their `.snupkg` is absent. To add a future embedded-symbol package, add its ID to `PACKAGES_WITHOUT_SNUPKG` in both locations.

## Learnings (2026-08-01)

**CI pipeline parallelization — `nuget-publish.yml` restructured.**

The single sequential `validate` job was split into five jobs:
- `resolve` — lightweight; resolves release shape and outputs `release_group` + `pack_version`. No SDK needed.
- `test-unit` — Metrics/Filters/Tca/Analyzers unit tests (parallel, no deps on resolve).
- `test-cli-functional` — CLI integration tests excluding performance gate (parallel).
- `test-cli-perf` — CLI P95 performance gate only (parallel, deliberately isolated per prior team decision in `.squad/decisions.md`).
- `validate` — packs artifacts and validates metadata; runs only after all three test jobs pass; proxies `resolve` outputs to `publish`.
- `publish` (unchanged) — tag-push-only gating, uses `validate` outputs.

NuGet package caching added to all runner jobs via `actions/cache@0057852...` (v4), keyed on `Directory.Packages.props` hash. Each parallel job does its own restore+build; with a warm NuGet cache, redundant restore cost is negligible and this avoids artifact upload/download complexity.

**Expected wall-clock impact:** Before ~12–15 min (sequential: unit tests + CLI functional + perf gate all in one job). After ~5–7 min (three test jobs run in parallel; `validate` follows immediately when all pass). NuGet cache hit reduces restore time from ~2–3 min to ~10–20 s per job on repeat runs.

**Trade-off accepted:** Each test job re-runs `dotnet restore` + `dotnet build`. This is simpler than staging build artifacts and fast enough with cached packages. Builds are identical in content (same source, same packages).

**Preservation guarantees:** All triggers, concurrency serialization, release shape logic, pack-per-group logic, metadata validation, analyzer consumer validation, and publish gating are unchanged. The perf gate remains its own isolated invocation (now a separate job rather than a separate step — isolation level is strictly equal or stronger).



**Current release:** `0.5.0` — first public NuGet release, shipped 2026-07-08. All five packages published together. `Directory.Build.props` and `CHANGELOG.md` are the version source of truth.

**All three May 28 NO-GO blockers were resolved before 0.5.0:**
1. CS8604 null-safety — fixed `261d7ac` (M1 stabilize, 2026-07-06); explicit null guards, `netstandard2.0` narrowing gap documented.
2. ReportGenerator/SF0003 complexity — fixed `84c549a`; dictionary dispatch replaces if/else chain.
3. Analyzer validation gate — added `ci.yml` in `261d7ac`; consumer validation hardened in `b25f100`.

**Three internal milestones (M1→M3) landed between May 28 review and 0.5.0:**
- M1: stability/CI; M2: measurement trust, watch robustness, versioned persistence; M3: O(n) reference counting, TCA overhaul, analyzer package split, CLI JSON output, complexity unification.

**Key file paths confirmed:**
- `Directory.Build.props` — `SimplicityToolsReleaseVersion` = `0.5.0`
- `CHANGELOG.md` — full 0.5.0 change narrative
- `.github/workflows/ci.yml` — added in M1; contains analyzer consumer validation gate
- `src/SimplicityTools.Analyzers.CodeFixes/` — new package from M3 analyzer split
- `artifacts/analyzer-package-validation-tests/` and `artifacts/analyzer-consumer-validation/` — CI contract suites

**Post-0.5.0 state:** One intentional TODO in `SingleImplementationInterfaceCodeFixProvider.cs` (emitted as user-facing reviewer guidance). `EstimatedOnboardingTime` is `null` — documented unimplemented gap. Stale sprint branches (m1-*/m2-*/m3-*) on origin are safe to prune.

**Decision inbox:** `.squad/decisions/inbox/morpheus-release-readiness-update.md` — documents blocker resolution and retroactive GO verdict.

---

## Archived History

**Pre-2026-05-28 work:** See `.squad/agents/morpheus/history-archive.md`
- Milestone 1–3 scaffold and core delivery
- Milestone 4–5 NuGet packaging and release orchestration
- Milestone 6–8 Astro website and GitHub Pages deployment
- NuGet workflow dispatch routing fix (2026-05-02)

---

## 2026-05-28T08:10:33Z — Codebase Review & Release Verdict Finalized

**Status:** Release readiness audit complete; verdict revised to **NO-GO until Phase 1 fixed**.

**Five-agent parallel audit identified:**
- ✅ Structurally solid architecture, feature-complete CLI, comprehensive docs, website deployed
- ⚠️ Three critical-path blockers: null-safety (CS8604), complexity refactor (ReportGenerator), analyzer validation gate
- ⚠️ Trust gaps: stale test baseline (23 vs. 24 files), broken help links (simplicity-first.dev → dead), analyzer package layout wrong (blocks Roslyn discovery), onboarding-time stubbed

**Phase 1 (48–72h parallel):**
- Track A: Fix null-safety warnings (Tank/Trinity, 2–4h)
- Track B: Refactor ReportGenerator complexity (Trinity, 4–6h)
- Track C: Add analyzer validation gate to CI (Trinity, 1–2h)
- Track D: Fix dead URLs, false claims in docs (Link)

**Phase 2 (1 week):** Audit analyzer logic, wire TCA/filter settings, docs improvements
**Phase 3 (post-release):** Performance benchmarking, extended tests

**Decision artifacts:** `.squad/decisions.md` merged 4 new entries; 5 orchestration logs written; `docs/CODEBASE_REVIEW_2026-05-28.md` consolidated all findings.


