// MarkdownEditor: text changes in the browser, value sync through DOM change events.
//
// The value reaches .NET only through `change` events dispatched on the textarea itself. Blazor attaches the
// textarea's value to such an event, so the re-render that follows does not overwrite newer keystrokes. Never
// push the value to .NET through a JS-to-.NET call instead.

const states = new WeakMap();
const isMac = /Mac|iPhone|iPad|iPod/.test(navigator.userAgentData?.platform ?? navigator.platform ?? navigator.userAgent);

export function attach(root, options) {
    if (!root || states.has(root)) {
        return;
    }

    const textarea = root.querySelector('.markdown-editor-input');
    if (!textarea) {
        return;
    }

    const state = { root, textarea, options: normalize(options), timer: 0, committed: textarea.value, cleanup: [], lastWidth: 0 };
    states.set(root, state);

    listen(state, textarea, 'input', () => {
        resize(state);
        schedule(state);
    });
    listen(state, textarea, 'change', () => {
        clearTimeout(state.timer);
        state.committed = textarea.value;
    });
    listen(state, textarea, 'keydown', event => onTextareaKeyDown(state, event));
    listen(state, textarea, 'paste', event => onPaste(state, event));
    listen(state, root, 'dragover', event => onDragOver(state, event));
    listen(state, root, 'dragleave', event => {
        if (!root.contains(event.relatedTarget)) {
            root.classList.remove('markdown-editor-dragover');
        }
    });
    listen(state, root, 'drop', event => onDrop(state, event));

    // Keep the focus (and the selection) in the textarea when a toolbar button is clicked with the mouse.
    listen(state, root, 'mousedown', event => {
        if (event.target.closest?.('[data-md-action], [data-md-menu]')) {
            event.preventDefault();
        }
    });
    // Send pending text before Blazor handles a view switch, so the preview shows it.
    listen(state, root, 'click', event => {
        if (event.target.closest?.('[data-md-mode]')) {
            closeMenu(state, false);
            flush(state);
        }
    }, true);
    listen(state, root, 'click', event => onClick(state, event));
    listen(state, root, 'keydown', event => onToolbarKeyDown(state, event));
    listen(state, document, 'pointerdown', event => {
        if (!openDropdown(state)?.contains(event.target)) {
            closeMenu(state, false);
        }
    });
    // The table picker highlights the size under the pointer or the focus.
    listen(state, root, 'pointerover', event => highlightTable(event.target));
    listen(state, root, 'focusin', event => highlightTable(event.target));

    if (typeof ResizeObserver !== 'undefined') {
        // Re-measure when the textarea becomes visible again or its width changes (lines re-wrap).
        const observer = new ResizeObserver(() => {
            if (textarea.clientWidth !== state.lastWidth) {
                state.lastWidth = textarea.clientWidth;
                resize(state);
            }
        });
        observer.observe(textarea);
        state.cleanup.push(() => observer.disconnect());
    }

    const toolbar = root.querySelector('.markdown-editor-toolbar');
    if (toolbar && typeof MutationObserver !== 'undefined') {
        const observer = new MutationObserver(() => setupRoving(state));
        observer.observe(toolbar, { subtree: true, childList: true, attributes: true, attributeFilter: ['disabled'] });
        state.cleanup.push(() => observer.disconnect());
    }

    applyOptions(state);
    showMacShortcuts(root);
    setupRoving(state);
    resize(state);
}

export function update(root, options) {
    const state = states.get(root);
    if (!state) {
        return;
    }

    state.options = normalize(options);
    applyOptions(state);
    setupRoving(state);
    resize(state);
}

/** Called after .NET set a new value: it is already in .NET, so it is not sent back. */
export function refresh(root) {
    const state = states.get(root);
    if (!state) {
        return;
    }

    clearTimeout(state.timer);
    state.committed = state.textarea.value;
    resize(state);
}

export function detach(root) {
    const state = states.get(root);
    if (!state) {
        return;
    }

    clearTimeout(state.timer);
    state.cleanup.forEach(dispose => dispose());
    states.delete(root);
}

export function focus(root) {
    states.get(root)?.textarea.focus();
}

/** Inserts text at the caret, replacing the selection, as one undoable edit. */
export function insert(root, text) {
    const state = states.get(root);
    if (!state || state.options.locked) {
        return;
    }

    const textarea = state.textarea;
    const start = textarea.selectionStart;
    replaceRange(state, start, textarea.selectionEnd, text, start + text.length);
    commit(state);
}

/** Replaces the placeholder of an upload. Returns false when the user deleted it. */
export function replace(root, token, markdown) {
    const state = states.get(root);
    if (!state) {
        return false;
    }

    const textarea = state.textarea;
    const pattern = new RegExp('!\\[(?:\\\\.|[^\\]\\\\\\n])*\\]\\(#upload-' + escapeRegExp(token) + '\\)');
    const match = pattern.exec(textarea.value);
    if (!match) {
        return false;
    }

    let start = match.index;
    let end = start + match[0].length;
    if (!markdown) {
        // Remove the line as well when the placeholder had it to itself.
        const value = textarea.value;
        const startsLine = start === 0 || value[start - 1] === '\n';
        if (startsLine && value[end] === '\n') {
            end++;
        } else if (start > 0 && value[start - 1] === '\n' && end === value.length) {
            start--;
        }
    }

    replaceRange(state, start, end, markdown, null, { keepSelection: true });
    commit(state);
    return true;
}

function normalize(options) {
    return {
        debounceMs: options?.debounceMs ?? 0,
        maxHeight: options?.maxHeight ?? null,
        locked: !!options?.locked,
        linkText: options?.linkText || 'link text',
        imageText: options?.imageText || 'image description',
        columnText: options?.columnText || 'Column {0}'
    };
}

function applyOptions(state) {
    state.textarea.style.maxHeight = state.options.maxHeight || '';
}

function listen(state, target, type, handler, capture = false) {
    target.addEventListener(type, handler, capture);
    state.cleanup.push(() => target.removeEventListener(type, handler, capture));
}

// Value sync

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
    const textarea = state.textarea;
    if (textarea.value !== state.committed) {
        state.committed = textarea.value;
        textarea.dispatchEvent(new Event('change', { bubbles: true }));
    }
}

// Auto-grow

function resize(state) {
    const textarea = state.textarea;
    if (!textarea.isConnected || textarea.offsetParent === null) {
        return;
    }

    const scroller = document.scrollingElement;
    const scrollTop = scroller?.scrollTop;
    textarea.style.height = 'auto';
    const borders = textarea.offsetHeight - textarea.clientHeight;
    textarea.style.height = (textarea.scrollHeight + borders) + 'px';
    textarea.style.overflowY = textarea.scrollHeight > textarea.clientHeight + 1 ? 'auto' : 'hidden';
    if (scroller && scrollTop !== undefined) {
        scroller.scrollTop = scrollTop;
    }
}

// Editing

/**
 * Replaces [start, end) with text. Uses execCommand so the browser's undo history keeps the edit; falls back to
 * setRangeText where that is not possible (no undo entry, but the text is right).
 */
function replaceRange(state, start, end, text, selectionStart, { selectionEnd = selectionStart, keepSelection = false } = {}) {
    const textarea = state.textarea;
    const before = { start: textarea.selectionStart, end: textarea.selectionEnd };
    const hadFocus = document.activeElement === textarea;
    const delta = text.length - (end - start);

    let done = false;
    if (hadFocus || !keepSelection) {
        textarea.focus();
        textarea.setSelectionRange(start, end);
        try {
            done = text
                ? document.execCommand('insertText', false, text)
                : start === end || document.execCommand('delete');
        } catch {
            done = false;
        }
        done = done && textarea.value.slice(start, start + text.length) === text;
    }

    if (!done) {
        textarea.setRangeText(text, start, end, keepSelection ? 'preserve' : 'end');
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    }

    if (keepSelection) {
        if (done) {
            textarea.setSelectionRange(shift(before.start, start, end, delta), shift(before.end, start, end, delta));
        }
    } else if (selectionStart !== null && selectionStart !== undefined) {
        textarea.setSelectionRange(selectionStart, selectionEnd ?? selectionStart);
    }
}

function shift(position, start, end, delta) {
    if (position <= start) {
        return position;
    }
    return position >= end ? position + delta : start;
}

function run(state, action, button) {
    if (state.options.locked) {
        return;
    }

    switch (action) {
        case 'table': table(state, Number(button?.dataset.mdColumns) || 2, Number(button?.dataset.mdRows) || 2); break;
        case 'bold': toggleWrap(state, '**'); break;
        case 'italic': toggleWrap(state, '_'); break;
        case 'code': code(state); break;
        case 'link': link(state, false); break;
        case 'image': link(state, true); break;
        case 'bulleted-list': prefixLines(state, 'bullet'); break;
        case 'numbered-list': prefixLines(state, 'number'); break;
        case 'quote': prefixLines(state, 'quote'); break;
        case 'h1': prefixLines(state, 'h1'); break;
        case 'h2': prefixLines(state, 'h2'); break;
        case 'h3': prefixLines(state, 'h3'); break;
        case 'paragraph': prefixLines(state, 'paragraph'); break;
        case 'upload':
            state.root.querySelector('input.markdown-editor-file')?.click();
            return;
        default:
            return;
    }
    commit(state);
}

function toggleWrap(state, marker) {
    const textarea = state.textarea;
    const value = textarea.value;
    let start = textarea.selectionStart;
    let end = textarea.selectionEnd;
    const m = marker.length;

    // **text** with only "text" selected: unwrap.
    if (value.slice(start - m, start) === marker && value.slice(end, end + m) === marker
        && !(marker === '_' && value.slice(start - 2, start) === '__')) {
        replaceRange(state, start - m, end + m, value.slice(start, end), start - m, { selectionEnd: end - m });
        return;
    }

    const selected = value.slice(start, end);
    if (selected.length >= 2 * m && selected.startsWith(marker) && selected.endsWith(marker)) {
        const inner = selected.slice(m, selected.length - m);
        replaceRange(state, start, end, inner, start, { selectionEnd: start + inner.length });
        return;
    }

    // Keep surrounding spaces outside the markers: "**word** " rather than "**word **".
    start += selected.length - selected.trimStart().length;
    end -= selected.length - selected.trimEnd().length;
    if (end <= start) {
        start = end = textarea.selectionStart;
        replaceRange(state, start, textarea.selectionEnd, marker + marker, start + m);
        return;
    }

    const inner = value.slice(start, end);
    replaceRange(state, start, end, marker + inner + marker, start + m, { selectionEnd: end + m });
}

function code(state) {
    const textarea = state.textarea;
    const value = textarea.value;
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const selected = value.slice(start, end);
    const lineStart = value.lastIndexOf('\n', start - 1) + 1;
    const lineEnd = indexOrLength(value, '\n', end);
    const lineIsEmpty = value.slice(lineStart, lineEnd).trim() === '';

    if (!selected.includes('\n') && !(start === end && lineIsEmpty)) {
        toggleWrap(state, '`');
        return;
    }

    const before = start > 0 && value[start - 1] !== '\n' ? '\n' : '';
    const after = end < value.length && value[end] !== '\n' ? '\n' : '';
    const block = `${before}\`\`\`\n${selected}\n\`\`\`${after}`;
    const innerStart = start + before.length + 4;
    replaceRange(state, start, end, block, innerStart, { selectionEnd: innerStart + selected.length });
}

function link(state, image) {
    const textarea = state.textarea;
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const selected = textarea.value.slice(start, end);
    const bang = image ? '!' : '';
    const placeholder = image ? state.options.imageText : state.options.linkText;

    if (/^(https?:\/\/|mailto:)\S+$/i.test(selected)) {
        const text = `${bang}[${placeholder}](${selected})`;
        const textStart = start + bang.length + 1;
        replaceRange(state, start, end, text, textStart, { selectionEnd: textStart + placeholder.length });
        return;
    }

    const label = selected.replace(/\n/g, ' ') || placeholder;
    const url = 'https://';
    const text = `${bang}[${label}](${url})`;
    if (selected) {
        const urlStart = start + bang.length + label.length + 3;
        replaceRange(state, start, end, text, urlStart, { selectionEnd: urlStart + url.length });
    } else {
        const textStart = start + bang.length + 1;
        replaceRange(state, start, end, text, textStart, { selectionEnd: textStart + label.length });
    }
}

/**
 * Inserts a table with a header row and rows - 1 empty rows on lines of its own, below the caret's line when that
 * has text, and selects the first header so it can be typed over.
 */
function table(state, columns, rows) {
    const textarea = state.textarea;
    const value = textarea.value;
    const end = textarea.selectionEnd;
    const lineStart = value.lastIndexOf('\n', end - 1) + 1;
    const lineEnd = indexOrLength(value, '\n', end);
    const at = value.slice(lineStart, lineEnd).trim() === '' ? lineStart : lineEnd;

    const headers = Array.from({ length: columns }, (_, i) => state.options.columnText.replace('{0}', String(i + 1)));
    const widths = headers.map(header => Math.max(3, header.length));
    const row = cells => '| ' + cells.map((cell, i) => cell.padEnd(widths[i])).join(' | ') + ' |';
    const lines = [row(headers), row(widths.map(width => '-'.repeat(width)))];
    for (let i = 1; i < rows; i++) {
        lines.push(row(headers.map(() => '')));
    }

    // A blank line before and after, so the table does not run into a paragraph.
    const head = value.slice(0, at);
    const tail = value.slice(lineEnd);
    const before = at === 0 || head.endsWith('\n\n') ? '' : head.endsWith('\n') ? '\n' : '\n\n';
    const after = tail === '' || tail.startsWith('\n\n') ? '' : tail.startsWith('\n') ? '\n' : '\n\n';
    const text = before + lines.join('\n') + after;
    const headerStart = at + before.length + 2;
    replaceRange(state, at, lineEnd, text, headerStart, { selectionEnd: headerStart + headers[0].length });
}

/** Enter at the end of a table row adds an empty row; Enter on an empty row ends the table. */
function continueTable(state, event, value, lineStart, lineEnd) {
    const line = value.slice(lineStart, lineEnd);
    const trimmed = line.trim();
    if (!trimmed.startsWith('|') || !trimmed.endsWith('|') || trimmed.length < 2
        || value.slice(state.textarea.selectionStart, lineEnd).trim() !== '') {
        return false;
    }

    const cells = trimmed.slice(1, -1).split(/(?<!\\)\|/);
    event.preventDefault();
    if (cells.every(cell => cell.trim() === '')) {
        replaceRange(state, lineStart, lineEnd, '', lineStart);
        return true;
    }

    const indent = /^\s*/.exec(line)[0];
    const text = '\n' + indent + '|' + cells.map(cell => ' '.repeat(Math.max(2, cell.length))).join('|') + '|';
    replaceRange(state, lineEnd, lineEnd, text, lineEnd + 1 + indent.length + 2);
    return true;
}

const LINE_PREFIXES = {
    bullet: /^(\s*)[-*+]\s+(\[[ xX]\]\s+)?/,
    number: /^(\s*)\d+[.)]\s+/,
    quote: /^>\s?/,
    heading: /^#{1,6}\s+/
};

function prefixLines(state, kind) {
    const textarea = state.textarea;
    const value = textarea.value;
    const selectionStart = textarea.selectionStart;
    const selectionEnd = textarea.selectionEnd;
    const start = value.lastIndexOf('\n', selectionStart - 1) + 1;
    // A selection ending right after a line break does not include the next line.
    const endFrom = selectionEnd > selectionStart && value[selectionEnd - 1] === '\n' ? selectionEnd - 1 : selectionEnd;
    const end = indexOrLength(value, '\n', endFrom);
    const original = value.slice(start, end);
    const lines = original.split('\n');
    const content = lines.filter(line => line.trim() !== '');
    const multiLine = lines.length > 1;

    let next;
    if (kind === 'bullet' || kind === 'number' || kind === 'quote') {
        const pattern = LINE_PREFIXES[kind];
        const remove = content.length > 0 && content.every(line => pattern.test(line));
        let number = 0;
        next = lines.map(line => {
            if (remove) {
                return line.replace(pattern, kind === 'quote' ? '' : '$1');
            }
            if (multiLine && line.trim() === '') {
                return line;
            }
            if (kind === 'quote') {
                return '> ' + line;
            }
            const indent = /^\s*/.exec(line)[0];
            const text = line.slice(indent.length).replace(LINE_PREFIXES.bullet, '').replace(LINE_PREFIXES.number, '');
            number++;
            return indent + (kind === 'bullet' ? '- ' : `${number}. `) + text;
        });
    } else {
        const level = kind === 'paragraph' ? 0 : Number(kind.slice(1));
        const marker = '#'.repeat(level) + ' ';
        const remove = level > 0 && content.length > 0 && content.every(line => line.startsWith(marker) && !line.startsWith(marker.trim() + '#'));
        next = lines.map(line => {
            if (multiLine && line.trim() === '') {
                return line;
            }
            const text = line.replace(LINE_PREFIXES.heading, '');
            return remove || level === 0 ? text : marker + text;
        });
    }

    const replacement = next.join('\n');
    if (replacement === original) {
        return;
    }

    if (selectionStart === selectionEnd && !multiLine) {
        // Keep the caret on the same character.
        const caret = Math.max(start, Math.min(start + replacement.length, selectionStart + replacement.length - original.length));
        replaceRange(state, start, end, replacement, caret);
    } else {
        replaceRange(state, start, end, replacement, start, { selectionEnd: start + replacement.length });
    }
}

/** Enter in a list or quote continues it; Enter on an empty item ends it. */
function continueBlock(state, event) {
    const textarea = state.textarea;
    const start = textarea.selectionStart;
    if (start !== textarea.selectionEnd) {
        return;
    }

    const value = textarea.value;
    const lineStart = value.lastIndexOf('\n', start - 1) + 1;
    const lineEnd = indexOrLength(value, '\n', start);
    const line = value.slice(lineStart, lineEnd);
    if (continueTable(state, event, value, lineStart, lineEnd)) {
        return;
    }

    const list =/^(\s*)(?:([-*+])|(\d+)([.)]))\s+(\[[ xX]\]\s+)?/.exec(line);
    const quote = list ? null : /^(\s*>\s?)+/.exec(line);
    const prefix = list ?? quote;
    if (!prefix || start - lineStart < prefix[0].length) {
        return;
    }

    event.preventDefault();
    if (line.slice(prefix[0].length).trim() === '') {
        // Empty item: end the list or quote.
        replaceRange(state, lineStart, lineStart + prefix[0].length, '', lineStart);
        return;
    }

    let marker;
    if (quote) {
        marker = quote[0].endsWith(' ') ? quote[0] : quote[0] + ' ';
    } else {
        const indent = list[1];
        const bullet = list[2] ? `${list[2]} ` : `${Number(list[3]) + 1}${list[4]} `;
        marker = indent + bullet + (list[5] ? '[ ] ' : '');
    }

    const text = '\n' + marker;
    replaceRange(state, start, start, text, start + text.length);
}

// Events

function onTextareaKeyDown(state, event) {
    if (state.options.locked || event.isComposing) {
        return;
    }

    if (event.key === 'Enter' && !event.shiftKey && !event.ctrlKey && !event.metaKey && !event.altKey) {
        continueBlock(state, event);
        return;
    }

    const modifier = isMac ? event.metaKey && !event.ctrlKey : event.ctrlKey && !event.metaKey;
    if (!modifier || event.altKey) {
        return;
    }

    let action = null;
    if (!event.shiftKey) {
        action = { b: 'bold', i: 'italic', k: 'link', e: 'code' }[event.key.toLowerCase()] ?? null;
    } else {
        action = { Digit8: 'bulleted-list', Digit7: 'numbered-list', Period: 'quote' }[event.code] ?? null;
    }

    if (action) {
        event.preventDefault();
        run(state, action);
    }
}

function hasFileInput(state) {
    return !state.options.locked && !!state.root.querySelector('input.markdown-editor-file');
}

function onPaste(state, event) {
    const data = event.clipboardData;
    if (!hasFileInput(state) || !data || data.files.length === 0) {
        return;
    }

    // Spreadsheets and word processors put both text and a picture on the clipboard: paste the text.
    if (Array.from(data.types).includes('text/plain')) {
        return;
    }

    event.preventDefault();
    sendFiles(state, data.files);
}

function hasFiles(event) {
    return Array.from(event.dataTransfer?.types ?? []).includes('Files');
}

function onDragOver(state, event) {
    if (!hasFileInput(state) || !hasFiles(event)) {
        return;
    }

    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
    state.root.classList.add('markdown-editor-dragover');
}

function onDrop(state, event) {
    state.root.classList.remove('markdown-editor-dragover');
    if (!hasFileInput(state) || !hasFiles(event) || event.dataTransfer.files.length === 0) {
        return;
    }

    event.preventDefault();
    state.textarea.focus();
    sendFiles(state, event.dataTransfer.files);
}

/** Hands files to the hidden InputFile, so Blazor streams them like picked files. */
function sendFiles(state, files) {
    const input = state.root.querySelector('input.markdown-editor-file');
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

function onClick(state, event) {
    const menuButton = event.target.closest?.('[data-md-menu]');
    if (menuButton && state.root.contains(menuButton)) {
        toggleMenu(state, menuButton, event.detail === 0);
        return;
    }

    const button = event.target.closest?.('[data-md-action]');
    if (!button || !state.root.contains(button) || button.disabled) {
        return;
    }

    closeMenu(state, false);
    run(state, button.dataset.mdAction, button);
}

// Toolbar: dropdown menus (heading, table) and arrow-key navigation

function menuParts(dropdown) {
    return { toggle: dropdown.querySelector('[data-md-menu]'), menu: dropdown.querySelector('.dropdown-menu') };
}

function openDropdown(state) {
    return state.root.querySelector('.markdown-editor-toolbar .dropdown-menu.show')?.closest('.dropdown') ?? null;
}

function toggleMenu(state, toggle, focusFirst) {
    const dropdown = toggle.closest('.dropdown');
    if (!dropdown) {
        return;
    }

    const wasOpen = openDropdown(state) === dropdown;
    closeMenu(state, false);
    if (wasOpen) {
        return;
    }

    const { menu } = menuParts(dropdown);
    menu.classList.add('show');
    toggle.setAttribute('aria-expanded', 'true');
    if (focusFirst) {
        menu.querySelector('[role="menuitem"]')?.focus();
    }
}

function closeMenu(state, focusToggle) {
    const dropdown = openDropdown(state);
    if (!dropdown) {
        return;
    }

    const { toggle, menu } = menuParts(dropdown);
    menu.classList.remove('show');
    toggle.setAttribute('aria-expanded', 'false');
    resetTable(menu);
    if (focusToggle) {
        toggle.focus();
    }
}

/** Marks the cells of the table size under the target, and names the size below the grid. */
function highlightTable(target) {
    const cell = target?.closest?.('.markdown-editor-table-cell');
    const menu = cell?.closest('.markdown-editor-table-menu');
    if (!menu) {
        return;
    }

    const columns = Number(cell.dataset.mdColumns);
    const rows = Number(cell.dataset.mdRows);
    for (const other of menu.querySelectorAll('.markdown-editor-table-cell')) {
        other.classList.toggle('active', Number(other.dataset.mdColumns) <= columns && Number(other.dataset.mdRows) <= rows);
    }
    // The caption is CSS content from an attribute: Blazor owns the element's text node.
    menu.querySelector('.markdown-editor-table-size')?.setAttribute('data-md-size', cell.getAttribute('aria-label'));
}

function resetTable(menu) {
    const size = menu.querySelector('.markdown-editor-table-size');
    if (!size) {
        return;
    }

    menu.querySelectorAll('.markdown-editor-table-cell.active').forEach(cell => cell.classList.remove('active'));
    size.setAttribute('data-md-size', size.dataset.mdDefault ?? '');
}

/** Arrow keys in a menu: up and down in a list, in all four directions in the table grid. */
function moveInMenu(menu, target, key) {
    const items = Array.from(menu.querySelectorAll('[role="menuitem"]'));
    const index = items.indexOf(target);
    const grid = target.closest('.markdown-editor-table-grid');
    const width = grid ? Number(grid.dataset.mdColumns) : 1;
    const column = index % width;
    let next;
    switch (key) {
        case 'ArrowDown': next = grid ? Math.min(index + width, items.length - 1 - (width - 1 - column)) : (index + 1) % items.length; break;
        case 'ArrowUp': next = grid ? Math.max(index - width, column) : (index - 1 + items.length) % items.length; break;
        case 'ArrowRight': next = grid && column < width - 1 ? index + 1 : index; break;
        case 'ArrowLeft': next = grid && column > 0 ? index - 1 : index; break;
        default: return false;
    }

    items[next].focus();
    return true;
}

function toolbarButtons(state) {
    return Array.from(state.root.querySelectorAll('.markdown-editor-toolbar button'))
        .filter(button => !button.closest('.dropdown-menu'));
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

function onToolbarKeyDown(state, event) {
    const target = event.target;
    if (!(target instanceof HTMLElement) || !target.closest('.markdown-editor-toolbar')) {
        return;
    }

    const menu = target.closest('.dropdown-menu');
    if (menu) {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeMenu(state, true);
        } else if (event.key === 'Tab') {
            closeMenu(state, false);
        } else if (moveInMenu(menu, target, event.key)) {
            event.preventDefault();
        }
        return;
    }

    if (target.matches('[data-md-menu]') && (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ')) {
        event.preventDefault();
        if (openDropdown(state) !== target.closest('.dropdown')) {
            toggleMenu(state, target, true);
        }
        return;
    }

    if (event.key === 'Escape') {
        closeMenu(state, false);
        state.textarea.focus();
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
            element.title = element.title.replace('Ctrl+', '⌘');
        }
    }
}

// Helpers

function indexOrLength(value, search, from) {
    const index = value.indexOf(search, from);
    return index < 0 ? value.length : index;
}

function escapeRegExp(text) {
    return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
