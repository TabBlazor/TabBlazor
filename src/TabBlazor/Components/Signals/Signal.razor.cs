using System;
using Microsoft.AspNetCore.Components;

namespace TabBlazor;

/// <summary>
/// A small row of stepped bars showing a level, such as priority, strength or coverage.
/// Color comes from <see cref="Color"/>; the height is set with <see cref="Size"/>.
/// </summary>
public partial class Signal : TablerBaseComponent
{
    /// <summary>Total number of bars, 2 to 5. Defaults to 3.</summary>
    [Parameter] public int Bars { get; set; } = 3;
    /// <summary>Number of active (lit) bars, 0 to <see cref="Bars"/>.</summary>
    [Parameter] public int Level { get; set; }
    /// <summary>The bar color. Defaults to <see cref="TablerColor.Default"/>, the secondary text color.</summary>
    [Parameter] public TablerColor Color { get; set; } = TablerColor.Default;
    /// <summary>Optional CSS length for the signal height, e.g. <c>"1.5rem"</c>. Defaults to the theme size.</summary>
    [Parameter] public string Size { get; set; }
    /// <summary>
    /// Accessible name. When null, defaults to "Level x of y". Set to an empty string when adjacent
    /// text already describes the level; the signal is then hidden from assistive technology.
    /// </summary>
    [Parameter] public string Label { get; set; }

    private const int MinimumBars = 2;
    private const int MaximumBars = 5;

    protected int BarCount => Math.Clamp(Bars, MinimumBars, MaximumBars);
    protected int ActiveCount => Math.Clamp(Level, 0, BarCount);

    protected bool IsDecorative => Label == string.Empty;
    protected string Role => IsDecorative ? null : "img";
    protected string AriaHidden => IsDecorative ? "true" : null;
    protected string AriaLabel => IsDecorative ? null : Label ?? $"Level {ActiveCount} of {BarCount}";

    protected string Style
    {
        get
        {
            var providedStyle = GetUnmatchedParameter("style")?.ToString();
            if (string.IsNullOrWhiteSpace(Size))
            {
                return providedStyle;
            }

            return $"{providedStyle} --tblr-signal-size: {Size};".Trim();
        }
    }

    protected override string ClassNames => ClassBuilder
        .Add("signal")
        .Add(Color.GetColorClass("signal"))
        .AddIf("cursor-pointer", OnClick.HasDelegate)
        .ToString();
}
