import { siteUrl } from '../data/site';

export function GET() {
  return new Response(`User-agent: *
Allow: /

Sitemap: ${siteUrl}/sitemap.xml
`, {
    headers: {
      'Content-Type': 'text/plain; charset=utf-8'
    }
  });
}
