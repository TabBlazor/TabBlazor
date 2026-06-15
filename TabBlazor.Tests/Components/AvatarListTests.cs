namespace TabBlazor.Tests.Components
{
    public class AvatarListTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_avatar_list_class()
        {
            var cut = Render<AvatarList>(p => p.AddChildContent("<span>x</span>"));

            var div = cut.Find("div");
            Assert.Contains("avatar-list", div.ClassList);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<AvatarList>(p => p.AddChildContent("<span>child</span>"));
            Assert.Equal("child", cut.Find("div span").TextContent);
        }

        [Fact]
        public void Adds_stacked_class_when_stacked()
        {
            var cut = Render<AvatarList>(p => p.Add(a => a.Stacked, true));
            Assert.Contains("avatar-list-stacked", cut.Find("div").ClassList);
        }

        [Fact]
        public void Does_not_add_stacked_class_by_default()
        {
            var cut = Render<AvatarList>(p => p.AddChildContent(""));
            Assert.DoesNotContain("avatar-list-stacked", cut.Find("div").ClassList);
        }
    }
}
