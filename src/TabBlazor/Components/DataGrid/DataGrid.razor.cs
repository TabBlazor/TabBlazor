using System;
using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>A grid of labelled fields (<see cref="DataGridItem"/>) for showing the details of a record.</summary>
    public partial class DataGrid : TablerBaseComponent
    {
        /// <summary>When true, each title sits beside its value instead of above it, one pair per row.</summary>
        [Parameter] public bool Horizontal { get; set; }
        /// <summary>
        /// Fixed number of columns, 1 to 4, kept at any container width. When null, the items fill the
        /// available width automatically. Combine with <see cref="Horizontal"/> for several pairs in a row.
        /// </summary>
        [Parameter] public int? Columns { get; set; }
        /// <summary>Optional CSS length for the title column of a horizontal datagrid, e.g. <c>"6rem"</c>. Defaults to the theme width.</summary>
        [Parameter] public string TitleWidth { get; set; }

        private const int MinimumColumns = 1;
        private const int MaximumColumns = 4;

        protected string Style => AppendCssVariable("--tblr-datagrid-title-width", TitleWidth);

        protected override string ClassNames => ClassBuilder
            .Add("datagrid")
            .AddIf("datagrid-horizontal", Horizontal)
            .AddIf($"datagrid-cols-{Math.Clamp(Columns ?? 0, MinimumColumns, MaximumColumns)}", Columns.HasValue)
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}
