using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnoPropertyGrid;

public sealed class PropertyGridEventViewModel : INotifyPropertyChanged
{
    string _handlerName = string.Empty;
    string? _error;

    public PropertyGridEventViewModel(PropertyGridEventDescriptor descriptor)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PropertyGridEventDescriptor Descriptor { get; }
    public string Name => Descriptor.Name;
    public string DisplayName => Descriptor.DisplayName;
    public string HandlerTypeName => Descriptor.HandlerType?.Name ?? string.Empty;
    public string Description => Descriptor.Description;
    public bool IsDefaultValue => string.IsNullOrWhiteSpace(HandlerName);

    public string HandlerName
    {
        get => _handlerName;
        set
        {
            if (_handlerName == value)
                return;

            _handlerName = value;
            Error = IsValidHandlerName(value) ? null : "Handler name must be a valid C# identifier.";
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDefaultValue));
        }
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (_error == value)
                return;

            _error = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(Error);

    static bool IsValidHandlerName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (!IsIdentifierStart(value[0]))
            return false;

        for (var i = 1; i < value.Length; i++)
        {
            if (!IsIdentifierPart(value[i]))
                return false;
        }

        return true;
    }

    static bool IsIdentifierStart(char ch) => ch == '_' || char.IsLetter(ch);
    static bool IsIdentifierPart(char ch) => ch == '_' || char.IsLetterOrDigit(ch);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
