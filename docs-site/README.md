# docs-site

Astro site for the public SimplicityTools website at https://tools.simplicity-first.dev.

## First run

```bash
npm install
npm run dev
```

Open `http://localhost:4321/`.

## Validation

```bash
npm run build
npm run check-links
# or
npm run build:validate
```

`check-links` validates internal links plus the required SEO and deployment artifacts (`canonical`, Open Graph, `robots.txt`, `sitemap.xml`, and `CNAME`).

## Preview

```bash
npm run preview
```

Preview serves the built site at `http://localhost:4321/`.

## Production deploy handoff

`../.github/workflows/deploy-site.yml` deploys on pushes to `main` and publishes `dist/` to `gh-pages`.

After merge, operators still need to:
1. Let the first successful deploy create `gh-pages`.
2. Set GitHub Pages to `gh-pages` / root.
3. Confirm the custom domain is `tools.simplicity-first.dev`.
4. Point DNS `CNAME` `tools.simplicity-first.dev` at `cwoodruff.github.io`.
