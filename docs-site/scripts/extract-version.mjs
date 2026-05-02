#!/usr/bin/env node

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const buildPropsPath = path.resolve(__dirname, '../../Directory.Build.props');
const versionOutputPath = path.resolve(__dirname, '../src/data/version.ts');

const xmlContent = fs.readFileSync(buildPropsPath, 'utf-8');

const versionMatch = xmlContent.match(/<SimplicityToolsReleaseVersion>([^<]+)<\/SimplicityToolsReleaseVersion>/);

if (!versionMatch) {
  throw new Error('Missing <SimplicityToolsReleaseVersion> in Directory.Build.props.');
}

const version = versionMatch[1].trim();

const tsContent = `// Auto-generated from Directory.Build.props
export const toolVersion = '${version}';
`;

fs.writeFileSync(versionOutputPath, tsContent, 'utf-8');
console.log(`✓ Extracted version: ${version}`);
console.log(`✓ Wrote version data to ${path.relative(process.cwd(), versionOutputPath)}`);
