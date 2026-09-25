using System.Collections.Generic;

namespace TabBlazor;

/// <summary>Settings for one confetti burst.</summary>
public class ConfettiOptions
{
    /// <summary>Number of pieces in the burst. Defaults to 220.</summary>
    public int Count { get; set; } = 220;
    /// <summary>How long pieces keep pouring, in milliseconds. Defaults to 3500.</summary>
    public int Duration { get; set; } = 3500;
    /// <summary>Fall speed multiplier. Defaults to 1.</summary>
    public double Speed { get; set; } = 1;
    /// <summary>Piece colors as CSS colors. When empty, the Tabler palette is used.</summary>
    public IReadOnlyList<string> Colors { get; set; }
}
