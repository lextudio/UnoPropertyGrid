using System.ComponentModel;
using System.Reflection;

namespace UnoPropertyGrid;

public sealed class TypeDescriptorPropertyProvider : IPropertyGridPropertyProvider
{
    public IEnumerable<PropertyGridPropertyDescriptor> GetProperties(object component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        var reflectionProperties = component.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(property => property.Name, StringComparer.Ordinal);
        var descriptorNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (PropertyDescriptor descriptor in GetSafeTypeDescriptorProperties(component))
        {
            descriptorNames.Add(descriptor.Name);
            if (!descriptor.IsBrowsable)
                continue;

            if (reflectionProperties.TryGetValue(descriptor.Name, out var property) && property.CanRead)
                yield return new PropertyGridPropertyDescriptor(component, property, descriptor);
            else
                yield return new PropertyGridPropertyDescriptor(component, descriptor);
        }

        foreach (var property in reflectionProperties.Values)
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

    static IEnumerable<PropertyDescriptor> GetSafeTypeDescriptorProperties(object component)
    {
        PropertyDescriptorCollection descriptors;
        try
        {
            descriptors = TypeDescriptor.GetProperties(component);
        }
        catch (Exception ex) when (IsTypeDescriptorProviderLoadFailure(ex))
        {
            PropertyGridLogger.Log($"TypeDescriptor provider failed for {component.GetType().FullName}: {ex.GetBaseException().Message}");
            yield break;
        }

        foreach (PropertyDescriptor descriptor in descriptors)
            yield return descriptor;
    }

    static bool IsTypeDescriptorProviderLoadFailure(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is FileNotFoundException or FileLoadException or TargetInvocationException)
                return true;
        }

        return false;
    }
}
