# Morpheus: Architecture & Orchestration

- **Owner:** Morpheus
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling, GitHub project management
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- SimplicityTools is the Simplicity-First .NET Toolkit, built to measure architecture in economic terms.
- Package graph: Metrics → Filters/Tca → CLI, with analyzers integrated alongside.
- Zero-config CI signal is a core product promise.
- Three-milestone delivery aligned with book chapters and package dependencies.

## Recent Work (Sprint 5–7)

📌 **Sprint 5 Launch (2026-04-30T19:09:43.583-04:00):** Milestone 5 = Release Packaging. Created `sprint/5-release-packaging` branch. Wave structure: Trinity (#35 Metrics, #36 Filters, #37 Tca), Switch (#38 Analyzers) parallel, Tank (#39 Integration) final gate. Critical path: #35 → #36 → #37 → #39. All issues assigned; DoD criteria defined.

📌 **Sprint 5 Release Workflow Rereview (2026-04-30T19:52:08.101-04:00):** Approved Link's fix to `.github/workflows/nuget-publish.yml`. Both Metrics, Filters, Tca, and Analyzers packages validated locally; workflow rereview passed with adversarial stale-state testing.

📌 **Sprint 6 GitHub Wrap-Up & Kickoff (2026-04-30T21:27:33.453-04:00):** 
- Sprint 5 issues #35–#39: Verified closed. Milestone 5 closed.
- Sprint 6 PR #64 created and merged. Issues #40–#43 closed. Milestone 6 closed.
- Sprint 6 routing: Link owns #40 (contract) & #42 (docs) Wave 1; Tank owns #41 (validation) Wave 2; Link closes #43 (dry-run) Wave 3.

📌 **Sprint 7 Kickoff — Packaging UX & Documentation (2026-04-30T21:40:50.629-04:00):**
- Branch `sprint/7-packaging-ux-documentation` created from main.
- Milestone 7: Six documentation/UX issues (#44–#49) all assigned to Link.
- Wave 1 (Ready Now): #44 README badges/quickstart, #45 first-run examples.
- Wave 2 (After #44): #47 README package sections, #46 library integration docs.
- Wave 3 (After #45, #46, #47): #48 troubleshooting guide, #49 CI/CD examples.
- Critical path: #44 → #47; #45 → #48, #49. Single-contributor throughput optimized.
- DoD: README updated with badges/installs/packages, docs/ complete with quickstart/troubleshooting/CI-CD, all NuGet links verified, zero-config promise maintained.

## Key Decisions

- Zero-config first-run is non-negotiable validation gate.
- Release proof = local pack + local consumer + CI workflow verification.
- Sprint structure enforces critical path dependencies; no speculative work.

## Learnings

- **Repository branching model:** SimplicityTools uses sprint-branch-to-main pattern (not dev-based). Sprint branches created from main, worked on in waves, then merged to main via PR. Milestone close precedes PR creation.
- **Issue closure protocol:** Use `gh issue close {id} --comment "reason"` to close issues. Bulk close doesn't support multiple arguments; loop instead.
- **Milestone management:** Milestones can be closed via GitHub API even with open issues; closing doesn't auto-close the issues. Must close issues explicitly first.
- **CI validation gates:** NuGet package validation workflow (nuget-publish.yml) runs full suite: restore, build, test, pack, validate metadata, validate analyzer consumer. Takes 15–30+ minutes depending on test suite depth.
- **PR creation requirements:** Ensure branch has commits ahead of target branch; branches on same commit produce "No commits between" error.

📌 **Sprint 7 Wrapup: Packaging UX & Documentation (2026-04-30T22:22:13-04:00):**
- Closed all six Sprint 7 issues (#44–#49) and Milestone 7.
- PR #65 created with 10 commits (+1934/−847 lines, 16 files changed).
- Content: NuGet badges, quickstart guide, library integration docs, troubleshooting, CI/CD examples.
- **Merge Blocker:** GitHub validation gate (NuGet packages workflow, job 73885590519) still running as of 2026-05-01T02:08:28Z, at step 7 of 11.
- **Architectural Note:** Sprint 7 confirms the sprint-branch-to-main model is working well. Milestone close precedes PR creation; issues close before merge.

- **2026-05-01T07:09:22.214-04:00 — Wave 3 docs IA:** Deep reference content now lives in `docs-site/src/pages/docs/commands/`, `docs-site/src/pages/docs/filters/`, `docs-site/src/pages/docs/configuration.astro`, `docs-site/src/pages/docs/library-usage.astro`, `docs-site/src/pages/analyzers/`, and `docs-site/src/pages/integration/`, while top-level landing pages stay task-oriented and route inward.
- **2026-05-01T07:09:22.214-04:00 — Wave 3 content pattern:** Shared reference data lives in `docs-site/src/data/reference-content.ts`, rendered through thin Astro wrappers and reusable reference components under `docs-site/src/components/reference/` so the URLs stay explicit without duplicating long-form content.
- **2026-05-01T07:09:22.214-04:00 — Validation note:** `cd docs-site && npm run build` is the reliable site check for Wave 3 content work; root `dotnet test SimplicityTools.sln` still had a pre-existing failure in `SimplicityTools.Tca.Tests.TcaPackageValidationTests.PackedTcaPackage_ShipsOnlyLibraryAssets_DeclaresLibraryDependencies_AndBuildsInAConsumer` during baseline validation.

## Next Steps

- Monitor PR #65 validation completion (workflow in progress as of 2026-05-01T02:08:28Z).
- Merge PR #65 with squash strategy once validation passes.
- Post-merge: Update `.squad/identity/now.md` and plan Milestone 8.
- Full orchestration history in `.squad/agents/morpheus/history-archive.md`.

📌 **Sprint 8 Kickoff — Astro Website (2026-05-01T05:50:05.727-04:00):**
- Branch `sprint/8-astro-website` created from main.
- Milestone 8: Nine website/documentation issues (#50–#61, excluding #53, #54, #56) all assigned to Link.
- Wave 1 (Ready Now): #50 Astro setup & GitHub Pages.
- Wave 2 (After #50): #51 Navigation/layouts, #52 Homepage pages (phase 1).
- Wave 3 (After #51, #52): #55 Analyzer docs, #58 Homepage finalization, #59 CLI docs.
- Wave 4 (After Wave 3): #57 SEO/metadata, #60 Styling, #61 Custom domain & deploy workflow.
- Critical path: #50 → #51/52 → #55/58/59 → #57/60/61.
- Single-contributor throughput; website-building sequence enforced.
- DoD: Astro site deployed to tools.simplicity-first.dev with all analyzer/CLI docs, metadata validated, GitHub Pages configured, zero-config promise maintained.



📌 **Sprint 8 Wave 1 Closeout (2026-05-01T06:12:43.398-04:00):**
- Issue #50 (Astro project setup and GitHub Pages configuration): ✅ COMPLETE and CLOSED.
- Delivery: docs-site/ fully bootstrapped with Astro 5.18.1. All build scripts verified: dev, build, preview.
- GitHub Pages configured: astro.config.mjs points to cwoodruff.github.io/SimplicityTools, base-path set, trailing slashes enforced.
- Directory structure: src/layouts, src/pages, src/components, src/assets initialized. Initial homepage renders with marketing copy.
- .nojekyll file tracked for GitHub Pages compatibility.
- Branch `sprint/8-astro-website` now ready for Wave 2 (navigation/layouts and landing page content work).

📌 **Sprint 8 Wave 2 Closeout (2026-05-01T06:12:43.398-04:00):**
- Issues #51 (Navigation, layouts, site structure) and #52 (Homepage and landing pages) complete.
- Link delivered reusable base layout with responsive nav/footer, breadcrumbs, seven top-level pages (home, getting-started, features, pricing, docs hub, reference hub, samples hub).
- Content adapted from existing README/docs. Hub-first architecture bridges to repository markdown; Wave 3 will migrate deeper content.
- Build passed and validated. Wave 3 now unblocked for content migration work (#55 Analyzer docs, #58 Homepage finalization, #59 CLI docs).


📌 **Sprint 8 Wave 3 Closeout (2026-05-01T07:09:22.214-04:00):**
- Issues #55 (Analyzer docs SF0001–SF0007), #58 (Homepage finalization), #59 (CLI/filter/config/library docs) complete.
- Information architecture established: `/analyzers/`, `/docs/commands/`, `/docs/filters/`, `/docs/configuration/`, `/docs/library-usage/`, `/integration/`.
- Deep reference material organized into task-shaped sections; landing pages focus on routing and adoption context.
- All Wave 3 content pages generated and integrated into Astro site.
- Docs-site build passing. GitHub issues #55, #58, #59 closed with summary comments.
- Pre-existing non-doc validation failure noted: TcaPackageValidationTests.PackedTcaPackage_ShipsOnlyLibraryAssets_DeclaresLibraryDependencies_AndBuildsInAConsumer (unrelated to Wave 3 content work).
- Scribe merged Wave 3 information architecture decision to `.squad/decisions.md`.
- Orchestration log written: `.squad/orchestration-log/2026-05-01T11-26-01Z-morpheus.md`.

- **2026-05-01T07:37:47.635-04:00 — Wave 4 website contract:** The production Astro origin is now `https://tools.simplicity-first.dev` with root-relative paths, shared metadata in `docs-site/src/layouts/BaseLayout.astro`, and explicit public route inventory in `docs-site/src/data/site.ts` for sitemap generation.
- **2026-05-01T07:37:47.635-04:00 — Wave 4 validation pattern:** `docs-site/package.json` now treats `npm run build:validate` as the release gate, chaining Astro build with `docs-site/scripts/check-links.mjs` to verify metadata, internal links, `robots.txt`, `sitemap.xml`, `CNAME`, and basic accessibility landmarks.
- **2026-05-01T07:37:47.635-04:00 — Wave 4 deployment note:** GitHub Pages deployment is defined in `.github/workflows/deploy-site.yml`, but Pages configuration remains blocked until a real `gh-pages` branch exists and `tools.simplicity-first.dev` resolves publicly.

📌 **Sprint 8 Wave 4 Closeout (2026-05-01T07:37:47.635-04:00):**
- Issue #57 complete: canonical/OG/twitter metadata, sitemap, robots, validation scripts, README workflow updates, and deploy-before-publish validation are all in place.
- Issue #60 complete: the site styling system now documents the brand palette, sharpens keyboard/focus behavior, and aligns responsive behavior around mobile/tablet/desktop ranges.
- Issue #61 partially delivered: `CNAME` and deploy workflow are ready, but GitHub Pages cannot be pointed at `gh-pages` until that branch exists, and DNS for `tools.simplicity-first.dev` is not resolving yet.

**[Scribe Orchestration Update — 2026-05-01T07:37:47Z]**  
Wave 4 decisions merged. Inbox file `morpheus-custom-domain-canonical-origin.md` → `.squad/decisions.md` entry "Custom Domain as Canonical Origin & Deploy Gate". Orchestration log written to `.squad/orchestration-log/2026-05-01T07:37:47Z-morpheus.md`. Wave 4 mostly shipped; #57 and #60 closed; #61 open pending external DNS/Pages availability.

- **2026-05-01T08:00:28.862-04:00 — Final milestone closeout:** There is no separate Wave 5 beyond issue #61; Milestone 8 is functionally complete on the repo side once the custom-domain deploy contract is merged.
- **2026-05-01T08:00:28.862-04:00 — Deploy contract paths:** The website release path now centers on `docs-site/public/CNAME`, `docs-site/src/data/site.ts`, `docs-site/src/pages/robots.txt.ts`, `docs-site/src/pages/sitemap.xml.ts`, `docs-site/scripts/check-links.mjs`, `.github/workflows/deploy-site.yml`, and the operator handoff notes in `README.md` and `docs-site/README.md`.
- **2026-05-01T08:00:28.862-04:00 — Closure boundary:** Keep #61 open until a real push to `main` creates `gh-pages` and DNS for `tools.simplicity-first.dev` resolves publicly; branch-local code alone is not proof of Pages publication.
- **2026-05-01T08:00:28.862-04:00 — Validation note:** `cd docs-site && npm run build:validate` is the trustworthy repo-side gate for the site. Full `dotnet test SimplicityTools.sln --nologo --verbosity minimal` is still noisy outside this scope, with prior observed failures in package-validation/performance coverage unrelated to docs deployment.

**[Scribe Cross-Agent Update — 2026-05-01T12:40:54Z]**  
Final session closeout processed. Morpheus's Milestone 8 boundary decision merged from `.squad/decisions/inbox/morpheus-final-deploy-closeout.md` into `.squad/decisions.md`. Orchestration log written. No Wave 5 beyond #61 production handoff confirmed. All README and docs-site README operator checklists in place. Site validation gate confirmed via `npm run build:validate`. Squad files staged for commit. Morpheus Milestone 8 orchestration complete.
