using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace UnoPropertyGrid;

public sealed class PropertyGridCategoryViewModel : INotifyPropertyChanged
{
    bool _isExpanded = true;

    public PropertyGridCategoryViewModel(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Misc" : name;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;

            _isExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RowsVisibility));
            OnPropertyChanged(nameof(ExpandGlyph));
        }
    }

    public Visibility RowsVisibility => IsExpanded ? Visibility.Visible : Visibility.Collapsed;

    public string ExpandGlyph => IsExpanded ? "\uE70D" : "\uE76C";

    public ObservableCollection<PropertyGridPropertyViewModel> Rows { get; } = new();

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
