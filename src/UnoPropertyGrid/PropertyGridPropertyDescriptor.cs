using System.ComponentModel;
using System.Globalization;

namespace UnoPropertyGrid;

public sealed class PropertyGridPropertyDescriptor
{
    readonly object _component;
    readonly PropertyDescriptor _descriptor;

    public PropertyGridPropertyDescriptor(object component, PropertyDescriptor descriptor)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public string Name => _descriptor.Name;
    public string DisplayName => string.IsNullOrWhiteSpace(_descriptor.DisplayName) ? _descriptor.Name : _descriptor.DisplayName;
    public string Category => string.IsNullOrWhiteSpace(_descriptor.Category) ? "Misc" : _descriptor.Category;
    public string Description => _descriptor.Description ?? string.Empty;
    public Type PropertyType => _descriptor.PropertyType;
    public bool IsReadOnly => _descriptor.IsReadOnly;
    public bool IsBrowsable => _descriptor.IsBrowsable;
    public AttributeCollection Attributes => _descriptor.Attributes;

    public object? GetValue()
    {
        return _descriptor.GetValue(_component);
    }

    public void SetValue(object? value)
    {
        if (IsReadOnly)
            throw new InvalidOperationException($"Property '{Name}' is read-only.");

        _descriptor.SetValue(_component, ConvertValue(value));
    }

    object? ConvertValue(object? value)
    {
        var targetType = Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;
        if (value == null)
            return Nullable.GetUnderlyingType(PropertyType) != null || !PropertyType.IsValueType
                ? null
                : Activator.CreateInstance(targetType);

        if (targetType.IsInstanceOfType(value))
            return value;

        if (targetType.IsEnum)
            return value is string text
                ? Enum.Parse(targetType, text)
                : Enum.ToObject(targetType, value);

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(value.GetType()))
            return converter.ConvertFrom(null, CultureInfo.CurrentCulture, value);

        if (value is string stringValue && converter.CanConvertFrom(typeof(string)))
            return converter.ConvertFromString(null, CultureInfo.CurrentCulture, stringValue);

        return Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
    }
}
