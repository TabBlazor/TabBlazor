# TabBlazor.TipTap

A rich text (WYSIWYG) editor for [TabBlazor](https://tabblazor.com), built on [TipTap](https://tiptap.dev) 3: a
Tabler toolbar, uploads, `@` mentions and app-defined extensions. The value is HTML, or the TipTap JSON document.

```csharp
builder.Services.AddTabBlazor();
builder.Services.AddTabBlazorTipTap(options => options.AllowHtml(html => sanitizer.Sanitize(html)));
```

```razor
<RichTextEditor @bind-Value="model.Body" Label="Body" />

<RichTextView Html="@article.Body" />
```

No script tag is needed: the editor loads its module and the TipTap bundle (`_content/TabBlazor.TipTap/tiptap.js`)
when it first renders. Its styles are part of the app's scoped CSS bundle (`YourApp.styles.css`).

## Features

`Extensions` turns on built-in features. `Default` is `Basic | Image`.

| Flag | What it adds |
| --- | --- |
| `Basic` | Headings (H1–H3), bold, italic, underline, strike, inline code, lists, quotes, code blocks, horizontal rules, links, undo/redo |
| `Image` | Images, added through uploads only |
| `Table` | Tables with a size picker and row and column tools |
| `TaskList` | Task lists with check boxes |
| `TextAlign` | Left, center and right alignment |
| `Highlight` | Highlighted text |
| `Typography` | Smart quotes and dashes |
| `Mention` | `@` mentions; needs `MentionSearch` |

Content of a feature that is off is reduced to plain paragraphs when it is pasted or loaded.

## Toolbar

`Toolbar` picks the buttons and their order. `RichTextToolbar.Full` (the default) has every button whose feature is on;
`RichTextToolbar.Minimal` has bold, italic, lists and link. Or list your own, with `Separator` between groups:

```razor
<RichTextEditor @bind-Value="model.Comment"
                Toolbar="RichTextToolbar.Minimal"
                Extensions="RichTextExtensions.Basic" />

<RichTextEditor @bind-Value="model.Note"
                Toolbar="@([RichTextTool.Bold, RichTextTool.Italic, RichTextTool.Separator, RichTextTool.Link])" />
```

The toolbar only picks buttons. What content is allowed is `Extensions`, so shortcuts (Ctrl+U) and pasted formatting
still work without a button; set both for a simple editor.

## Markdown value

`Format="RichTextEditorFormat.Markdown"` edits visually but stores markdown (CommonMark with GitHub tables and task
lists):

```razor
<RichTextEditor @bind-Value="model.Notes" Format="RichTextEditorFormat.Markdown" />
```

- Features markdown cannot store are off and leave the toolbar: underline, highlight and alignment.
- Mentions are stored as `[@ id="42" label="Anna"]`; `RichTextMentions.GetIds(value, RichTextEditorFormat.Markdown)`
  reads the ids.
- App extensions need their own markdown rules (`renderMarkdown`/`parseMarkdown`), or their content is lost on save.
- The support is a separate file (`tiptap-markdown.js`, 19 KB gzipped) that only these editors load. A custom bundle
  that exports `Markdown` itself makes it unnecessary; otherwise `TipTapOptions.MarkdownScriptUrl` sets where it is.
- Show saved markdown with an `IMarkdownRenderer`, such as the one from `TabBlazor.Markdown`; the read-only editor in
  static rendering uses it too.

## Showing saved HTML safely

The browser can post any HTML, so sanitize it where you save it and again where you show it. `RichTextView` shows
HTML only through the sanitizer registered with `AllowHtml`; without one it throws (set `AllowUnsanitizedHtml = true`
if you already sanitize at save time). The package has no sanitizer dependency. With
[HtmlSanitizer](https://github.com/mganss/HtmlSanitizer), allow what the editor writes:

```csharp
var sanitizer = new Ganss.Xss.HtmlSanitizer();
sanitizer.AllowedSchemes.Add("mailto");
foreach (var attribute in new[] { "data-type", "data-id", "data-label", "data-mention-suggestion-char", "data-checked", "rel" })
{
    sanitizer.AllowedAttributes.Add(attribute);   // mentions and task lists
}
sanitizer.AllowedTags.Add("input");               // task list check boxes
sanitizer.AllowedTags.Add("label");
sanitizer.AllowedAttributes.Add("type");
sanitizer.AllowedAttributes.Add("checked");
sanitizer.AllowedCssProperties.Clear();
sanitizer.AllowedCssProperties.Add("text-align"); // TextAlign

builder.Services.AddTabBlazorTipTap(options => options.AllowHtml(html => sanitizer.Sanitize(html)));
```

Links accept only `http`, `https`, `mailto` and relative URLs, and get `rel="noopener noreferrer nofollow"`. The JSON
format (`Format="RichTextEditorFormat.Json"`) carries no markup, but an app that turns it into HTML must allow-list node
types and URL schemes itself.

## Uploads

Set `UploadFile` to let users paste, drop or pick files. The contract is the same as `MarkdownEditor`'s:

```csharp
private async Task<EditorUploadResult> UploadAsync(EditorUploadFile file, CancellationToken token)
{
    await using var stream = file.OpenReadStream(token);
    var url = await storage.SaveAsync(file.Name, file.ContentType, stream, token);
    return new EditorUploadResult(url);
}
```

Images are embedded and other files linked. Images are never stored inline as base64: images pasted from Word or web
pages as `data:` URLs are uploaded instead, and dropped without `UploadFile`. Serve uploaded files with
`Content-Disposition: attachment` (or from another domain) and `X-Content-Type-Options: nosniff`, and check the
type on the server.

## Mentions

```razor
<RichTextEditor @bind-Value="model.Comment"
                Extensions="RichTextExtensions.Default | RichTextExtensions.Mention"
                MentionSearch="SearchPeopleAsync" />

@code {
    Task<IReadOnlyList<RichTextMention>> SearchPeopleAsync(string query, CancellationToken token) =>
        people.SearchAsync(query, take: 8, token);
}
```

A mention is stored as `<span data-type="mention" data-id="42" data-label="Anna">@Anna</span>`.
`RichTextMentions.GetIds(html)` returns the ids, for example to notify people when the content is saved. The search
runs after `MentionDebounceMs` (150) and a newer query cancels the older one's token.

## App extensions

Write a TipTap extension in an ES module that imports TipTap from the editor's bundle, so both share one copy of
ProseMirror:

```js
// wwwroot/js/callout.js
import { Node, mergeAttributes } from '/_content/TabBlazor.TipTap/tiptap.js';

export function register(registry) {
    registry.add('callout', options => Node.create({
        name: 'callout',
        group: 'block',
        content: 'inline*',
        parseHTML: () => [{ tag: 'div[data-callout]' }],
        renderHTML: ({ HTMLAttributes }) => ['div', mergeAttributes(HTMLAttributes, { 'data-callout': '', class: 'alert alert-info' }), 0],
        addCommands() {
            return { toggleCallout: () => ({ commands }) => commands.toggleNode(this.name, 'paragraph') };
        }
    }));
}
```

```csharp
builder.Services.AddTabBlazorTipTap(options => options.ExtensionModules.Add("js/callout.js"));
```

```razor
<RichTextEditor @bind-Value="model.Body" CustomExtensions="@(["callout"])">
    <ToolbarExtra>
        <button type="button" class="btn btn-ghost-secondary" data-rt-command="toggleCallout">Callout</button>
    </ToolbarExtra>
</RichTextEditor>
```

`ExtensionOptions` passes options to the factory by extension name. A `ToolbarExtra` button with `data-rt-command`
(and optional JSON in `data-rt-args`) runs the command in the browser; `RunCommandAsync` and `InsertContentAsync`
do the same from .NET. To bundle TipTap yourself, point `TipTapOptions.ScriptUrl` at your build; it must export the
same names. The import path in an extension module must resolve to the same URL as `ScriptUrl`, or the browser loads a
second copy.

## Notes

- The value reaches .NET after `DebounceMs` (300 ms) without typing, when the editor loses focus, and after toolbar
  actions. On Blazor Server it travels in one SignalR message; raise `MaximumReceiveMessageSize` (32 KB by default)
  for long documents.
- In static server rendering the editor shows the content read-only (sanitized HTML, or its text without a sanitizer)
  and posts the value through a hidden input named after the bound field.
- Keyboard: Alt+F10 moves to the toolbar, Escape returns to the text, Ctrl+K edits the link.
- The TipTap bundles are built from `tools/tiptap` in the repository (`npm ci && npm run build`); CI checks that the
  committed bundles match the lockfile and that every bundled package is MIT licensed.
