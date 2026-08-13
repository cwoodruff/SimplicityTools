# Squad Decisions


### 2026-08-13: Remove Pricing Page — Packages Are Free/Open-Source
**By:** Link (DevRel)  
**Requested by:** Chris Woody Woodruff (Owner)  
**Status:** ✅ APPLIED

**Decision:** SimplicityTools NuGet packages (Metrics, Filters, Tca, Analyzers, Cli) are free and open-source. The pricing page (`/pricing/`) has been removed. No pricing UI, nav item, or route should exist.

**Changes:** Deleted `docs-site/src/pages/pricing.astro`, removed pricing references from `site.ts`, cleaned pricing CSS from `site.css`. Build validated: 31 pages, 0 errors (previously 32).

**Implications:** Do NOT reintroduce pricing page without explicit direction. If support pricing becomes real future initiative, build deliberately—do not restore deleted page. Packages remain free.

---

### 2026-08-13T09:20:58Z: Docs-Site Hero Callout Panels — Full-Width Section Pattern
**By:** Link (DevRel)
**Status:** ✅ APPLIED

## Decision

Moved the "The first five minutes" callout (`index.astro`) and "Recommended first command" callout (`getting-started.astro`) from the `slot="aside"` of `<LandingPageTemplate>` into the default slot as the first `<section class="section">` block on each page.

## Options Considered

1. **Reuse `.callout-panel` inside `.section`** ← chosen
2. **Create a new full-width CSS class (e.g., `.hero-callout`)** — rejected; unnecessary new surface when `.callout-panel` already styles correctly and `.section` provides the full-width container semantics.
3. **Extend `LandingPageTemplate` with a new slot** — rejected; the template already provides the default slot for full-width content stacking.

## Why

- The aside slot constrained callout panels to `minmax(320px, 0.8fr)` at wide viewports, wrapping multi-line shell commands.
- `.callout-panel` + `.section` achieves full content-width rendering with no new CSS.
- Callout content (code blocks, pills, descriptive text) reads better in a full-width horizontal flow than crammed into a narrow sidebar.

## CSS Adjustment

Removed the responsive `.hero-grid { grid-template-columns: minmax(0, 1.4fr) minmax(320px, 0.8fr); }` rule from `site.css`. Both landing pages now have no aside child, so the two-column grid rule would have left a phantom empty column. The base `.hero-grid` (single-column, `display: grid; gap: 1.5rem`) remains and renders correctly.

## Guidance for Future Pages

If a page in `LandingPageTemplate` genuinely needs a narrow aside (e.g., a table of contents, a short metadata panel), the `slot="aside"` is still available and the CSS rule can be restored or scoped to that page class. But for teaching-oriented content like command callouts, default-slot full-width sections are the correct pattern.

## Verification

✓ Files touched: `docs-site/src/pages/index.astro`, `docs-site/src/pages/getting-started.astro`, `docs-site/src/styles/site.css`
✓ Build validated with `npm run build` (32 pages, 0 errors)
✓ No new dependencies or breaking changes
✓ Callout panels render at full content-width on all viewports

## Implications

- Future landing pages should use `.section` + full-width content for teaching-oriented callouts
- The `.hero-grid` two-column rule can be restored when pages genuinely have aside content
- Refer to this decision when adding similar callout patterns
