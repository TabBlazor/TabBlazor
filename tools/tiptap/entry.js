// Everything RichTextEditor and app extensions need from TipTap, in one module.
//
// App extensions import from the bundle (`/_content/TabBlazor.TipTap/tiptap.js`), not from npm, so they share the
// editor's copy of TipTap and ProseMirror. Two copies break at runtime ("duplicate prosemirror-model").
export * from '@tiptap/core';
export { StarterKit } from '@tiptap/starter-kit';
export { Image } from '@tiptap/extension-image';
export { TableKit, Table, TableRow, TableCell, TableHeader } from '@tiptap/extension-table';
export { TaskList, TaskItem } from '@tiptap/extension-list';
export { TextAlign } from '@tiptap/extension-text-align';
export { Highlight } from '@tiptap/extension-highlight';
export { Typography } from '@tiptap/extension-typography';
export { Mention } from '@tiptap/extension-mention';
export { Placeholder, CharacterCount } from '@tiptap/extensions';
export { Suggestion } from '@tiptap/suggestion';
export { Plugin, PluginKey, TextSelection } from '@tiptap/pm/state';
export { Decoration, DecorationSet } from '@tiptap/pm/view';
