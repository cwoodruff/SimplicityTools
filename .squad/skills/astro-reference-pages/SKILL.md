---
name: "astro-reference-pages"
description: "Build deep Astro documentation pages from a shared content module while keeping stable explicit routes"
domain: "docs"
confidence: "high"
source: "earned"
---

## Context
Use this pattern when a docs-site needs many reference pages that share structure but still need explicit, linkable routes. It works well when the information architecture is already known and the main risk is duplication or broken internal linking.

## Patterns
1. Keep task-shaped URLs explicit (`/docs/commands/foo/`, `/analyzers/sf0001/`) even if the content is rendered from shared data.
2. Put long-form reusable content in a single typed module (for this repo, `docs-site/src/data/reference-content.ts`) and keep page files thin.
3. Use small Astro reference components (for this repo, `docs-site/src/components/reference/`) to render repeated layouts like command contracts, filter pages, analyzer pages, and syntax-highlighted code blocks.
4. Keep top-level landing pages focused on routing and adoption context; push deep reference detail into dedicated subdirectories.
5. Rebuild the site after structural content work with `cd docs-site && npm run build`.

## Examples
- Shared content: `docs-site/src/data/reference-content.ts`
- Reusable rendering: `docs-site/src/components/reference/ReferenceCodeBlock.astro`
- Thin wrappers: `docs-site/src/pages/docs/commands/*.astro`, `docs-site/src/pages/analyzers/*.astro`

## Anti-Patterns
- Do not duplicate long command or analyzer text across many Astro pages.
- Do not hide stable URLs behind clever dynamic routing when the docs contract benefits from explicit files.
- Do not leave hub pages talking about implementation waves once the deep content exists.
