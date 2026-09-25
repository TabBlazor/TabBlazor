using System.Linq;

namespace TabBlazor.Tests.Components
{
    public class SparklineTests : TabBlazorTestContext
    {
        private static readonly double[] Series = { 3, 4, 2, 6, 5, 8, 7 };

        private IRenderedComponent<Sparkline> RenderSparkline(System.Action<ComponentParameterCollectionBuilder<Sparkline>> configure = null)
        {
            return Render<Sparkline>(p =>
            {
                p.Add(s => s.Values, Series);
                configure?.Invoke(p);
            });
        }

        [Fact]
        public void Renders_svg_with_view_box()
        {
            var cut = RenderSparkline();

            var svg = cut.Find("svg.sparkline-svg");
            Assert.Equal("0 0 80 24", svg.GetAttribute("viewBox"));
            Assert.Contains("sparkline", cut.Find("span.sparkline").ClassList);
        }

        [Fact]
        public void Renders_nothing_without_values()
        {
            var cut = Render<Sparkline>();
            Assert.Empty(cut.FindAll("svg"));
        }

        [Fact]
        public void Line_has_one_point_per_value()
        {
            var cut = RenderSparkline();

            var points = cut.Find("polyline").GetAttribute("points").Split(' ');
            Assert.Equal(Series.Length, points.Length);
            Assert.StartsWith("0,", points[0]);
            Assert.StartsWith("80,", points[^1]);
        }

        [Fact]
        public void Fill_adds_area_path()
        {
            var cut = RenderSparkline(p => p.Add(s => s.Fill, true));

            var area = cut.Find("path");
            Assert.Equal("var(--tblr-sparkline-fill)", area.GetAttribute("fill"));
            Assert.EndsWith("Z", area.GetAttribute("d"));
        }

        [Fact]
        public void Spot_marks_last_value()
        {
            var cut = RenderSparkline(p => p.Add(s => s.Spot, SparklineSpot.Last));

            var spot = cut.Find("circle");
            Assert.Equal("80", spot.GetAttribute("cx"));
            Assert.Equal("var(--tblr-sparkline-spot)", spot.GetAttribute("fill"));
        }

        [Fact]
        public void Spot_marks_max_value()
        {
            var cut = RenderSparkline(p => p.Add(s => s.Spot, SparklineSpot.Max));

            var maxIndex = System.Array.IndexOf(Series, Series.Max());
            var expectedX = System.Math.Round(maxIndex * 80.0 / (Series.Length - 1), 3);
            Assert.Equal(expectedX.ToString(System.Globalization.CultureInfo.InvariantCulture), cut.Find("circle").GetAttribute("cx"));
        }

        [Fact]
        public void Threshold_draws_dashed_guide()
        {
            var cut = RenderSparkline(p => p.Add(s => s.Threshold, 5.0));

            var guide = cut.Find("line");
            Assert.Equal("3 2", guide.GetAttribute("stroke-dasharray"));
            Assert.Equal("var(--tblr-sparkline-threshold)", guide.GetAttribute("stroke"));
        }

        [Fact]
        public void Bars_render_one_rect_per_value()
        {
            var cut = RenderSparkline(p => p.Add(s => s.Type, SparklineType.Bar));

            var rects = cut.FindAll("rect");
            Assert.Equal(Series.Length, rects.Count);
            Assert.All(rects, r => Assert.Equal("var(--tblr-sparkline-stroke)", r.GetAttribute("fill")));
            Assert.Empty(cut.FindAll("line"));
        }

        [Fact]
        public void Negative_bars_use_negative_color_and_zero_line()
        {
            var cut = Render<Sparkline>(p => p
                .Add(s => s.Type, SparklineType.Bar)
                .Add(s => s.Values, new double[] { 3, -1, 4 }));

            var rects = cut.FindAll("rect");
            Assert.Equal("var(--tblr-sparkline-negative)", rects[1].GetAttribute("fill"));
            Assert.Equal("var(--tblr-sparkline-zero)", cut.Find("line").GetAttribute("stroke"));
        }

        [Fact]
        public void Tristate_renders_up_down_and_tick()
        {
            var cut = Render<Sparkline>(p => p
                .Add(s => s.Type, SparklineType.Tristate)
                .Add(s => s.Values, new double[] { 1, -1, 0 }));

            var rects = cut.FindAll("rect");
            Assert.Equal("var(--tblr-sparkline-stroke)", rects[0].GetAttribute("fill"));
            Assert.Equal("0", rects[0].GetAttribute("y"));
            Assert.Equal("var(--tblr-sparkline-negative)", rects[1].GetAttribute("fill"));
            Assert.Equal("12", rects[1].GetAttribute("y"));
            Assert.Equal("var(--tblr-sparkline-track)", rects[2].GetAttribute("fill"));
        }

        [Fact]
        public void Circle_dash_offset_matches_ratio()
        {
            var cut = Render<Sparkline>(p => p
                .Add(s => s.Type, SparklineType.Circle)
                .Add(s => s.Values, new double[] { 25 })
                .Add(s => s.Width, 24.0)
                .Add(s => s.Height, 24.0));

            var circles = cut.FindAll("circle");
            Assert.Equal(2, circles.Count);
            var ring = circles[1];
            var circumference = double.Parse(ring.GetAttribute("stroke-dasharray"), System.Globalization.CultureInfo.InvariantCulture);
            var offset = double.Parse(ring.GetAttribute("stroke-dashoffset"), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(0.75, offset / circumference, 3);
        }

        [Fact]
        public void Auto_label_shows_circle_percentage()
        {
            var cut = Render<Sparkline>(p => p
                .Add(s => s.Type, SparklineType.Circle)
                .Add(s => s.Values, new double[] { 10 })
                .Add(s => s.Label, "auto"));

            Assert.Equal("10%", cut.Find("span.sparkline-label").TextContent);
        }

        [Fact]
        public void Auto_label_shows_last_value_for_line()
        {
            var cut = RenderSparkline(p => p.Add(s => s.Label, "auto"));
            Assert.Equal("7", cut.Find("span.sparkline-label").TextContent);
        }

        [Theory]
        [InlineData(SparklineSize.Small, "sparkline-sm")]
        [InlineData(SparklineSize.Large, "sparkline-lg")]
        public void Adds_size_class(SparklineSize size, string expected)
        {
            var cut = RenderSparkline(p => p.Add(s => s.Size, size));
            Assert.Contains(expected, cut.Find("span.sparkline").ClassList);
        }

        [Fact]
        public void Adds_square_disabled_and_color_classes()
        {
            var cut = RenderSparkline(p => p
                .Add(s => s.Square, true)
                .Add(s => s.Disabled, true)
                .Add(s => s.Color, TablerColor.Green));

            var classes = cut.Find("span.sparkline").ClassList;
            Assert.Contains("sparkline-square", classes);
            Assert.Contains("sparkline-disabled", classes);
            Assert.Contains("text-green", classes);
        }
    }
}
