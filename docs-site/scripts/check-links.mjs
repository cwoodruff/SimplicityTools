import fs from 'node:fs';
import path from 'node:path';

const distDir = path.resolve('dist');
const siteUrl = 'https://tools.simplicity-first.dev';
const requiredRootFiles = ['CNAME', 'robots.txt', 'sitemap.xml'];
const requiredMetaChecks = [
  { label: 'canonical link', pattern: /<link[^>]+rel=["']canonical["'][^>]+href=["'][^"']+["']/i },
  { label: 'meta description', pattern: /<meta[^>]+name=["']description["'][^>]+content=["'][^"']+["']/i },
  { label: 'viewport meta', pattern: /<meta[^>]+name=["']viewport["'][^>]+content=["'][^"']+["']/i },
  { label: 'Open Graph title', pattern: /<meta[^>]+property=["']og:title["'][^>]+content=["'][^"']+["']/i },
  { label: 'Open Graph description', pattern: /<meta[^>]+property=["']og:description["'][^>]+content=["'][^"']+["']/i },
  { label: 'Open Graph url', pattern: /<meta[^>]+property=["']og:url["'][^>]+content=["'][^"']+["']/i },
  { label: 'twitter card', pattern: /<meta[^>]+name=["']twitter:card["'][^>]+content=["'][^"']+["']/i }
];
const requiredAccessibilityChecks = [
  { label: 'document language', pattern: /<html[^>]+lang=["']en["']/i },
  { label: 'skip link', pattern: /<a[^>]+class=["'][^"']*skip-link[^"']*["'][^>]+href=["']#main-content["']/i },
  { label: 'main landmark', pattern: /<main[^>]+id=["']main-content["']/i },
  { label: 'primary nav label', pattern: /<nav[^>]+aria-label=["']Primary["']/i }
];

if (!fs.existsSync(distDir)) {
  throw new Error('dist/ does not exist. Run npm run build first.');
}

for (const file of requiredRootFiles) {
  const fullPath = path.join(distDir, file);
  if (!fs.existsSync(fullPath)) {
    throw new Error(`Missing required build artifact: ${file}`);
  }
}

const cname = fs.readFileSync(path.join(distDir, 'CNAME'), 'utf8').trim();
if (cname !== 'tools.simplicity-first.dev') {
  throw new Error(`CNAME mismatch. Expected tools.simplicity-first.dev but found ${cname || '<empty>'}`);
}

const robots = fs.readFileSync(path.join(distDir, 'robots.txt'), 'utf8');
if (!robots.includes(`${siteUrl}/sitemap.xml`)) {
  throw new Error('robots.txt is missing the sitemap URL.');
}

const sitemap = fs.readFileSync(path.join(distDir, 'sitemap.xml'), 'utf8');
if (!sitemap.includes(siteUrl)) {
  throw new Error('sitemap.xml does not reference the production site URL.');
}

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      return walk(fullPath);
    }

    return [fullPath];
  });
}

function toRoute(filePath) {
  const relativePath = path.relative(distDir, filePath).replaceAll(path.sep, '/');
  if (relativePath === 'index.html') {
    return '/';
  }

  return `/${relativePath.replace(/index\.html$/, '')}`;
}

function resolveTarget(link, currentRoute) {
  const resolved = new URL(link, `${siteUrl}${currentRoute}`);
  let pathname = resolved.pathname;

  if (pathname.endsWith('/')) {
    pathname = `${pathname}index.html`;
  }

  const directPath = path.join(distDir, pathname.replace(/^\//, ''));
  if (fs.existsSync(directPath)) {
    return directPath;
  }

  const nestedIndex = path.join(distDir, pathname.replace(/^\//, ''), 'index.html');
  if (fs.existsSync(nestedIndex)) {
    return nestedIndex;
  }

  return null;
}

const htmlFiles = walk(distDir).filter((filePath) => filePath.endsWith('.html'));
const missingLinks = [];
const metadataIssues = [];
const accessibilityIssues = [];
const hrefRegex = /(?:href|src)=["']([^"']+)["']/gi;

for (const filePath of htmlFiles) {
  const html = fs.readFileSync(filePath, 'utf8');
  const route = toRoute(filePath);

  for (const check of requiredMetaChecks) {
    if (!check.pattern.test(html)) {
      metadataIssues.push(`${route} missing ${check.label}`);
    }
  }

  for (const check of requiredAccessibilityChecks) {
    if (!check.pattern.test(html)) {
      accessibilityIssues.push(`${route} missing ${check.label}`);
    }
  }

  const h1Count = [...html.matchAll(/<h1\b/gi)].length;
  if (h1Count !== 1) {
    accessibilityIssues.push(`${route} expected exactly one h1, found ${h1Count}`);
  }

  for (const match of html.matchAll(hrefRegex)) {
    const target = match[1];
    if (
      !target ||
      target.startsWith('#') ||
      target.startsWith('mailto:') ||
      target.startsWith('tel:') ||
      target.startsWith('data:') ||
      target.startsWith('javascript:') ||
      /^https?:\/\//i.test(target)
    ) {
      continue;
    }

    const normalized = target.split('#')[0];
    if (!normalized) {
      continue;
    }

    if (!resolveTarget(normalized, route)) {
      missingLinks.push(`${route} -> ${target}`);
    }
  }
}

if (metadataIssues.length > 0 || accessibilityIssues.length > 0 || missingLinks.length > 0) {
  const messages = [];
  if (metadataIssues.length > 0) {
    messages.push(`Metadata issues:\n- ${metadataIssues.join('\n- ')}`);
  }
  if (accessibilityIssues.length > 0) {
    messages.push(`Accessibility issues:\n- ${accessibilityIssues.join('\n- ')}`);
  }
  if (missingLinks.length > 0) {
    messages.push(`Broken internal links/assets:\n- ${missingLinks.join('\n- ')}`);
  }
  throw new Error(messages.join('\n\n'));
}

console.log(`Validated ${htmlFiles.length} HTML files, sitemap.xml, robots.txt, CNAME, and accessibility landmarks.`);
