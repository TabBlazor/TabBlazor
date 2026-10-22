using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace TabBlazor.Tests.Components
{
    public class NumberInputTests : TabBlazorTestContext
    {
        private const string DecrementButton = "button[data-bs-number-input-action=decrement]";
        private const string IncrementButton = "button[data-bs-number-input-action=increment]";

        private IRenderedComponent<NumberInput<TValue>> RenderNumberInput<TValue>(TValue value,
            Action<ComponentParameterCollectionBuilder<NumberInput<TValue>>>? parameters = null,
            Action<TValue>? onChanged = null)
        {
            return Render<NumberInput<TValue>>(p =>
            {
                p.Add(n => n.Value, value);
                p.Add(n => n.ValueChanged, onChanged ?? (_ => { }));
                parameters?.Invoke(p);
            });
        }

        private static void WithCulture(string name, Action test)
        {
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            try
            {
                test();
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Renders_input_between_decrement_and_increment_buttons()
        {
            var cut = RenderNumberInput(1);

            var children = cut.Find("div.number-input").Children;
            Assert.Equal(3, children.Length);
            Assert.Equal("decrement", children[0].GetAttribute("data-bs-number-input-action"));
            Assert.Contains("form-control", children[1].ClassList);
            Assert.Equal("increment", children[2].GetAttribute("data-bs-number-input-action"));
        }

        [Fact]
        public void Renders_spinbutton_with_value_and_bounds()
        {
            var cut = RenderNumberInput(3, p => p.Add(n => n.Min, 1).Add(n => n.Max, 5));

            var input = cut.Find("input");
            Assert.Equal("3", input.GetAttribute("value"));
            Assert.Equal("spinbutton", input.GetAttribute("role"));
            Assert.Equal("numeric", input.GetAttribute("inputmode"));
            Assert.Equal("1", input.GetAttribute("aria-valuemin"));
            Assert.Equal("5", input.GetAttribute("aria-valuemax"));
            Assert.Equal("3", input.GetAttribute("aria-valuenow"));
        }

        [Fact]
        public void Increment_button_adds_step()
        {
            var changed = 0;
            var cut = RenderNumberInput(1, p => p.Add(n => n.Step, 2), v => changed = v);

            cut.Find(IncrementButton).Click();

            Assert.Equal(3, changed);
            Assert.Equal("3", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void Decrement_button_subtracts_step()
        {
            var changed = 0;
            var cut = RenderNumberInput(5, onChanged: v => changed = v);

            cut.Find(DecrementButton).Click();

            Assert.Equal(4, changed);
        }

        [Fact]
        public void Disables_button_at_bound()
        {
            var cut = RenderNumberInput(5, p => p.Add(n => n.Min, 1).Add(n => n.Max, 5));

            Assert.True(cut.Find(IncrementButton).HasAttribute("disabled"));
            Assert.False(cut.Find(DecrementButton).HasAttribute("disabled"));
        }

        [Fact]
        public void Disables_buttons_when_disabled()
        {
            var cut = RenderNumberInput(3, p => p.Add(n => n.Disabled, true));

            Assert.True(cut.Find("input").HasAttribute("disabled"));
            Assert.True(cut.Find(DecrementButton).HasAttribute("disabled"));
            Assert.True(cut.Find(IncrementButton).HasAttribute("disabled"));
        }

        [Fact]
        public void Disables_buttons_when_read_only()
        {
            var cut = RenderNumberInput(3, p => p.Add(n => n.ReadOnly, true));

            Assert.True(cut.Find("input").HasAttribute("readonly"));
            Assert.True(cut.Find(IncrementButton).HasAttribute("disabled"));
        }

        [Fact]
        public void Rounds_to_step_precision()
        {
            WithCulture("en-US", () =>
            {
                var changed = 0m;
                var cut = RenderNumberInput(1.5m, p => p.Add(n => n.Step, 0.25), v => changed = v);

                Assert.Equal("1.50", cut.Find("input").GetAttribute("value"));
                Assert.Equal("decimal", cut.Find("input").GetAttribute("inputmode"));

                cut.Find(IncrementButton).Click();

                Assert.Equal(1.75m, changed);
                Assert.Equal("1.75", cut.Find("input").GetAttribute("value"));
            });
        }

        [Fact]
        public void Steps_doubles_without_drift()
        {
            var changed = 0d;
            var cut = RenderNumberInput(0.2, p => p.Add(n => n.Step, 0.1), v => changed = v);

            cut.Find(IncrementButton).Click();

            Assert.Equal(0.3, changed);
        }

        [Fact]
        public void Clamps_typed_value_on_change()
        {
            var changed = 0;
            var cut = RenderNumberInput(1, p => p.Add(n => n.Max, 10), v => changed = v);

            cut.Find("input").Change("15");

            Assert.Equal(10, changed);
            Assert.Equal("10", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void Restores_last_value_when_text_is_not_a_number()
        {
            var calls = 0;
            var cut = RenderNumberInput(3, onChanged: _ => calls++);

            cut.Find("input").Change("abc");

            Assert.Equal(0, calls);
            Assert.Equal("3", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void Clears_nullable_value_when_text_is_empty()
        {
            int? changed = 3;
            var cut = RenderNumberInput<int?>(3, onChanged: v => changed = v);

            cut.Find("input").Change("");

            Assert.Null(changed);
        }

        [Fact]
        public void Empty_field_steps_from_positive_min()
        {
            int? changed = null;
            var cut = RenderNumberInput<int?>(null, p => p.Add(n => n.Min, 5), v => changed = v);

            cut.Find(IncrementButton).Click();

            Assert.Equal(6, changed);
        }

        [Fact]
        public void Parses_culture_decimal_separator()
        {
            WithCulture("sv-SE", () =>
            {
                var changed = 0m;
                var cut = RenderNumberInput(1m, p => p.Add(n => n.Step, 0.1).Add(n => n.Min, -5), v => changed = v);

                cut.Find("input").Change("-1,5");

                Assert.Equal(-1.5m, changed);
                Assert.Equal((-1.5m).ToString("F1", CultureInfo.CurrentCulture), cut.Find("input").GetAttribute("value"));
            });
        }

        [Fact]
        public void Does_not_read_group_separator_as_decimal_point()
        {
            WithCulture("en-US", () =>
            {
                var calls = 0;
                var cut = RenderNumberInput(3, onChanged: _ => calls++);

                cut.Find("input").Change("1,500");

                Assert.Equal(0, calls);
                Assert.Equal("3", cut.Find("input").GetAttribute("value"));
            });
        }

        [Fact]
        public void Shows_float_without_conversion_noise()
        {
            WithCulture("en-US", () =>
            {
                var changed = 0f;
                var cut = RenderNumberInput(0.1f, p => p.Add(n => n.Step, 0.1), v => changed = v);

                Assert.Equal("0.1", cut.Find("input").GetAttribute("value"));

                cut.Find(IncrementButton).Click();

                Assert.Equal(0.2f, changed);
                Assert.Equal("0.2", cut.Find("input").GetAttribute("value"));
            });
        }

        [Fact]
        public void Rounds_fractional_step_to_whole_numbers_for_integer_types()
        {
            var changed = 0;
            var cut = RenderNumberInput(2, p => p.Add(n => n.Step, 0.5), v => changed = v);

            cut.Find(IncrementButton).Click();

            Assert.Equal(3, changed);
            Assert.Equal("numeric", cut.Find("input").GetAttribute("inputmode"));
        }

        [Fact]
        public void Steps_to_type_limit_without_overflow()
        {
            var changed = 0m;
            var cut = RenderNumberInput(decimal.MaxValue - 0.5m, onChanged: v => changed = v);

            cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

            Assert.Equal(decimal.MaxValue, changed);
        }

        [Theory]
        [InlineData("ArrowUp", 4)]
        [InlineData("ArrowDown", 2)]
        [InlineData("Home", 1)]
        [InlineData("End", 9)]
        public void Keyboard_changes_value(string key, int expected)
        {
            var changed = 0;
            var cut = RenderNumberInput(3, p => p.Add(n => n.Min, 1).Add(n => n.Max, 9), v => changed = v);

            cut.Find("input").KeyDown(new KeyboardEventArgs { Key = key });

            Assert.Equal(expected, changed);
        }

        [Fact]
        public void Arrow_keys_step_from_typed_text()
        {
            var changed = 0;
            var cut = RenderNumberInput(3, onChanged: v => changed = v);

            cut.Find("input").Input("7");
            cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

            Assert.Equal(8, changed);
        }

        [Fact]
        public void Holding_a_button_repeats_until_bound()
        {
            var changed = 0;
            var cut = RenderNumberInput(0, p => p
                .Add(n => n.Max, 4)
                .Add(n => n.RepeatDelay, 1)
                .Add(n => n.RepeatInterval, 1), v => changed = v);

            cut.Find(IncrementButton).PointerDown();

            cut.WaitForAssertion(() => Assert.Equal(4, changed));
            Assert.True(cut.Find(IncrementButton).HasAttribute("disabled"));
        }

        [Fact]
        public async Task Click_that_ends_a_hold_does_not_step_again()
        {
            var changed = 0;
            var cut = RenderHeldAfterFirstRepeat(v => changed = v);
            cut.WaitForAssertion(() => Assert.Equal(1, changed));

            await cut.Find(IncrementButton).PointerUpAsync(new PointerEventArgs());
            await cut.Find(IncrementButton).ClickAsync(new MouseEventArgs { Detail = 1 });

            Assert.Equal(1, changed);
        }

        [Fact]
        public async Task Keyboard_click_after_a_hold_ended_off_the_button_steps()
        {
            var changed = 0;
            var cut = RenderHeldAfterFirstRepeat(v => changed = v);
            cut.WaitForAssertion(() => Assert.Equal(1, changed));

            await cut.Find(IncrementButton).PointerLeaveAsync(new PointerEventArgs());
            await cut.Find(IncrementButton).ClickAsync(new MouseEventArgs { Detail = 0 });

            Assert.Equal(2, changed);
        }

        private IRenderedComponent<NumberInput<int>> RenderHeldAfterFirstRepeat(Action<int> onChanged)
        {
            var cut = RenderNumberInput(0, p => p
                .Add(n => n.RepeatDelay, 1)
                .Add(n => n.RepeatInterval, 60_000), onChanged);

            cut.Find(IncrementButton).PointerDown();
            return cut;
        }

        [Fact]
        public void Does_not_repeat_when_repeat_is_off()
        {
            var calls = 0;
            var cut = RenderNumberInput(0, p => p
                .Add(n => n.Repeat, false)
                .Add(n => n.RepeatDelay, 1), _ => calls++);

            cut.Find(IncrementButton).PointerDown();
            cut.Find(IncrementButton).Click();

            Assert.Equal(1, calls);
        }

        [Theory]
        [InlineData(NumberInputSize.Small, "number-input-sm")]
        [InlineData(NumberInputSize.Large, "number-input-lg")]
        public void Adds_size_class(NumberInputSize size, string expected)
        {
            var cut = RenderNumberInput(1, p => p.Add(n => n.Size, size));

            Assert.Contains(expected, cut.Find("div.number-input").ClassList);
        }

        [Fact]
        public void Sets_input_width_variable_after_provided_style()
        {
            var cut = RenderNumberInput(1, p => p
                .Add(n => n.InputWidth, "6rem")
                .AddUnmatched("style", "color: red;"));

            Assert.Equal("color: red; --tblr-number-input-input-width: 6rem;", cut.Find("div.number-input").GetAttribute("style"));
        }

        [Fact]
        public void Keeps_provided_style_when_rendering_itself()
        {
            var cut = RenderNumberInput(1, p => p.AddUnmatched("style", "color: red;"));

            cut.Find(IncrementButton).Click();

            Assert.Equal("color: red;", cut.Find("div.number-input").GetAttribute("style"));
        }

        [Fact]
        public void Links_label_to_input()
        {
            var cut = RenderNumberInput(1, p => p.Add(n => n.Label, "Quantity"));

            var label = cut.Find("label.form-label");
            Assert.Equal("Quantity", label.TextContent);
            Assert.Equal(cut.Find("input").Id, label.GetAttribute("for"));
        }

        [Fact]
        public void Puts_class_on_box_and_other_attributes_on_input()
        {
            var cut = RenderNumberInput(1, p => p
                .AddUnmatched("class", "custom")
                .AddUnmatched("id", "seats")
                .AddUnmatched("aria-label", "Seats"));

            Assert.Contains("custom", cut.Find("div.number-input").ClassList);
            var input = cut.Find("input");
            Assert.Equal("seats", input.Id);
            Assert.Equal("Seats", input.GetAttribute("aria-label"));
            Assert.DoesNotContain("custom", input.ClassList);
        }

        [Fact]
        public void Sets_accessible_button_labels()
        {
            var cut = RenderNumberInput(1, p => p.Add(n => n.IncrementLabel, "More"));

            Assert.Equal("Decrease", cut.Find(DecrementButton).GetAttribute("aria-label"));
            Assert.Equal("More", cut.Find(IncrementButton).GetAttribute("aria-label"));
        }

        [Fact]
        public void Notifies_edit_context_and_shows_validation_state()
        {
            var model = new Order { Quantity = 1 };
            var editContext = new EditContext(model);
            editContext.SetFieldCssClassProvider(new TabFieldCssClassProvider());
            var cut = RenderNumberInput(model.Quantity, p => p
                .AddCascadingValue(editContext)
                .Add(n => n.ValueExpression, () => model.Quantity), v => model.Quantity = v);

            cut.Find(IncrementButton).Click();

            var field = editContext.Field(nameof(Order.Quantity));
            Assert.True(editContext.IsModified(field));

            var messages = new ValidationMessageStore(editContext);
            messages.Add(field, "Too many");
            editContext.NotifyValidationStateChanged();

            cut.WaitForAssertion(() => Assert.Contains("is-invalid", cut.Find("input").ClassList));
        }

        [Fact]
        public void Throws_for_non_numeric_type()
        {
            Assert.Throws<InvalidOperationException>(() => RenderNumberInput("1"));
        }

        private class Order
        {
            public int Quantity { get; set; }
        }
    }
}
