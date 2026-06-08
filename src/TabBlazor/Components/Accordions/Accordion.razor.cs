namespace TabBlazor;

/// <summary>
/// A container for collapsible <see cref="AccordionItem"/> panels. By default only one item is open at a time.
/// </summary>
public partial class Accordion : TablerBaseComponent
{
    private List<AccordionItem> Items { get; set; } = new();
    /// <summary>When true, multiple items can be expanded at once. Defaults to false.</summary>
    [Parameter]public bool MultipleOpen { get; set; }

    public void AddAccordionItem(AccordionItem item)
    {
        Items.Add(item);
        StateHasChanged();
    }

    public void RemoveAccordionItem(AccordionItem item)
    {
        if (Items.Contains(item))
        {
            Items.Remove(item);
        }
        StateHasChanged();
    }

    private void SetExpanded(AccordionItem item)
    {
        var oldExpanded = item.IsExpanded;
        
            foreach (var accordionItem in Items)
            {
                if (item == accordionItem)
                {
                    accordionItem.IsExpanded = !oldExpanded;
                }
                else if (!MultipleOpen)
                {
                    accordionItem.IsExpanded = false;    
                }
            }
        
        
        StateHasChanged();
    }
}