---
name: "astro-pages-validate-deploy"
description: "Ship an Astro docs site to GitHub Pages with SEO, validation, and custom-domain safety checks"
domain: "docs"
confidence: "high"
source: "earned"
---

## Context
Use this when an Astro site has moved beyond initial bootstrap and now needs production-safe metadata, validation, and GitHub Pages deployment behavior.

## Patterns
1. Set one canonical production origin in `astro.config.mjs`; do not mix project-subpath URLs with custom-domain metadata.
2. Put shared SEO metadata in the base layout so every page gets canonical, Open Graph, Twitter, viewport, and robots tags by default.
3. Keep a small explicit route inventory for static sitemap generation when the page set is finite and intentionally hand-authored.
4. Add a build gate like `npm run build:validate` that checks built HTML for internal links, required root artifacts (`CNAME`, `robots.txt`, `sitemap.xml`), and basic accessibility landmarks before deploy.
5. Keep one committed custom-domain source of truth (for this repo, the root `CNAME`) and sync it into the Astro public assets before building so deploy output cannot drift from Pages configuration.
6. For domain cutovers, validate exact canonical and `og:url` values per route and fail the build if any generated artifact still contains the legacy domain.
7. Publish to `gh-pages` only after validation passes, and treat Pages configuration as incomplete until the branch exists and DNS resolves.
6. Keep the repository-root `CNAME` as the custom-domain source of truth and sync it into the Astro public output during deploy so local builds and published Pages artifacts cannot drift.

## Examples
- `docs-site/src/layouts/BaseLayout.astro`
- `docs-site/scripts/check-links.mjs`
- `docs-site/src/pages/robots.txt.ts`
- `docs-site/src/pages/sitemap.xml.ts`
- `CNAME`
- `.github/workflows/deploy-site.yml`

## Anti-Patterns
- Leaving canonical URLs pointed at a GitHub project subpath after introducing a custom domain.
- Letting the workflow `cname`, committed `CNAME`, and generated `dist/CNAME` drift apart during a domain move.
- Publishing docs without validating the built output.
- Calling custom-domain setup complete before both `gh-pages` and public DNS exist.
