using System.ComponentModel;

namespace UnoPropertyGrid;

public sealed class TypeDescriptorPropertyProvider : IPropertyGridPropertyProvider
{
    public IEnumerable<PropertyGridPropertyDescriptor> GetProperties(object component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(component))
        {
            if (!descriptor.IsBrowsable)
                continue;

            yield return new PropertyGridPropertyDescriptor(component, descriptor);
        }
    }
}
