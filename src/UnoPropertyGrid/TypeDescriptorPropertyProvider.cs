using System.ComponentModel;
using System.Reflection;

namespace UnoPropertyGrid;

public sealed class TypeDescriptorPropertyProvider : IPropertyGridPropertyProvider
{
    public IEnumerable<PropertyGridPropertyDescriptor> GetProperties(object component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        foreach (var property in component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var browsable = property.GetCustomAttribute<BrowsableAttribute>();
            if (browsable != null && !browsable.Browsable)
                continue;

            if (!property.CanRead)
                continue;

            yield return new PropertyGridPropertyDescriptor(component, property);
        }
    }
}
