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

    const highlight = root.querySelector('.markdown-editor-highlight');
    const state = { root, textarea, highlight, options: normalize(options), timer: 0, committed: textarea.value, cleanup: [], lastWidth: 0 };
    states.set(root, state);

    listen(state, textarea, 'input', () => {
        paint(state);
        resize(state);
        schedule(state);
    });
    listen(state, textarea, 'scroll', () => syncScroll(state));
    listen(state, textarea, 'change', () => {
        clearTimeout(state.timer);
        state.committed = textarea.value;
    });
    listen(state, textarea, 'keydown', event => onTextareaKeyDown(state, event));
    // The table button becomes "Edit table" while the caret is in a table.
    for (const type of ['keyup', 'mouseup', 'input', 'focus', 'select']) {
        listen(state, textarea, type, () => scheduleTableButton(state));
    }
    listen(state, document, 'selectionchange', () => {
        if (document.activeElement === textarea) {
            scheduleTableButton(state);
        }
    });
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
        if (event.target.closest?.('[data-md-action], [data-md-menu], [data-md-close-menu]')) {
            event.preventDefault();
        }
    });
    // The table dialog: spreadsheet paste, Enter to move down, Ctrl+Enter to save.
    listen(state, root, 'paste', event => onTableDialogPaste(event));
    listen(state, root, 'keydown', event => onTableDialogKeyDown(event));
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
    updateTableButton(state);
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
    paint(state);
    resize(state);
    updateTableButton(state);
}

export function detach(root) {
    const state = states.get(root);
    if (!state) {
        return;
    }

    clearTimeout(state.timer);
    cancelAnimationFrame(state.tableFrame);
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

/** The table at the caret for the table dialog, or where a new one goes when the caret is not in a table. */
export function readTable(root) {
    const state = states.get(root);
    if (!state) {
        return null;
    }

    const textarea = state.textarea;
    const value = textarea.value;
    const table = findTable(value, textarea.selectionStart);
    if (!table) {
        return { found: false, start: textarea.selectionStart, end: textarea.selectionEnd, text: null, indent: '', rows: [] };
    }
    return { found: true, start: table.start, end: table.end, text: value.slice(table.start, table.end), indent: table.indent, rows: table.rows };
}

/**
 * Writes the table from the dialog back, lined up, as one undoable edit, and puts the caret after it. When the text
 * around it changed while the dialog was open, it finds the table by its old text; when it cannot, it inserts the
 * table as a new one rather than overwrite something else.
 */
export function writeTable(root, table) {
    const state = states.get(root);
    if (!state || state.options.locked || !table?.rows?.length) {
        return;
    }

    const value = state.textarea.value;
    const columns = Math.max(...table.rows.map(row => row.length));
    const text = formatTable(table.rows, columns, table.found ? table.indent ?? '' : '').lines.join('\n');
    let start = table.found ? table.start : -1;
    if (table.found && value.slice(table.start, table.end) !== table.text) {
        const at = value.indexOf(table.text);
        start = at >= 0 && value.indexOf(table.text, at + 1) < 0 ? at : -1;
    }

    if (start >= 0) {
        const end = start + table.text.length;
        replaceRange(state, start, end, text, start + text.length);
    } else {
        insertBlock(state, text.split('\n'), Math.min(table.start, value.length), (blockStart, block) => [blockStart + block.length, blockStart + block.length]);
    }
    commit(state);
}

/** Focuses the first cell of the table dialog once the dialog has taken the focus itself. */
export function focusTableEditor(root) {
    setTimeout(() => {
        const input = root.querySelector('[data-md-table-editor] input[data-md-row]');
        input?.focus();
        input?.select();
    }, 50);
}

/**
 * Spreadsheet cells pasted into a dialog cell go to .NET through the hidden input as JSON: an input's value cannot
 * hold line breaks, JSON escapes them.
 */
function onTableDialogPaste(event) {
    const input = event.target.closest?.('[data-md-table-editor] input[data-md-row]');
    const text = event.clipboardData?.getData('text/plain') ?? '';
    // A single copied cell comes with a trailing line break: paste that as usual.
    if (!input || !/[\t\n]/.test(text.replace(/\r?\n$/, ''))) {
        return;
    }

    event.preventDefault();
    const hidden = input.closest('[data-md-table-editor]').querySelector('[data-md-table-paste]');
    hidden.value = JSON.stringify({ row: Number(input.dataset.mdRow), column: Number(input.dataset.mdColumn), text });
    hidden.dispatchEvent(new Event('change', { bubbles: true }));
}

/** Enter moves to the cell below (adding a row at the end); Ctrl+Enter saves. */
function onTableDialogKeyDown(event) {
    const input = event.target.closest?.('[data-md-table-editor] input[data-md-row]');
    if (!input || event.key !== 'Enter' || event.isComposing || event.altKey || event.shiftKey) {
        return;
    }

    event.preventDefault();
    const editor = input.closest('[data-md-table-editor]');
    if (event.ctrlKey || event.metaKey) {
        // Commit the cell first: clicking from script does not take the focus from it.
        input.dispatchEvent(new Event('change', { bubbles: true }));
        editor.querySelector('[data-md-table-save]')?.click();
        return;
    }

    const below = `input[data-md-row="${Number(input.dataset.mdRow) + 1}"][data-md-column="${input.dataset.mdColumn}"]`;
    const next = editor.querySelector(below);
    if (next) {
        next.focus();
        next.select();
        return;
    }

    input.dispatchEvent(new Event('change', { bubbles: true }));
    editor.querySelector('[data-md-table-add-row]')?.click();
    let frames = 0;
    const focusNew = () => {
        const added = editor.querySelector(below);
        if (added) {
            added.focus();
        } else if (frames++ < 60) {
            requestAnimationFrame(focusNew);
        }
    };
    requestAnimationFrame(focusNew);
}

function normalize(options) {
    return {
        debounceMs: options?.debounceMs ?? 0,
        maxHeight: options?.maxHeight ?? null,
        locked: !!options?.locked,
        linkText: options?.linkText || 'link text',
        imageText: options?.imageText || 'image description',
        columnText: options?.columnText || 'Column {0}',
        hybrid: !!options?.hybrid
    };
}

function applyOptions(state) {
    state.textarea.style.maxHeight = state.options.maxHeight || '';
    paint(state);
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
    // A scroll bar appearing or going away changes where lines wrap.
    fitHighlight(state);
}

// Hybrid mode: a styled copy of the text sits under the text area, whose own text is transparent. Typing, the
// selection, undo and every editing feature stay the text area's; the copy only has to wrap exactly like it, so it
// takes the text area's font and padding and never changes the size of a character.

const HIGHLIGHT_STYLES = ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 'letterSpacing', 'wordSpacing', 'tabSize',
    'textIndent', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft', 'borderTopWidth', 'borderRightWidth',
    'borderBottomWidth', 'borderLeftWidth'];

function paint(state) {
    const highlight = state.highlight;
    if (!highlight) {
        return;
    }

    if (!state.options.hybrid) {
        if (highlight.firstChild) {
            highlight.replaceChildren();
        }
        return;
    }

    const value = state.textarea.value;
    // A trailing line break needs something after it, or the copy is a line shorter than the text area.
    highlight.innerHTML = highlightMarkdown(value) + (value.endsWith('\n') || value === '' ? ' ' : '');
    fitHighlight(state);
}

function fitHighlight(state) {
    const { highlight, textarea } = state;
    if (!highlight || !state.options.hybrid || textarea.offsetParent === null) {
        return;
    }

    const style = getComputedStyle(textarea);
    for (const name of HIGHLIGHT_STYLES) {
        highlight.style[name] = style[name];
    }
    highlight.style.borderStyle = 'solid';
    highlight.style.borderColor = 'transparent';
    // The text area's scroll bar narrows its text: narrow the copy's text by as much.
    const scrollbar = textarea.offsetWidth - textarea.clientWidth - parseFloat(style.borderLeftWidth) - parseFloat(style.borderRightWidth);
    highlight.style.paddingRight = `${parseFloat(style.paddingRight) + Math.max(0, scrollbar)}px`;
    syncScroll(state);
}

function syncScroll(state) {
    if (state.highlight && state.options.hybrid) {
        state.highlight.scrollTop = state.textarea.scrollTop;
        state.highlight.scrollLeft = state.textarea.scrollLeft;
    }
}

/** Markdown to HTML spans that keep every character of the source, so the copy lines up with the text area. */
export function highlightMarkdown(value) {
    const lines = value.split('\n');
    const out = [];
    let fence = null;
    let tableRow = -1;
    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        if (fence) {
            const closes = new RegExp(`^\\s{0,3}${fence[0]}{${fence.length},}\\s*$`).test(line);
            out.push(span('md-code-block' + (closes ? ' md-fence' : ''), escapeHtml(line)));
            if (closes) {
                fence = null;
            }
            continue;
        }

        const opens = /^\s{0,3}(`{3,}|~{3,})/.exec(line);
        if (opens && !(opens[1][0] === '`' && line.slice(opens[0].length).includes('`'))) {
            fence = opens[1];
            out.push(span('md-code-block md-fence', escapeHtml(line)));
            continue;
        }

        // Table rows get a background (only behind the text, so aligned columns read as a grid): the header
        // row, the line above a separator row, stands out; body rows are striped. The outer pipes are optional.
        if (TABLE_LINE.test(line)) {
            let kind;
            if (tableRow < 0 && TABLE_SEPARATOR.test(lines[i + 1] ?? '')) {
                kind = 'md-table-header';
                tableRow = -2;
            } else if (tableRow === -2) {
                kind = 'md-table-separator';
                tableRow = 0;
            } else if (tableRow >= 0) {
                kind = tableRow++ % 2 ? 'md-table-row md-table-even' : 'md-table-row';
            }
            if (kind) {
                out.push(span(kind, highlightTableRow(line, kind === 'md-table-separator')));
                continue;
            }
        } else {
            tableRow = -1;
        }

        out.push(highlightLine(line));
    }
    return out.join('\n');
}

// A table line has a pipe that is not escaped; with or without the outer pipes. Whether it is in a table depends on
// a separator row below the header.
const TABLE_LINE = /(^|[^\\])\|/;
const TABLE_SEPARATOR = /^\s*\|?(\s*:?-+:?\s*\|)+\s*(:?-+:?)?\s*$/;

function highlightTableRow(line, separator) {
    if (separator) {
        return mark(line);
    }
    let html = '';
    let position = 0;
    for (const cell of parseRow(line)) {
        html += mark(line.slice(position, cell.start)) + highlightInline(line.slice(cell.start, cell.end));
        position = cell.end;
    }
    return html + mark(line.slice(position));
}

function highlightLine(line) {
    let m;
    if ((m = /^(\s{0,3})(#{1,6})(\s+|$)(.*)$/.exec(line))) {
        return span(`md-heading md-h${m[2].length}`, escapeHtml(m[1]) + mark(m[2] + m[3]) + highlightInline(m[4]));
    }
    if (/^\s{0,3}([-*_])(\s*\1){2,}\s*$/.test(line)) {
        return mark(line);
    }
    if ((m = /^(\s*)((?:>\s?)+)(.*)$/.exec(line))) {
        return span('md-quote', escapeHtml(m[1]) + mark(m[2]) + highlightLine(m[3]));
    }
    if ((m = /^(\s*)([-*+]|\d{1,9}[.)])(\s+)(\[[ xX]\](?=\s|$))?(.*)$/.exec(line))) {
        const task = m[4] ? span('md-task', escapeHtml(m[4])) : '';
        const rest = m[5];
        return escapeHtml(m[1]) + span('md-list', escapeHtml(m[2])) + escapeHtml(m[3]) + task
            + (m[4] && /x/i.test(m[4]) ? span('md-done', highlightInline(rest)) : highlightInline(rest));
    }
    return highlightInline(line);
}

const INLINE = new RegExp([
    /(`+)([^`]|[^`][\s\S]*?[^`])\1(?!`)/.source,                       // 1, 2: code
    /(!?)\[((?:\\.|[^\]\\])*)\]\(([^()\s]*(?:\s+"[^"]*")?)\)/.source,   // 3, 4, 5: link or image
    /<((?:https?:\/\/|mailto:)[^>\s]+)>/.source,                        // 6: autolink
    /\b(https?:\/\/[^\s<]*[^\s<.,:;"')\]])/.source,                     // 7: bare URL
    /(\*\*|__)(?=\S)([\s\S]*?\S)\8/.source,                             // 8, 9: bold
    /(\*|_)(?=[^\s*_])([\s\S]*?[^\s\\])\10/.source,                     // 10, 11: italic
    /~~(?=\S)([\s\S]*?\S)~~/.source,                                    // 12: strikethrough
    /\\([\\`*_{}[\]()#+\-.!|~<>])/.source                               // 13: escape
].join('|'), 'g');

function highlightInline(text) {
    const pattern = new RegExp(INLINE.source, 'g');
    let out = '';
    let position = 0;
    let m;
    while ((m = pattern.exec(text))) {
        const html = inlineToken(m, text);
        if (html === null) {
            pattern.lastIndex = m.index + 1;
            continue;
        }
        out += escapeHtml(text.slice(position, m.index)) + html;
        position = pattern.lastIndex = m.index + m[0].length;
    }
    return out + escapeHtml(text.slice(position));
}

function inlineToken(m, text) {
    if (m[1] !== undefined) {
        return mark(m[1]) + span('md-code', escapeHtml(m[2])) + mark(m[1]);
    }
    if (m[4] !== undefined) {
        const label = span(m[3] ? 'md-image' : 'md-link', highlightInline(m[4]));
        return mark(`${m[3]}[`) + label + mark(`](${m[5]})`);
    }
    if (m[6] !== undefined) {
        return mark('<') + span('md-link', escapeHtml(m[6])) + mark('>');
    }
    if (m[7] !== undefined) {
        return span('md-link', escapeHtml(m[7]));
    }
    // Underscores inside a word are not emphasis: snake_case_names.
    const underscore = (m[8] ?? m[10]) === '_' || m[8] === '__';
    if (underscore && (/\w/.test(text[m.index - 1] ?? '') || /\w/.test(text[m.index + m[0].length] ?? ''))) {
        return null;
    }
    if (m[8] !== undefined) {
        return mark(m[8]) + span('md-bold', highlightInline(m[9])) + mark(m[8]);
    }
    if (m[10] !== undefined) {
        return mark(m[10]) + span('md-italic', highlightInline(m[11])) + mark(m[10]);
    }
    if (m[12] !== undefined) {
        return mark('~~') + span('md-strike', highlightInline(m[12])) + mark('~~');
    }
    return mark('\\') + escapeHtml(m[13]);
}

function span(className, html) {
    return html ? `<span class="${className}">${html}</span>` : '';
}

function mark(text) {
    return span('md-mark', escapeHtml(text));
}

function escapeHtml(text) {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
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
    const headers = Array.from({ length: columns }, (_, i) => state.options.columnText.replace('{0}', String(i + 1)));
    const widths = headers.map(header => Math.max(3, header.length));
    const row = cells => '| ' + cells.map((cell, i) => cell.padEnd(widths[i])).join(' | ') + ' |';
    const lines = [row(headers), row(widths.map(width => '-'.repeat(width)))];
    for (let i = 1; i < rows; i++) {
        lines.push(row(headers.map(() => '')));
    }

    insertBlock(state, lines, state.textarea.selectionEnd, blockStart => [blockStart + 2, blockStart + 2 + headers[0].length]);
}

/**
 * Inserts lines as a block of their own at position: below its line when that has text, with a blank line before and
 * after so the block does not run into a paragraph. selection maps where the block starts to the selection to set.
 */
function insertBlock(state, lines, position, selection) {
    const value = state.textarea.value;
    const lineStart = value.lastIndexOf('\n', position - 1) + 1;
    const lineEnd = indexOrLength(value, '\n', position);
    const at = value.slice(lineStart, lineEnd).trim() === '' ? lineStart : lineEnd;

    const head = value.slice(0, at);
    const tail = value.slice(lineEnd);
    const before = at === 0 || head.endsWith('\n\n') ? '' : head.endsWith('\n') ? '\n' : '\n\n';
    const after = tail === '' || tail.startsWith('\n\n') ? '' : tail.startsWith('\n') ? '\n' : '\n\n';
    const block = lines.join('\n');
    const blockStart = at + before.length;
    const [selectionStart, selectionEnd] = selection(blockStart, block);
    replaceRange(state, at, lineEnd, before + block + after, selectionStart, { selectionEnd });
}

/**
 * Enter at the end of a table row adds an empty row below it and lines up the columns; Enter on an empty row ends
 * the table.
 */
function continueTable(state, event, value, lineStart, lineEnd) {
    const caret = state.textarea.selectionStart;
    const line = value.slice(lineStart, lineEnd);
    const trimmed = line.trim();
    // A table (outer pipes optional), or a row with outer pipes being typed before its separator row exists.
    const table = findTable(value, caret);
    const wrapped = trimmed.length >= 2 && trimmed.startsWith('|') && trimmed.endsWith('|');
    if ((!table && !wrapped) || value.slice(caret, lineEnd).trim() !== '') {
        return false;
    }

    const cells = parseRow(line);
    event.preventDefault();
    if (cells.every(cell => cell.text === '')) {
        replaceRange(state, lineStart, lineEnd, '', lineStart);
        return true;
    }

    if (table) {
        // Below the header the new row goes after the separator row.
        const row = Math.max(table.caretRow, 1) + 1;
        table.rows.splice(row, 0, []);
        applyTable(state, table, row, 0);
        return true;
    }

    const indent = /^\s*/.exec(line)[0];
    const text = '\n' + indent + '|' + cells.map(cell => ' '.repeat(Math.max(2, cell.end - cell.start))).join('|') + '|';
    replaceRange(state, lineEnd, lineEnd, text, lineEnd + 1 + indent.length + 2);
    return true;
}

/**
 * Tab in a table lines up its columns and selects the next cell; Shift+Tab the previous one. Tab in the last cell
 * adds a row. Returns false outside a table, where Tab moves the focus as usual (also right after Escape).
 */
function tabInTable(state, event) {
    const textarea = state.textarea;
    const table = findTable(textarea.value, textarea.selectionStart);
    if (!table) {
        return false;
    }

    event.preventDefault();
    const columns = table.columns;
    let row = table.caretRow;
    let column = table.caretCell + (event.shiftKey ? -1 : 1);
    if (row === 1) {
        // On the separator row: Tab goes to the first body cell, Shift+Tab to the last header cell.
        row = event.shiftKey ? 0 : 2;
        column = event.shiftKey ? columns - 1 : 0;
    } else if (column >= columns) {
        row = row === 0 ? 2 : row + 1;
        column = 0;
    } else if (column < 0) {
        row = row === 2 ? 0 : Math.max(0, row - 1);
        column = row === 0 && table.caretRow === 0 ? 0 : columns - 1;
    }

    if (row >= table.rows.length) {
        table.rows.push([]);
    }
    applyTable(state, table, row, column, true);
    return true;
}

/**
 * The table around position: lines with a pipe (the outer pipes are optional) whose second line is a separator row.
 * Lines with pipes just above the header are not part of it. Returns its range, the cell texts per row and the
 * caret's row, cell and offset in that cell's text.
 */
function findTable(value, position) {
    const lineStart = value.lastIndexOf('\n', position - 1) + 1;
    const lineEnd = indexOrLength(value, '\n', position);
    if (!TABLE_LINE.test(value.slice(lineStart, lineEnd))) {
        return null;
    }

    let blockStart = lineStart;
    while (blockStart > 0) {
        const previous = value.lastIndexOf('\n', blockStart - 2) + 1;
        if (!TABLE_LINE.test(value.slice(previous, blockStart - 1))) {
            break;
        }
        blockStart = previous;
    }
    let end = lineEnd;
    while (end < value.length) {
        const next = indexOrLength(value, '\n', end + 1);
        if (!TABLE_LINE.test(value.slice(end + 1, next))) {
            break;
        }
        end = next;
    }

    const block = value.slice(blockStart, end).split('\n');
    const separator = block.findIndex((line, i) => i > 0 && TABLE_SEPARATOR.test(line));
    const caretLine = value.slice(blockStart, lineStart).split('\n').length - 1;
    if (separator < 0 || caretLine < separator - 1) {
        return null;
    }

    const lines = block.slice(separator - 1);
    const start = blockStart + block.slice(0, separator - 1).reduce((sum, line) => sum + line.length + 1, 0);
    const parsed = lines.map(parseRow);
    const caretRow = caretLine - (separator - 1);
    const caretInLine = position - lineStart;
    const cells = parsed[caretRow];
    let caretCell = cells.findIndex(cell => caretInLine <= cell.end);
    if (caretCell < 0) {
        caretCell = Math.max(0, cells.length - 1);
    }
    const cell = cells[caretCell];
    const caretOffset = cell ? Math.max(0, Math.min(cell.text.length, caretInLine - cell.contentStart)) : 0;

    return {
        start,
        end,
        indent: /^\s*/.exec(lines[0])[0],
        rows: parsed.map(row => row.map(c => c.text)),
        columns: Math.max(...parsed.map(row => row.length)),
        caretRow,
        caretCell,
        caretOffset
    };
}

/**
 * The cells of a table row: their raw range in the line and their trimmed text. The outer pipes are optional. Pipes in
 * code or escaped do not split.
 */
function parseRow(line) {
    const cells = [];
    const leading = /^\s*\|/.exec(line);
    let start = leading ? leading[0].length : 0;
    let code = 0;
    for (let i = start; i < line.length; i++) {
        const char = line[i];
        if (char === '\\') {
            i++;
        } else if (char === '`') {
            let run = 1;
            while (line[i + run] === '`') {
                run++;
            }
            code = code === 0 ? run : code === run ? 0 : code;
            i += run - 1;
        } else if (char === '|' && code === 0) {
            cells.push(tableCell(line, start, i));
            start = i + 1;
        }
    }
    if (line.slice(start).trim() !== '' || cells.length === 0) {
        cells.push(tableCell(line, start, line.length));
    }
    return cells;
}

function tableCell(line, start, end) {
    const raw = line.slice(start, end);
    return { start, end, contentStart: start + raw.length - raw.trimStart().length, text: raw.trim() };
}

/**
 * Rewrites the table with its columns lined up, as one undoable edit, and puts the caret in the given cell: where it
 * was, or with the cell's text selected when select is true.
 */
function applyTable(state, table, row, column, select = false) {
    const { lines, cellStarts } = formatTable(table.rows, table.columns, table.indent);
    const text = lines.join('\n');
    const lineStart = table.start + lines.slice(0, row).reduce((sum, line) => sum + line.length + 1, 0);
    const cellStart = lineStart + cellStarts[row][column].start;
    const cellLength = cellStarts[row][column].length;
    let selectionStart;
    let selectionEnd;
    if (select) {
        selectionStart = cellStart;
        selectionEnd = cellStart + cellLength;
    } else {
        selectionStart = selectionEnd = cellStart + Math.min(table.caretOffset, cellLength);
        if (row !== table.caretRow || column !== table.caretCell) {
            selectionStart = selectionEnd = cellStart;
        }
    }

    const textarea = state.textarea;
    if (textarea.value.slice(table.start, table.end) === text) {
        textarea.setSelectionRange(selectionStart, selectionEnd);
    } else {
        replaceRange(state, table.start, table.end, text, selectionStart, { selectionEnd });
    }
}

/**
 * Pads every cell to its column's width (at least 3), honouring :--- / :---: / ---: alignment, and rebuilds the
 * separator row. Returns the lines and where each cell's text starts in its line.
 */
export function formatTable(rows, columns, indent = '') {
    const range = Array.from({ length: columns }, (_, i) => i);
    const aligns = range.map(i => {
        const marker = rows[1]?.[i] ?? '';
        const left = marker.startsWith(':');
        const right = marker.length > 1 && marker.endsWith(':');
        return left && right ? 'center' : right ? 'right' : left ? 'left' : 'none';
    });
    const widths = range.map(i => Math.max(3, ...rows.map((row, r) => r === 1 ? 0 : textWidth(row[i] ?? ''))));

    const cellStarts = [];
    const lines = rows.map((row, r) => {
        let line = indent + '|';
        const starts = [];
        for (const i of range) {
            const width = widths[i];
            let cell;
            let before = 0;
            if (r === 1) {
                const dashes = width - (aligns[i] === 'center' ? 2 : aligns[i] === 'none' ? 0 : 1);
                cell = (aligns[i] === 'left' || aligns[i] === 'center' ? ':' : '') + '-'.repeat(dashes)
                    + (aligns[i] === 'right' || aligns[i] === 'center' ? ':' : '');
            } else {
                const text = row[i] ?? '';
                const space = width - textWidth(text);
                before = aligns[i] === 'right' ? space : aligns[i] === 'center' ? Math.floor(space / 2) : 0;
                cell = ' '.repeat(before) + text + ' '.repeat(space - before);
            }
            starts.push({ start: line.length + 1 + before, length: r === 1 ? 0 : (row[i] ?? '').length });
            line += ' ' + cell + ' |';
        }
        cellStarts.push(starts);
        return line;
    });
    return { lines, cellStarts };
}

const WIDE = /[ᄀ-ᅟ⺀-꓏가-힣豈-﫿︰-﹏＀-｠￠-￦\u{1F300}-\u{1FAFF}\u{20000}-\u{3FFFD}]/u;

/** Columns a text takes in a monospace font: wide East Asian characters and emoji take two, combining marks none. */
function textWidth(text) {
    let width = 0;
    for (const char of text) {
        width += /\p{M}/u.test(char) ? 0 : WIDE.test(char) ? 2 : 1;
    }
    return width;
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
    // Escape then Tab leaves the editor even in a table, so the keyboard is never trapped.
    const escaped = state.escaped;
    state.escaped = event.key === 'Escape';
    if (state.options.locked || event.isComposing) {
        return;
    }

    if (event.key === 'Enter' && !event.shiftKey && !event.ctrlKey && !event.metaKey && !event.altKey) {
        continueBlock(state, event);
        return;
    }

    if (event.key === 'Tab' && !escaped && !event.ctrlKey && !event.metaKey && !event.altKey) {
        tabInTable(state, event);
        return;
    }

    const modifier = isMac ? event.metaKey && !event.ctrlKey : event.ctrlKey && !event.metaKey;
    // Ctrl+Alt+T (Option+Command+T on a Mac) opens the table editor. AltGr is Ctrl+Alt on Windows, so a key that
    // types a character with AltGr is left alone.
    if (modifier && event.altKey && !event.shiftKey && event.code === 'KeyT' && (isMac || event.key.toLowerCase() === 't')) {
        if (openTableEditor(state)) {
            event.preventDefault();
        }
        return;
    }
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
        // In a table the table button edits that table instead of offering a new one.
        if (menuButton.matches('[data-md-table-button]') && caretInTable(state)) {
            closeMenu(state, false);
            openTableEditor(state);
            return;
        }
        toggleMenu(state, menuButton, event.detail === 0);
        return;
    }

    // A menu item Blazor handles, such as the table editor: just close the menu.
    if (event.target.closest?.('[data-md-close-menu]')) {
        closeMenu(state, false);
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

/**
 * Arrow keys in a menu: up and down in a list, in all four directions in the table grid. Down from the grid's last
 * row goes to the items below it, and up from the first of those back into the grid.
 */
function moveInMenu(menu, target, key) {
    const grid = menu.querySelector('.markdown-editor-table-grid');
    const cells = grid ? Array.from(grid.querySelectorAll('[role="menuitem"]')) : [];
    const items = Array.from(menu.querySelectorAll('[role="menuitem"]')).filter(item => !cells.includes(item));

    if (cells.includes(target)) {
        const width = Number(grid.dataset.mdColumns);
        const index = cells.indexOf(target);
        const column = index % width;
        let next = index;
        switch (key) {
            case 'ArrowDown':
                if (index + width < cells.length) {
                    next = index + width;
                } else if (items.length > 0) {
                    items[0].focus();
                    return true;
                }
                break;
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
    const index = items.indexOf(target);
    if (key === 'ArrowUp' && index === 0 && cells.length > 0) {
        cells[cells.length - Number(grid.dataset.mdColumns)].focus();
        return true;
    }
    items[(index + (key === 'ArrowDown' ? 1 : -1) + items.length) % items.length].focus();
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
        if (target.matches('[data-md-table-button]') && caretInTable(state)) {
            openTableEditor(state);
            return;
        }
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
    for (const element of root.querySelectorAll('[data-md-shortcut]')) {
        element.textContent = macShortcut(element.dataset.mdShortcut);
    }
}

/** "Ctrl+Alt+T" as a Mac shows it: "⌥⌘T". */
function macShortcut(shortcut) {
    const keys = shortcut.split('+');
    const key = keys.pop();
    return (keys.includes('Alt') ? '⌥' : '') + (keys.includes('Shift') ? '⇧' : '') + (keys.includes('Ctrl') ? '⌘' : '') + key;
}

// Table button

function caretInTable(state) {
    const textarea = state.textarea;
    return !!findTable(textarea.value, textarea.selectionStart);
}

function scheduleTableButton(state) {
    cancelAnimationFrame(state.tableFrame);
    state.tableFrame = requestAnimationFrame(() => updateTableButton(state));
}

/** Shows the table button as "Edit table" while the caret is in a table. */
function updateTableButton(state) {
    const button = state.root.querySelector('[data-md-table-button]');
    if (!button) {
        return;
    }

    const inTable = caretInTable(state);
    const label = inTable ? button.dataset.mdEditLabel : button.dataset.mdLabel;
    const title = inTable ? `${label} (${isMac ? macShortcut('Ctrl+Alt+T') : 'Ctrl+Alt+T'})` : label;
    if (button.title === title) {
        return;
    }

    button.classList.toggle('markdown-editor-in-table', inTable);
    button.title = title;
    button.setAttribute('aria-label', label);
    button.setAttribute('aria-haspopup', inTable ? 'dialog' : 'menu');
}

/** Opens the table dialog through its menu item, so .NET reads the table at the caret. */
function openTableEditor(state) {
    const item = state.root.querySelector('[data-md-open-table-editor]');
    const button = state.root.querySelector('[data-md-table-button]');
    if (!item || button?.disabled) {
        return false;
    }

    item.click();
    return true;
}

// Helpers

function indexOrLength(value, search, from) {
    const index = value.indexOf(search, from);
    return index < 0 ? value.length : index;
}

function escapeRegExp(text) {
    return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
