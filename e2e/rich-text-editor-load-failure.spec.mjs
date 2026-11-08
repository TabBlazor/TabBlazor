// When the TipTap bundle cannot load (blocked by a proxy or CSP, missing), RichTextEditor must fall back to a plain
// text area and keep the Blazor Server circuit alive.
import { expect, test } from '@playwright/test';

test('a blocked tiptap.js falls back to a text area and keeps the page working', async ({ page }) => {
    const pageErrors = [];
    page.on('pageerror', error => pageErrors.push(error.message));
    await page.route('**/TabBlazor.TipTap/tiptap.js', route => route.abort());

    await page.goto('/docs/forms/rich-text-editors');

    const section = page.locator('section', { has: page.getByRole('heading', { name: 'Basic', exact: true }) });
    const editor = section.locator('.rich-text-editor').first();
    const fallback = editor.locator('textarea.rich-text-editor-fallback');
    await expect(fallback).toBeVisible();
    await expect(fallback).toHaveValue(/Select text and use the toolbar/);
    await expect(editor.locator('.rich-text-editor-toolbar')).toHaveCount(0);
    await expect(editor.locator('.rich-text-editor-fallback-note')).toContainText('could not load');

    // The value still reaches .NET, which shows it under the example.
    await fallback.fill('<p>Typed without TipTap</p>');
    await fallback.blur();
    await expect(section.locator('pre').filter({ hasText: 'Typed without TipTap' }).first()).toBeVisible();

    // The circuit is alive: Blazor's error bar is hidden and client-side navigation still works.
    await expect(page.locator('#blazor-error-ui')).toBeHidden();
    await page.evaluate(() => window.Blazor.navigateTo('/docs/forms/markdown-editors'));
    await expect(page.locator('.markdown-editor').first()).toBeVisible();
    expect(pageErrors).toEqual([]);
});
