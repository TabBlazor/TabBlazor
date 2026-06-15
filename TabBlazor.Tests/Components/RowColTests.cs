namespace TabBlazor.Tests.Components
{
    public class RowColTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_child_content()
        {
            var cut = Render<RowCol>(p => p.AddChildContent("content"));

            var div = cut.Find("div");
            Assert.Contains("content", div.TextContent);
        }

        [Fact]
        public void Adds_columns_class()
        {
            var cut = Render<RowCol>(p => p.Add(c => c.Columns, 6));
            Assert.Contains("col-6", cut.Find("div").ClassList);
        }

        [Fact]
        public void Adds_sm_breakpoint_class()
        {
            var cut = Render<RowCol>(p => p.Add(c => c.Sm, 4));
            Assert.Contains("col-sm-4", cut.Find("div").ClassList);
        }

        [Fact]
        public void Adds_md_breakpoint_class()
        {
            var cut = Render<RowCol>(p => p.Add(c => c.Md, 4));
            Assert.Contains("col-md-4", cut.Find("div").ClassList);
        }

        [Fact]
        public void Adds_lg_breakpoint_class()
        {
            var cut = Render<RowCol>(p => p.Add(c => c.Lg, 4));
            Assert.Contains("col-lg-4", cut.Find("div").ClassList);
        }

        [Fact]
        public void Adds_col_auto_class_when_auto()
        {
            var cut = Render<RowCol>(p => p.Add(c => c.Auto, true));
            Assert.Contains("col-auto", cut.Find("div").ClassList);
        }

        [Fact]
        public void Omits_column_class_when_unset()
        {
            var cut = Render<RowCol>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("col-0", cut.Find("div").ClassList);
        }
    }
}
