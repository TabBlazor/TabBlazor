using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace TabBlazor;

/// <summary>The chart drawn by a <see cref="Sparkline"/>.</summary>
public enum SparklineType
{
    /// <summary>A line through the values, optionally filled.</summary>
    Line,
    /// <summary>One bar per value, growing from the zero line.</summary>
    Bar,
    /// <summary>Win/loss: a full bar up for positive, down for negative, a tick for zero.</summary>
    Tristate,
    /// <summary>A progress ring for a single value.</summary>
    Circle
}

/// <summary>Which value a <see cref="Sparkline"/> line marks with a dot.</summary>
public enum SparklineSpot
{
    /// <summary>No marker.</summary>
    None,
    /// <summary>The last value.</summary>
    Last,
    /// <summary>The smallest value.</summary>
    Min,
    /// <summary>The largest value.</summary>
    Max
}

/// <summary>The box size of a <see cref="Sparkline"/>.</summary>
public enum SparklineSize
{
    /// <summary>5rem by 1.5rem.</summary>
    Default,
    /// <summary>3rem by 1rem.</summary>
    Small,
    /// <summary>8rem by 2.5rem.</summary>
    Large
}

/// <summary>
/// A tiny inline SVG chart (line, bar, win/loss or circle) for a trend in a table cell or next to a number.
/// Rendered without JavaScript; colors come from <see cref="Color"/> through the Tabler sparkline CSS variables.
/// </summary>
public partial class Sparkline : TablerBaseComponent
{
    /// <summary>The values to plot. For <see cref="SparklineType.Circle"/> the first value is the share of <see cref="Max"/> (default 100).</summary>
    [Parameter] public IEnumerable<double> Values { get; set; }
    /// <summary>The chart type. Defaults to <see cref="SparklineType.Line"/>.</summary>
    [Parameter] public SparklineType Type { get; set; } = SparklineType.Line;
    /// <summary>When <c>true</c>, fills the area under a line. Defaults to <c>false</c>.</summary>
    [Parameter] public bool Fill { get; set; }
    /// <summary>Marks one value on a line with a dot. Defaults to <see cref="SparklineSpot.None"/>.</summary>
    [Parameter] public SparklineSpot Spot { get; set; } = SparklineSpot.None;
    /// <summary>Forces the lower bound of the value range. Defaults to the smallest value.</summary>
    [Parameter] public double? Min { get; set; }
    /// <summary>Forces the upper bound of the value range. Defaults to the largest value.</summary>
    [Parameter] public double? Max { get; set; }
    /// <summary>Draws a dashed guide line at this value. Bars below it use the negative color.</summary>
    [Parameter] public double? Threshold { get; set; }
    /// <summary>Text centered over the chart. <c>"auto"</c> shows the circle percentage or the last value.</summary>
    [Parameter] public string Label { get; set; }
    /// <summary>Accessible name for the chart, e.g. <c>"Weekly trend"</c>. When set, the element gets <c>role="img"</c>.</summary>
    [Parameter] public string AriaLabel { get; set; }
    /// <summary>The chart color. Defaults to <see cref="TablerColor.Default"/>, the current text color.</summary>
    [Parameter] public TablerColor Color { get; set; } = TablerColor.Default;
    /// <summary>The box size. Defaults to <see cref="SparklineSize.Default"/>.</summary>
    [Parameter] public SparklineSize Size { get; set; } = SparklineSize.Default;
    /// <summary>When <c>true</c>, makes the box as wide as it is tall, for the circle type. Defaults to <c>false</c>.</summary>
    [Parameter] public bool Square { get; set; }
    /// <summary>When <c>true</c>, greys the chart out. Defaults to <c>false</c>.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Drawing width in SVG units. Defaults to 80.</summary>
    [Parameter] public double Width { get; set; } = 80;
    /// <summary>Drawing height in SVG units. Defaults to 24.</summary>
    [Parameter] public double Height { get; set; } = 24;
    /// <summary>Vertical padding of a line in SVG units. Defaults to 2.</summary>
    [Parameter] public double Pad { get; set; } = 2;
    /// <summary>Gap between bars in SVG units. Defaults to 2.</summary>
    [Parameter] public double BarGap { get; set; } = 2;
    /// <summary>Bar corner radius in SVG units. Defaults to 2.</summary>
    [Parameter] public double BarRadius { get; set; } = 2;
    /// <summary>Ring thickness of the circle type in SVG units. Defaults to 3.</summary>
    [Parameter] public double StrokeWidth { get; set; } = 3;
    /// <summary>Dot radius of the spot marker in SVG units. Defaults to 2.</summary>
    [Parameter] public double SpotSize { get; set; } = 2;

    private const double DefaultCircleMax = 100;

    protected record struct Hairline(double Y, string Color, bool Dashed);
    protected record struct BarRect(double X, double Y, double Width, double Height, string Color);
    protected record struct Point(double X, double Y);

    private double[] values = Array.Empty<double>();

    protected override void OnParametersSet()
    {
        values = Values?.Where(value => !double.IsNaN(value) && !double.IsInfinity(value)).ToArray() ?? Array.Empty<double>();
    }

    protected bool HasValues => values.Length > 0;
    protected string ViewBox => $"0 0 {Format(Width)} {Format(Height)}";

    protected override string ClassNames => ClassBuilder
        .Add("sparkline")
        .AddCompare("sparkline-sm", Size, SparklineSize.Small)
        .AddCompare("sparkline-lg", Size, SparklineSize.Large)
        .AddIf("sparkline-square", Square)
        .AddIf("sparkline-disabled", Disabled)
        .Add(Color.GetColorClass("text"))
        .AddIf("cursor-pointer", OnClick.HasDelegate)
        .ToString();

    protected static string Format(double value) => Math.Round(value, 3).ToString(CultureInfo.InvariantCulture);
    protected static string CssVar(string name) => $"var(--tblr-sparkline-{name})";

    private (double Min, double Max, double Span) RangeOf(params double[] include)
    {
        var all = values.Concat(include).ToArray();
        var min = Min ?? all.Min();
        var max = Max ?? all.Max();
        var span = max - min;
        return (min, max, span == 0 ? 1 : span);
    }

    private double[] ThresholdInclude => Threshold is { } threshold ? new[] { threshold } : Array.Empty<double>();

    private double LineY(double value)
    {
        var (min, _, span) = RangeOf(ThresholdInclude);
        return Pad + (1 - (value - min) / span) * (Height - Pad * 2);
    }

    private double LineStep => values.Length > 1 ? Width / (values.Length - 1) : Width;

    protected string LinePoints => string.Join(" ", values.Select((value, index) => $"{Format(index * LineStep)},{Format(LineY(value))}"));

    protected string AreaPath
    {
        get
        {
            var top = string.Join(" ", values.Select((value, index) => $"{(index == 0 ? "M" : "L")} {Format(index * LineStep)} {Format(LineY(value))}"));
            var lastX = (values.Length - 1) * LineStep;
            return $"{top} L {Format(lastX)} {Format(Height)} L 0 {Format(Height)} Z";
        }
    }

    protected Point? SpotPoint
    {
        get
        {
            var index = SpotIndex();
            return index < 0 ? null : new Point(index * LineStep, LineY(values[index]));
        }
    }

    private int SpotIndex()
    {
        if (values.Length == 0 || Spot == SparklineSpot.None)
        {
            return -1;
        }

        if (Spot == SparklineSpot.Last)
        {
            return values.Length - 1;
        }

        var index = 0;
        for (var i = 1; i < values.Length; i++)
        {
            var better = Spot == SparklineSpot.Min ? values[i] < values[index] : values[i] > values[index];
            if (better)
            {
                index = i;
            }
        }

        return index;
    }

    private double BarY(double value)
    {
        var (min, _, span) = RangeOf(ThresholdInclude.Append(0).ToArray());
        return Height - (value - min) / span * Height;
    }

    private double BarWidth => (Width - BarGap * (values.Length - 1)) / values.Length;

    protected IEnumerable<Hairline> Hairlines
    {
        get
        {
            switch (Type)
            {
                case SparklineType.Line:
                    if (Threshold is { } lineThreshold)
                    {
                        yield return new Hairline(LineY(lineThreshold), "threshold", true);
                    }
                    break;
                case SparklineType.Bar:
                    var (min, _, _) = RangeOf(ThresholdInclude.Append(0).ToArray());
                    if (min < 0)
                    {
                        yield return new Hairline(BarY(0), "zero", false);
                    }
                    if (Threshold is { } barThreshold)
                    {
                        yield return new Hairline(BarY(barThreshold), "threshold", true);
                    }
                    break;
                case SparklineType.Tristate:
                    yield return new Hairline(Height / 2, "zero", false);
                    break;
            }
        }
    }

    protected IEnumerable<BarRect> Bars => Type == SparklineType.Tristate ? TristateBars() : ValueBars();

    private IEnumerable<BarRect> ValueBars()
    {
        double Clip(double y) => Math.Max(0, Math.Min(Height, y));

        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            var top = Clip(BarY(Math.Max(value, 0)));
            var bottom = Clip(BarY(Math.Min(value, 0)));
            var below = Threshold is { } threshold ? value < threshold : value < 0;
            yield return new BarRect(index * (BarWidth + BarGap), top, BarWidth, bottom - top, below ? "negative" : "stroke");
        }
    }

    private IEnumerable<BarRect> TristateBars()
    {
        var zeroY = Height / 2;
        var tick = Math.Min(2, zeroY);

        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            var x = index * (BarWidth + BarGap);
            if (value > 0)
            {
                yield return new BarRect(x, 0, BarWidth, zeroY, "stroke");
            }
            else if (value < 0)
            {
                yield return new BarRect(x, zeroY, BarWidth, zeroY, "negative");
            }
            else
            {
                yield return new BarRect(x, zeroY - tick / 2, BarWidth, tick, "track");
            }
        }
    }

    protected double CircleCenterX => Width / 2;
    protected double CircleCenterY => Height / 2;
    protected double CircleRadius => Math.Max(0, Math.Min(Width, Height) / 2 - StrokeWidth / 2);
    protected double CircleCircumference => 2 * Math.PI * CircleRadius;
    protected double CircleDashOffset => CircleCircumference * (1 - CircleRatio);
    protected string CircleTransform => $"rotate(-90 {Format(CircleCenterX)} {Format(CircleCenterY)})";

    private double CircleRatio
    {
        get
        {
            var value = values.Length > 0 ? values[0] : 0;
            var max = Max ?? (values.Length > 1 ? values[1] : DefaultCircleMax);
            var min = Min ?? 0;
            var span = max - min;
            return Math.Max(0, Math.Min(1, (value - min) / (span == 0 ? 1 : span)));
        }
    }

    protected string LabelText
    {
        get
        {
            if (string.IsNullOrEmpty(Label))
            {
                return string.Empty;
            }

            if (!string.Equals(Label, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return Label;
            }

            return Type == SparklineType.Circle
                ? $"{Math.Round(CircleRatio * 100)}%"
                : values[^1].ToString(CultureInfo.InvariantCulture);
        }
    }
}
