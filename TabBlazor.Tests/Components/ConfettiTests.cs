using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components
{
    public class ConfettiTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_child_content_in_clickable_wrapper()
        {
            var cut = Render<Confetti>(p => p.AddChildContent("<button>Go</button>"));

            var wrapper = cut.Find("span");
            Assert.Contains("cursor-pointer", wrapper.ClassList);
            Assert.Single(cut.FindAll("button"));
        }

        [Fact]
        public void Click_bursts_and_raises_on_start()
        {
            JSInterop.Setup<int>("tabBlazor.confetti.burst", _ => true).SetResult(1);
            var started = false;
            var cut = Render<Confetti>(p => p
                .Add(c => c.Count, 60)
                .Add(c => c.OnStart, EventCallback.Factory.Create(this, () => started = true))
                .AddChildContent("<button>Go</button>"));

            cut.Find("span").Click();

            Assert.True(started);
            var call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "tabBlazor.confetti.burst");
            var options = call.Arguments[0];
            Assert.Equal(60, options.GetType().GetProperty("count")!.GetValue(options));
        }

        [Fact]
        public void Disabled_does_not_burst()
        {
            var cut = Render<Confetti>(p => p.Add(c => c.Disabled, true).AddChildContent("x"));

            cut.Find("span").Click();

            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "tabBlazor.confetti.burst");
            Assert.DoesNotContain("cursor-pointer", cut.Find("span").ClassList);
        }

        [Fact]
        public void End_callback_raises_on_end()
        {
            JSInterop.Setup<int>("tabBlazor.confetti.burst", _ => true).SetResult(1);
            var ended = false;
            var cut = Render<Confetti>(p => p
                .Add(c => c.OnEnd, EventCallback.Factory.Create(this, () => ended = true))
                .AddChildContent("x"));
            cut.Find("span").Click();

            cut.InvokeAsync(() => cut.Instance.OnConfettiEnd());

            Assert.True(ended);
        }
    }
}
