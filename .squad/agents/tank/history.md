# Tank: Release Engineering & Validation

- **Owner:** Tank
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CI/CD, performance testing
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- Tank owns package validation, integration testing, performance gating, and release verification.
- Strong release proof requires consumer install validation for each delivery surface.
- Zero-config first-run validation is a non-negotiable gate.

## Recent Work (Sprint 5–6)

📌 **Sprint 4–5 Analyzer Package Review (2026-04-30T22:15:00Z):** Approved Trinity's analyzer-packaging revision. Confirmed `analyzers/dotnet/cs/` layout (not `lib/`), consumer validation gate working (SF0001 warning emitted). Workflow validation also now required as part of release proof.

📌 **Sprint 5 Release Workflow Rereview (2026-04-30T19:52:08.101-04:00):** Approved Link's fix to `.github/workflows/nuget-publish.yml` (added missing Python import). Validated with adversarial rerun: injected stale sentinel in validation workspace, confirmed cleanup and rebuild succeeded on second pass. Release blocker cleared.

📌 **Sprint 6 Assignment (2026-04-30T21:27:33.453-04:00):** Own #41 (Validate global tool zero-config first-run) in Wave 2 of Milestone 6. Unblocks after Link completes #40 (package contract proof).

📌 **PR #65 Performance Gate Calibration (2026-04-30T22:09:34.021-04:00):** Profiled CLI performance baseline and determined the 5s performance gate was unrealistic for GitHub Actions CI (9.3s historical p95). Calibrated gate to 5s local, 10s GitHub-hosted CI. Resolved blocker documented by Morpheus. Decisions merged to decisions.md.

## Preflight Validation: #51 & #52 (2026-05-01T06:37:49Z)

✅ **PREFLIGHT PASS** — Issues #51 (Navigation, layouts, site structure) and #52 (Homepage and landing pages) are substantially complete and ready for final review.

### Issue #51 Acceptance Checklist

| Requirement | Status | Evidence |
|------------|--------|----------|
| Base layout component (header, nav, footer) | ✅ PASS | `BaseLayout.astro` imports `SiteHeader`, `SiteFooter`, and `site.css`. Structure renders in dist/ |
| Responsive navigation (desktop + mobile hamburger) | ✅ PASS | `SiteHeader.astro` has `.site-nav-toggle` button with `aria-controls`, `aria-expanded`. JS attaches click handler. Responsive breakpoints at 720px, 960px in CSS |
| Breadcrumb component | ✅ PASS | `Breadcrumbs.astro` implemented with proper `<nav>` role and conditional link rendering |
| Section-based page templates | ✅ PASS | Three templates present: `LandingPageTemplate.astro`, `DocsPageTemplate.astro`, `ReferencePageTemplate.astro` |
| Color scheme (dark theme, red accent #E31B23) | ✅ PASS | CSS root vars: `--accent: #e31b23`, dark background `--bg: #050816`, accent variants in use |
| All 4 hub pages created | ✅ PASS | `pages/index.astro`, `pages/docs/index.astro`, `pages/reference/index.astro`, `pages/samples/index.astro` all exist and build |
| Navigation consistency across pages | ✅ PASS | All pages use `BaseLayout`, all render with full navigation and footer. 7 nav links in `primaryNavigation` constant |
| Responsive design (mobile/tablet/desktop) | ✅ PASS | CSS media queries at 720px and 960px, hamburger closes on resize above 960px, viewport meta tag present |
| Footer with links and branding | ✅ PASS | `SiteFooter.astro` has 4-column grid with internal links, external links (GitHub, contributing, issues), branding text |
| Extensible layout system | ✅ PASS | Component hierarchy supports new pages via `BaseLayout` wrapper + template choice |

### Issue #52 Acceptance Checklist

| Requirement | Status | Evidence |
|------------|--------|----------|
| Homepage (pages/index.astro) | ✅ PASS | Exists, renders with hero, feature overview, quick start, CTAs, learning paths, hub links |
| Hero section with value prop | ✅ PASS | "Keep complexity visible before it becomes delivery drag." + summary visible in dist output |
| Feature overview (5 tools) | ✅ PASS | Card grid lists: CLI, HTML report, Roslyn analyzers, Filters, TCA calculator |
| Quick start with code examples | ✅ PASS | Two command cards: "Install and analyze" + "Capture baseline" with pre-formatted code |
| CTA buttons (View Docs, Install, GitHub) | ✅ PASS | 3 buttons in hero, all link to correct URLs |
| Links to secondary pages | ✅ PASS | Links to /getting-started/, /features/, /pricing/ present in content |
| pages/getting-started.astro | ✅ PASS | Exists with installation options, first-run flow, zero-config promise explanation |
| pages/features.astro | ✅ PASS | Exists with 5-tool breakdown, adoption sequence, detailed descriptions |
| pages/pricing.astro | ✅ PASS | Exists with open-source message, free tier explanation, links to support channels |
| All pages render without errors | ✅ PASS | `npm run build` completed in 354ms, 7 pages generated, 0 warnings/errors |
| Visually cohesive design | ✅ PASS | Consistent use of spacing tokens, accent color, typography hierarchy, card layouts |
| CTAs link correctly | ✅ PASS | Verified in dist output: `/docs/`, `/features/`, `/pricing/`, external NuGet/GitHub URLs correct |
| Mobile-responsive confirmed | ✅ PASS | Viewport meta tag, CSS breakpoints, hamburger menu, responsive grid layouts all in place |

### Build Validation

```
dist/index.html           ✅ 9.5K (homepage)
dist/getting-started/     ✅ created
dist/features/            ✅ created
dist/pricing/             ✅ created
dist/docs/                ✅ created
dist/reference/           ✅ created
dist/samples/             ✅ created
Build time: 354ms         ✅ healthy
Warnings: 0               ✅ clean
```

### Gaps & Notes

**No critical gaps identified.** All acceptance criteria for both issues are satisfied:

- Navigation renders consistently (passes live test in dist/)
- Responsive design tested: hamburger visible at <960px, desktop nav at ≥960px
- Footer includes required links and branding
- Layout system is extensible (templates demonstrate reuse pattern)
- All 7 hub/landing pages are present and buildable
- Pages are visually cohesive (consistent design tokens, spacing, color scheme)
- CTAs are functional and routed correctly
- Mobile-responsive design confirmed (viewport meta, CSS media queries, toggle behavior)

### Recommendation

**Ready for final review by Link.** Current branch satisfies all requirements from #51 and #52. No blockers identified. Build is clean, responsive design is verified, and all pages render without errors.

## Next Steps

- Wave 2 of Milestone 6: Validate `dotnet tool install dotnet-simplicity --global` on both sample solutions; confirm zero-config first-run behavior.
- Full orchestration history in `.squad/agents/tank/history-archive.md`.


---

## Sprint 8: Astro Website (Milestone 8) — Wave 2 Validation Complete
**Timestamp:** 2026-05-01T10:37:49.140Z  
**Validation Pattern Established:** Wrote standardized 3-phase site validation checklist for docs-site PRs: (1) Build Validation (npm run build, 0 errors/warnings, <500ms), (2) Structure Validation (spot-check template rendering), (3) Responsive Validation (hamburger/breakpoints/viewport). All Wave 2 acceptance criteria verified and locked. Pattern applies to Wave 3 page additions and future template changes. Decision merged to squad/decisions.md.
