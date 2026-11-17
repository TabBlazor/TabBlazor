// Locators for the docs examples: each example is a <section> whose h4 carries a stable, slugified id
// (see DocsExample.Slugify) plus the anchor link, so match on the id rather than the heading text.

/** The id the docs give an example's heading, slugified from its title. */
export function anchor(title) {
    return title
        .normalize('NFD')
        .replace(/[̀-ͯ]/g, '')
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '');
}

/** The docs example with the given title. */
export function section(page, title) {
    return page.locator('section', { has: page.locator(`h4.docs-heading#${anchor(title)}`) });
}
