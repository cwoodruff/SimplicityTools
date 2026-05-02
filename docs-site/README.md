# docs-site

Astro site for the public SimplicityTools website at https://simplicitytools.dev.

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

## Version synchronization

The site footer automatically displays the current SimplicityTools release version, which is extracted from the shared `SimplicityToolsReleaseVersion` property in `Directory.Build.props` at build time.

When you run `npm run build` or `npm run dev`, the matching npm pre-step (`prebuild` / `predev`) executes `scripts/extract-version.mjs`, which:
1. Reads `SimplicityToolsReleaseVersion` from `../../Directory.Build.props`
2. Generates `src/data/version.ts` with the current version
3. The footer component imports and displays it

Update that one MSBuild property when you are preparing the next release line, and the package defaults plus site footer stay in sync.

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
3. Confirm the custom domain is `simplicitytools.dev`.
4. Create or verify the apex-domain DNS records required by GitHub Pages for `simplicitytools.dev`.
