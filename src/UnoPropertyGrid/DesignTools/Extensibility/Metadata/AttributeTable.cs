namespace LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.Metadata;

public sealed class AttributeTable
{
    readonly IReadOnlyDictionary<AttributeTableKey, IReadOnlyList<Attribute>> _attributes;

    internal AttributeTable(IReadOnlyDictionary<AttributeTableKey, IReadOnlyList<Attribute>> attributes)
    {
        _attributes = attributes;
    }

    public IEnumerable<Attribute> GetCustomAttributes(Type componentType, string propertyName)
    {
        foreach (var type in EnumerateTypeHierarchy(componentType))
        {
            var key = new AttributeTableKey(type.FullName ?? type.Name, propertyName);
            if (_attributes.TryGetValue(key, out var attributes))
            {
                foreach (var attribute in attributes)
                    yield return attribute;
            }
        }
    }

    static IEnumerable<Type> EnumerateTypeHierarchy(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
            yield return current;

        foreach (var @interface in type.GetInterfaces())
            yield return @interface;
    }

    internal readonly record struct AttributeTableKey(string TypeName, string PropertyName);
}
