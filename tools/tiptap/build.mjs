// Builds ../../src/TabBlazor.TipTap/wwwroot/tiptap.js from entry.js.
//
//   npm ci && npm run build
//
// CI runs the same and fails when the committed bundle differs, so the bundle always matches package-lock.json.
// The build also fails when a package that ends up in the bundle is not MIT licensed.
import { build } from 'esbuild';
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const outfile = resolve(here, '../../src/TabBlazor.TipTap/wwwroot/tiptap.js');
const lock = readFileSync(join(here, 'package-lock.json'));
const lockHash = createHash('sha256').update(lock).digest('hex').slice(0, 16);
const version = JSON.parse(readFileSync(join(here, 'node_modules/@tiptap/core/package.json'), 'utf8')).version;

const result = await build({
    entryPoints: [join(here, 'entry.js')],
    outfile,
    bundle: true,
    format: 'esm',
    platform: 'browser',
    target: ['es2022'],
    minify: true,
    legalComments: 'none',
    metafile: true,
    write: false,
    logLevel: 'warning',
    banner: { js: `/* TipTap ${version} for TabBlazor.TipTap, built from tools/tiptap (lock ${lockHash}). MIT licensed: https://github.com/ueberdosis/tiptap, https://github.com/ProseMirror. Do not edit; run npm run build. */` }
});

// Every package with code in the bundle must be MIT.
const packages = new Set();
for (const input of Object.keys(result.metafile.inputs)) {
    const match = /node_modules\/((?:@[^/]+\/)?[^/]+)\//.exec(input.replaceAll('\\', '/'));
    if (match) {
        packages.add(match[1]);
    }
}
const notMit = [...packages].sort().filter(name => {
    const license = JSON.parse(readFileSync(join(here, 'node_modules', name, 'package.json'), 'utf8')).license;
    return license !== 'MIT';
});
if (notMit.length > 0) {
    console.error(`Not MIT licensed, refusing to bundle: ${notMit.join(', ')}`);
    process.exit(1);
}

const code = result.outputFiles[0].text;
writeFileSync(outfile, code);
const gzip = (await import('node:zlib')).gzipSync(code).length;
console.log(`${outfile}: ${(code.length / 1024).toFixed(0)} KB, ${(gzip / 1024).toFixed(0)} KB gzipped, ${packages.size} packages (all MIT)`);
