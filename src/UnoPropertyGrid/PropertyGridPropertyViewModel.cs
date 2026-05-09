using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

namespace UnoPropertyGrid;

public sealed class PropertyGridPropertyViewModel : INotifyPropertyChanged
{
    readonly PropertyGridPropertyDescriptor _property;
    object? _value;
    object? _defaultValue;
    string? _error;

    public PropertyGridPropertyViewModel(PropertyGridPropertyDescriptor property)
    {
        _property = property ?? throw new ArgumentNullException(nameof(property));
        RefreshValue();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name => _property.Name;
    public string DisplayName => _property.DisplayName;
    public string Category => _property.Category;
    public string Description => _property.Description;
    public Type PropertyType => _property.PropertyType;
    public bool IsReadOnly => _property.IsReadOnly;
    public bool IsEditable => !IsReadOnly && EditorKind != PropertyEditorKind.ReadOnly;
    public bool IsDefaultValue => ValuesEqual(Value, _defaultValue);
    public PropertyEditorKind EditorKind => PropertyEditorKindExtensions.FromType(PropertyType, IsReadOnly);

    public object? Value
    {
        get => _value;
        set
        {
            PropertyGridLogger.Log($"VM [{Name}]: Value.set incoming={value} (type={value?.GetType().Name}), current _value={_value}, equal={Equals(_value, value)}");
            if (Equals(_value, value))
                return;

            try
            {
                _property.SetValue(value);
                PropertyGridLogger.Log($"VM [{Name}]: SetValue succeeded");
                _error = null;
                RefreshValue();
            }
            catch (Exception ex)
            {
                PropertyGridLogger.Log($"VM [{Name}]: SetValue FAILED: {ex}");
                Error = ex.Message;
            }
        }
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (_error != value)
            {
                _error = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(Error);

    public IReadOnlyList<object> EnumValues
    {
        get
        {
            var type = Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;
            return type.IsEnum ? Enum.GetValues(type).Cast<object>().ToArray() : Array.Empty<object>();
        }
    }

    public void RefreshValue()
    {
        try
        {
            _value = _property.GetValue();
            _defaultValue ??= _property.GetDefaultValue();
            PropertyGridLogger.Log($"VM [{Name}]: RefreshValue read back _value={_value}");
            Error = null;
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(IsDefaultValue));
            OnPropertyChanged(nameof(DisplayValue));
            OnPropertyChanged(nameof(BooleanValue));
            OnPropertyChanged(nameof(StringValue));
            OnPropertyChanged(nameof(NumberValue));
            OnPropertyChanged(nameof(EnumValue));
            OnPropertyChanged(nameof(BrushValue));
            OnPropertyChanged(nameof(BrushPreview));
            OnPropertyChanged(nameof(FontFamilyValue));
            OnPropertyChanged(nameof(FontWeightValue));
            OnPropertyChanged(nameof(FontStyleValue));
            OnPropertyChanged(nameof(FontStretchValue));
        }
        catch (Exception ex)
        {
            PropertyGridLogger.Log($"VM [{Name}]: RefreshValue FAILED: {ex.Message}");
            Error = ex.Message;
        }
    }

    public string DisplayValue => Value?.ToString() ?? "(null)";

    public bool? BooleanValue
    {
        get => Value is bool value ? value : null;
        set { if (EditorKind == PropertyEditorKind.Boolean) Value = value; }
    }

    public string StringValue
    {
        get => Value?.ToString() ?? string.Empty;
        set { if (EditorKind == PropertyEditorKind.Text) Value = value; }
    }

    public string NumberValue
    {
        get => Value?.ToString() ?? string.Empty;
        set { if (EditorKind == PropertyEditorKind.Number) Value = string.IsNullOrWhiteSpace(value) ? null : value; }
    }

    public object? EnumValue
    {
        get => Value;
        set { if (EditorKind == PropertyEditorKind.Enum) Value = value; }
    }

    public string BrushValue
    {
        get => GetBrushName(Value);
        set { if (EditorKind == PropertyEditorKind.Brush) Value = value; }
    }

    public Brush BrushPreview => Value as Brush ?? new SolidColorBrush(Colors.Transparent);

    public string FontFamilyValue
    {
        get => Value is FontFamily family ? family.Source : Value?.ToString() ?? "Segoe UI";
        set { if (EditorKind == PropertyEditorKind.FontFamily) Value = value; }
    }

    public string FontWeightValue
    {
        get => GetFontWeightName(Value);
        set { if (EditorKind == PropertyEditorKind.FontWeight) Value = value; }
    }

    public object? FontStyleValue
    {
        get => Value;
        set { if (EditorKind == PropertyEditorKind.FontStyle) Value = value; }
    }

    public object? FontStretchValue
    {
        get => Value;
        set { if (EditorKind == PropertyEditorKind.FontStretch) Value = value; }
    }

    static string GetBrushName(object? value)
    {
        if (value == null)
            return "No brush";

        if (value is SolidColorBrush solidColorBrush)
            return GetColorName(solidColorBrush.Color);

        return value.ToString() ?? "Custom brush";
    }

    static string GetColorName(Color color)
    {
        if (color == Colors.Transparent)
            return "Transparent";
        if (color == Colors.Black)
            return "Black";
        if (color == Colors.White)
            return "White";
        if (color == Colors.Red)
            return "Red";
        if (color == Colors.Green)
            return "Green";
        if (color == Colors.Blue)
            return "Blue";
        if (color == Colors.Yellow)
            return "Yellow";
        if (color == Colors.Gray)
            return "Gray";

        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    static string GetFontWeightName(object? value)
    {
        if (value is not FontWeight weight)
            return "Normal";

        return weight.Weight switch
        {
            100 => "Thin",
            200 => "ExtraLight",
            300 => "Light",
            400 => "Normal",
            500 => "Medium",
            600 => "SemiBold",
            700 => "Bold",
            800 => "ExtraBold",
            900 => "Black",
            _ => weight.Weight.ToString()
        };
    }

    static bool ValuesEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null)
            return false;
        if (left is FontFamily leftFamily && right is FontFamily rightFamily)
            return string.Equals(leftFamily.Source, rightFamily.Source, StringComparison.Ordinal);
        if (left is FontWeight leftWeight && right is FontWeight rightWeight)
            return leftWeight.Weight == rightWeight.Weight;
        if (left is SolidColorBrush leftBrush && right is SolidColorBrush rightBrush)
            return leftBrush.Color == rightBrush.Color;

        return Equals(left, right);
    }

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
