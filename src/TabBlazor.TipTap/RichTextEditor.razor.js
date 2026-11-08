// RichTextEditor: TipTap in the browser, value sync through DOM change events.
//
// The value reaches .NET only through `change` events dispatched on the hidden input. Blazor attaches the input's
// value to such an event, so the re-render that follows cannot overwrite newer typing. Never push the value to .NET
// through a JS-to-.NET call instead. The one JS-to-.NET call is the mention search, which is request/response.
//
// The toolbar is rendered by Blazor, but its clicks and its active states are handled here, without round trips.
//
// attach and update never throw to .NET: on Blazor Server an exception there would end the whole circuit. When TipTap
// cannot start (the bundle is blocked or missing, an extension throws), they return { error } instead and .NET shows
// a plain text area.

const states = new WeakMap();
const registry = new Map();
const loadedModules = new Map();
const tiptapModules = new Map();
const isMac = /Mac|iPhone|iPad|iPod/.test(navigator.userAgentData?.platform ?? navigator.platform ?? navigator.userAgent);
const ALLOWED_SCHEMES = /^(https?:|mailto:)/i;
const HAS_SCHEME = /^[a-z][a-z0-9+.-]*:/i;

/** The registry app extension modules receive: `registry.add('name', options => Extension)`. */
const extensionRegistry = Object.freeze({
    add(name, factory) {
        if (typeof name !== 'string' || typeof factory !== 'function') {
            throw new TypeError('registry.add(name, factory) needs a name and a function that returns a TipTap extension.');
        }
        registry.set(name, factory);
    },
    has: name => registry.has(name)
});

export async function attach(root, options, dotnet) {
    if (!root || states.has(root)) {
        return null;
    }

    const content = root.querySelector('.rich-text-editor-content');
    const hidden = root.querySelector('.rich-text-editor-value');
    if (!content || !hidden) {
        return null;
    }

    const state = { root, content, hidden, dotnet, options: normalize(options), editor: null, timer: 0, frame: 0, committed: hidden.value, cleanup: [] };
    states.set(root, state);

    listen(state, root, 'mousedown', event => {
        // Keep the focus (and the selection) in the editor when a toolbar button is clicked with the mouse.
        if (event.target.closest?.('.rich-text-editor-toolbar button')) {
            event.preventDefault();
        }
    });
    listen(state, root, 'click', event => onClick(state, event));
    listen(state, root, 'keydown', event => onToolbarKeyDown(state, event));
    listen(state, root, 'pointerover', event => highlightTable(event.target));
    listen(state, root, 'focusin', event => highlightTable(event.target));
    listen(state, document, 'pointerdown', event => {
        if (!openDropdown(state)?.contains(event.target)) {
            closeMenu(state, false);
        }
    });
    // The label is outside the root: clicking it focuses the editor, as it would a text area.
    const label = root.id ? document.querySelector(`[data-rt-label-for="${CSS.escape(root.id)}"]`) : null;
    if (label) {
        listen(state, label, 'click', () => state.editor?.commands.focus());
    }

    const toolbar = root.querySelector('.rich-text-editor-toolbar');
    if (toolbar && typeof MutationObserver !== 'undefined') {
        const observer = new MutationObserver(() => setupRoving(state));
        observer.observe(toolbar, { subtree: true, childList: true, attributes: true, attributeFilter: ['disabled', 'hidden'] });
        state.cleanup.push(() => observer.disconnect());
    }

    showMacShortcuts(root);
    setupRoving(state);
    return await start(state);
}

export async function update(root, options) {
    const state = states.get(root);
    if (!state) {
        return null;
    }

    const next = normalize(options);
    const previous = state.options;
    state.options = next;
    if (!state.editor) {
        return null;
    }

    if (next.setupKey !== previous.setupKey || next.placeholder !== previous.placeholder) {
        flush(state);
        state.editor.destroy();
        state.editor = null;
        return await start(state);
    }

    if (next.locked !== previous.locked) {
        state.editor.setEditable(!next.locked, false);
    }
    state.editor.setOptions({ editorProps: { ...state.editor.options.editorProps, attributes: editorAttributes(state) } });
    scheduleToolbar(state);
    return null;
}

/** Creates the editor. Never throws: returns null, or { error } with the reason it could not start. */
async function start(state) {
    try {
        await create(state);
        return null;
    } catch (error) {
        console.error('RichTextEditor: TipTap could not start, so the editor falls back to a plain text area.', error);
        if (states.get(state.root) === state) {
            clearTimeout(state.timer);
            state.cleanup.forEach(dispose => dispose());
            try {
                state.editor?.destroy();
            } catch {
                // Already broken.
            }
            states.delete(state.root);
        }
        return { error: String(error?.message ?? error) };
    }
}

/** Called after .NET set a new value: it is already in .NET, so it is not sent back. */
export function refresh(root) {
    const state = states.get(root);
    const editor = state?.editor;
    if (!editor) {
        return;
    }

    clearTimeout(state.timer);
    const value = state.hidden.value;
    state.committed = value;
    if (value === serialize(state)) {
        return;
    }

    const { from, to } = editor.state.selection;
    editor.commands.setContent(parse(state, value), { emitUpdate: false, contentType: contentType(state) });
    const size = editor.state.doc.content.size;
    editor.commands.setTextSelection({ from: Math.min(from, size), to: Math.min(to, size) });
    scheduleToolbar(state);
}

export function detach(root) {
    const state = states.get(root);
    if (!state) {
        return;
    }

    clearTimeout(state.timer);
    cancelAnimationFrame(state.frame);
    state.cleanup.forEach(dispose => dispose());
    state.editor?.destroy();
    state.editor = null;
    states.delete(root);
}

export function focus(root) {
    states.get(root)?.editor?.commands.focus();
}

export function insertContent(root, html) {
    const state = states.get(root);
    if (!state?.editor?.isEditable) {
        return;
    }

    state.editor.chain().focus().insertContent(html).run();
    flush(state);
}

export function runCommand(root, command, args) {
    const state = states.get(root);
    return state ? run(state, command, args) : false;
}

/** Shows a placeholder at the caret for each upload, in order. Placeholders are decorations: never part of the value. */
export function startUploads(root, items) {
    const editor = states.get(root)?.editor;
    if (!editor || !items?.length) {
        return;
    }

    const pos = editor.state.selection.from;
    editor.view.dispatch(editor.state.tr.setMeta(uploadsKey(), { add: items.map(item => ({ token: item.token, label: item.label })), pos }));
}

/**
 * Replaces an upload's placeholder with the file: an image, or a link with the file name. With no content it only
 * removes the placeholder. Returns false when the placeholder is gone (the editor was recreated or closed).
 */
export function finishUpload(root, token, content) {
    const state = states.get(root);
    const editor = state?.editor;
    if (!editor) {
        return false;
    }

    const key = uploadsKey();
    const found = key.getState(editor.state)?.find(undefined, undefined, spec => spec.token === token)?.[0];
    if (!found) {
        return false;
    }

    const pos = found.from;
    editor.view.dispatch(editor.state.tr.setMeta(key, { remove: token }));
    if (!content) {
        return true;
    }

    const schema = editor.schema;
    let node;
    if (content.embed && schema.nodes.image) {
        node = { type: 'image', attrs: { src: content.url, alt: content.text } };
    } else if (schema.marks.link && isAllowedUrl(content.url)) {
        node = { type: 'text', text: content.text || content.url, marks: [{ type: 'link', attrs: { href: content.url } }] };
    } else {
        node = { type: 'text', text: `${content.text} (${content.url})` };
    }
    editor.chain().insertContentAt(pos, node).run();
    flush(state);
    return true;
}

// Setup

function normalize(options) {
    return {
        scriptUrl: options?.scriptUrl || './_content/TabBlazor.TipTap/tiptap.js',
        markdownScriptUrl: options?.markdownScriptUrl || './_content/TabBlazor.TipTap/tiptap-markdown.js',
        extensionModules: options?.extensionModules ?? [],
        setupKey: options?.setupKey ?? '',
        format: ['json', 'markdown'].includes(options?.format) ? options.format : 'html',
        debounceMs: options?.debounceMs ?? 0,
        locked: !!options?.locked,
        placeholder: options?.placeholder ?? '',
        labelId: options?.labelId ?? null,
        invalid: !!options?.invalid,
        maxLength: options?.maxLength ?? null,
        uploads: !!options?.uploads,
        extensions: options?.extensions ?? { basic: true },
        customExtensions: options?.customExtensions ?? [],
        extensionOptions: options?.extensionOptions ?? {},
        mention: options?.mention ?? { trigger: '@', minChars: 0, debounceMs: 150 },
        texts: options?.texts ?? {}
    };
}

function absolute(url) {
    return new URL(url, document.baseURI).href;
}

/** Loads the TipTap bundle and the app's extension modules, once each. */
async function load(options) {
    const url = absolute(options.scriptUrl);
    const tiptap = await importOnce(url);
    if (typeof tiptap.Editor !== 'function') {
        throw new Error(`${options.scriptUrl} is not a TipTap bundle: it does not export Editor.`);
    }

    for (const moduleUrl of options.extensionModules) {
        const resolved = absolute(moduleUrl);
        if (!loadedModules.has(resolved)) {
            loadedModules.set(resolved, import(resolved).then(module => {
                if (typeof module.register !== 'function') {
                    console.error(`RichTextEditor: ${moduleUrl} does not export register(registry).`);
                    return;
                }
                return module.register(extensionRegistry);
            }).catch(error => console.error(`RichTextEditor: could not load ${moduleUrl}.`, error)));
        }
        await loadedModules.get(resolved);
    }

    // Markdown support is a separate file that imports this bundle, unless a custom bundle includes it.
    if (options.format === 'markdown' && !tiptap.Markdown) {
        const markdown = await importOnce(absolute(options.markdownScriptUrl));
        if (!markdown.Markdown) {
            throw new Error(`${options.markdownScriptUrl} does not export Markdown.`);
        }
        return { ...tiptap, Markdown: markdown.Markdown };
    }
    return tiptap;
}

/** Imports a module once; a failed import is forgotten, so the next editor tries again. */
function importOnce(url) {
    if (!tiptapModules.has(url)) {
        tiptapModules.set(url, import(url).catch(error => {
            tiptapModules.delete(url);
            throw error;
        }));
    }
    return tiptapModules.get(url);
}

let uploadsPluginKey = null;

function uploadsKey() {
    return uploadsPluginKey;
}

async function create(state) {
    const tiptap = await load(state.options);
    if (states.get(state.root) !== state || state.editor) {
        return;
    }

    uploadsPluginKey ??=new tiptap.PluginKey('tabblazorUploads');

    const editor = new tiptap.Editor({
        element: state.content,
        extensions: buildExtensions(tiptap, state),
        content: parse(state, state.hidden.value),
        contentType: contentType(state),
        editable: !state.options.locked,
        injectCSS: false,
        editorProps: {
            attributes: editorAttributes(state),
            handlePaste: (view, event) => onPaste(state, event),
            handleDrop: (view, event, slice, moved) => onDrop(state, view, event, moved),
            handleKeyDown: (view, event) => onEditorKeyDown(state, event)
        },
        onUpdate: () => schedule(state),
        onBlur: () => flush(state),
        onTransaction: () => scheduleToolbar(state)
    });
    state.editor = editor;
    state.committed = state.hidden.value;
    updateToolbar(state);
}

function editorAttributes(state) {
    const attributes = { role: 'textbox', 'aria-multiline': 'true', class: 'rich-text-editor-prose' };
    if (state.options.labelId) {
        attributes['aria-labelledby'] = state.options.labelId;
    }
    if (state.options.invalid) {
        attributes['aria-invalid'] = 'true';
    }
    if (state.options.locked) {
        attributes['aria-readonly'] = 'true';
    }
    return attributes;
}

function buildExtensions(tiptap, state) {
    const { extensions: on, maxLength, placeholder } = state.options;
    const basic = !!on.basic;
    const extensions = [
        tiptap.StarterKit.configure({
            heading: basic ? { levels: [1, 2, 3] } : false,
            bold: basic ? {} : false,
            italic: basic ? {} : false,
            underline: basic && on.underline !== false ? {} : false,
            strike: basic ? {} : false,
            code: basic ? {} : false,
            codeBlock: basic ? {} : false,
            blockquote: basic ? {} : false,
            bulletList: basic ? {} : false,
            orderedList: basic ? {} : false,
            listItem: basic || on.taskList ? {} : false,
            listKeymap: basic ? {} : false,
            horizontalRule: basic ? {} : false,
            link: basic ? {
                openOnClick: false,
                autolink: true,
                defaultProtocol: 'https',
                HTMLAttributes: { rel: 'noopener noreferrer nofollow', target: null },
                isAllowedUri: (url, ctx) => ctx.defaultValidate(url) && isAllowedUrl(url),
                shouldAutoLink: url => isAllowedUrl(url)
            } : false
        }),
        tiptap.Extension.create({
            name: 'tabblazorUploads',
            addProseMirrorPlugins: () => [uploadsPlugin(tiptap)]
        }),
        tiptap.Extension.create({
            name: 'tabblazorShortcuts',
            addKeyboardShortcuts: () => ({
                'Mod-k': () => {
                    // Without a link button in the toolbar, leave Ctrl+K to the browser.
                    const button = state.root.querySelector('[data-rt-link-button]');
                    if (!button || button.disabled) {
                        return false;
                    }
                    openMenu(state, button);
                    return true;
                }
            })
        })
    ];

    if (state.options.format === 'markdown') {
        extensions.push(tiptap.Markdown);
    }
    if (on.image) {
        extensions.push(tiptap.Image.configure({ allowBase64: false, inline: false }));
    }
    if (on.table) {
        extensions.push(tiptap.TableKit.configure({ table: { resizable: false } }));
    }
    if (on.taskList) {
        extensions.push(tiptap.TaskList, tiptap.TaskItem.configure({ nested: true }));
    }
    if (on.textAlign) {
        extensions.push(tiptap.TextAlign.configure({ types: ['heading', 'paragraph'] }));
    }
    if (on.highlight) {
        extensions.push(tiptap.Highlight);
    }
    if (on.typography) {
        extensions.push(tiptap.Typography);
    }
    if (placeholder) {
        extensions.push(tiptap.Placeholder.configure({ placeholder }));
    }
    if (maxLength) {
        extensions.push(tiptap.CharacterCount.configure({ limit: maxLength }));
    }
    if (on.mention) {
        extensions.push(tiptap.Mention.configure({
            HTMLAttributes: { class: 'rich-text-mention' },
            suggestion: {
                char: state.options.mention.trigger || '@',
                minQueryLength: state.options.mention.minChars,
                debounce: state.options.mention.debounceMs,
                items: async ({ query }) => {
                    try {
                        return await state.dotnet.invokeMethodAsync('SearchMentionsAsync', query) ?? [];
                    } catch {
                        return [];
                    }
                },
                render: mentionRenderer(state)
            }
        }));
    }

    for (const name of state.options.customExtensions) {
        const factory = registry.get(name);
        if (!factory) {
            console.error(`RichTextEditor: no extension named "${name}" was registered. Add its module to TipTapOptions.ExtensionModules.`);
            continue;
        }
        try {
            const extension = factory(state.options.extensionOptions?.[name] ?? {});
            extensions.push(...(Array.isArray(extension) ? extension : [extension]));
        } catch (error) {
            console.error(`RichTextEditor: extension "${name}" failed to start.`, error);
        }
    }
    return extensions;
}

/** Upload placeholders as widget decorations: they move with the text and are never serialized. */
function uploadsPlugin(tiptap) {
    const key = uploadsPluginKey;
    return new tiptap.Plugin({
        key,
        state: {
            init: () => tiptap.DecorationSet.empty,
            apply(tr, set) {
                set = set.map(tr.mapping, tr.doc);
                const meta = tr.getMeta(key);
                if (meta?.add) {
                    const widgets = meta.add.map((item, index) => tiptap.Decoration.widget(meta.pos, () => placeholderElement(item.label),
                        { token: item.token, side: index + 1, key: `upload-${item.token}` }));
                    set = set.add(tr.doc, widgets);
                }
                if (meta?.remove) {
                    set = set.remove(set.find(undefined, undefined, spec => spec.token === meta.remove));
                }
                return set;
            }
        },
        props: {
            decorations(editorState) {
                return key.getState(editorState);
            }
        }
    });
}

function placeholderElement(label) {
    const element = document.createElement('span');
    element.className = 'rich-text-upload-placeholder';
    element.contentEditable = 'false';
    const spinner = document.createElement('span');
    spinner.className = 'spinner-border spinner-border-sm';
    spinner.setAttribute('aria-hidden', 'true');
    const text = document.createElement('span');
    text.textContent = label;
    element.append(spinner, text);
    return element;
}

// Value sync

/** TipTap's content type option: only markdown needs one (HTML strings and JSON objects are recognized). */
function contentType(state) {
    return state.options.format === 'markdown' ? 'markdown' : undefined;
}

function parse(state, value) {
    if (!value) {
        return '';
    }
    if (state.options.format !== 'json') {
        return value;
    }
    try {
        return JSON.parse(value);
    } catch {
        console.error('RichTextEditor: Value is not valid TipTap JSON; starting empty.');
        return '';
    }
}

function serialize(state) {
    const editor = state.editor;
    if (!editor || editor.isEmpty) {
        return '';
    }
    switch (state.options.format) {
        case 'json': return JSON.stringify(editor.getJSON());
        // TipTap leaves blank lines after the last block; they carry no content.
        case 'markdown': return editor.getMarkdown().replace(/\s+$/, '');
        default: return editor.getHTML();
    }
}

function schedule(state) {
    clearTimeout(state.timer);
    if (state.options.debounceMs > 0) {
        state.timer = setTimeout(() => commit(state), state.options.debounceMs);
    }
}

function flush(state) {
    clearTimeout(state.timer);
    commit(state);
}

function commit(state) {
    clearTimeout(state.timer);
    if (!state.editor) {
        return;
    }

    const value = serialize(state);
    if (value !== state.committed) {
        state.committed = value;
        state.hidden.value = value;
        state.hidden.dispatchEvent(new Event('change', { bubbles: true }));
    }
}

// Commands and toolbar

function run(state, command, args) {
    const editor = state.editor;
    if (!editor || !editor.isEditable || !command) {
        return false;
    }

    const chain = editor.chain().focus();
    if (typeof chain[command] !== 'function') {
        console.warn(`RichTextEditor: unknown command "${command}".`);
        return false;
    }
    const ran = (args === undefined || args === null ? chain[command]() : chain[command](args)).run();
    flush(state);
    return ran;
}

function parseArgs(text) {
    if (text === undefined || text === null || text === '') {
        return undefined;
    }
    try {
        return JSON.parse(text);
    } catch {
        return text;
    }
}

function onClick(state, event) {
    const target = event.target;
    if (!(target instanceof Element) || !state.root.contains(target)) {
        return;
    }

    const menuButton = target.closest('[data-rt-menu]');
    if (menuButton) {
        if (openDropdown(state) === menuButton.closest('.dropdown')) {
            closeMenu(state, false);
        } else {
            openMenu(state, menuButton, event.detail === 0);
        }
        return;
    }

    if (target.closest('[data-rt-link-apply]')) {
        applyLink(state);
        return;
    }
    if (target.closest('[data-rt-link-remove]')) {
        closeMenu(state, false);
        run(state, 'unsetLink');
        return;
    }
    if (target.closest('[data-rt-upload]')) {
        state.root.querySelector('input.rich-text-editor-file')?.click();
        return;
    }

    const button = target.closest('[data-rt-command]');
    if (!button || button.disabled || !button.closest('.rich-text-editor-toolbar')) {
        return;
    }
    closeMenu(state, false);
    run(state, button.dataset.rtCommand, parseArgs(button.dataset.rtArgs));
}

function scheduleToolbar(state) {
    cancelAnimationFrame(state.frame);
    state.frame = requestAnimationFrame(() => updateToolbar(state));
}

/** Pressed and disabled states, the text style label, the table tools and the character counter. */
function updateToolbar(state) {
    const editor = state.editor;
    if (!editor || editor.isDestroyed) {
        return;
    }

    const root = state.root;
    for (const element of root.querySelectorAll('.rich-text-editor-toolbar [data-rt-active]')) {
        let active = false;
        try {
            active = editor.isActive(...JSON.parse(element.dataset.rtActive));
        } catch {
            // Not valid JSON: leave it unpressed.
        }
        element.classList.toggle('active', active);
        element.setAttribute(element.getAttribute('role') === 'menuitemradio' ? 'aria-checked' : 'aria-pressed', active ? 'true' : 'false');
    }

    const label = root.querySelector('[data-rt-style-label]');
    if (label) {
        const active = root.querySelector('.rich-text-editor-style + .dropdown-menu .active');
        label.setAttribute('data-rt-text', active?.dataset.rtLabel ?? label.dataset.rtDefault ?? '');
    }

    const locked = state.options.locked || !editor.isEditable;
    for (const button of root.querySelectorAll('.rich-text-editor-toolbar [data-rt-command]')) {
        if (locked) {
            continue;
        }
        let can = false;
        try {
            const command = button.dataset.rtCommand;
            const args = parseArgs(button.dataset.rtArgs);
            const chain = editor.can().chain();
            can = typeof chain[command] === 'function' && (args === undefined ? chain[command]() : chain[command](args)).run();
        } catch {
            can = false;
        }
        if (button.disabled === can) {
            button.disabled = !can;
        }
    }

    const tableTools = root.querySelector('[data-rt-table-tools]');
    if (tableTools) {
        tableTools.hidden = !editor.isActive('table');
    }

    const counter = root.querySelector('[data-rt-count]');
    if (counter && state.options.maxLength) {
        const count = editor.storage.characterCount?.characters?.() ?? 0;
        counter.setAttribute('data-rt-count-text', format(state.options.texts.characterCount || '{0} / {1}', count, state.options.maxLength));
        counter.classList.toggle('text-danger', count >= state.options.maxLength);
    }
}

function format(template, ...values) {
    return template.replace(/\{(\d+)\}/g, (match, index) => values[Number(index)] ?? match);
}

// Links

function isAllowedUrl(url) {
    const value = String(url ?? '').trim();
    return value.length > 0 && (ALLOWED_SCHEMES.test(value) || !HAS_SCHEME.test(value));
}

function applyLink(state) {
    const input = state.root.querySelector('[data-rt-link-input]');
    const editor = state.editor;
    closeMenu(state, false);
    if (!input || !editor) {
        return;
    }

    let href = input.value.trim();
    if (!href) {
        run(state, 'unsetLink');
        return;
    }
    if (!HAS_SCHEME.test(href) && !href.startsWith('/') && !href.startsWith('#') && /^[^\s/]+\.[^\s/]+/.test(href)) {
        href = 'https://' + href;
    }
    if (!isAllowedUrl(href)) {
        input.classList.add('is-invalid');
        return;
    }

    const chain = editor.chain().focus();
    if (editor.state.selection.empty && !editor.isActive('link')) {
        chain.insertContent({ type: 'text', text: href, marks: [{ type: 'link', attrs: { href } }] }).run();
    } else {
        chain.extendMarkRange('link').setLink({ href }).run();
    }
    flush(state);
}

// Paste, drop and uploads

function hasUploads(state) {
    return state.options.uploads && !state.options.locked && !!state.root.querySelector('input.rich-text-editor-file');
}

function onPaste(state, event) {
    const data = event.clipboardData;
    if (!data) {
        return false;
    }

    const types = Array.from(data.types);
    if (hasUploads(state) && data.files.length > 0 && !types.includes('text/plain') && !types.includes('text/html')) {
        sendFiles(state, data.files);
        return true;
    }

    // Word and web pages paste images as data: URLs, which are never stored inline: upload them instead.
    const html = types.includes('text/html') ? data.getData('text/html') : '';
    if (html && /<img[^>]+src\s*=\s*["']?data:/i.test(html) && hasUploads(state)) {
        const files = dataImages(html);
        if (files.length > 0) {
            // After the paste itself, so the uploads go where the caret ends up.
            setTimeout(() => sendFiles(state, files), 0);
        }
    }
    return false;
}

function dataImages(html) {
    const files = [];
    const doc = new DOMParser().parseFromString(html, 'text/html');
    doc.querySelectorAll('img[src^="data:"]').forEach((img, index) => {
        const match = /^data:([^;,]+)(;base64)?,(.*)$/s.exec(img.getAttribute('src') ?? '');
        if (!match || !match[1].startsWith('image/') || !match[2]) {
            return;
        }
        try {
            const binary = atob(match[3]);
            const bytes = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++) {
                bytes[i] = binary.charCodeAt(i);
            }
            const extension = match[1].split('/')[1].replace('jpeg', 'jpg').replace(/\+.*/, '');
            files.push(new File([bytes], `pasted-image-${index + 1}.${extension}`, { type: match[1] }));
        } catch {
            // Not valid base64: skip it.
        }
    });
    return files;
}

function onDrop(state, view, event, moved) {
    const files = event.dataTransfer?.files;
    if (moved || !files || files.length === 0) {
        return false;
    }
    if (!hasUploads(state)) {
        event.preventDefault();
        return true;
    }

    event.preventDefault();
    const pos = view.posAtCoords({ left: event.clientX, top: event.clientY });
    if (pos) {
        state.editor.commands.setTextSelection(pos.pos);
    }
    sendFiles(state, files);
    return true;
}

/** Hands files to the hidden InputFile, so Blazor streams them like picked files. */
function sendFiles(state, files) {
    const input = state.root.querySelector('input.rich-text-editor-file');
    if (!input) {
        return;
    }

    const transfer = new DataTransfer();
    for (const file of files) {
        transfer.items.add(file);
    }
    input.files = transfer.files;
    input.dispatchEvent(new Event('change', { bubbles: true }));
}

// Mentions

function mentionRenderer(state) {
    return () => {
        let element = null;
        let unmount = null;
        let props = null;
        let items = [];
        let index = 0;

        const select = i => {
            const item = items[i];
            if (item) {
                props.command({ id: String(item.id), label: item.label });
            }
        };

        const render = () => {
            if (!element) {
                return;
            }
            element.replaceChildren();
            const dom = state.editor?.view.dom;
            if (items.length === 0) {
                if (props?.loading) {
                    const spinner = document.createElement('div');
                    spinner.className = 'dropdown-item disabled';
                    spinner.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';
                    element.append(spinner);
                } else {
                    const empty = document.createElement('div');
                    empty.className = 'dropdown-item disabled';
                    empty.textContent = state.options.texts.noMentionMatches || 'No matches';
                    element.append(empty);
                }
                dom?.removeAttribute('aria-activedescendant');
                return;
            }

            items.forEach((item, i) => {
                const option = document.createElement('button');
                option.type = 'button';
                // Utility classes only: the list is added to the page body, out of reach of the component's scoped CSS.
                option.className = 'dropdown-item d-flex align-items-center gap-2 rich-text-mention-option' + (i === index ? ' active' : '');
                option.id = `${state.root.id}-mention-${i}`;
                option.setAttribute('role', 'option');
                option.setAttribute('aria-selected', i === index ? 'true' : 'false');
                option.tabIndex = -1;
                if (item.avatarUrl) {
                    const avatar = document.createElement('img');
                    avatar.className = 'avatar avatar-xs rounded-circle';
                    avatar.alt = '';
                    avatar.src = item.avatarUrl;
                    option.append(avatar);
                }
                const text = document.createElement('span');
                text.className = 'd-flex flex-column lh-sm rich-text-mention-text';
                const label = document.createElement('span');
                label.textContent = item.label;
                text.append(label);
                if (item.description) {
                    const description = document.createElement('small');
                    description.className = 'text-secondary';
                    description.textContent = item.description;
                    text.append(description);
                }
                option.append(text);
                option.addEventListener('mousedown', event => {
                    event.preventDefault();
                    select(i);
                });
                element.append(option);
            });
            dom?.setAttribute('aria-activedescendant', `${state.root.id}-mention-${index}`);
            element.children[index]?.scrollIntoView?.({ block: 'nearest' });
        };

        return {
            onStart: next => {
                props = next;
                items = next.items ?? [];
                index = 0;
                element = document.createElement('div');
                element.className = 'dropdown-menu show rich-text-mention-menu';
                element.setAttribute('role', 'listbox');
                element.style.zIndex = '1080';
                element.style.maxHeight = '16rem';
                element.style.overflowY = 'auto';
                element.style.minWidth = '14rem';
                unmount = next.mount(element);
                render();
            },
            onUpdate: next => {
                props = next;
                if (!next.loading) {
                    items = next.items ?? [];
                    index = 0;
                }
                render();
            },
            onKeyDown: ({ event }) => {
                if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                    if (items.length > 0) {
                        index = (index + (event.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length;
                        render();
                    }
                    return true;
                }
                if (event.key === 'Enter' || event.key === 'Tab') {
                    if (items.length === 0) {
                        return false;
                    }
                    select(index);
                    return true;
                }
                return false;
            },
            onExit: () => {
                state.editor?.view.dom.removeAttribute('aria-activedescendant');
                unmount?.();
                unmount = null;
                element = null;
            }
        };
    };
}

// Toolbar menus (text style, link, table) and keyboard navigation

function listen(state, target, type, handler, capture = false) {
    target.addEventListener(type, handler, capture);
    state.cleanup.push(() => target.removeEventListener(type, handler, capture));
}

function openDropdown(state) {
    return state.root.querySelector('.rich-text-editor-toolbar .dropdown-menu.show')?.closest('.dropdown') ?? null;
}

function openMenu(state, toggle, focusFirst = false) {
    const dropdown = toggle.closest('.dropdown');
    if (!dropdown) {
        return;
    }

    closeMenu(state, false);
    const menu = dropdown.querySelector('.dropdown-menu');
    menu.classList.add('show');
    toggle.setAttribute('aria-expanded', 'true');

    const input = menu.querySelector('[data-rt-link-input]');
    if (input) {
        input.value = state.editor?.getAttributes('link').href ?? '';
        input.classList.remove('is-invalid');
        menu.querySelector('[data-rt-link-remove]')?.toggleAttribute('hidden', !state.editor?.isActive('link'));
        input.focus();
        input.select();
        return;
    }
    if (focusFirst) {
        menu.querySelector('[role^="menuitem"]')?.focus();
    }
}

function closeMenu(state, focusToggle) {
    const dropdown = openDropdown(state);
    if (!dropdown) {
        return;
    }

    const toggle = dropdown.querySelector('[data-rt-menu]');
    const menu = dropdown.querySelector('.dropdown-menu');
    menu.classList.remove('show');
    toggle.setAttribute('aria-expanded', 'false');
    resetTable(menu);
    if (focusToggle) {
        toggle.focus();
    }
}

function highlightTable(target) {
    const cell = target?.closest?.('.rich-text-editor-table-cell');
    const menu = cell?.closest('.rich-text-editor-table-menu');
    if (!menu) {
        return;
    }

    const columns = Number(cell.dataset.rtColumns);
    const rows = Number(cell.dataset.rtRows);
    for (const other of menu.querySelectorAll('.rich-text-editor-table-cell')) {
        other.classList.toggle('active', Number(other.dataset.rtColumns) <= columns && Number(other.dataset.rtRows) <= rows);
    }
    menu.querySelector('.rich-text-editor-table-size')?.setAttribute('data-rt-size', cell.getAttribute('aria-label'));
}

function resetTable(menu) {
    const size = menu.querySelector('.rich-text-editor-table-size');
    if (!size) {
        return;
    }
    menu.querySelectorAll('.rich-text-editor-table-cell.active').forEach(cell => cell.classList.remove('active'));
    size.setAttribute('data-rt-size', size.dataset.rtDefault ?? '');
}

function toolbarButtons(state) {
    return Array.from(state.root.querySelectorAll('.rich-text-editor-toolbar button'))
        .filter(button => !button.closest('.dropdown-menu') && !button.closest('[hidden]'));
}

function setupRoving(state) {
    const buttons = toolbarButtons(state);
    const enabled = buttons.filter(button => !button.disabled);
    if (enabled.length === 0) {
        return;
    }

    const current = enabled.find(button => button.getAttribute('tabindex') === '0') ?? enabled[0];
    for (const button of buttons) {
        const tabindex = button === current ? '0' : '-1';
        if (button.getAttribute('tabindex') !== tabindex) {
            button.setAttribute('tabindex', tabindex);
        }
    }
}

/** Alt+F10 moves to the toolbar, as in other rich text editors; Escape there returns to the text. */
function onEditorKeyDown(state, event) {
    if (event.key === 'F10' && event.altKey) {
        const buttons = toolbarButtons(state).filter(button => !button.disabled);
        (buttons.find(button => button.getAttribute('tabindex') === '0') ?? buttons[0])?.focus();
        return true;
    }
    return false;
}

function moveInMenu(menu, target, key) {
    const grid = menu.querySelector('.rich-text-editor-table-grid');
    if (grid && target.closest('.rich-text-editor-table-grid')) {
        const cells = Array.from(grid.querySelectorAll('[role="menuitem"]'));
        const width = Number(grid.dataset.rtColumns);
        const index = cells.indexOf(target);
        const column = index % width;
        let next = index;
        switch (key) {
            case 'ArrowDown': next = index + width < cells.length ? index + width : index; break;
            case 'ArrowUp': next = index - width >= 0 ? index - width : index; break;
            case 'ArrowRight': next = column < width - 1 ? index + 1 : index; break;
            case 'ArrowLeft': next = column > 0 ? index - 1 : index; break;
            default: return false;
        }
        cells[next].focus();
        return true;
    }

    if (key !== 'ArrowDown' && key !== 'ArrowUp') {
        return false;
    }
    const items = Array.from(menu.querySelectorAll('[role^="menuitem"]'));
    const index = items.indexOf(target);
    items[(index + (key === 'ArrowDown' ? 1 : -1) + items.length) % items.length]?.focus();
    return true;
}

function onToolbarKeyDown(state, event) {
    const target = event.target;
    if (!(target instanceof HTMLElement) || !target.closest('.rich-text-editor-toolbar')) {
        return;
    }

    const menu = target.closest('.dropdown-menu');
    if (menu) {
        if (target.matches('[data-rt-link-input]')) {
            if (event.key === 'Enter') {
                event.preventDefault();
                applyLink(state);
            } else if (event.key === 'Escape') {
                event.preventDefault();
                closeMenu(state, false);
                state.editor?.commands.focus();
            }
            return;
        }
        if (event.key === 'Escape') {
            event.preventDefault();
            closeMenu(state, true);
        } else if (event.key === 'Tab') {
            if (!menu.matches('.rich-text-editor-link-menu')) {
                closeMenu(state, false);
            }
        } else if (moveInMenu(menu, target, event.key)) {
            event.preventDefault();
        }
        return;
    }

    if (target.matches('[data-rt-menu]') && (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ')) {
        event.preventDefault();
        if (openDropdown(state) !== target.closest('.dropdown')) {
            openMenu(state, target, true);
        }
        return;
    }

    if (event.key === 'Escape') {
        closeMenu(state, false);
        state.editor?.commands.focus();
        return;
    }

    const buttons = toolbarButtons(state).filter(button => !button.disabled);
    const index = buttons.indexOf(target);
    if (index < 0) {
        return;
    }

    let next = -1;
    switch (event.key) {
        case 'ArrowRight': next = (index + 1) % buttons.length; break;
        case 'ArrowLeft': next = (index - 1 + buttons.length) % buttons.length; break;
        case 'Home': next = 0; break;
        case 'End': next = buttons.length - 1; break;
        default: return;
    }

    event.preventDefault();
    closeMenu(state, false);
    target.setAttribute('tabindex', '-1');
    buttons[next].setAttribute('tabindex', '0');
    buttons[next].focus();
}

function showMacShortcuts(root) {
    if (!isMac) {
        return;
    }
    for (const element of root.querySelectorAll('[aria-keyshortcuts]')) {
        element.setAttribute('aria-keyshortcuts', element.getAttribute('aria-keyshortcuts').replace('Control+', 'Meta+'));
        if (element.title) {
            element.title = element.title.replace('Ctrl+', '⌘').replace('Alt+', '⌥');
        }
    }
}
