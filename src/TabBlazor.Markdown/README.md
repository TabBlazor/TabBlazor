# TabBlazor.Markdown

A [Markdig](https://github.com/xoofx/markdig) renderer for the preview of TabBlazor's `MarkdownEditor`. The
editor itself is in the `TabBlazor` package; without a renderer it works but has no preview.

```csharp
builder.Services.AddTabBlazor();
builder.Services.AddTabBlazorMarkdown();
```

```razor
<MarkdownEditor @bind-Value="model.Description" Label="Description" Mode="MarkdownEditorMode.Split" />
```

The renderer is a plain `IMarkdownRenderer`, so it also renders saved markdown elsewhere:

```razor
@inject IMarkdownRenderer Markdown

<div class="markdown">@Markdown.Render(article.Body)</div>
```

## Safe by default

Markdown typed by users ends up as HTML in other users' browsers, so the renderer:

- shows raw HTML as text (`<script>` appears as typed instead of running);
- lets links use only `http`, `https` and `mailto`, and images only `http` and `https`. Relative URLs are always
  allowed. Other links (such as `javascript:`) point to `#`, and other images have no source;
- leaves out Markdig extensions that write raw attributes, such as generic attributes (`{onclick=…}`).

The pipeline is CommonMark plus pipe tables, task lists, strikethrough and automatic links.

## Options

```csharp
builder.Services.AddTabBlazorMarkdown(options =>
{
    options.SoftLineBreaksAsHardLineBreaks = true;          // line breaks like GitHub comments
    options.AllowedImageUrlSchemes.Add("data");             // images stored inline as data URLs
    options.ConfigurePipeline = pipeline => pipeline.UseFootnotes();
});
```

### Raw HTML

To let raw HTML through, pass a sanitizer. The renderer runs it on the rendered HTML. For example, with the
[HtmlSanitizer](https://github.com/mganss/HtmlSanitizer) package:

```csharp
var sanitizer = new Ganss.Xss.HtmlSanitizer();
builder.Services.AddTabBlazorMarkdown(options => options.AllowHtml(sanitizer.Sanitize));
```

HtmlSanitizer is not a dependency of this package: it pulls in AngleSharp, which adds a lot to a WebAssembly
download, and most apps do not need raw HTML.
