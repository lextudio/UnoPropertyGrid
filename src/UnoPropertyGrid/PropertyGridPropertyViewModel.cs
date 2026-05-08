using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnoPropertyGrid;

public sealed class PropertyGridPropertyViewModel : INotifyPropertyChanged
{
    readonly PropertyGridPropertyDescriptor _property;
    object? _value;
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
            PropertyGridLogger.Log($"VM [{Name}]: RefreshValue read back _value={_value}");
            Error = null;
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(DisplayValue));
            OnPropertyChanged(nameof(BooleanValue));
            OnPropertyChanged(nameof(StringValue));
            OnPropertyChanged(nameof(NumberValue));
            OnPropertyChanged(nameof(EnumValue));
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

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
