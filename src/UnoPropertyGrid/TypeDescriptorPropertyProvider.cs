using System.ComponentModel;
using System.Reflection;

namespace UnoPropertyGrid;

public sealed class TypeDescriptorPropertyProvider : IPropertyGridPropertyProvider
{
    public IEnumerable<PropertyGridPropertyDescriptor> GetProperties(object component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        var descriptorNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(component))
        {
            descriptorNames.Add(descriptor.Name);
            if (!descriptor.IsBrowsable)
                continue;

            yield return new PropertyGridPropertyDescriptor(component, descriptor);
        }

        foreach (var property in component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (descriptorNames.Contains(property.Name))
                continue;

            var browsable = property.GetCustomAttribute<BrowsableAttribute>();
            if (browsable != null && !browsable.Browsable)
                continue;

            if (!property.CanRead)
                continue;

            yield return new PropertyGridPropertyDescriptor(component, property);
        }
    }
}
