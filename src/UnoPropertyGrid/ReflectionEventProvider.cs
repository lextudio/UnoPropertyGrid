using System.Reflection;

namespace UnoPropertyGrid;

public sealed class ReflectionEventProvider : IPropertyGridEventProvider
{
    public IEnumerable<PropertyGridEventDescriptor> GetEvents(object component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        foreach (var @event in component.GetType().GetEvents(BindingFlags.Public | BindingFlags.Instance))
            yield return new PropertyGridEventDescriptor(component, @event);
    }
}
