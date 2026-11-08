using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace TabBlazor.Tests.Components
{
    public class RichTextEditorTests : TabBlazorTestContext
    {
        private const string ModulePath = "./_content/TabBlazor.TipTap/RichTextEditor.razor.js";

        private bool interactive = true;

        private sealed class Model
        {
            [Required]
            public string? Body { get; set; }
        }

        private IRenderedComponent<RichTextEditor> RenderEditor(string? value = null,
            Action<ComponentParameterCollectionBuilder<RichTextEditor>>? parameters = null,
            Action<string?>? onChanged = null)
        {
            SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
            return Render<RichTextEditor>(p =>
            {
                p.Add(e => e.Value, value);
                p.Add(e => e.ValueChanged, onChanged ?? (_ => { }));
                parameters?.Invoke(p);
            });
        }

        private static JsonElement Json(object? value) => JsonSerializer.SerializeToElement(value);

        /// <summary>A browser where the editor's module cannot be imported (bUnit cannot fail an import).</summary>
        private sealed class FailingImportRuntime : IJSRuntime
        {
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
                InvokeAsync<TValue>(identifier, CancellationToken.None, args);

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
                identifier == "import"
                    ? ValueTask.FromException<TValue>(new JSException("404 RichTextEditor.razor.js"))
                    : ValueTask.FromResult(default(TValue)!);
        }

        [Fact]
        public void Renders_frame_label_hidden_value_and_content_area()
        {
            var cut = RenderEditor("<p>Hi</p>", p => p.Add(e => e.Label, "Body"));

            var root = cut.Find("div.rich-text-editor");
            Assert.Equal("Body", cut.Find("label.form-label").TextContent);
            Assert.Equal(root.Id, cut.Find("label").GetAttribute("data-rt-label-for"));
            Assert.Equal("<p>Hi</p>", cut.Find("input.rich-text-editor-value").GetAttribute("value"));
            Assert.NotNull(cut.Find(".rich-text-editor-frame.form-control .rich-text.rich-text-editor-content"));
        }

        [Fact]
        public void Content_area_stays_empty_while_interactive_so_the_browser_editor_owns_it()
        {
            var cut = RenderEditor("<p>Hi</p>");

            Assert.Empty(cut.Find(".rich-text-editor-content").Children);
        }

        [Fact]
        public void Uses_bound_member_path_as_form_field_name()
        {
            var model = new Model();
            var cut = RenderEditor(null, p => p.Add(e => e.ValueExpression, () => model.Body));

            Assert.Equal("model.Body", cut.Find("input.rich-text-editor-value").GetAttribute("name"));
        }

        [Fact]
        public void Toolbar_shows_buttons_for_the_turned_on_extensions_only()
        {
            var basic = RenderEditor(null);
            Assert.NotEmpty(basic.FindAll("[data-rt-command=toggleBold]"));
            Assert.Empty(basic.FindAll("[data-rt-command=insertTable]"));
            Assert.Empty(basic.FindAll("[data-rt-command=toggleTaskList]"));
            Assert.Empty(basic.FindAll("[data-rt-command=setTextAlign]"));
            Assert.Empty(basic.FindAll("[data-rt-command=toggleHighlight]"));

            var all = RenderEditor(null, p => p.Add(e => e.Extensions, RichTextExtensions.All));
            Assert.Equal(36, all.FindAll("[data-rt-command=insertTable]").Count);
            Assert.NotEmpty(all.FindAll("[data-rt-command=toggleTaskList]"));
            Assert.Equal(3, all.FindAll("[data-rt-command=setTextAlign]").Count);
            Assert.NotEmpty(all.FindAll("[data-rt-command=toggleHighlight]"));
            Assert.True(all.Find("[data-rt-table-tools]").HasAttribute("hidden"));
        }

        [Fact]
        public void Minimal_toolbar_has_only_the_essentials_in_groups()
        {
            var cut = RenderEditor(null, p => p.Add(e => e.Toolbar, RichTextToolbar.Minimal));

            var groups = cut.FindAll(".rich-text-editor-toolbar > .rich-text-editor-toolbar-group");
            Assert.Equal(3, groups.Count);
            Assert.Equal(new[] { "toggleBold", "toggleItalic" }, groups[0].QuerySelectorAll("[data-rt-command]").Select(b => b.GetAttribute("data-rt-command")));
            Assert.Equal(new[] { "toggleBulletList", "toggleOrderedList" }, groups[1].QuerySelectorAll("[data-rt-command]").Select(b => b.GetAttribute("data-rt-command")));
            Assert.NotNull(groups[2].QuerySelector("[data-rt-link-button]"));
            Assert.Empty(cut.FindAll("[data-rt-style-label]"));
            Assert.Empty(cut.FindAll("[data-rt-command=undo]"));
        }

        [Fact]
        public void Custom_toolbar_keeps_order_and_drops_unavailable_tools_and_empty_groups()
        {
            var cut = RenderEditor(null, p => p
                .Add(e => e.Extensions, RichTextExtensions.Basic)
                .Add(e => e.Toolbar, new[]
                {
                    RichTextTool.Separator, RichTextTool.Redo, RichTextTool.Undo, RichTextTool.Undo,
                    RichTextTool.Separator, RichTextTool.Separator, RichTextTool.Table, RichTextTool.Highlight, RichTextTool.Upload,
                    RichTextTool.Separator, RichTextTool.Italic, RichTextTool.Separator
                }));

            var groups = cut.FindAll(".rich-text-editor-toolbar > .rich-text-editor-toolbar-group");
            Assert.Equal(2, groups.Count);
            Assert.Equal(new[] { "redo", "undo" }, groups[0].QuerySelectorAll("[data-rt-command]").Select(b => b.GetAttribute("data-rt-command")));
            Assert.Equal(new[] { "toggleItalic" }, groups[1].QuerySelectorAll("[data-rt-command]").Select(b => b.GetAttribute("data-rt-command")));
            Assert.Empty(cut.FindAll("[data-rt-table-tools]"));
        }

        [Fact]
        public void Table_tools_follow_the_group_with_the_table_button()
        {
            var cut = RenderEditor(null, p => p
                .Add(e => e.Extensions, RichTextExtensions.Basic | RichTextExtensions.Table)
                .Add(e => e.Toolbar, new[] { RichTextTool.Bold, RichTextTool.Separator, RichTextTool.Table, RichTextTool.Separator, RichTextTool.Undo }));

            var groups = cut.FindAll(".rich-text-editor-toolbar > .rich-text-editor-toolbar-group");
            Assert.Equal(4, groups.Count);
            Assert.True(groups[2].HasAttribute("data-rt-table-tools"));
            Assert.NotNull(groups[1].QuerySelector(".rich-text-editor-table-menu"));
        }

        [Fact]
        public void Upload_tool_shows_only_with_an_upload_handler()
        {
            var tools = new[] { RichTextTool.Bold, RichTextTool.Upload };
            Assert.Empty(RenderEditor(null, p => p.Add(e => e.Toolbar, tools)).FindAll("[data-rt-upload]"));

            var cut = RenderEditor(null, p => p
                .Add(e => e.Toolbar, tools)
                .Add(e => e.UploadFile, (_, _) => Task.FromResult(new EditorUploadResult("/x"))));
            Assert.Single(cut.FindAll("[data-rt-upload]"));
        }

        [Fact]
        public void Hides_toolbar_when_read_only_or_turned_off()
        {
            Assert.Empty(RenderEditor(null, p => p.Add(e => e.ReadOnly, true)).FindAll(".rich-text-editor-toolbar"));
            Assert.Empty(RenderEditor(null, p => p.Add(e => e.ShowToolbar, false)).FindAll(".rich-text-editor-toolbar"));
        }

        [Fact]
        public void Disables_toolbar_buttons_when_disabled()
        {
            var cut = RenderEditor(null, p => p.Add(e => e.Disabled, true));

            Assert.All(cut.FindAll(".rich-text-editor-toolbar > .rich-text-editor-toolbar-group > button"), b => Assert.True(b.HasAttribute("disabled")));
            Assert.Contains("rich-text-editor-disabled", cut.Find(".rich-text-editor").ClassName);
        }

        [Fact]
        public void Renders_extra_toolbar_content()
        {
            var cut = RenderEditor(null, p => p.Add(e => e.ToolbarExtra, b => b.AddMarkupContent(0, "<button class=\"extra\">X</button>")));

            Assert.NotNull(cut.Find(".rich-text-editor-toolbar button.extra"));
        }

        [Fact]
        public void Raises_value_changed_from_hidden_input_change_and_maps_empty_to_null()
        {
            var values = new List<string?>();
            var cut = RenderEditor("<p>a</p>", onChanged: values.Add);

            cut.Find("input.rich-text-editor-value").Change("<p>b</p>");
            cut.Find("input.rich-text-editor-value").Change("");

            Assert.Equal(new string?[] { "<p>b</p>", null }, values);
        }

        [Fact]
        public void Ignores_change_with_the_current_value()
        {
            var values = new List<string?>();
            var cut = RenderEditor("<p>a</p>", onChanged: values.Add);

            cut.Find("input.rich-text-editor-value").Change("<p>a</p>");

            Assert.Empty(values);
        }

        [Fact]
        public void Notifies_edit_context_and_shows_validation_state()
        {
            var model = new Model();
            var editContext = new EditContext(model);
            editContext.EnableDataAnnotationsValidation(Services);
            SetRendererInfo(new RendererInfo("Server", true));
            var cut = Render<CascadingValue<EditContext>>(p => p
                .Add(c => c.Value, editContext)
                .AddChildContent<RichTextEditor>(e => e
                    .Add(x => x.Value, model.Body)
                    .Add(x => x.ValueChanged, v => model.Body = v)
                    .Add(x => x.ValueExpression, () => model.Body)));

            editContext.Validate();
            cut.WaitForAssertion(() => Assert.Contains("invalid", cut.Find(".rich-text-editor-frame").ClassName));

            cut.Find("input.rich-text-editor-value").Change("<p>Done</p>");

            Assert.Equal("<p>Done</p>", model.Body);
            Assert.True(editContext.IsModified(() => model.Body));
        }

        [Fact]
        public void Attaches_with_options_for_extensions_format_and_mentions()
        {
            var module = JSInterop.SetupModule(ModulePath);

            RenderEditor(null, p => p
                .Add(e => e.Format, RichTextEditorFormat.Json)
                .Add(e => e.Extensions, RichTextExtensions.Basic | RichTextExtensions.Table | RichTextExtensions.Mention)
                .Add(e => e.MentionSearch, (_, _) => Task.FromResult<IReadOnlyList<RichTextMention>>([]))
                .Add(e => e.MentionTrigger, '#')
                .Add(e => e.CustomExtensions, new[] { "callout" })
                .Add(e => e.MaxLength, 100)
                .Add(e => e.DebounceMs, 500));

            var options = Json(Assert.Single(module.Invocations, i => i.Identifier == "attach").Arguments[1]);
            Assert.Equal("json", options.GetProperty("format").GetString());
            Assert.Equal(500, options.GetProperty("debounceMs").GetInt32());
            Assert.Equal(100, options.GetProperty("maxLength").GetInt32());
            Assert.True(options.GetProperty("extensions").GetProperty("table").GetBoolean());
            Assert.False(options.GetProperty("extensions").GetProperty("image").GetBoolean());
            Assert.True(options.GetProperty("extensions").GetProperty("mention").GetBoolean());
            Assert.Equal("#", options.GetProperty("mention").GetProperty("trigger").GetString());
            Assert.Equal("callout", options.GetProperty("customExtensions")[0].GetString());
            Assert.Equal(TipTapOptions.DefaultScriptUrl, options.GetProperty("scriptUrl").GetString());
        }

        [Fact]
        public void Markdown_format_turns_off_features_markdown_cannot_store()
        {
            var module = JSInterop.SetupModule(ModulePath);

            var cut = RenderEditor("**Hi**", p => p
                .Add(e => e.Format, RichTextEditorFormat.Markdown)
                .Add(e => e.Extensions, RichTextExtensions.All));

            var options = Json(module.Invocations.Single(i => i.Identifier == "attach").Arguments[1]);
            Assert.Equal("markdown", options.GetProperty("format").GetString());
            Assert.Equal(TipTapOptions.DefaultMarkdownScriptUrl, options.GetProperty("markdownScriptUrl").GetString());
            var extensions = options.GetProperty("extensions");
            Assert.False(extensions.GetProperty("underline").GetBoolean());
            Assert.False(extensions.GetProperty("highlight").GetBoolean());
            Assert.False(extensions.GetProperty("textAlign").GetBoolean());
            Assert.True(extensions.GetProperty("table").GetBoolean());
            Assert.True(extensions.GetProperty("taskList").GetBoolean());

            Assert.Empty(cut.FindAll("[data-rt-command=toggleUnderline]"));
            Assert.Empty(cut.FindAll("[data-rt-command=toggleHighlight]"));
            Assert.Empty(cut.FindAll("[data-rt-command=setTextAlign]"));
            Assert.NotEmpty(cut.FindAll("[data-rt-command=toggleTaskList]"));
        }

        [Fact]
        public void Static_rendering_shows_markdown_through_the_markdown_renderer()
        {
            interactive = false;
            Services.AddTabBlazorMarkdown();

            var cut = RenderEditor("**Hi**", p => p.Add(e => e.Format, RichTextEditorFormat.Markdown));

            Assert.Contains("<strong>Hi</strong>", cut.Find(".rich-text-editor-content").InnerHtml);
        }

        [Fact]
        public void Static_rendering_shows_markdown_text_without_a_renderer()
        {
            interactive = false;

            var cut = RenderEditor("**Hi**", p => p.Add(e => e.Format, RichTextEditorFormat.Markdown));

            Assert.Equal("**Hi**", cut.Find(".rich-text-editor-content div").TextContent);
        }

        [Fact]
        public void Falls_back_to_a_text_area_when_tiptap_cannot_start()
        {
            var module = JSInterop.SetupModule(ModulePath);
            module.Setup<RichTextEditor.LoadResult?>("attach", _ => true).SetResult(new RichTextEditor.LoadResult("Failed to fetch tiptap.js"));
            var errors = new List<string>();
            var values = new List<string?>();

            var cut = RenderEditor("<p>Hi</p>", p => p
                .Add(e => e.Label, "Body")
                .Add(e => e.OnLoadError, errors.Add), values.Add);

            cut.WaitForAssertion(() => Assert.True(cut.Instance.LoadFailed));
            Assert.Equal(new[] { "Failed to fetch tiptap.js" }, errors);
            Assert.Empty(cut.FindAll(".rich-text-editor-toolbar"));
            Assert.Empty(cut.FindAll(".rich-text-editor-content"));
            var textarea = cut.Find("textarea.rich-text-editor-fallback");
            Assert.Equal("<p>Hi</p>", textarea.GetAttribute("value"));
            Assert.Equal(cut.Find("label").Id, textarea.GetAttribute("aria-labelledby"));
            Assert.Contains("could not load", cut.Find(".rich-text-editor-fallback-note").TextContent);

            textarea.Change("<p>Changed</p>");
            Assert.Equal(new string?[] { "<p>Changed</p>" }, values);
            Assert.Equal("<p>Changed</p>", cut.Find("input.rich-text-editor-value").GetAttribute("value"));
        }

        [Fact]
        public void Falls_back_when_the_editor_module_itself_fails_to_load()
        {
            Services.AddSingleton<IJSRuntime>(new FailingImportRuntime());
            SetRendererInfo(new RendererInfo("Server", true));
            string? error = null;

            var cut = Render<RichTextEditor>(p => p.Add(e => e.OnLoadError, e => error = e));

            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("textarea.rich-text-editor-fallback")));
            Assert.Contains("404", error);
        }

        [Fact]
        public void Falls_back_when_recreating_the_editor_fails()
        {
            var module = JSInterop.SetupModule(ModulePath);
            module.Setup<RichTextEditor.LoadResult?>("update", _ => true).SetResult(new RichTextEditor.LoadResult("extension threw"));
            var cut = RenderEditor(null);

            cut.Render(p => p.Add(e => e.Extensions, RichTextExtensions.All));

            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("textarea.rich-text-editor-fallback")));
        }

        [Fact]
        public void Mention_is_off_without_a_search_callback()
        {
            var module = JSInterop.SetupModule(ModulePath);

            RenderEditor(null, p => p.Add(e => e.Extensions, RichTextExtensions.Default | RichTextExtensions.Mention));

            var options = Json(module.Invocations.Single(i => i.Identifier == "attach").Arguments[1]);
            Assert.False(options.GetProperty("extensions").GetProperty("mention").GetBoolean());
        }

        [Fact]
        public void Passes_app_options_from_dependency_injection()
        {
            Services.AddTabBlazorTipTap(o =>
            {
                o.ScriptUrl = "/js/my-tiptap.js";
                o.ExtensionModules.Add("/js/callout.js");
            });
            var module = JSInterop.SetupModule(ModulePath);

            RenderEditor(null);

            var options = Json(module.Invocations.Single(i => i.Identifier == "attach").Arguments[1]);
            Assert.Equal("/js/my-tiptap.js", options.GetProperty("scriptUrl").GetString());
            Assert.Equal("/js/callout.js", options.GetProperty("extensionModules")[0].GetString());
        }

        [Fact]
        public void Refreshes_browser_editor_when_parent_sets_a_new_value_but_not_for_its_own_echo()
        {
            var module = JSInterop.SetupModule(ModulePath);
            string? value = "<p>a</p>";
            var cut = RenderEditor(value, onChanged: v => value = v);

            cut.Find("input.rich-text-editor-value").Change("<p>typed</p>");
            cut.Render(p => p.Add(e => e.Value, value));
            Assert.DoesNotContain(module.Invocations, i => i.Identifier == "refresh");

            cut.Render(p => p.Add(e => e.Value, "<p>from parent</p>"));
            Assert.Single(module.Invocations, i => i.Identifier == "refresh");
        }

        [Fact]
        public void Updates_browser_editor_when_options_change()
        {
            var module = JSInterop.SetupModule(ModulePath);
            var cut = RenderEditor(null);

            cut.Render(p => p.Add(e => e.Disabled, true));

            var update = Assert.Single(module.Invocations, i => i.Identifier == "update");
            Assert.True(Json(update.Arguments[1]).GetProperty("locked").GetBoolean());
        }

        [Fact]
        public async Task Search_mentions_calls_callback_and_cancels_the_previous_search()
        {
            var tokens = new List<CancellationToken>();
            var first = new TaskCompletionSource<IReadOnlyList<RichTextMention>>();
            var cut = RenderEditor(null, p => p
                .Add(e => e.Extensions, RichTextExtensions.Default | RichTextExtensions.Mention)
                .Add(e => e.MentionSearch, (query, token) =>
                {
                    tokens.Add(token);
                    return query == "a" ? first.Task : Task.FromResult<IReadOnlyList<RichTextMention>>([new RichTextMention("1", "Anna")]);
                }));

            var pending = cut.Instance.SearchMentionsAsync("a");
            var results = await cut.Instance.SearchMentionsAsync("an");

            Assert.True(tokens[0].IsCancellationRequested);
            Assert.Equal("Anna", Assert.Single(results).Label);
            first.SetCanceled(tokens[0]);
            Assert.Empty(await pending);
        }

        [Fact]
        public void Uploads_file_and_inserts_image()
        {
            var module = JSInterop.SetupModule(ModulePath);
            module.Setup<bool>("finishUpload", _ => true).SetResult(true);
            var states = new List<bool>();

            var cut = RenderEditor(null, p => p
                .Add(e => e.UploadFile, async (file, token) =>
                {
                    await using var stream = file.OpenReadStream(token);
                    await stream.CopyToAsync(System.IO.Stream.Null, token);
                    return new EditorUploadResult("/files/photo.png");
                })
                .Add(e => e.OnUploadStateChanged, uploading => states.Add(uploading)));

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[2048], "photo.png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.Equal(new[] { true, false }, states));
            var start = Json(Assert.Single(module.Invocations, i => i.Identifier == "startUploads").Arguments[1]);
            Assert.Equal("Uploading photo.png…", start[0].GetProperty("label").GetString());
            var finish = Assert.Single(module.Invocations, i => i.Identifier == "finishUpload");
            Assert.Equal(start[0].GetProperty("token").GetString(), finish.Arguments[1]);
            var content = Json(finish.Arguments[2]);
            Assert.Equal("/files/photo.png", content.GetProperty("url").GetString());
            Assert.Equal("photo", content.GetProperty("text").GetString());
            Assert.True(content.GetProperty("embed").GetBoolean());
        }

        [Fact]
        public void Links_images_when_the_image_extension_is_off()
        {
            var module = JSInterop.SetupModule(ModulePath);
            module.Setup<bool>("finishUpload", _ => true).SetResult(true);

            var cut = RenderEditor(null, p => p
                .Add(e => e.Extensions, RichTextExtensions.Basic)
                .Add(e => e.UploadFile, (_, _) => Task.FromResult(new EditorUploadResult("/a.png"))));

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[1], "a.png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.Single(module.Invocations, i => i.Identifier == "finishUpload"));
            var content = Json(module.Invocations.Single(i => i.Identifier == "finishUpload").Arguments[2]);
            Assert.False(content.GetProperty("embed").GetBoolean());
            Assert.Equal("a.png", content.GetProperty("text").GetString());
        }

        [Fact]
        public void Rejects_files_that_are_too_large_or_not_accepted()
        {
            var module = JSInterop.SetupModule(ModulePath);
            var errors = new List<EditorUploadError>();

            var cut = RenderEditor(null, p => p
                .Add(e => e.MaxFileSize, 10)
                .Add(e => e.UploadFile, (_, _) => Task.FromResult(new EditorUploadResult("/x")))
                .Add(e => e.OnUploadError, errors.Add));

            cut.FindComponent<InputFile>().UploadFiles(
                InputFileContent.CreateFromText("x", "a.pdf", contentType: "application/pdf"),
                InputFileContent.CreateFromBinary(new byte[100], "big.png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.Equal(2, errors.Count));
            Assert.Contains(errors, e => e.Message == "a.pdf is not a file type you can add here.");
            Assert.Contains(errors, e => e.FileName == "big.png");
            Assert.DoesNotContain(module.Invocations, i => i.Identifier == "startUploads");
            Assert.Equal(2, cut.FindAll(".rich-text-editor-upload-error").Count);
        }

        [Fact]
        public void Raises_discarded_when_the_placeholder_is_gone()
        {
            JSInterop.SetupModule(ModulePath).Setup<bool>("finishUpload", _ => true).SetResult(false);
            EditorUploadResult? discarded = null;

            var cut = RenderEditor(null, p => p
                .Add(e => e.UploadFile, (_, _) => Task.FromResult(new EditorUploadResult("/x.png")))
                .Add(e => e.OnUploadDiscarded, r => discarded = r));

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[1], "x.png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.Equal("/x.png", discarded?.Url));
        }

        [Fact]
        public void Upload_exception_message_is_shown_and_placeholder_removed()
        {
            var module = JSInterop.SetupModule(ModulePath);
            module.Setup<bool>("finishUpload", _ => true).SetResult(true);

            var cut = RenderEditor(null, p => p
                .Add(e => e.UploadFile, (_, _) => throw new EditorUploadException("Too many images")));

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[1], "x.png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.Equal("Too many images", cut.Find(".rich-text-editor-upload-error .text-danger").TextContent));
            Assert.Null(module.Invocations.Single(i => i.Identifier == "finishUpload").Arguments[2]);
        }

        [Fact]
        public void Static_rendering_shows_sanitized_html_without_toolbar()
        {
            interactive = false;
            Services.AddTabBlazorTipTap(o => o.AllowHtml(html => html.Replace("<script>x</script>", "")));

            var cut = RenderEditor("<p>Hi</p><script>x</script>");

            Assert.Empty(cut.FindAll(".rich-text-editor-toolbar"));
            Assert.Equal("<p>Hi</p>", cut.Find(".rich-text-editor-content").InnerHtml);
            Assert.Equal("<p>Hi</p><script>x</script>", cut.Find("input.rich-text-editor-value").GetAttribute("value"));
        }

        [Fact]
        public void Static_rendering_shows_text_without_a_sanitizer()
        {
            interactive = false;

            var cut = RenderEditor("<p>Hello <strong>you</strong></p><p>Next</p>");

            Assert.Equal("Hello you\nNext", cut.Find(".rich-text-editor-content").TextContent);
        }

        [Fact]
        public void Uses_text_overrides()
        {
            var cut = RenderEditor(null, p => p.Add(e => e.Text, RichTextEditorText.Default with { Bold = "Fet" }));

            Assert.Equal("Fet", cut.Find("[data-rt-command=toggleBold]").GetAttribute("aria-label"));
        }
    }

    public class RichTextViewTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_html_through_the_sanitizer()
        {
            Services.AddTabBlazorTipTap(o => o.AllowHtml(html => html.Replace(" onclick=\"x()\"", "")));

            var cut = Render<RichTextView>(p => p.Add(v => v.Html, "<p onclick=\"x()\">Hi</p>"));

            var view = cut.Find("div.rich-text.rich-text-view");
            Assert.Equal("<p>Hi</p>", view.InnerHtml);
        }

        [Fact]
        public void Throws_without_a_sanitizer()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => Render<RichTextView>(p => p.Add(v => v.Html, "<p>Hi</p>")));

            Assert.Contains("sanitizer", exception.Message);
        }

        [Fact]
        public void Renders_unsanitized_html_when_allowed()
        {
            Services.AddTabBlazorTipTap(o => o.AllowUnsanitizedHtml = true);

            var cut = Render<RichTextView>(p => p.Add(v => v.Html, "<p>Hi</p>"));

            Assert.Equal("<p>Hi</p>", cut.Find(".rich-text-view").InnerHtml);
        }

        [Fact]
        public void Renders_nothing_for_empty_html_without_a_sanitizer()
        {
            var cut = Render<RichTextView>(p => p.Add(v => v.Html, null));

            Assert.Equal("", cut.Find(".rich-text-view").InnerHtml);
        }
    }

    public class RichTextMentionsTests
    {
        [Fact]
        public void Gets_distinct_ids_from_html_in_order()
        {
            var html = """
                <p>Hi <span class="rich-text-mention" data-type="mention" data-id="2" data-label="Björn">@Björn</span>
                and <span data-id='7' data-type='mention'>@Eva</span>, <span data-type="mention" data-id="2">@Björn</span>
                <span data-type="other" data-id="9">x</span> <span data-type="mention" data-id="a&amp;b">@A&amp;B</span></p>
                """;

            Assert.Equal(new[] { "2", "7", "a&b" }, RichTextMentions.GetIds(html));
        }

        [Fact]
        public void Gets_ids_from_json()
        {
            var json = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"mention","attrs":{"id":"5","label":"Anna"}},{"type":"text","text":" hi "},{"type":"mention","attrs":{"id":12}}]}]}""";

            Assert.Equal(new[] { "5", "12" }, RichTextMentions.GetIds(json, RichTextEditorFormat.Json));
        }

        [Fact]
        public void Gets_ids_from_markdown()
        {
            var markdown = "Thanks [@ id=\"2\" label=\"Björn Lind\"] and [@ id=\"7\"], and [@ id=\"2\" label=\"Björn Lind\"] again. [link](x) [@nope]";

            Assert.Equal(new[] { "2", "7" }, RichTextMentions.GetIds(markdown, RichTextEditorFormat.Markdown));
        }

        [Fact]
        public void Returns_empty_for_empty_or_invalid_content()
        {
            Assert.Empty(RichTextMentions.GetIds(null));
            Assert.Empty(RichTextMentions.GetIds("<p>none</p>"));
            Assert.Empty(RichTextMentions.GetIds("{not json", RichTextEditorFormat.Json));
        }
    }
}
