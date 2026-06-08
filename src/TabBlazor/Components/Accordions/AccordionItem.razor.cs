namespace TabBlazor;

/// <summary>A single collapsible panel within an <see cref="Accordion"/>.</summary>
public class AccordionItem : TablerBaseComponent, IDisposable
{
    /// <summary>The parent accordion, supplied via cascading parameter.</summary>
    [CascadingParameter(Name = "Accordion")]
    public Accordion Accordion { get; set; }

    /// <summary>The header text. Ignored when <see cref="TitleTemplate"/> is set.</summary>
    [Parameter] public string Title { get; set; }
    /// <summary>Optional custom header content, overriding <see cref="Title"/>.</summary>
    [Parameter] public RenderFragment TitleTemplate { get; set; }
    /// <summary>Whether the item starts expanded. Defaults to false.</summary>
    [Parameter] public bool Expanded { get; set; }
    /// <summary>The current expanded state.</summary>
    public bool IsExpanded { get; set; }

   

    protected override void OnInitialized()
    {
        IsExpanded = Expanded;    
        Accordion?.AddAccordionItem(this);
    }

    public void Dispose()
    {
        Accordion?.RemoveAccordionItem(this);
    }

}