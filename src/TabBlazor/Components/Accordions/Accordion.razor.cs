namespace TabBlazor;

public partial class Accordion : TablerBaseComponent
{
    private List<AccordionItem> Items { get; set; } = new();

    public void AddAccordionItem(AccordionItem item)
    {
        Items.Add(item);
        StateHasChanged();
    }

    private void SetExpanded(AccordionItem item)
    {
        var oldExpanded = item.Expanded;
        foreach (var accordionItem in Items)
        {
            accordionItem.Expanded = false;
        }

        item.Expanded = !oldExpanded;
        
        StateHasChanged();
    }
}