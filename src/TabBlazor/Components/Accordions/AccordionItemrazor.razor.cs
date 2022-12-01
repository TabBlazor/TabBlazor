namespace TabBlazor;

public partial class AccordionItem : TablerBaseComponent
{
    [CascadingParameter(Name = "Accordion")] public Accordion Accordion { get; set; }
    [Parameter] public string Title { get; set; }
    public bool Expanded { get; set; }

    protected override void OnInitialized()
    {
        Accordion.AddAccordionItem(this);
    }
}