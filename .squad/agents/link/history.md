## Sprint 8: Astro Website 🚀 WAVES 1-2 COMPLETE, WAVE 3 ACTIVE

**Timespan:** 2026-05-01 to present  
**Status:** Waves 1–2 complete; Wave 3 ready  
**Impact:** Astro foundation locked with hub-first site IA; content migration now unblocked

### Sprint 8 Wave Summary

| Wave | Issues | Deliverables | Outcome |
|------|--------|--------------|---------|
| 1 | #50 | Astro bootstrap, GitHub Pages config, `.nojekyll`, starter structure, local README | Foundation complete; Wave 2 unlocked ✅ |
| 2 | #51, #52 | Base layout with nav/footer, breadcrumbs, seven top-level pages, hub templates | Site shell complete; Wave 3 unlocked ✅ |

### Key Decisions Locked

1. **Hub-first migration:** Keep site centered on shared layout + landing/docs/reference templates until Wave 3 migrates deeper content.
2. **Repository Pages path:** Astro config mirrors `/SimplicityTools/` base path in local dev/build/preview.
3. **Content reuse:** Homepage, getting-started, features, pricing, docs/reference hubs reuse existing README/docs language for first-run coherence.

### Astro Website Outcomes

- ✅ `docs-site/` bootstrapped with starter layouts, pages, components, and assets structure
- ✅ GitHub Pages-ready Astro config and npm scripts verified
- ✅ Responsive base layout with navigation, breadcrumbs, footer
- ✅ Seven top-level pages (home, getting-started, features, pricing, docs, reference, samples)
- ✅ Three reusable page templates (landing, docs hub, reference hub)
- ✅ Build passed and validated
- ✅ Wave 3 issues (#55, #58, #59) ready to start

---

## Current Status

**Milestone 8 (Astro Website):** 🚀 WAVES 1-2 COMPLETE, WAVE 3 ACTIVE  
**Latest:** Waves 1–2 (`#50`, `#51`, `#52`) complete on `sprint/8-astro-website`  
**Next:** Wave 3 — `#55` Analyzer docs, `#58` Homepage finalization, `#59` CLI docs

---

## Historical Context

Earlier work archived in history-archive.md:
- M1–M3 scaffold and core delivery  
- M4 NuGet packaging foundation  
- M5 release workflow setup  
- Ongoing DevRel strategy and packaging UX planning

---

## Sprint 8 (Ongoing): Copilot Instructions Analysis
**Timestamp:** 2026-05-28T07:40:02.687+02:00  
**Status:** Analysis complete; recommendations documented; ready for team review

### Copilot Quick Start Instructions Scope

Created `.github/copilot-instructions.md` content strategy to bridge Copilot sessions and the repository's workflow. This file answers "what do I need to know to work effectively here?" within the first 5 minutes of a session.

**Key sections recommended:**
1. Repository overview (SimplicityTools, complexity metrics, Roslyn analyzers)
2. Project structure (5 packages, version grouping strategy)
3. Team & AI context (Squad, decisions.md as compass)
4. Central version management (Directory.Build.props drives all versions)
5. Release & packaging (libraries/analyzers/cli tag strategy)
6. Development workflow (xunit, round-trip validation, branch naming)
7. Documentation principles (zero-config, teaching-first)
8. Code standards (C# preview, nullable, net10.0, no external deps)
9. CI/CD patterns (workflows, custom domain, GitHub Pages)
10. Common task decision gates (package changes, API changes, doc changes)

**Content architecture:**
- **400–600 words** — scannable, links to full docs
- Complements `.squad/templates/copilot-instructions.md` (Squad-specific agent guidance)
- Leverages `.squad/decisions.md` as shared wisdom source
- Embeds zero-config principle (core DevRel value) at code-standard level

**What NOT to include:**
- Step-by-step walkthroughs (docs/)
- Architecture diagrams (decisions.md or docs/)
- Personal preferences (.squad/ agent charters)
- Credentials or secrets
- Detailed implementation patterns (code/skills/)

**Handoff readiness:**
- Structure template prepared
- All content sections identified
- Cross-references mapped (decisions.md, CONTRIBUTING.md, .squad/)
- Ready for implementation by DevRel or team lead

---

## Learnings

### 2026-05-01T13:51:44.498-04:00

- The Astro production origin now lives at `https://simplicitytools.dev` in both `docs-site/astro.config.mjs` and `docs-site/src/data/site.ts`, so SEO metadata, robots.txt, and sitemap generation stay on the apex domain.
- Treat the repository-root `CNAME` as the custom-domain source of truth and sync it into `docs-site/public/CNAME` from `.github/workflows/deploy-site.yml` before validation/deploy to prevent Pages drift.
- Domain-change touchpoints for this site are `README.md`, `docs-site/README.md`, `docs-site/scripts/check-links.mjs`, and `docs-site/public/CNAME`; update all four when GitHub Pages moves again.

### 2026-05-01T05:50:05.727-04:00

- Sprint 8 Wave 1 bootstraps the Astro website in `docs-site/` so later waves can focus on navigation, landing pages, and deployment polish instead of setup.
- The GitHub Pages bootstrap uses `docs-site/astro.config.mjs` with `site: https://cwoodruff.github.io` and `base: /SimplicityTools/` so local dev, build, and preview mirror the repository Pages path.
- `.nojekyll` should live at `docs-site/public/.nojekyll`; Astro copies it to `dist/` automatically and keeps underscore-prefixed assets safe on Pages.
- First-run guidance belongs in `docs-site/README.md`, with the exact local URL (`/SimplicityTools/`) called out so the next contributor knows what to open immediately.

### 2026-05-01T06:12:43.398-04:00

- Sprint 8 Wave 2 turned the Astro bootstrap into a usable docs shell with a shared base layout, responsive navigation, breadcrumbs, footer, and three reusable page-template patterns (landing, docs hub, reference hub).
- The homepage, getting-started, features, pricing, docs hub, reference hub, and samples hub now reuse existing README/docs language so first-run questions are answered in-site while deeper markdown content waits for Wave 3 migration.
- Wave 3 is now cleanly unblocked because the site IA, top-level pages, and GitHub Pages-aware internal routing are stable enough for content migration work instead of more structural rework.


### 2026-05-01T06:37:49.140-04:00

- Auditing Wave 2 confirmed issues #51 and #52 are already satisfied by the shared Astro shell in `docs-site/src/layouts/BaseLayout.astro`, `docs-site/src/components/`, and the seven top-level pages under `docs-site/src/pages/`.
- `docs-site/src/data/site.ts` is the routing/source-of-truth layer for internal and external links, while `docs-site/src/styles/site.css` carries the shared dark theme, spacing tokens, and responsive breakpoints that keep the landing pages visually cohesive.
- The current validation proof for the site shell is `cd docs-site && npm run build`, which successfully generated all seven static routes for the homepage, docs hub, reference hub, samples hub, getting started, features, and pricing pages.

---

## Sprint 8: Astro Website (Milestone 8) — Wave 2 Complete
**Timestamp:** 2026-05-01T10:37:49.140Z  
**Agent Sync Note (Scribe):** Orchestration completed for Wave 2 handoff. Tank has established standardized 3-phase site validation checklist (Build, Structure, Responsive validation) to ensure consistent quality on future Wave 3 additions. Pattern documented in decisions.md. Issues #51 and #52 confirmed satisfied and ready for closure.

## Sprint 8: Astro Website (Milestone 8) — Wave 3 Complete
**Timestamp:** 2026-05-01T07:09:22.214Z

- Wave 3 content delivery complete: Analyzer docs (SF0001–SF0007), CLI/filter/config/library guides, integration pages, homepage finalization all merged into Astro site.
- Information architecture established: deep reference material organized into `/analyzers/`, `/docs/commands/`, `/docs/filters/`, `/docs/configuration/`, `/docs/library-usage/`, `/integration/` task-shaped sections.
- Docs-site build passing. GitHub issues #55, #58, #59 closed with summary comments.
- Cross-agent context propagated to team histories. Scribe decision decision merged to `.squad/decisions.md`.

## Sprint 8: Astro Website (Milestone 8) — Custom Domain Configuration
**Timestamp:** 2026-05-01T17:51:44Z  
**Session:** docs-site-domain

- Updated `docs-site/astro.config.mjs` production URL to `https://simplicitytools.dev`
- Synced repo-root `CNAME` file into `.github/workflows/deploy-site.yml` as single source of truth
- Validated docs-site build passes with new domain configuration
- Cross-agent sync with Tank: Established validation gates for domain consistency

**Decision merged:** "Docs-site custom domain source of truth and validation gate" — Repository-root `CNAME` is the canonical custom-domain artifact; workflow syncs to `docs-site/public/CNAME` before validation/deploy to prevent Pages drift.

**Key artifact:** `docs-site/` Astro config, `.github/workflows/deploy-site.yml`, and repo-root `CNAME` now unified on `simplicitytools.dev`.

## Sprint 8: Astro Website (Milestone 8) — Version Sync & Footer Display
**Timestamp:** 2026-05-02T06:08:59.230-04:00  
**Session:** version-footer-integration

- **Central version source established:** `Directory.Build.props` is the single source of truth (currently `0.4.0-local`)
- **Automated version extraction:** Created `docs-site/scripts/extract-version.mjs` that runs at prebuild time to populate `src/data/version.ts`
- **Footer integration complete:** `SiteFooter.astro` now imports and displays the version with styled typography
- **Developer experience improved:** Build process is transparent — `npm run build` automatically syncs version, no manual steps needed
- **Documentation updated:** `docs-site/README.md` explains the auto-generated version pattern so contributors understand the workflow
- **Validation passed:** Full site build, all 32 pages generated, links verified, footer version displays correctly in HTML output

**Decision merged:** "Central Version Source & Website Footer Display" — Single version source eliminates drift and makes first-run experience clearer for users checking what version they're on.

---

## 2026-05-02T10:08:59Z — Website Footer Version Display Complete

**Squad Orchestration Input:** Link background task updated docs-site footer to show shared SimplicityTools version.

**Implementation Delivered:**
- Created `docs-site/scripts/extract-version.mjs` to parse `Directory.Build.props` XML and extract canonical version
- Added `prebuild` script to `docs-site/package.json` for automatic version extraction before every build
- Generated `docs-site/src/data/version.ts` containing the current version
- Updated `docs-site/src/components/SiteFooter.astro` to import and display `toolVersion`
- Added `.footer-version` styling to `docs-site/src/styles/site.css` (muted color, top border)
- Documented version sync workflow in `docs-site/README.md`

**Validation Passed:**
- Full build: `npm run build:validate` ✅
- Footer displays: `<p class="footer-version">Version 0.4.0-local</p>` ✅
- All 32 pages built successfully ✅
- Link checker passed ✅

**Morpheus Approved:** Central release version contract ✅  
**Trinity Implemented:** Workflow feeds version to build system ✅  
**Tank Validated:** Footer rendering confirmed ✅  

**Release Handoff:** Operators can now trust website always displays correct version. No manual version sync needed.

**Decision Propagated to:** `.squad/decisions.md`  
**Orchestration Logs:** `.squad/orchestration-log/2026-05-02T10-08-59Z-link.md`

---

## 2026-05-28T07:40:02.687+02:00 — Copilot Instructions Refresh Complete

**Task:** Update `.github/copilot-instructions.md` with accurate, actionable guidance for future Copilot sessions.

**Changes Made:**
- **CLI test filtering (lines 79–88):** Documented the performance-test gating strategy with two separate test commands: exclude performance tests for functional CI, run performance gate independently for P95 validation.
- **CI/CD workflow detail (lines 123–128):** Added explicit performance test strategy: excluded from main CLI run due to resource intensity, validates Sample.OverEngineered P95 threshold, isolates regression detection.
- **Docs-site requirements (lines 204–210):** Added Node.js >= 20.0.0 requirement, `prebuild` script detail, and clarified `build:validate` includes link checking.
- **Troubleshooting expansion (lines 324–331):** New subsection specifically for CLI performance test failures, with local profiling guidance and root-cause hints (O(n²) logic, Roslyn initialization, fixture compilation).

**Reasoning:**
- The file was already comprehensive and correct. Improvements were surgical, not wholesale replacement.
- Performance test gating is a repo-specific detail Copilot sessions need on first run, not buried in CI logs.
- Node version requirement for docs-site is operational knowledge critical for dev environment setup.
- Troubleshooting section bridges "test failed" to "how to profile and fix" — teaching-first DevRel principle.

**File Status:** 342 lines → 350 lines (8-line net addition, pure value).

---

## Learnings

### Copilot Instructions Strategy

- Copilot instructions are product surface, not meta-documentation. Every line must answer "what do I do next?" for someone unfamiliar with the repo.
- Performance test gating is architectural knowledge, not implementation detail. It belongs in the Build section, not buried in workflow YAML comments.
- CLI test filtering (`--filter` patterns) should include realistic examples (both exclude and single-gate) so Copilot sessions can adapt patterns to their needs.
- Docs-site Node version requirement is "systems thinking" knowledge: it prevents dev environment setup failures that lead to 30-minute troubleshooting sessions.

### Repository-Specific Conventions to Remember

- **Version source:** `Directory.Build.props` is the single source of truth (`SimplicityToolsReleaseVersion`). All six locations derive from it: local package defaults, CI validation versions, release versions, and docs-site footer.
- **Test gating:** Main CLI test suite excludes `AnalyzeCommandPerformanceTests`; performance gate runs separately as `AnalyzeCommand_OverEngineeredSample_CompletesWithinExpectedThresholdAtP95`. This pattern isolates regression detection from functional test feedback.
- **Release groups:** Three independent package groups (libraries, analyzers, cli) with synchronized library versions but independent cadences for analyzers and CLI.
- **Docs-site automation:** Version extraction runs at prebuild time; footer displays automatically; no manual sync needed.

### Future Copilot Session Guidance

When future Copilot sessions ask "why is this test excluded?" or "how do I run just the performance test?" — point them to `.github/copilot-instructions.md` lines 79–88 and 324–331. The file should answer 80% of "how do I..." questions for this repo without requiring deep archaeology.

---


## 2026-05-28T05:40:02.687Z — Copilot Instructions Refresh Complete

**Task:** Review existing `.github/copilot-instructions.md` and apply surgical improvements based on verified findings.

**Improvements Implemented:**
1. CLI Test Filtering (lines 79–88): Two separate commands for functional tests (excluding performance) and performance gate separately
2. CI/CD Workflow Detail (lines 123–128): Explicit performance test strategy showing main tests exclude performance gate
3. Docs-Site Node Requirement (lines 204–210): Explicit Node.js >= 20.0.0 requirement, local dev command, build:validate clarification
4. Troubleshooting Performance Gate (lines 324–331): New subsection with local profiling command and root-cause hints

**Verification:**
- File size: 342 → 350 lines (8-line net addition, pure signal)
- All changes answer specific Copilot session questions
- Signal-to-noise ratio maximized

**Status:** ✅ Complete. `.github/copilot-instructions.md` refreshed and ready for team merge.
