using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace TabBlazor.Tests.Components
{
    public class InputStatusTests : TabBlazorTestContext
    {
        private const string Input = "<input class=\"form-control\" />";
        private static readonly TimeSpan SaveDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan SavedDuration = TimeSpan.FromSeconds(2);
        private readonly FakeTimeProvider time = new();

        public InputStatusTests()
        {
            Services.AddSingleton<TimeProvider>(time);
        }

        private IRenderedComponent<InputStatus> RenderAutosave(Func<Task> save, Action<ComponentParameterCollectionBuilder<InputStatus>> configure = null, TimeSpan saveDelay = default) =>
            Render<InputStatus>(p =>
            {
                p.Add(s => s.Value, 1)
                    .Add(s => s.SaveDelay, saveDelay)
                    .Add(s => s.SavedDuration, SavedDuration)
                    .Add(s => s.Save, save);
                configure?.Invoke(p);
            });

        private IRenderedComponent<InputStatus> RenderRecordingAutosave(List<object> savedValues, TimeSpan saveDelay = default)
        {
            IRenderedComponent<InputStatus> cut = null;
            cut = RenderAutosave(() => { savedValues.Add(cut.Instance.Value); return Task.CompletedTask; }, saveDelay: saveDelay);
            return cut;
        }

        private static void ChangeValue(IRenderedComponent<InputStatus> cut, object value) =>
            cut.Render(p => p.Add(s => s.Value, value));

        [Fact]
        public void Wraps_child_content_without_indicator_when_idle()
        {
            var cut = Render<InputStatus>(p => p.AddChildContent(Input));
            Assert.Contains("input-status", cut.Find("div").ClassList);
            Assert.NotNull(cut.Find("div > input"));
            Assert.Empty(cut.FindAll(".input-status-addon"));
        }

        [Fact]
        public void Shows_spinner_when_state_is_saving()
        {
            var cut = Render<InputStatus>(p => p.Add(s => s.State, InputSaveState.Saving));
            Assert.NotNull(cut.Find(".input-status-addon .spinner-border"));
            Assert.Contains("input-status-busy", cut.Find("div").ClassList);
            Assert.Equal("true", cut.Find("div").GetAttribute("aria-busy"));
        }

        [Fact]
        public void Shows_saved_bubble_when_state_is_saved()
        {
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Saved)
                .Add(s => s.SavedText, "Done"));
            Assert.Equal("Done", cut.Find(".input-status-addon .input-status-bubble-saved").TextContent);
        }

        [Fact]
        public void Hides_saved_bubble_and_raises_saved_hidden_after_duration()
        {
            var hidden = false;
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Saved)
                .Add(s => s.SavedDuration, SavedDuration)
                .Add(s => s.SavedHidden, () => hidden = true));

            time.Advance(SavedDuration);

            cut.WaitForAssertion(() =>
            {
                Assert.Empty(cut.FindAll(".input-status-bubble-saved"));
                Assert.True(hidden);
            });
        }

        [Fact]
        public void Adds_active_and_failed_classes_when_state_is_error()
        {
            var cut = Render<InputStatus>(p => p.Add(s => s.State, InputSaveState.Error));
            Assert.Contains("input-status-active", cut.Find("div").ClassList);
            Assert.Contains("input-status-failed", cut.Find("div").ClassList);
        }

        [Fact]
        public void Shows_error_text_without_retry_when_state_is_error_without_save()
        {
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Error)
                .Add(s => s.ErrorText, "Failed"));
            Assert.Equal("Failed", cut.Find(".input-status-bubble-error").TextContent);
            Assert.Contains("text-danger", cut.Find(".input-status-addon").ClassList);
            Assert.Empty(cut.FindAll(".input-status-retry"));
        }

        [Fact]
        public void Does_not_save_initial_value()
        {
            var savedValues = new List<object>();
            var cut = RenderRecordingAutosave(savedValues);

            ChangeValue(cut, 2);

            cut.WaitForAssertion(() => Assert.Equal([2], savedValues));
        }

        [Fact]
        public void Saves_after_value_changes()
        {
            var saves = 0;
            var cut = RenderAutosave(() => { saves++; return Task.CompletedTask; });

            ChangeValue(cut, 2);

            cut.WaitForAssertion(() =>
            {
                Assert.Equal(1, saves);
                Assert.NotNull(cut.Find(".input-status-bubble-saved"));
            });
        }

        [Fact]
        public void Saves_changes_made_within_save_delay_once()
        {
            var savedValues = new List<object>();
            var cut = RenderRecordingAutosave(savedValues, SaveDelay);

            ChangeValue(cut, 2);
            ChangeValue(cut, 3);
            ChangeValue(cut, 4);
            time.Advance(SaveDelay);

            cut.WaitForAssertion(() => Assert.Equal([4], savedValues));
        }

        [Fact]
        public void Does_not_save_when_value_changes_back_before_delay()
        {
            var savedValues = new List<object>();
            var cut = RenderRecordingAutosave(savedValues, SaveDelay);

            ChangeValue(cut, 2);
            ChangeValue(cut, 1);
            time.Advance(SaveDelay);
            ChangeValue(cut, 3);
            time.Advance(SaveDelay);

            cut.WaitForAssertion(() => Assert.Equal([3], savedValues));
        }

        [Fact]
        public void Saves_again_when_value_changes_during_save()
        {
            var savedValues = new List<object>();
            var firstSave = new TaskCompletionSource();
            IRenderedComponent<InputStatus> cut = null;
            cut = RenderAutosave(() =>
            {
                savedValues.Add(cut.Instance.Value);
                return savedValues.Count == 1 ? firstSave.Task : Task.CompletedTask;
            });

            ChangeValue(cut, 2);
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".spinner-border")));
            ChangeValue(cut, 3);
            firstSave.SetResult();

            cut.WaitForAssertion(() => Assert.Equal([2, 3], savedValues));
        }

        [Fact]
        public void Shows_error_and_retries_when_error_icon_is_clicked()
        {
            var saves = 0;
            Exception failure = null;
            var cut = RenderAutosave(
                () => ++saves == 1 ? throw new InvalidOperationException("boom") : Task.CompletedTask,
                p => p.Add(s => s.SaveFailed, ex => failure = ex));

            ChangeValue(cut, 2);
            cut.WaitForAssertion(() => Assert.Equal("Could not save", cut.Find(".input-status-bubble-error").TextContent));
            Assert.Equal("boom", failure?.Message);

            cut.Find(".input-status-retry").Click();

            cut.WaitForAssertion(() =>
            {
                Assert.Equal(2, saves);
                Assert.NotNull(cut.Find(".input-status-bubble-saved"));
            });
        }

        [Fact]
        public void Retries_on_next_change_after_error()
        {
            var saves = 0;
            var cut = RenderAutosave(() => ++saves == 1 ? throw new InvalidOperationException() : Task.CompletedTask);

            ChangeValue(cut, 2);
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".input-status-bubble-error")));

            ChangeValue(cut, 3);

            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".input-status-bubble-saved")));
            Assert.Equal(2, saves);
        }

        [Fact]
        public void Clears_error_when_value_changes_back_to_saved_value()
        {
            var cut = RenderAutosave(() => throw new InvalidOperationException());

            ChangeValue(cut, 2);
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".input-status-bubble-error")));

            ChangeValue(cut, 1);

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".input-status-addon")));
        }

        [Fact]
        public async Task Does_not_show_saved_when_disposed_during_save()
        {
            var save = new TaskCompletionSource();
            var hidden = false;
            var cut = RenderAutosave(() => save.Task, p => p.Add(s => s.SavedHidden, () => hidden = true));

            ChangeValue(cut, 2);
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".spinner-border")));
            cut.Instance.Dispose();
            save.SetResult();
            await cut.InvokeAsync(() => { });
            time.Advance(SavedDuration);

            Assert.Contains("input-status-busy", cut.Find("div").ClassList);
            Assert.False(hidden);
        }

        [Fact]
        public void Formats_saved_duration_with_invariant_culture()
        {
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
            try
            {
                var cut = Render<InputStatus>(p => p
                    .Add(s => s.State, InputSaveState.Saved)
                    .Add(s => s.SavedDuration, TimeSpan.FromMilliseconds(1500.5)));

                Assert.Equal("--input-status-duration: 1500.5ms", cut.Find(".input-status-bubble-saved").GetAttribute("style"));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Saves_once_blocking_field_becomes_valid(bool setFor)
        {
            var model = new FormModel();
            var editContext = new EditContext(model);
            var messages = new ValidationMessageStore(editContext);
            messages.Add(editContext.Field(setFor ? nameof(FormModel.Age) : nameof(FormModel.Name)), "Invalid");
            var invalidAtSave = new List<bool>();
            var cut = RenderAutosave(
                () => { invalidAtSave.Add(editContext.GetValidationMessages().Any()); return Task.CompletedTask; },
                p =>
                {
                    p.AddCascadingValue(editContext);
                    if (setFor)
                    {
                        p.Add(s => s.For, () => model.Age);
                    }
                });

            ChangeValue(cut, 200);
            Assert.Empty(invalidAtSave);
            messages.Clear();
            await cut.InvokeAsync(editContext.NotifyValidationStateChanged);

            cut.WaitForAssertion(() => Assert.Equal([false], invalidAtSave));
        }

        [Fact]
        public void Ignores_errors_on_other_fields_when_for_is_set()
        {
            var model = new FormModel();
            var editContext = new EditContext(model);
            new ValidationMessageStore(editContext).Add(editContext.Field(nameof(FormModel.Name)), "Required");
            var saves = 0;
            var cut = RenderAutosave(
                () => { saves++; return Task.CompletedTask; },
                p => p.AddCascadingValue(editContext).Add(s => s.For, () => model.Age));

            ChangeValue(cut, 5);

            cut.WaitForAssertion(() => Assert.Equal(1, saves));
        }

        [Fact]
        public void Marks_field_unmodified_after_save()
        {
            var model = new FormModel();
            var editContext = new EditContext(model);
            var field = editContext.Field(nameof(FormModel.Age));
            editContext.NotifyFieldChanged(field);
            var cut = RenderAutosave(
                () => Task.CompletedTask,
                p => p.AddCascadingValue(editContext).Add(s => s.For, () => model.Age));

            ChangeValue(cut, 5);

            cut.WaitForAssertion(() => Assert.False(editContext.IsModified(field)));
        }

        [Fact]
        public void Announces_state_in_live_region()
        {
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Saved)
                .Add(s => s.SavedText, "Done"));

            Assert.Equal("Done", cut.Find("[role=status]").TextContent);
            Assert.Equal("true", cut.Find(".input-status-addon").GetAttribute("aria-hidden"));

            cut.Render(p => p.Add(s => s.State, InputSaveState.Idle));
            Assert.Equal("", cut.Find("[role=status]").TextContent);
        }

        [Fact]
        public void Raises_state_changed_when_saved_fades()
        {
            var states = new List<InputSaveState>();
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Saved)
                .Add(s => s.SavedDuration, SavedDuration)
                .Add(s => s.StateChanged, state => states.Add(state)));

            time.Advance(SavedDuration);

            cut.WaitForAssertion(() => Assert.Equal([InputSaveState.Idle], states));
        }

        [Fact]
        public void Shows_saved_again_after_fade_when_state_is_bound()
        {
            var state = InputSaveState.Saved;
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, state)
                .Add(s => s.SavedDuration, SavedDuration)
                .Add(s => s.StateChanged, value => state = value));
            time.Advance(SavedDuration);
            cut.WaitForAssertion(() => Assert.Equal(InputSaveState.Idle, state));

            cut.Render(p => p.Add(s => s.State, InputSaveState.Idle));
            cut.Render(p => p.Add(s => s.State, InputSaveState.Saved));

            Assert.NotNull(cut.Find(".input-status-bubble-saved"));
        }

        [Fact]
        public void Saves_pending_change_when_disposed_before_delay()
        {
            var saves = 0;
            var cut = RenderAutosave(
                () => { saves++; return Task.CompletedTask; },
                saveDelay: SaveDelay);

            ChangeValue(cut, 2);
            cut.Instance.Dispose();

            Assert.Equal(1, saves);
        }

        [Fact]
        public void Reports_exception_from_saved_hidden()
        {
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Saved)
                .Add(s => s.SavedDuration, SavedDuration)
                .Add(s => s.SavedHidden, () => throw new InvalidOperationException("hidden failed")));

            Assert.Throws<InvalidOperationException>(() =>
            {
                time.Advance(SavedDuration);
                cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".input-status-bubble-saved")));
            });
        }

        private class FormModel
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }
    }
}
