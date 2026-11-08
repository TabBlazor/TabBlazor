// Smoke test of RichTextEditor's browser side (TipTap glue) on the docs page: typing and value sync, a toolbar
// command, paste with a data: image (uploaded, never stored inline), mentions, and a clean dispose.
import { expect, test } from '@playwright/test';

const page_ = '/docs/forms/rich-text-editors';

/** The docs example (a section with an h4 title). */
function section(page, title) {
    return page.locator('section', { has: page.getByRole('heading', { name: title, exact: true }) });
}

/** The editor in the docs example with the given title. */
function example(page, title) {
    return section(page, title).locator('.rich-text-editor').first();
}

test.beforeEach(async ({ page }) => {
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => {
        if (message.type() === 'error' && !message.text().includes('Failed to load resource')) {
            errors.push(message.text());
        }
    });
    page.errors = errors;
    await page.goto(page_);
    // Interactive once TipTap has mounted.
    await expect(page.locator('.rich-text-editor .ProseMirror').first()).toBeVisible();
});

test.afterEach(async ({ page }) => {
    expect(page.errors).toEqual([]);
});

test('typing reaches .NET after the debounce', async ({ page }) => {
    const editor = example(page, 'Basic');
    const prose = editor.locator('.ProseMirror');
    await prose.click();
    await page.keyboard.press('ControlOrMeta+End');
    await page.keyboard.press('Enter');
    await page.keyboard.type('Typed by Playwright');

    await expect(editor.locator('input.rich-text-editor-value')).toHaveValue(/<p>Typed by Playwright<\/p>/);
    await expect(page.locator('pre').filter({ hasText: 'Typed by Playwright' }).first()).toBeVisible();
});

test('toolbar bolds the selection and shows the button pressed', async ({ page }) => {
    const editor = example(page, 'Basic');
    const prose = editor.locator('.ProseMirror');
    await prose.click();
    await page.keyboard.press('ControlOrMeta+End');
    await page.keyboard.press('Enter');
    await page.keyboard.type('make me bold');
    await page.keyboard.press('Shift+Home');

    const bold = editor.locator('[data-rt-command="toggleBold"]');
    await bold.click();

    await expect(prose.locator('strong', { hasText: 'make me bold' })).toBeVisible();
    await expect(bold).toHaveAttribute('aria-pressed', 'true');
    await expect(editor.locator('input.rich-text-editor-value')).toHaveValue(/<strong>make me bold<\/strong>/);
});

test('a pasted data: image is uploaded instead of stored inline', async ({ page }) => {
    const editor = example(page, 'Images and files');
    const prose = editor.locator('.ProseMirror');
    await prose.click();
    await page.keyboard.press('ControlOrMeta+End');

    await prose.evaluate(element => {
        const png = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';
        const data = new DataTransfer();
        data.setData('text/html', `<p>From Word</p><img src="data:image/png;base64,${png}">`);
        data.setData('text/plain', 'From Word');
        element.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
    });

    // The text is pasted at once; the image arrives through UploadFile (the docs demo returns a data URL).
    await expect(prose.getByText('From Word')).toBeVisible();
    await expect(prose.locator('img[alt="pasted-image-1"]')).toBeVisible({ timeout: 15_000 });
    await expect(editor.locator('.rich-text-upload-placeholder')).toHaveCount(0);
});

test('mentions search .NET and store the id', async ({ page }) => {
    const card = section(page, 'Mentions');
    const editor = card.locator('.rich-text-editor').first();
    const prose = editor.locator('.ProseMirror');
    await prose.click();
    await page.keyboard.press('ControlOrMeta+End');
    await page.keyboard.type(' @ann');

    const option = page.locator('.rich-text-mention-menu [role="option"]', { hasText: 'Anna Svensson' });
    await expect(option).toBeVisible();
    await page.keyboard.press('Enter');

    await expect(prose.locator('span[data-type="mention"][data-id="1"]')).toBeVisible();
    await expect(card.getByText(/Mentioned ids: 2, 1/)).toBeVisible();
});

test('markdown value loads as rich text and saves as markdown', async ({ page }) => {
    const editor = example(page, 'Markdown value');
    const prose = editor.locator('.ProseMirror');
    await expect(prose.locator('table')).toBeVisible();
    await expect(prose.locator('li[data-checked="true"]')).toHaveCount(1);
    await expect(editor.locator('[data-rt-command="toggleUnderline"]')).toHaveCount(0);

    await prose.locator('h2').click();
    await page.keyboard.press('End');
    await page.keyboard.type(' now');
    await page.keyboard.press('Shift+Home');
    await editor.locator('[data-rt-command="toggleItalic"]').click();

    await expect(editor.locator('input.rich-text-editor-value')).toHaveValue(/^## \*Stored as markdown now\*\n/);
});

test('leaving the page disposes the editors cleanly', async ({ page }) => {
    // Client-side navigation, so the components are disposed rather than the page unloaded.
    await page.evaluate(() => window.Blazor.navigateTo('/docs/forms/markdown-editors'));
    await expect(page.locator('.markdown-editor').first()).toBeVisible();
});
