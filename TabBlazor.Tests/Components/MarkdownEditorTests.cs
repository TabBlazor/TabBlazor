using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor.Tests.Components
{
    public class MarkdownEditorTests : TabBlazorTestContext
    {
        private const string ModulePath = "./_content/TabBlazor/Components/Forms/MarkdownEditors/MarkdownEditor.razor.js";

        private bool interactive = true;

        // Set just before rendering: it resolves services, after which tests can no longer register any.
        private void UseRendererInfo() => SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));

        private sealed class FakeRenderer : IMarkdownRenderer
        {
            public int Calls { get; private set; }

            public MarkupString Render(string? markdown)
            {
                Calls++;
                return new MarkupString($"<p class=\"rendered\">{markdown}</p>");
            }
        }

        private sealed class Model
        {
            public string? Notes { get; set; }
        }

        private IRenderedComponent<MarkdownEditor> RenderEditor(string? value = null,
            Action<ComponentParameterCollectionBuilder<MarkdownEditor>>? parameters = null,
            Action<string?>? onChanged = null)
        {
            UseRendererInfo();
            return Render<MarkdownEditor>(p =>
            {
                p.Add(e => e.Value, value);
                p.Add(e => e.ValueChanged, onChanged ?? (_ => { }));
                parameters?.Invoke(p);
            });
        }

        [Fact]
        public void Renders_textarea_with_value_label_and_rows()
        {
            var cut = RenderEditor("# Hello", p => p.Add(e => e.Label, "Notes").Add(e => e.Rows, 3));

            var textarea = cut.Find("textarea.form-control.markdown-editor-input");
            Assert.Equal("# Hello", textarea.GetAttribute("value"));
            Assert.Equal("3", textarea.GetAttribute("rows"));
            Assert.Equal(textarea.Id, cut.Find("label.form-label").GetAttribute("for"));
        }

        [Fact]
        public void Uses_bound_member_path_as_form_field_name()
        {
            var model = new Model();
            var cut = RenderEditor(null, p => p.Add(e => e.ValueExpression, () => model.Notes));

            Assert.Equal("model.Notes", cut.Find("textarea").GetAttribute("name"));
        }

        [Fact]
        public void Name_parameter_overrides_bound_member_path()
        {
            var model = new Model();
            var cut = RenderEditor(null, p => p.Add(e => e.ValueExpression, () => model.Notes).Add(e => e.Name, "Input.Body"));

            Assert.Equal("Input.Body", cut.Find("textarea").GetAttribute("name"));
        }

        [Fact]
        public void Renders_plain_textarea_without_toolbar_when_not_interactive()
        {
            interactive = false;
            Services.AddSingleton<IMarkdownRenderer>(new FakeRenderer());

            var cut = RenderEditor("text", p => p.Add(e => e.Mode, MarkdownEditorMode.Split));

            Assert.Empty(cut.FindAll(".markdown-editor-toolbar"));
            Assert.Empty(cut.FindAll(".markdown-editor-preview"));
            Assert.Single(cut.FindAll("textarea"));
        }

        [Fact]
        public void Renders_toolbar_with_accessible_buttons_when_interactive()
        {
            var cut = RenderEditor();

            var toolbar = cut.Find("[role=toolbar]");
            Assert.Equal("Formatting", toolbar.GetAttribute("aria-label"));
            var bold = cut.Find("button[data-md-action=bold]");
            Assert.Equal("Bold (Ctrl+B)", bold.GetAttribute("title"));
            Assert.Equal("Control+B", bold.GetAttribute("aria-keyshortcuts"));
            Assert.Equal(4, cut.FindAll("[data-md-menu][aria-label=Heading] + [role=menu] [role=menuitem]").Count);
            Assert.Equal(2, cut.FindAll("[data-md-menu][aria-haspopup=menu]").Count);
        }

        [Fact]
        public void Hides_toolbar_when_ShowToolbar_is_false()
        {
            var cut = RenderEditor(null, p => p.Add(e => e.ShowToolbar, false));

            Assert.Empty(cut.FindAll(".markdown-editor-toolbar"));
        }

        [Fact]
        public void Has_no_view_buttons_or_preview_without_renderer()
        {
            var cut = RenderEditor("text", p => p.Add(e => e.Mode, MarkdownEditorMode.Split));

            Assert.Empty(cut.FindAll("[data-md-mode]"));
            Assert.Empty(cut.FindAll(".markdown-editor-preview"));
        }

        [Fact]
        public void Shows_rendered_preview_side_by_side_with_registered_renderer()
        {
            Services.AddSingleton<IMarkdownRenderer>(new FakeRenderer());

            var cut = RenderEditor("text", p => p.Add(e => e.Mode, MarkdownEditorMode.Split));

            Assert.Equal(["Write", "Hybrid", "Preview", "Side by side"], cut.FindAll("[data-md-mode]").Select(b => b.TextContent));
            Assert.Contains("markdown-editor-body-split", cut.Find(".markdown-editor-body").ClassList);
            Assert.Equal("text", cut.Find(".markdown-editor-preview p.rendered").TextContent);
        }

        [Fact]
        public void Renderer_parameter_takes_precedence_over_registered_renderer()
        {
            var registered = new FakeRenderer();
            var parameter = new FakeRenderer();
            Services.AddSingleton<IMarkdownRenderer>(registered);

            RenderEditor("text", p => p.Add(e => e.Mode, MarkdownEditorMode.Preview).Add(e => e.Renderer, parameter));

            Assert.Equal(0, registered.Calls);
            Assert.Equal(1, parameter.Calls);
        }

        [Fact]
        public void Shows_empty_preview_message_for_empty_text()
        {
            Services.AddSingleton<IMarkdownRenderer>(new FakeRenderer());

            var cut = RenderEditor("", p => p.Add(e => e.Mode, MarkdownEditorMode.Preview));

            Assert.Equal("Nothing to preview", cut.Find(".markdown-editor-preview").TextContent.Trim());
            Assert.Contains("markdown-editor-body-preview", cut.Find(".markdown-editor-body").ClassList);
        }

        [Fact]
        public void Disables_formatting_buttons_in_preview()
        {
            Services.AddSingleton<IMarkdownRenderer>(new FakeRenderer());

            var cut = RenderEditor("text", p => p.Add(e => e.Mode, MarkdownEditorMode.Preview));

            Assert.True(cut.Find("button[data-md-action=bold]").HasAttribute("disabled"));
            Assert.False(cut.FindAll("[data-md-mode]")[0].HasAttribute("disabled"));
        }

        [Fact]
        public void Clicking_view_button_raises_ModeChanged()
        {
            Services.AddSingleton<IMarkdownRenderer>(new FakeRenderer());
            MarkdownEditorMode? changed = null;

            var cut = RenderEditor("text", p => p.Add(e => e.ModeChanged, mode => changed = mode));
            cut.FindAll("[data-md-mode]")[2].Click();

            Assert.Equal(MarkdownEditorMode.Preview, changed);
            Assert.Equal("true", cut.FindAll("[data-md-mode]")[2].GetAttribute("aria-pressed"));
        }

        [Fact]
        public void Hybrid_mode_styles_the_text_area_without_preview_or_renderer()
        {
            var module = JSInterop.SetupModule(ModulePath);

            var cut = RenderEditor("**text**", p => p.Add(e => e.Mode, MarkdownEditorMode.Hybrid));

            Assert.Contains("markdown-editor-body-hybrid", cut.Find(".markdown-editor-body").ClassList);
            Assert.Equal("true", cut.Find(".markdown-editor-highlight").GetAttribute("aria-hidden"));
            Assert.Empty(cut.FindAll(".markdown-editor-preview"));
            Assert.False(cut.Find("button[data-md-action=bold]").HasAttribute("disabled"));
            var options = Assert.Single(module.Invocations, i => i.Identifier == "attach").Arguments[1]!;
            Assert.Equal(true, options.GetType().GetProperty("hybrid")!.GetValue(options));
        }

        [Fact]
        public void Hybrid_mode_falls_back_to_plain_text_area_when_not_interactive()
        {
            interactive = false;

            var cut = RenderEditor("text", p => p.Add(e => e.Mode, MarkdownEditorMode.Hybrid));

            Assert.DoesNotContain("markdown-editor-body-hybrid", cut.Find(".markdown-editor-body").ClassList);
            Assert.Empty(cut.FindAll(".markdown-editor-highlight"));
        }

        private static string[] ModeButtons(IRenderedComponent<MarkdownEditor> cut) =>
            cut.FindAll("[data-md-mode]").Select(b => b.TextContent).ToArray();

        [Fact]
        public void Modes_limits_and_orders_the_view_buttons()
        {
            Services.AddSingleton<IMarkdownRenderer>(new FakeRenderer());

            var cut = RenderEditor("text", p => p.Add(e => e.Modes, [MarkdownEditorMode.Split, MarkdownEditorMode.Edit]));

            Assert.Equal(["Side by side", "Write"], ModeButtons(cut));
            Assert.Equal("true", cut.FindAll("[data-md-mode]")[1].GetAttribute("aria-pressed"));
        }

        [Fact]
        public void Modes_without_renderer_offers_only_write_and_hybrid()
        {
            var cut = RenderEditor("text", p => p.Add(e => e.Modes,
                [MarkdownEditorMode.Edit, MarkdownEditorMode.Hybrid, MarkdownEditorMode.Preview]));

            Assert.Equal(["Write", "Hybrid"], ModeButtons(cut));
            cut.FindAll("[data-md-mode]")[1].Click();
            Assert.Contains("markdown-editor-body-hybrid", cut.Find(".markdown-editor-body").ClassList);
        }

        [Fact]
        public void Single_mode_has_no_view_buttons_and_is_used()
        {
            Services.AddSingleton<IMarkdownRenderer>(new FakeRenderer());

            var cut = RenderEditor("text", p => p.Add(e => e.Modes, [MarkdownEditorMode.Hybrid]));

            Assert.Empty(cut.FindAll("[data-md-mode]"));
            Assert.Contains("markdown-editor-body-hybrid", cut.Find(".markdown-editor-body").ClassList);
        }

        [Fact]
        public void Mode_that_is_not_offered_shows_the_first_offered_view()
        {
            Services.AddSingleton<IMarkdownRenderer>(new FakeRenderer());

            var cut = RenderEditor("text", p => p
                .Add(e => e.Mode, MarkdownEditorMode.Split)
                .Add(e => e.Modes, [MarkdownEditorMode.Hybrid, MarkdownEditorMode.Edit]));

            Assert.Empty(cut.FindAll(".markdown-editor-preview"));
            Assert.Contains("markdown-editor-body-hybrid", cut.Find(".markdown-editor-body").ClassList);
            Assert.Equal("true", cut.FindAll("[data-md-mode]")[0].GetAttribute("aria-pressed"));
        }

        [Fact]
        public void Without_renderer_and_Modes_there_are_no_view_buttons_but_Mode_works()
        {
            var cut = RenderEditor("text", p => p.Add(e => e.Mode, MarkdownEditorMode.Hybrid));

            Assert.Empty(cut.FindAll("[data-md-mode]"));
            Assert.Contains("markdown-editor-body-hybrid", cut.Find(".markdown-editor-body").ClassList);
        }

        [Fact]
        public void Change_event_raises_ValueChanged_and_notifies_edit_context()
        {
            var model = new Model();
            var editContext = new EditContext(model);
            string? changed = null;

            UseRendererInfo();
            var cut = Render<CascadingValue<EditContext>>(p => p
                .Add(c => c.Value, editContext)
                .AddChildContent<MarkdownEditor>(e => e
                    .Add(x => x.Value, model.Notes)
                    .Add(x => x.ValueChanged, value => changed = value)
                    .Add(x => x.ValueExpression, () => model.Notes!)));

            cut.Find("textarea").Change("**new**");

            Assert.Equal("**new**", changed);
            Assert.True(editContext.IsModified(() => model.Notes!));
        }

        [Fact]
        public void Change_event_with_unchanged_text_does_not_raise_ValueChanged()
        {
            var calls = 0;
            var cut = RenderEditor("same", onChanged: _ => calls++);

            cut.Find("textarea").Change("same");

            Assert.Equal(0, calls);
        }

        [Fact]
        public void Works_without_edit_context()
        {
            string? changed = null;
            var model = new Model();

            var cut = RenderEditor(null, p => p.Add(e => e.ValueExpression, () => model.Notes), value => changed = value);
            cut.Find("textarea").Change("text");

            Assert.Equal("text", changed);
            Assert.Null(cut.Find("textarea").GetAttribute("aria-invalid"));
        }

        [Fact]
        public void Adds_validation_classes_from_edit_context()
        {
            var model = new Model();
            var editContext = new EditContext(model);
            var messages = new ValidationMessageStore(editContext);
            messages.Add(() => model.Notes!, "Required");

            UseRendererInfo();
            var cut = Render<CascadingValue<EditContext>>(p => p
                .Add(c => c.Value, editContext)
                .AddChildContent<MarkdownEditor>(e => e
                    .Add(x => x.Value, model.Notes)
                    .Add(x => x.ValueExpression, () => model.Notes!)));

            Assert.Contains("invalid", cut.Find(".markdown-editor").ClassList);
            Assert.Equal("true", cut.Find("textarea").GetAttribute("aria-invalid"));
        }

        [Fact]
        public void Uses_app_wide_texts_from_options()
        {
            Services.Configure<TablerOptions>(o => o.MarkdownEditorText = new MarkdownEditorText { Bold = "Fet" });

            var cut = RenderEditor();

            Assert.Equal("Fet", cut.Find("button[data-md-action=bold]").GetAttribute("aria-label"));
        }

        [Fact]
        public void Text_parameter_takes_precedence_over_options()
        {
            Services.Configure<TablerOptions>(o => o.MarkdownEditorText = new MarkdownEditorText { Bold = "Fet" });

            var cut = RenderEditor(null, p => p.Add(e => e.Text, new MarkdownEditorText { Bold = "Gras" }));

            Assert.Equal("Gras", cut.Find("button[data-md-action=bold]").GetAttribute("aria-label"));
        }

        [Fact]
        public void Table_picker_offers_sizes_with_localized_names()
        {
            var cut = RenderEditor(null, p => p.Add(e => e.Text, new MarkdownEditorText { TableSize = "Tabell {0} × {1}" }));

            var cells = cut.FindAll("button[data-md-action=table]");
            Assert.Equal(36, cells.Count);
            var last = cells[^1];
            Assert.Equal("6", last.GetAttribute("data-md-columns"));
            Assert.Equal("6", last.GetAttribute("data-md-rows"));
            Assert.Equal("Tabell 3 × 2", cut.Find("button[data-md-columns='3'][data-md-rows='2']").GetAttribute("aria-label"));
            Assert.Equal("Table", cut.Find(".markdown-editor-table-menu").GetAttribute("aria-label"));
        }

        private IRenderedComponent<MarkdownEditor> OpenTableDialog(MarkdownTableSource source, out BunitJSModuleInterop module)
        {
            module = JSInterop.SetupModule(ModulePath);
            module.Setup<MarkdownTableSource?>("readTable", _ => true).SetResult(source);
            var cut = RenderEditor("text");
            cut.Find("[data-md-close-menu]").Click();
            return cut;
        }

        private static object? Payload(BunitJSModuleInterop module, string name)
        {
            var payload = module.Invocations.Single(i => i.Identifier == "writeTable").Arguments[1]!;
            return payload.GetType().GetProperty(name)!.GetValue(payload);
        }

        [Fact]
        public void Table_editor_opens_the_table_at_the_caret_and_saves_it()
        {
            var cut = OpenTableDialog(new MarkdownTableSource
            {
                Found = true, Start = 5, End = 40, Text = "table", Indent = "",
                Rows = [["Name", "Qty"], [":--", "---"], ["Apples", "3"]]
            }, out var module);

            Assert.Equal("Edit table", cut.Find(".modal-title").TextContent);
            var inputs = cut.FindAll("[data-md-table-editor] input[data-md-row]");
            Assert.Equal(["Name", "Qty", "Apples", "3"], inputs.Select(i => i.GetAttribute("value")));
            Assert.Equal("Header, column 1", inputs[0].GetAttribute("aria-label"));
            Assert.Equal("Row 1, column 2", inputs[3].GetAttribute("aria-label"));
            Assert.Equal("true", cut.FindAll("[aria-label='Align left']")[0].GetAttribute("aria-pressed"));

            cut.Find("input[data-md-row='1'][data-md-column='0']").Change("Pears | plums");
            cut.FindAll("[aria-label='Align right']")[1].Click();
            cut.Find("[data-md-table-save]").Click();

            Assert.Equal(true, Payload(module, "found"));
            Assert.Equal(5, Payload(module, "start"));
            var rows = (string[][])Payload(module, "rows")!;
            Assert.Equal(["Name", "Qty"], rows[0]);
            Assert.Equal([":--", "--:"], rows[1]);
            Assert.Equal(["Pears \\| plums", "3"], rows[2]);
            Assert.Empty(cut.FindAll("[data-md-table-editor]"));
        }

        [Fact]
        public void Table_editor_starts_a_new_table_when_the_caret_is_not_in_one()
        {
            var cut = OpenTableDialog(new MarkdownTableSource { Found = false, Start = 3, End = 3 }, out _);

            Assert.Equal("Insert table", cut.Find(".modal-title").TextContent);
            Assert.Equal(9, cut.FindAll("[data-md-table-editor] input[data-md-row]").Count);
            Assert.Equal("Column 1", cut.Find("input[data-md-row='0'][data-md-column='0']").GetAttribute("value"));
        }

        [Fact]
        public void Table_editor_pastes_spreadsheet_cells_and_edits_rows_and_columns()
        {
            var cut = OpenTableDialog(new MarkdownTableSource
            {
                Found = true, Rows = [["A", "B"], ["---", "---"], ["1", "2"]]
            }, out _);

            cut.Find("[data-md-table-paste]").Change("{\"row\":1,\"column\":1,\"text\":\"x\\ty\\tz\\r\\nu\\tv\\tw\\r\\n\"}");
            Assert.Equal(4, cut.FindAll("input[data-md-row='0']").Count);
            Assert.Equal("z", cut.Find("input[data-md-row='1'][data-md-column='3']").GetAttribute("value"));
            Assert.Equal(3, cut.FindAll("input[data-md-column='0']").Count);

            cut.Find("[aria-label='Delete column']").Click();
            Assert.Equal("x", cut.Find("input[data-md-row='1'][data-md-column='0']").GetAttribute("value"));
            cut.FindAll("[aria-label='Move row down']")[0].Click();
            Assert.Equal("u", cut.Find("input[data-md-row='1'][data-md-column='0']").GetAttribute("value"));
            cut.Find("[data-md-table-add-row]").Click();
            Assert.Equal(4, cut.FindAll("input[data-md-column='0']").Count);
        }

        [Fact]
        public void Cancelling_the_table_editor_writes_nothing()
        {
            var cut = OpenTableDialog(new MarkdownTableSource { Found = false }, out var module);

            cut.FindAll(".markdown-table-footer button")[0].Click();

            Assert.Empty(cut.FindAll("[data-md-table-editor]"));
            Assert.DoesNotContain(module.Invocations, i => i.Identifier == "writeTable");
        }

        [Fact]
        public void Shows_image_button_without_upload_handler()
        {
            var cut = RenderEditor();

            Assert.Single(cut.FindAll("button[data-md-action=image]"));
            Assert.Empty(cut.FindAll("button[data-md-action=upload]"));
            Assert.Empty(cut.FindAll("input[type=file]"));
        }

        [Fact]
        public void Shows_attach_button_and_file_input_with_upload_handler()
        {
            var cut = RenderEditor(null, p => p
                .Add(e => e.UploadFile, (_, _) => Task.FromResult(new MarkdownUploadResult("/f")))
                .Add(e => e.AcceptedFileTypes, "image/*,.pdf"));

            Assert.Equal("Attach files", cut.Find("button[data-md-action=upload]").GetAttribute("aria-label"));
            Assert.Equal("image/*,.pdf", cut.Find("input[type=file]").GetAttribute("accept"));
        }

        [Fact]
        public void Uploads_file_and_replaces_placeholder_with_image()
        {
            var module = JSInterop.SetupModule(ModulePath);
            module.Setup<bool>("replace", _ => true).SetResult(true);
            var states = new List<bool>();
            MarkdownUploadFile? received = null;

            var cut = RenderEditor(null, p => p
                .Add(e => e.UploadFile, async (file, token) =>
                {
                    received = file;
                    await using var stream = file.OpenReadStream(token);
                    await stream.CopyToAsync(System.IO.Stream.Null, token);
                    return new MarkdownUploadResult("/files/photo one.png");
                })
                .Add(e => e.OnUploadStateChanged, uploading => states.Add(uploading)));

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[2048], "photo [1].png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.Equal(new[] { true, false }, states));
            Assert.Equal("photo [1].png", received!.Name);
            var insert = Assert.Single(module.Invocations, i => i.Identifier == "insert");
            Assert.Matches(@"^!\[Uploading photo \\\[1\\\]\.png…\]\(#upload-[0-9a-f]{12}\)$", (string)insert.Arguments[1]!);
            var replace = Assert.Single(module.Invocations, i => i.Identifier == "replace");
            Assert.Equal(@"![photo \[1\]](/files/photo%20one.png)", replace.Arguments[2]);
            Assert.Empty(cut.FindAll(".markdown-editor-upload"));
        }

        [Fact]
        public void Links_non_image_files_and_honours_result_overrides()
        {
            var module = JSInterop.SetupModule(ModulePath);
            module.Setup<bool>("replace", _ => true).SetResult(true);

            var cut = RenderEditor(null, p => p
                .Add(e => e.AcceptedFileTypes, "*")
                .Add(e => e.UploadFile, (file, _) => Task.FromResult(file.Name == "b.png"
                    ? new MarkdownUploadResult("/b") { Embed = false, Text = "Diagram" }
                    : new MarkdownUploadResult("/a"))));

            cut.FindComponent<InputFile>().UploadFiles(
                InputFileContent.CreateFromText("x", "a.pdf", contentType: "application/pdf"),
                InputFileContent.CreateFromBinary(new byte[1], "b.png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.Equal(2, module.Invocations.Count(i => i.Identifier == "replace")));
            var markdown = module.Invocations.Where(i => i.Identifier == "replace").Select(i => (string)i.Arguments[2]!).ToList();
            Assert.Contains("[a.pdf](/a)", markdown);
            Assert.Contains("[Diagram](/b)", markdown);
        }

        [Fact]
        public void Rejects_files_that_are_too_large_or_not_accepted()
        {
            var module = JSInterop.SetupModule(ModulePath);
            var errors = new List<MarkdownUploadError>();
            var uploads = 0;

            var cut = RenderEditor(null, p => p
                .Add(e => e.MaxFileSize, 10)
                .Add(e => e.UploadFile, (_, _) => { uploads++; return Task.FromResult(new MarkdownUploadResult("/x")); })
                .Add(e => e.OnUploadError, error => errors.Add(error)));

            cut.FindComponent<InputFile>().UploadFiles(
                InputFileContent.CreateFromBinary(new byte[11], "big.png", contentType: "image/png"),
                InputFileContent.CreateFromText("x", "notes.txt", contentType: "text/plain"));

            cut.WaitForAssertion(() => Assert.Equal(2, errors.Count));
            Assert.Equal(0, uploads);
            Assert.DoesNotContain(module.Invocations, i => i.Identifier == "insert");
            Assert.Contains("big.png is larger than 10 B.", errors.Select(e => e.Message));
            Assert.Contains("notes.txt is not an accepted file type.", errors.Select(e => e.Message));
            Assert.Equal(2, cut.FindAll(".markdown-editor-upload-error").Count);
        }

        [Fact]
        public void Shows_message_from_upload_exception_and_removes_placeholder()
        {
            var module = JSInterop.SetupModule(ModulePath);
            module.Setup<bool>("replace", _ => true).SetResult(true);
            MarkdownUploadError? error = null;

            var cut = RenderEditor(null, p => p
                .Add(e => e.UploadFile, (_, _) => throw new MarkdownUploadException("Images are limited to 1 per comment"))
                .Add(e => e.OnUploadError, e => error = e));

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[1], "a.png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.NotNull(error));
            Assert.Equal("Images are limited to 1 per comment", cut.Find(".markdown-editor-upload-error").TextContent.Trim());
            Assert.Equal("", module.Invocations.Single(i => i.Identifier == "replace").Arguments[2]);
        }

        [Fact]
        public void Shows_generic_message_when_upload_handler_fails()
        {
            JSInterop.SetupModule(ModulePath).Setup<bool>("replace", _ => true).SetResult(true);
            MarkdownUploadError? error = null;

            var cut = RenderEditor(null, p => p
                .Add(e => e.UploadFile, (_, _) => throw new InvalidOperationException("storage down"))
                .Add(e => e.OnUploadError, e => error = e));

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[1], "a.png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.NotNull(error));
            Assert.Equal("Could not upload a.png.", error!.Message);
            Assert.IsType<InvalidOperationException>(error.Exception);
        }

        [Fact]
        public void Raises_discarded_when_placeholder_was_deleted()
        {
            JSInterop.SetupModule(ModulePath).Setup<bool>("replace", _ => true).SetResult(false);
            MarkdownUploadResult? discarded = null;

            var cut = RenderEditor(null, p => p
                .Add(e => e.UploadFile, (_, _) => Task.FromResult(new MarkdownUploadResult("/orphan")))
                .Add(e => e.OnUploadDiscarded, result => discarded = result));

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[1], "a.png", contentType: "image/png"));

            cut.WaitForAssertion(() => Assert.Equal("/orphan", discarded?.Url));
        }

        [Fact]
        public void Cancel_button_cancels_upload_and_removes_placeholder()
        {
            var module = JSInterop.SetupModule(ModulePath);
            module.Setup<bool>("replace", _ => true).SetResult(true);
            var started = new TaskCompletionSource();

            var cut = RenderEditor(null, p => p
                .Add(e => e.UploadFile, async (_, token) =>
                {
                    started.SetResult();
                    await Task.Delay(Timeout.Infinite, token);
                    return new MarkdownUploadResult("/never");
                }));

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[1], "a.png", contentType: "image/png"));
            cut.WaitForAssertion(() => Assert.True(started.Task.IsCompleted));
            Assert.True(cut.Instance.IsUploading);

            cut.Find(".markdown-editor-upload button").Click();

            cut.WaitForAssertion(() => Assert.False(cut.Instance.IsUploading));
            Assert.Equal("", module.Invocations.Single(i => i.Identifier == "replace").Arguments[2]);
            Assert.Empty(cut.FindAll(".markdown-editor-upload-error"));
        }
    }
}
