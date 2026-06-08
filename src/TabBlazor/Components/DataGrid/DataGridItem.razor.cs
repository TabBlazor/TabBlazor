using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>A single labelled field within a datagrid layout (a title plus its content).</summary>
    public partial class DataGridItem : TablerBaseComponent
    {
        /// <summary>The field label. Ignored when <see cref="TitleTemplate"/> is set.</summary>
        [Parameter] public string Title { get; set; }

        /// <summary>Optional custom title content, overriding <see cref="Title"/>.</summary>
        [Parameter] public RenderFragment TitleTemplate { get; set; }


        protected override string ClassNames => ClassBuilder
            .Add("datagrid-item")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}