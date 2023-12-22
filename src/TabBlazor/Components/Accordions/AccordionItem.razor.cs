namespace TabBlazor;

public class AccordionItem : TablerBaseComponent, IDisposable
{
    [CascadingParameter(Name = "Accordion")]
    public Accordion Accordion { get; set; }

    [Parameter] public string Title { get; set; }
    [Parameter] public RenderFragment TitleTemplate { get; set; }
    [Parameter] public bool Expanded { get; set; }
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