using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.Metadata;
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
        : this(component, property, null)
    {
    }

    public PropertyGridPropertyDescriptor(object component, PropertyInfo property, PropertyDescriptor? descriptor)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _property = property ?? throw new ArgumentNullException(nameof(property));
        _descriptor = descriptor;
    }

    public PropertyGridPropertyDescriptor(object component, PropertyDescriptor descriptor)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public object Component => _component;

    public string Name => _descriptor?.Name ?? _property!.Name;

    public string DisplayName =>
        _descriptor?.DisplayName is { Length: > 0 } descriptorName && descriptorName != Name
            ? descriptorName
            : Attributes.OfType<DisplayNameAttribute>().LastOrDefault()?.DisplayName is { Length: > 0 } dn
                ? dn
                : Name;

    public string Category =>
        _descriptor?.Category is { Length: > 0 } descriptorCategory && descriptorCategory != CategoryAttribute.Default.Category
            ? descriptorCategory
            : Attributes.OfType<CategoryAttribute>().LastOrDefault()?.Category is { Length: > 0 } cat
                ? cat
                : "Miscellaneous";

    public string Description =>
        _descriptor?.Description is { Length: > 0 } descriptorDescription
            ? descriptorDescription
            : Attributes.OfType<DescriptionAttribute>().LastOrDefault()?.Description ?? string.Empty;

    public Type PropertyType => _descriptor?.PropertyType ?? _property!.PropertyType;

    public bool IsReadOnly =>
        _descriptor?.IsReadOnly == true
        || (_property != null && !_property.CanWrite)
        || Attributes.OfType<ReadOnlyAttribute>().LastOrDefault()?.IsReadOnly == true;

    public bool IsBrowsable =>
        Attributes.OfType<BrowsableAttribute>().LastOrDefault()?.Browsable
        ?? _descriptor?.IsBrowsable
        ?? true;

    public IEnumerable<Attribute> Attributes =>
        GetAttributes();

    IEnumerable<Attribute> GetAttributes()
    {
        var localAttributes = _descriptor?.Attributes.Cast<Attribute>() ?? _property!.GetCustomAttributes().OfType<Attribute>();
        foreach (var attribute in localAttributes)
            yield return attribute;

        foreach (var attribute in AttributeTableStore.GetCustomAttributes(_component.GetType(), Name))
            yield return attribute;
    }

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
        return _property != null
            ? _property.GetValue(_component)
            : _descriptor!.GetValue(_component);
    }

    public void SetValue(object? value)
    {
        if (IsReadOnly)
            throw new InvalidOperationException($"Property '{Name}' is read-only.");

        var converted = ConvertValue(value);
        PropertyGridLogger.Log($"Descriptor [{Name}]: SetValue component={_component?.GetType().Name}, value={value}, converted={converted}");
        if (_property != null)
            _property.SetValue(_component, converted);
        else
            _descriptor!.SetValue(_component, converted);
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

        if (IsBrushType(targetType) && IsBrushType(value.GetType()))
            return ConvertBrushObject(value, targetType);

        if (targetType.IsEnum)
            return value is string text
                ? Enum.Parse(targetType, text)
                : Enum.ToObject(targetType, value);

        if (value is string stringValue)
        {
            if (targetType == typeof(FontFamily) || targetType == typeof(Microsoft.UI.Xaml.Media.FontFamily))
                return new Microsoft.UI.Xaml.Media.FontFamily(stringValue);

            if (targetType == typeof(FontWeight) || targetType.FullName == "Microsoft.UI.Text.FontWeight")
                return ConvertFontWeight(stringValue, targetType);

            if (targetType == typeof(FontStyle) || targetType.FullName == "Microsoft.UI.Text.FontStyle")
                return ConvertFontStyle(stringValue, targetType);

            if (targetType == typeof(FontStretch) || targetType.FullName == "Microsoft.UI.Text.FontStretch")
                return ConvertFontStretch(stringValue, targetType);

            if (IsBrushType(targetType))
                return ConvertBrush(stringValue, targetType);
        }

        if (value is FontWeight winuiFontWeight && targetType.FullName == "Microsoft.UI.Text.FontWeight")
            return ConvertFontWeight(winuiFontWeight, targetType);

        if (value.GetType().FullName == "Microsoft.UI.Text.FontWeight" && targetType == typeof(FontWeight))
            return ConvertFontWeight(value, targetType);

        if (value is FontStyle winuiFontStyle && targetType.FullName == "Microsoft.UI.Text.FontStyle")
            return ConvertFontStyle(winuiFontStyle, targetType);

        if (value.GetType().FullName == "Microsoft.UI.Text.FontStyle" && targetType == typeof(FontStyle))
            return ConvertFontStyle(value, targetType);

        if (value is FontStretch winuiFontStretch && targetType.FullName == "Microsoft.UI.Text.FontStretch")
            return ConvertFontStretch(winuiFontStretch, targetType);

        if (value.GetType().FullName == "Microsoft.UI.Text.FontStretch" && targetType == typeof(FontStretch))
            return ConvertFontStretch(value, targetType);

        // TypeDescriptor.GetConverter(Type) is safe — it doesn't touch COM descriptors
        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(value.GetType()))
            return converter.ConvertFrom(null, CultureInfo.CurrentCulture, value);

        if (value is string textValue && converter.CanConvertFrom(typeof(string)))
            return converter.ConvertFromString(null, CultureInfo.CurrentCulture, textValue);

        return Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
    }

    static object ConvertFontWeight(string value, Type targetType)
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

        if (namedWeight == 0 && ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericWeight))
            namedWeight = numericWeight;

        if (namedWeight == 0)
            namedWeight = 400;

        return CreateFontWeight(targetType, (ushort)namedWeight);
    }

    static object ConvertFontWeight(FontWeight value, Type targetType)
    {
        if (targetType == typeof(FontWeight))
            return new FontWeight { Weight = value.Weight };

        if (targetType.FullName == "Microsoft.UI.Text.FontWeight")
            return CreateFontStruct(targetType, "Weight", value.Weight);

        return value;
    }

    static object ConvertFontWeight(object value, Type targetType)
    {
        if (value.GetType().FullName == "Microsoft.UI.Text.FontWeight")
        {
            var weight = (ushort)(value.GetType().GetProperty("Weight")?.GetValue(value) ?? 400);
            return targetType == typeof(FontWeight)
                ? new FontWeight { Weight = weight }
                : CreateFontStruct(targetType, "Weight", weight);
        }

        return value;
    }

    static object CreateFontWeight(Type targetType, ushort weight)
    {
        if (targetType == typeof(FontWeight))
            return new FontWeight { Weight = weight };

        if (targetType.FullName == "Microsoft.UI.Text.FontWeight")
            return CreateFontStruct(targetType, "Weight", weight);

        return Activator.CreateInstance(targetType)!;
    }

    static object ConvertFontStyle(string value, Type targetType)
    {
        if (targetType == typeof(FontStyle))
        {
            return Enum.TryParse<FontStyle>(value, true, out var parsed) ? parsed : FontStyle.Normal;
        }

        if (targetType.FullName == "Microsoft.UI.Text.FontStyle")
        {
            return Enum.Parse(targetType, value, true)!;
        }

        return Activator.CreateInstance(targetType)!;
    }

    static object ConvertFontStyle(object value, Type targetType)
    {
        if (targetType == typeof(FontStyle))
        {
            if (value.GetType().FullName == "Microsoft.UI.Text.FontStyle")
                return Enum.Parse(typeof(FontStyle), value.ToString()!, true)!;
            return value;
        }

        if (targetType.FullName == "Microsoft.UI.Text.FontStyle")
        {
            return Enum.Parse(targetType, value.ToString()!, true)!;
        }

        return value;
    }

    static object ConvertFontStretch(string value, Type targetType)
    {
        if (targetType == typeof(FontStretch))
        {
            return Enum.TryParse<FontStretch>(value, true, out var parsed) ? parsed : default(FontStretch);
        }

        if (targetType.FullName == "Microsoft.UI.Text.FontStretch")
        {
            return Enum.Parse(targetType, value, true)!;
        }

        return Activator.CreateInstance(targetType)!;
    }

    static object ConvertFontStretch(object value, Type targetType)
    {
        if (targetType == typeof(FontStretch))
        {
            return Enum.Parse(typeof(FontStretch), value.ToString()!, true)!;
        }

        if (targetType.FullName == "Microsoft.UI.Text.FontStretch")
        {
            return Enum.Parse(targetType, value.ToString()!, true)!;
        }

        return value;
    }

    static object CreateFontStruct(Type targetType, string propertyName, object propertyValue)
    {
        var instance = Activator.CreateInstance(targetType)!;
        targetType.GetProperty(propertyName)?.SetValue(instance, propertyValue);
        return instance;
    }

    static object? ConvertBrushObject(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value))
            return value;

        var color = GetBrushColor(value);
        if (color == null)
            return null;

        return CreateBrushInstance(targetType, color.Value);
    }

    static bool IsBrushType(Type type)
    {
        var fullName = type.FullName;
        return typeof(Brush).IsAssignableFrom(type)
            || fullName == "Windows.UI.Xaml.Media.Brush"
            || fullName == "Microsoft.UI.Xaml.Media.Brush"
            || fullName == "Windows.UI.Xaml.Media.SolidColorBrush"
            || fullName == "Microsoft.UI.Xaml.Media.SolidColorBrush";
    }

    static object? ConvertBrush(string value, Type targetType)
    {
        if (string.Equals(value, "No brush", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Transparent", StringComparison.OrdinalIgnoreCase))
        {
            var transparentColor = Colors.Transparent;
            return CreateBrushInstance(targetType, transparentColor);
        }

        var color = ConvertColor(value);
        return CreateBrushInstance(targetType, color);
    }

    static Color? GetBrushColor(object brush)
    {
        var type = brush.GetType();
        var colorProperty = type.GetProperty("Color");
        if (colorProperty == null)
            return null;

        var colorValue = colorProperty.GetValue(brush);
        if (colorValue == null)
            return null;

        if (colorValue is Color uiColor)
            return uiColor;

        var colorType = colorValue.GetType();
        if (colorType.FullName == "Microsoft.UI.Color" || colorType.FullName == "Windows.UI.Color")
        {
            var a = (byte)(colorType.GetProperty("A")?.GetValue(colorValue) ?? 255);
            var r = (byte)(colorType.GetProperty("R")?.GetValue(colorValue) ?? 0);
            var g = (byte)(colorType.GetProperty("G")?.GetValue(colorValue) ?? 0);
            var b = (byte)(colorType.GetProperty("B")?.GetValue(colorValue) ?? 0);
            return Color.FromArgb(a, r, g, b);
        }

        return null;
    }

    static object? CreateBrushInstance(Type targetType, Color color)
    {
        try
        {
            var targetColorType = targetType.Assembly.GetType("Microsoft.UI.Color")
                ?? targetType.Assembly.GetType("Windows.UI.Color")
                ?? typeof(Color);

            var targetColor = CreateColorInstance(targetColorType, color);
            var ctor = targetType.GetConstructor(new[] { targetColor.GetType() });
            if (ctor != null)
                return ctor.Invoke(new[] { targetColor });

            var instance = Activator.CreateInstance(targetType);
            instance?.GetType().GetProperty("Color")?.SetValue(instance, targetColor);
            return instance;
        }
        catch
        {
            return null;
        }
    }

    static object CreateColorInstance(Type colorType, Color sourceColor)
    {
        if (colorType.FullName == "Microsoft.UI.Color")
            return Activator.CreateInstance(colorType, sourceColor.A, sourceColor.R, sourceColor.G, sourceColor.B)!;
        if (colorType.FullName == "Windows.UI.Color")
            return Activator.CreateInstance(colorType, sourceColor.A, sourceColor.R, sourceColor.G, sourceColor.B)!;
        return sourceColor;
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
