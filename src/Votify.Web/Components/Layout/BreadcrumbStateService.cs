namespace Votify.Web.Components.Layout;

public class BreadcrumbStateService
{
    public record BreadcrumbItem(string Label, string? Href);

    private List<BreadcrumbItem> _items = new();
    public IReadOnlyList<BreadcrumbItem> Items => _items;
    public event Action? OnChange;

    public void SetItems(IEnumerable<BreadcrumbItem> items)
    {
        _items = items.ToList();
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _items = new();
        OnChange?.Invoke();
    }
}
