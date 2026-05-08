namespace UnoPropertyGrid;

public sealed class PropertyGroupHeader
{
    public PropertyGroupHeader(string category) => Category = category;
    public string Category { get; }
}
