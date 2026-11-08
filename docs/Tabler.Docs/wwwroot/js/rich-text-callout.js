// An app-defined RichTextEditor extension: a Tabler callout block.
// Listed in TipTapOptions.ExtensionModules and turned on with CustomExtensions="['callout']".
// Import TipTap from the editor's bundle, never from npm, so the extension shares the editor's ProseMirror.
import { Node, mergeAttributes } from '../../TabBlazor.TipTap/tiptap.js';

export function register(registry) {
    registry.add('callout', options => Node.create({
        name: 'callout',
        group: 'block',
        content: 'inline*',
        defining: true,

        parseHTML: () => [{ tag: 'div[data-callout]' }],

        renderHTML: ({ HTMLAttributes }) =>
            ['div', mergeAttributes(HTMLAttributes, { 'data-callout': '', class: `alert alert-${options.color ?? 'info'}` }), 0],

        addCommands() {
            return {
                toggleCallout: () => ({ commands }) => commands.toggleNode(this.name, 'paragraph')
            };
        }
    }));
}
