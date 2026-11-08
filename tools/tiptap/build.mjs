// Builds the TipTap bundles of TabBlazor.TipTap into ../../src/TabBlazor.TipTap/wwwroot:
//
//   tiptap.js           TipTap, ProseMirror and the curated extensions (entry.js)
//   tiptap-markdown.js  markdown support (markdown-entry.js), loaded only by editors with Format = Markdown;
//                       it imports @tiptap/core from ./tiptap.js, so both share one copy of TipTap
//
//   npm ci && npm run build
//
// CI runs the same and fails when the committed bundles differ, so they always match package-lock.json.
// The build also fails when a package that ends up in a bundle is not MIT licensed.
import { build } from 'esbuild';
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { gzipSync } from 'node:zlib';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const outdir = resolve(here, '../../src/TabBlazor.TipTap/wwwroot');
const lock = readFileSync(join(here, 'package-lock.json'));
const lockHash = createHash('sha256').update(lock).digest('hex').slice(0, 16);
const version = JSON.parse(readFileSync(join(here, 'node_modules/@tiptap/core/package.json'), 'utf8')).version;

/** Resolves @tiptap/core to the main bundle instead of bundling a second copy. */
const coreFromMainBundle = {
    name: 'core-from-main-bundle',
    setup(builder) {
        builder.onResolve({ filter: /^@tiptap\/core$/ }, () => ({ path: './tiptap.js', external: true }));
    }
};

async function bundle(entry, file, plugins = []) {
    const result = await build({
        entryPoints: [join(here, entry)],
        outfile: join(outdir, file),
        bundle: true,
        format: 'esm',
        platform: 'browser',
        target: ['es2022'],
        minify: true,
        legalComments: 'none',
        metafile: true,
        write: false,
        logLevel: 'warning',
        plugins,
        banner: { js: `/* TipTap ${version} for TabBlazor.TipTap, built from tools/tiptap (lock ${lockHash}). MIT licensed: https://github.com/ueberdosis/tiptap, https://github.com/ProseMirror, https://github.com/markedjs/marked. Do not edit; run npm run build. */` }
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
        console.error(`Not MIT licensed, refusing to bundle into ${file}: ${notMit.join(', ')}`);
        process.exit(1);
    }

    const code = result.outputFiles[0].text;
    writeFileSync(join(outdir, file), code);
    console.log(`${file}: ${(code.length / 1024).toFixed(0)} KB, ${(gzipSync(code).length / 1024).toFixed(0)} KB gzipped, ${packages.size} packages (all MIT)`);
}

await bundle('entry.js', 'tiptap.js');
await bundle('markdown-entry.js', 'tiptap-markdown.js', [coreFromMainBundle]);
