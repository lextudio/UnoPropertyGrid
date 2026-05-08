using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace UnoPropertyGrid;

public sealed class PropertyGridPropertyDescriptor
{
    readonly object _component;
    readonly PropertyInfo _property;

    public PropertyGridPropertyDescriptor(object component, PropertyInfo property)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public string Name => _property.Name;

    public string DisplayName =>
        _property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName is { Length: > 0 } dn ? dn : _property.Name;

    public string Category =>
        _property.GetCustomAttribute<CategoryAttribute>()?.Category is { Length: > 0 } cat ? cat : "Misc";

    public string Description =>
        _property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

    public Type PropertyType => _property.PropertyType;

    public bool IsReadOnly =>
        !_property.CanWrite || _property.GetCustomAttribute<ReadOnlyAttribute>()?.IsReadOnly == true;

    public bool IsBrowsable =>
        _property.GetCustomAttribute<BrowsableAttribute>()?.Browsable ?? true;

    public IEnumerable<Attribute> Attributes =>
        _property.GetCustomAttributes().OfType<Attribute>();

    public object? GetValue() => _property.GetValue(_component);

    public void SetValue(object? value)
    {
        if (IsReadOnly)
            throw new InvalidOperationException($"Property '{Name}' is read-only.");

        var converted = ConvertValue(value);
        PropertyGridLogger.Log($"Descriptor [{Name}]: SetValue component={_component?.GetType().Name}, value={value}, converted={converted}");
        _property.SetValue(_component, converted);
        PropertyGridLogger.Log($"Descriptor [{Name}]: SetValue done, read-back={_property.GetValue(_component)}");
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

        // TypeDescriptor.GetConverter(Type) is safe — it doesn't touch COM descriptors
        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(value.GetType()))
            return converter.ConvertFrom(null, CultureInfo.CurrentCulture, value);

        if (value is string stringValue && converter.CanConvertFrom(typeof(string)))
            return converter.ConvertFromString(null, CultureInfo.CurrentCulture, stringValue);

        return Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
    }
}
