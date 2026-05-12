namespace LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.Metadata;

public sealed class AttributeTableBuilder
{
    readonly Dictionary<AttributeTable.AttributeTableKey, List<Attribute>> _attributes = new();

    public void AddCustomAttributes(string typeName, string propertyName, params Attribute[] attributes)
    {
        var key = new AttributeTable.AttributeTableKey(typeName, propertyName);
        if (!_attributes.TryGetValue(key, out var list))
        {
            list = new List<Attribute>();
            _attributes[key] = list;
        }

        list.AddRange(attributes);
    }

    public void AddCustomAttributes(Type componentType, string propertyName, params Attribute[] attributes)
    {
        AddCustomAttributes(componentType.FullName ?? componentType.Name, propertyName, attributes);
    }

    public AttributeTable CreateTable()
    {
        return new AttributeTable(_attributes.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<Attribute>)pair.Value.ToArray()));
    }
}
