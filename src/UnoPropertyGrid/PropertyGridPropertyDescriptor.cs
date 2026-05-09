using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

namespace UnoPropertyGrid;

public sealed class PropertyGridPropertyDescriptor
{
    readonly object _component;
    readonly PropertyInfo? _property;
    readonly PropertyDescriptor? _descriptor;

    public PropertyGridPropertyDescriptor(object component, PropertyInfo property)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public PropertyGridPropertyDescriptor(object component, PropertyDescriptor descriptor)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public string Name => _descriptor?.Name ?? _property!.Name;

    public string DisplayName =>
        _descriptor?.DisplayName is { Length: > 0 } descriptorName && descriptorName != Name
            ? descriptorName
            : _property?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName is { Length: > 0 } dn
                ? dn
                : Name;

    public string Category =>
        _descriptor?.Category is { Length: > 0 } descriptorCategory && descriptorCategory != CategoryAttribute.Default.Category
            ? descriptorCategory
            : _property?.GetCustomAttribute<CategoryAttribute>()?.Category is { Length: > 0 } cat
                ? cat
                : "Misc";

    public string Description =>
        _descriptor?.Description is { Length: > 0 } descriptorDescription
            ? descriptorDescription
            : _property?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

    public Type PropertyType => _descriptor?.PropertyType ?? _property!.PropertyType;

    public bool IsReadOnly =>
        _descriptor?.IsReadOnly
        ?? (!_property!.CanWrite || _property.GetCustomAttribute<ReadOnlyAttribute>()?.IsReadOnly == true);

    public bool IsBrowsable =>
        _descriptor?.IsBrowsable
        ?? (_property!.GetCustomAttribute<BrowsableAttribute>()?.Browsable ?? true);

    public IEnumerable<Attribute> Attributes =>
        _descriptor?.Attributes.Cast<Attribute>() ?? _property!.GetCustomAttributes().OfType<Attribute>();

    public object? GetDefaultValue()
    {
        if (_component is DependencyObject dependencyObject && TryGetDependencyPropertyDefaultValue(dependencyObject, out var dependencyDefaultValue))
            return dependencyDefaultValue;

        var defaultValue = Attributes.OfType<DefaultValueAttribute>().FirstOrDefault();
        if (defaultValue != null)
            return defaultValue.Value;

        return GetValue();
    }

    bool TryGetDependencyPropertyDefaultValue(DependencyObject dependencyObject, out object? defaultValue)
    {
        defaultValue = null;
        string fieldName = Name + "Property";
        var field = dependencyObject.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (field?.GetValue(null) is not DependencyProperty dependencyProperty)
            return false;

        defaultValue = dependencyProperty.GetMetadata(dependencyObject.GetType()).DefaultValue;
        return true;
    }

    public object? GetValue()
    {
        return _descriptor != null
            ? _descriptor.GetValue(_component)
            : _property!.GetValue(_component);
    }

    public void SetValue(object? value)
    {
        if (IsReadOnly)
            throw new InvalidOperationException($"Property '{Name}' is read-only.");

        var converted = ConvertValue(value);
        PropertyGridLogger.Log($"Descriptor [{Name}]: SetValue component={_component?.GetType().Name}, value={value}, converted={converted}");
        if (_descriptor != null)
            _descriptor.SetValue(_component, converted);
        else
            _property!.SetValue(_component, converted);
        PropertyGridLogger.Log($"Descriptor [{Name}]: SetValue done, read-back={GetValue()}");
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

        if (value is string stringValue)
        {
            if (targetType == typeof(FontFamily))
                return new FontFamily(stringValue);

            if (targetType == typeof(FontWeight))
                return ConvertFontWeight(stringValue);

            if (typeof(Brush).IsAssignableFrom(targetType))
                return ConvertBrush(stringValue);
        }

        // TypeDescriptor.GetConverter(Type) is safe — it doesn't touch COM descriptors
        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(value.GetType()))
            return converter.ConvertFrom(null, CultureInfo.CurrentCulture, value);

        if (value is string textValue && converter.CanConvertFrom(typeof(string)))
            return converter.ConvertFromString(null, CultureInfo.CurrentCulture, textValue);

        return Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
    }

    static FontWeight ConvertFontWeight(string value)
    {
        var namedWeight = value.ToLowerInvariant() switch
        {
            "thin" => 100,
            "extralight" => 200,
            "light" => 300,
            "normal" => 400,
            "medium" => 500,
            "semibold" => 600,
            "bold" => 700,
            "extrabold" => 800,
            "black" => 900,
            _ => 0
        };

        if (namedWeight != 0)
            return new FontWeight { Weight = (ushort)namedWeight };

        if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericWeight))
            return new FontWeight { Weight = numericWeight };

        return new FontWeight { Weight = 400 };
    }

    static Brush? ConvertBrush(string value)
    {
        if (string.Equals(value, "No brush", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Transparent", StringComparison.OrdinalIgnoreCase))
            return value.Equals("Transparent", StringComparison.OrdinalIgnoreCase)
                ? new SolidColorBrush(Colors.Transparent)
                : null;

        return new SolidColorBrush(ConvertColor(value));
    }

    static Color ConvertColor(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "black" => Colors.Black,
            "white" => Colors.White,
            "red" => Colors.Red,
            "green" => Colors.Green,
            "blue" => Colors.Blue,
            "yellow" => Colors.Yellow,
            "gray" or "grey" => Colors.Gray,
            "transparent" => Colors.Transparent,
            _ => Colors.Black
        };
    }
}
