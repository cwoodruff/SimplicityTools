# Link — DevRel, Docs & User Experience

**Project:** SimplicityTools  
**Role:** DevRel lead, documentation strategy, first-run UX  
**Created:** 2026-04-29

## Core Responsibilities

- Documentation strategy and content architecture
- First-run user experience (quickstart, tutorials, troubleshooting)
- Release notes and changelog  
- Community-facing communication
- Developer experience (DX) improvements

## Key Outcomes (Recent)

- ✅ NuGet quick-install badges in README with copy-paste commands
- ✅ `docs/quickstart.md` with 5 essential CLI commands and real output
- ✅ Library integration guides for Metrics, Filters, Tca, Analyzers
- ✅ Troubleshooting guide (symptom-first diagnostic flow)
- ✅ CI/CD integration examples (GitHub Actions, Azure Pipelines, GitLab CI)
- ✅ `.github/copilot-instructions.md` with architecture and conventions
- ✅ Codebase review consolidated and documented

## Archived History

**Pre-2026-05-28 work:** See `.squad/agents/link/history-archive.md`
- Sprint 7 packaging UX (Waves 1–3)
- NuGet badge integration
- Quickstart and integration guides
- CI/CD and troubleshooting documentation
- Copilot instructions initial draft

---

## 2026-05-28T05:40:02.687Z — Copilot Instructions Refresh Complete

**Task:** Review existing `.github/copilot-instructions.md` and apply surgical improvements based on verified findings.

**Improvements Implemented:**
1. CLI Test Filtering: Two separate commands for functional tests (excluding performance) and performance gate separately
2. CI/CD Workflow Detail: Explicit performance test strategy
3. Docs-Site Node Requirement: Explicit Node.js >= 20.0.0 requirement
4. Troubleshooting Performance Gate: New subsection with local profiling command and root-cause hints

**Verification:**
- ✓ File exists and is discoverable at `.github/copilot-instructions.md`
- ✓ All content is implementation guidance (appropriate for public repo)
- ✓ Build commands tested locally against current state
- ✓ New contributor reads file → knows to check .squad/decisions.md

**Status:** ✅ Complete. `.github/copilot-instructions.md` refreshed and ready for team merge.

---

## 2026-05-28T06:10:33Z — Codebase Review Consolidation: Release Verdict & Trust Assessment

**Task:** Consolidate all five-agent parallel audit findings into honest release readiness assessment.

**Consolidation scope:** Morpheus (architecture), Trinity (libraries), Tank (QA), Switch (analyzers), Link (docs)

**Key discovery:** Initial draft was too optimistic. Revised to reflect actual blockers.

**Release verdict revision:**
- **Was:** "GO for 0.4.0 with conditions; 1 sprint to release"
- **Now:** "NO-GO until Phase 1 fixed; dead URLs + broken analyzer package layout + null-safety + baseline drift = release integrity risk"

**Trust gaps identified by Link:**
1. **Dead URLs:** Analyzer help links point to simplicity-first.dev (404)
2. **False claims:** Docs promise snapshot command, TCA in reports, history workflows that don't exist
3. **Library API mismatch:** Docs use ProjectCount, code has TotalProjects (compile-break)
4. **Non-existent config behavior:** simplicity.json advertises features CLI doesn't actually use
5. **Stale baseline output:** Quickstart and sample docs show 23 files; current is 24

**Phase 1 Track D (Link):** Fix dead URLs + false claims in docs (2–4h)

**Critical path execution:** Phase 1 blockers fixed in 48–72 hours (parallel tracks A–D)

**Status:** ✅ Complete. Five-agent findings merged into `.squad/decisions.md`. Release gate now honest and accountable.

---

## 2026-08-13T09:20:58Z — Docs-Site Hero Restructure: Callout Panels to Full-Width Sections

**Task:** Move the "The first five minutes" callout (index.astro) and "Recommended first command" callout (getting-started.astro) from `slot="aside"` in `<LandingPageTemplate>` into full-width section blocks in the default slot.

**Changes made:**
- `docs-site/src/pages/index.astro`: Removed `<div slot="aside" class="callout-panel">`. Replaced with `<section class="section"><div class="callout-panel">…</div></section>` as the first child of the default slot (above "Five surfaces, one contract").
- `docs-site/src/pages/getting-started.astro`: Same pattern — aside callout became the first full-width section (above "Pick the fastest honest install path").
- `docs-site/src/styles/site.css`: Removed the responsive rule setting `.hero-grid { grid-template-columns: minmax(0, 1.4fr) minmax(320px, 0.8fr); }`. With both asides gone, the two-column hero grid would have left a phantom empty right column at wide viewports. Single-column hero now renders cleanly on all breakpoints.

**Pattern used:** Reused existing `.callout-panel` class (unchanged) inside a standard `.section` wrapper. No new CSS classes introduced.

**Build result:** ✅ 32 pages built, 0 errors.

---

## Learnings

### 2026-08-13: Pricing page removed — packages are free/open-source

**Decision:** Removed the `/pricing/` page and all nav/CSS references to it. SimplicityTools NuGet packages (Metrics, Filters, Tca, Analyzers, Cli) are free and open-source. Support-based pricing may be introduced later as a separate initiative, but should NOT be pre-built or implied in the docs-site until Chris explicitly decides to offer it.

**What was removed:**
- `docs-site/src/pages/pricing.astro` (deleted)
- `pricing: '/pricing/'` from `docsLinks` in `site.ts`
- `{ label: 'Pricing', href: '/pricing/' }` from `primaryNavigation` in `site.ts`
- `'/pricing/'` from `publicRoutes` in `site.ts`
- `.pricing-card`, `.pricing-grid`, `.pricing-grid--wide` from all shared CSS selector groups in `site.css`

**What was NOT touched:** `IPricingService` in `reference-content.ts` — that's an unrelated SF0001 code sample, not a pricing UI element.

**Build result:** ✅ 31 pages (was 32), 0 errors.

**Guidance for future work:** Do NOT reintroduce a pricing page without explicit direction from Chris. If support pricing becomes a real initiative, build it as a fresh, separate page — do not restore the deleted one from git without review.

---

### 2026-08-13: Install-path cards redesigned — command as visual focal point

**Task:** Redesign the "Pick the fastest honest install path" three-card grid in `getting-started.astro` so each command block is the visual focal point, not a trailing afterthought.

**Changes made:**

- `docs-site/src/pages/getting-started.astro`: Replaced the three plain `.card` divs with `.card.install-card` divs. Each card now has:
  1. A use-case eyebrow badge (`.install-card__eyebrow`) positioned at the top using site pill/badge token language (`--accent-warm`, `rgba(255,107,114,...)`).
  2. The `<h3>` title beneath the badge.
  3. A `.install-card__terminal` wrapper with a `.terminal-bar` (macOS-style colored dots + shell-type label) topping a `<pre>` block — making the command visually prominent.
  4. The description (`<p>`) dropped beneath as secondary context (`.install-card__description`, muted color).
  Use-case labels added: "Recommended for CI parity", "Recommended for contributors", "Recommended for IDE-first teams".
  All command text preserved exactly.

- `docs-site/src/styles/site.css`: Added new classes (`.install-card`, `.install-card__eyebrow`, `.install-card__terminal`, `.terminal-bar`, `.terminal-dot`, `.terminal-dot--red/yellow/green`, `.terminal-bar__label`, `.install-card__description`). All use existing design tokens (`--bg-panel`, `--accent-warm`, `--text-muted`, `--line`, `--radius-md`, `rgba(...)` background/border patterns). No new arbitrary colors. Terminal dot colors (`#ff5f57`, `#febc2e`, `#28c840`) mirror the familiar macOS traffic-light convention that readers already associate with "real terminal."

**Why:** The old layout buried commands below prose, making readers scan down to find the thing they actually needed (the exact command to copy). Moving the terminal block above the description with a distinct visual frame (dots + type label) follows the "command is the thing" principle — description is supporting context, not the primary read.

**Build result:** ✅ 31 pages built, 0 errors.
