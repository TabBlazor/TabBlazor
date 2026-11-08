// The markdown support of RichTextEditor (Format = Markdown), in its own file so other editors don't download it.
// @tiptap/core is not bundled here: the build maps it to ./tiptap.js, so this shares the editor's TipTap.
export { Markdown } from '@tiptap/markdown';
