using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor.Tests.Components
{
    public class InputStatusTests : TabBlazorTestContext
    {
        private const string Input = "<input class=\"form-control\" />";
        private static readonly TimeSpan ShortDelay = TimeSpan.FromMilliseconds(1);

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
            Assert.Equal("Done", cut.Find(".input-status-bubble-saved").TextContent);
        }

        [Fact]
        public void Hides_saved_bubble_and_raises_saved_hidden_after_duration()
        {
            var hidden = false;
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Saved)
                .Add(s => s.SavedDuration, TimeSpan.FromMilliseconds(10))
                .Add(s => s.SavedHidden, () => hidden = true));

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
            IRenderedComponent<InputStatus> cut = null;
            cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => { savedValues.Add(cut.Instance.Value); return Task.CompletedTask; }));

            cut.Render(p => p.Add(s => s.Value, 2));

            cut.WaitForAssertion(() => Assert.Equal([2], savedValues));
        }

        [Fact]
        public void Saves_after_value_changes()
        {
            var saves = 0;
            var cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => { saves++; return Task.CompletedTask; }));

            cut.Render(p => p.Add(s => s.Value, 2));

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
            IRenderedComponent<InputStatus> cut = null;
            cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, TimeSpan.FromMilliseconds(100))
                .Add(s => s.Save, () => { savedValues.Add(cut.Instance.Value); return Task.CompletedTask; }));

            cut.Render(p => p.Add(s => s.Value, 2));
            cut.Render(p => p.Add(s => s.Value, 3));
            cut.Render(p => p.Add(s => s.Value, 4));

            cut.WaitForAssertion(() => Assert.Equal([4], savedValues));
        }

        [Fact]
        public async Task Does_not_save_when_value_changes_back_before_delay()
        {
            var savedValues = new List<object>();
            IRenderedComponent<InputStatus> cut = null;
            cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, TimeSpan.FromMilliseconds(20))
                .Add(s => s.Save, () => { savedValues.Add(cut.Instance.Value); return Task.CompletedTask; }));

            cut.Render(p => p.Add(s => s.Value, 2));
            cut.Render(p => p.Add(s => s.Value, 1));
            await Task.Delay(100);
            cut.Render(p => p.Add(s => s.Value, 3));

            cut.WaitForAssertion(() => Assert.Equal([3], savedValues));
        }

        [Fact]
        public void Saves_again_when_value_changes_during_save()
        {
            var savedValues = new List<object>();
            var firstSave = new TaskCompletionSource();
            IRenderedComponent<InputStatus> cut = null;
            cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () =>
                {
                    savedValues.Add(cut.Instance.Value);
                    return savedValues.Count == 1 ? firstSave.Task : Task.CompletedTask;
                }));

            cut.Render(p => p.Add(s => s.Value, 2));
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".spinner-border")));
            cut.Render(p => p.Add(s => s.Value, 3));
            firstSave.SetResult();

            cut.WaitForAssertion(() => Assert.Equal([2, 3], savedValues));
        }

        [Fact]
        public void Shows_error_and_retries_when_error_icon_is_clicked()
        {
            var saves = 0;
            Exception failure = null;
            var cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => ++saves == 1 ? throw new InvalidOperationException("boom") : Task.CompletedTask)
                .Add(s => s.SaveFailed, ex => failure = ex));

            cut.Render(p => p.Add(s => s.Value, 2));
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
            var cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => ++saves == 1 ? throw new InvalidOperationException() : Task.CompletedTask));

            cut.Render(p => p.Add(s => s.Value, 2));
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".input-status-bubble-error")));

            cut.Render(p => p.Add(s => s.Value, 3));

            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".input-status-bubble-saved")));
            Assert.Equal(2, saves);
        }

        [Fact]
        public void Clears_error_when_value_changes_back_to_saved_value()
        {
            var cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => throw new InvalidOperationException()));

            cut.Render(p => p.Add(s => s.Value, 2));
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".input-status-bubble-error")));

            cut.Render(p => p.Add(s => s.Value, 1));

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".input-status-addon")));
        }

        [Fact]
        public async Task Does_not_show_saved_when_disposed_during_save()
        {
            var save = new TaskCompletionSource();
            var hidden = false;
            var cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => save.Task)
                .Add(s => s.SavedDuration, ShortDelay)
                .Add(s => s.SavedHidden, () => hidden = true));

            cut.Render(p => p.Add(s => s.Value, 2));
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".spinner-border")));
            cut.Instance.Dispose();
            save.SetResult();
            await cut.InvokeAsync(() => { });

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

        [Fact]
        public async Task Saves_once_field_becomes_valid()
        {
            var model = new FormModel();
            var editContext = new EditContext(model);
            var messages = new ValidationMessageStore(editContext);
            messages.Add(editContext.Field(nameof(FormModel.Age)), "Too old");
            var invalidAtSave = new List<bool>();
            var cut = Render<InputStatus>(p => p
                .AddCascadingValue(editContext)
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => { invalidAtSave.Add(editContext.GetValidationMessages().Any()); return Task.CompletedTask; })
                .Add(s => s.For, () => model.Age));

            cut.Render(p => p.Add(s => s.Value, 200));
            await Task.Delay(50);
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
            var cut = Render<InputStatus>(p => p
                .AddCascadingValue(editContext)
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => { saves++; return Task.CompletedTask; })
                .Add(s => s.For, () => model.Age));

            cut.Render(p => p.Add(s => s.Value, 5));

            cut.WaitForAssertion(() => Assert.Equal(1, saves));
        }

        [Fact]
        public async Task Saves_once_other_fields_become_valid_when_for_is_not_set()
        {
            var editContext = new EditContext(new FormModel());
            var messages = new ValidationMessageStore(editContext);
            messages.Add(editContext.Field(nameof(FormModel.Name)), "Required");
            var invalidAtSave = new List<bool>();
            var cut = Render<InputStatus>(p => p
                .AddCascadingValue(editContext)
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => { invalidAtSave.Add(editContext.GetValidationMessages().Any()); return Task.CompletedTask; }));

            cut.Render(p => p.Add(s => s.Value, 5));
            await Task.Delay(50);
            messages.Clear();
            await cut.InvokeAsync(editContext.NotifyValidationStateChanged);

            cut.WaitForAssertion(() => Assert.Equal([false], invalidAtSave));
        }

        [Fact]
        public void Marks_field_unmodified_after_save()
        {
            var model = new FormModel();
            var editContext = new EditContext(model);
            var field = editContext.Field(nameof(FormModel.Age));
            editContext.NotifyFieldChanged(field);
            var cut = Render<InputStatus>(p => p
                .AddCascadingValue(editContext)
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, ShortDelay)
                .Add(s => s.Save, () => Task.CompletedTask)
                .Add(s => s.For, () => model.Age));

            cut.Render(p => p.Add(s => s.Value, 5));

            cut.WaitForAssertion(() => Assert.False(editContext.IsModified(field)));
        }

        [Fact]
        public void Announces_state_in_live_region()
        {
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Saved)
                .Add(s => s.SavedText, "Done"));

            Assert.Equal("Done", cut.Find("[role=status]").TextContent);
            Assert.Equal("true", cut.Find(".input-status-bubble-saved").GetAttribute("aria-hidden"));

            cut.Render(p => p.Add(s => s.State, InputSaveState.Idle));
            Assert.Equal("", cut.Find("[role=status]").TextContent);
        }

        [Fact]
        public void Raises_state_changed_when_saved_fades()
        {
            var states = new List<InputSaveState>();
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Saved)
                .Add(s => s.SavedDuration, ShortDelay)
                .Add(s => s.StateChanged, state => states.Add(state)));

            cut.WaitForAssertion(() => Assert.Equal([InputSaveState.Idle], states));
        }

        [Fact]
        public void Shows_saved_again_after_fade_when_state_is_bound()
        {
            var state = InputSaveState.Saved;
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, state)
                .Add(s => s.SavedDuration, ShortDelay)
                .Add(s => s.StateChanged, value => state = value));
            cut.WaitForAssertion(() => Assert.Equal(InputSaveState.Idle, state));

            cut.Render(p => p.Add(s => s.State, InputSaveState.Idle));
            cut.Render(p => p.Add(s => s.State, InputSaveState.Saved).Add(s => s.SavedDuration, TimeSpan.FromSeconds(5)));

            Assert.NotNull(cut.Find(".input-status-bubble-saved"));
        }

        [Fact]
        public void Saves_pending_change_when_disposed_before_delay()
        {
            var saves = 0;
            var cut = Render<InputStatus>(p => p
                .Add(s => s.Value, 1)
                .Add(s => s.SaveDelay, TimeSpan.FromSeconds(5))
                .Add(s => s.Save, () => { saves++; return Task.CompletedTask; }));

            cut.Render(p => p.Add(s => s.Value, 2));
            cut.Instance.Dispose();

            Assert.Equal(1, saves);
        }

        [Fact]
        public void Reports_exception_from_saved_hidden()
        {
            var cut = Render<InputStatus>(p => p
                .Add(s => s.State, InputSaveState.Saved)
                .Add(s => s.SavedDuration, ShortDelay)
                .Add(s => s.SavedHidden, () => throw new InvalidOperationException("hidden failed")));

            Assert.Throws<InvalidOperationException>(() =>
                cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".input-status-bubble-saved"))));
        }

        private class FormModel
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }
    }
}
