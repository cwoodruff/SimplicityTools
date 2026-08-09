export type NavigationLink = {
  label: string;
  href: string;
};

export type DesignToken = {
  label: string;
  value: string;
  usage: string;
};

export const siteTitle = 'SimplicityTools';
export const siteDescription =
  'Measure solution complexity, teach healthier architecture, and give teams a clearer path from first run to CI gating.';
export const siteUrl = 'https://simplicitytools.dev';
export const siteDomain = 'simplicitytools.dev';
export const socialImagePath = '/social-card.svg';
export const socialImageAlt =
  'SimplicityTools social card with the headline Measure complexity before it measures your team.';

export const repoUrl = 'https://github.com/cwoodruff/SimplicityTools';
export const maintainerUrl = 'https://github.com/cwoodruff';
export const cliPackageUrl = 'https://www.nuget.org/packages/SimplicityTools.Cli/';
export const analyzerPackageUrl = 'https://www.nuget.org/packages/SimplicityTools.Analyzers/';

export const docsLinks = {
  docsHub: '/docs/',
  gettingStarted: '/getting-started/',
  features: '/features/',
  pricing: '/pricing/',
  reference: '/reference/',
  samples: '/samples/',
  quickstart: `${repoUrl}/blob/main/docs/quickstart.md`,
  guide: `${repoUrl}/blob/main/docs/using-the-simplicity-tools.md`,
  troubleshooting: `${repoUrl}/blob/main/docs/troubleshooting.md`,
  schema: `${repoUrl}/blob/main/docs/simplicity-schema.json`,
  contributing: `${repoUrl}/blob/main/CONTRIBUTING.md`,
  repoDocs: `${repoUrl}/tree/main/docs`,
  readme: `${repoUrl}/blob/main/README.md`,
  simplifiedSample: `${repoUrl}/tree/main/samples/Sample.Simplified`,
  overEngineeredSample: `${repoUrl}/tree/main/samples/Sample.OverEngineered`,
  claimsPortalSample: `${repoUrl}/tree/main/samples/Sample.ClaimsPortal`,
  issues: `${repoUrl}/issues`
};

export const primaryNavigation: NavigationLink[] = [
  { label: 'Home', href: '/' },
  { label: 'Getting Started', href: '/getting-started/' },
  { label: 'Features', href: '/features/' },
  { label: 'Docs', href: '/docs/' },
  { label: 'Reference', href: '/reference/' },
  { label: 'Samples', href: '/samples/' },
  { label: 'Pricing', href: '/pricing/' }
];

export const toolHighlights = [
  {
    name: 'dotnet-simplicity CLI',
    summary: 'Analyze a solution, capture a baseline, compare drift, enforce a complexity budget, and keep CI honest.',
    href: '/getting-started/'
  },
  {
    name: 'HTML report',
    summary: 'Generate a self-contained report you can hand to stakeholders or attach to CI artifacts.',
    href: '/features/'
  },
  {
    name: 'Roslyn analyzers',
    summary: 'Surface simplification opportunities inside the IDE and normal builds with teach-first diagnostics.',
    href: '/features/'
  },
  {
    name: 'Filters',
    summary: 'Turn raw metrics into understandable verdicts with TwoAmTest, HalfRule, and PrimaryPathFirst.',
    href: '/reference/'
  },
  {
    name: 'TCA calculator',
    summary: 'Translate architectural drift into an annual cost signal teams and stakeholders can discuss together.',
    href: '/features/'
  }
];

export const designTokens: DesignToken[] = [
  { label: 'Background', value: '#050816', usage: 'Default canvas and page chrome.' },
  { label: 'Panel', value: '#0B1222', usage: 'Cards, sections, and elevated surfaces.' },
  { label: 'Text', value: '#E5EEFB', usage: 'Primary copy and headings.' },
  { label: 'Muted text', value: '#9FB1CC', usage: 'Body copy, metadata, and supporting labels.' },
  { label: 'Brand red', value: '#E31B23', usage: 'Primary action, active state, and key emphasis.' },
  { label: 'Warm accent', value: '#FF6B72', usage: 'Eyebrows, badges, and secondary emphasis.' },
  { label: 'Success', value: '#62D39F', usage: 'Positive status and validation cues.' }
];

export const publicRoutes = [
  '/',
  '/getting-started/',
  '/features/',
  '/pricing/',
  '/docs/',
  '/docs/commands/',
  '/docs/commands/analyze/',
  '/docs/commands/baseline/',
  '/docs/commands/budget/',
  '/docs/commands/diff/',
  '/docs/commands/report/',
  '/docs/commands/watch/',
  '/docs/filters/',
  '/docs/filters/halfrule/',
  '/docs/filters/primarypathfirst/',
  '/docs/filters/twowamtest/',
  '/docs/configuration/',
  '/docs/library-usage/',
  '/reference/',
  '/samples/',
  '/analyzers/',
  '/analyzers/sf0001/',
  '/analyzers/sf0002/',
  '/analyzers/sf0003/',
  '/analyzers/sf0004/',
  '/analyzers/sf0005/',
  '/analyzers/sf0006/',
  '/analyzers/sf0007/',
  '/integration/',
  '/integration/ci-cd/',
  '/integration/csproj-reference/',
  '/integration/ide-setup/'
] as const;

export function absoluteUrl(path: string): string {
  return new URL(path, siteUrl).toString();
}

export function withBase(path: string): string {
  const base = import.meta.env.BASE_URL.endsWith('/')
    ? import.meta.env.BASE_URL.slice(0, -1)
    : import.meta.env.BASE_URL;
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;

  if (normalizedPath === '/') {
    return `${base}/`;
  }

  return `${base}${normalizedPath.endsWith('/') ? normalizedPath : `${normalizedPath}/`}`;
}
