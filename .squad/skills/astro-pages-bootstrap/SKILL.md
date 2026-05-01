---
name: "astro-pages-bootstrap"
description: "Bootstrap Astro in docs-site/ for GitHub Pages with a matching local base path"
domain: "docs"
confidence: "medium"
source: "Sprint 8 Wave 1 issue #50"
---

## Context

When this repo hosts an Astro site from a project Pages path, the local dev and preview URLs should match the deployed subpath instead of pretending the site lives at root.

## Pattern

1. Create the app in `docs-site/` with `dev`, `build`, and `preview` scripts.
2. Set `astro.config.mjs` with the GitHub Pages origin and the repository base path (for this repo, `/SimplicityTools/`).
3. Put `.nojekyll` in `public/` so Astro copies it to `dist/` on every build.
4. Add a tiny `README.md` that tells contributors the exact local URL to open.
5. Verify `npm run dev`, `npm run build`, and `npm run preview` against the repository subpath, not `/`.
