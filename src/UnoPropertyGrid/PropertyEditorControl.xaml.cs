using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoPropertyGrid;

public sealed partial class PropertyEditorControl : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PropertyGridPropertyViewModel),
            typeof(PropertyEditorControl),
            new PropertyMetadata(null, OnViewModelChanged));

    bool _updatingFromEditor;

    public PropertyEditorControl()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PropertyGridPropertyViewModel? ViewModel
    {
        get => (PropertyGridPropertyViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PropertyEditorControl)d;
        if (e.OldValue is PropertyGridPropertyViewModel oldViewModel)
            oldViewModel.PropertyChanged -= control.OnViewModelPropertyChanged;
        if (e.NewValue is PropertyGridPropertyViewModel newViewModel)
            newViewModel.PropertyChanged += control.OnViewModelPropertyChanged;
        control.NotifyBindingsChanged();
    }

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_updatingFromEditor)
            NotifyBindingsChanged();
    }

    void NotifyBindingsChanged()
    {
        OnPropertyChanged(nameof(BooleanValue));
        OnPropertyChanged(nameof(TextValue));
        OnPropertyChanged(nameof(NumberValue));
        OnPropertyChanged(nameof(EnumValue));
        OnPropertyChanged(nameof(EnumValues));
        OnPropertyChanged(nameof(ReadOnlyValue));
        OnPropertyChanged(nameof(BooleanEditorVisibility));
        OnPropertyChanged(nameof(TextEditorVisibility));
        OnPropertyChanged(nameof(NumberEditorVisibility));
        OnPropertyChanged(nameof(EnumEditorVisibility));
        OnPropertyChanged(nameof(ReadOnlyEditorVisibility));
    }

    public bool? BooleanValue
    {
        get => ViewModel?.BooleanValue;
        set => SetFromEditor(() => { if (ViewModel != null) ViewModel.BooleanValue = value; });
    }

    public string TextValue
    {
        get => ViewModel?.StringValue ?? string.Empty;
        set => SetFromEditor(() => { if (ViewModel != null) ViewModel.StringValue = value; });
    }

    public string NumberValue
    {
        get => ViewModel?.NumberValue ?? string.Empty;
        set => SetFromEditor(() => { if (ViewModel != null) ViewModel.NumberValue = value; });
    }

    public object? EnumValue
    {
        get => ViewModel?.EnumValue;
        set => SetFromEditor(() => { if (ViewModel != null) ViewModel.EnumValue = value; });
    }

    public IReadOnlyList<object> EnumValues => ViewModel?.EnumValues ?? Array.Empty<object>();
    public string ReadOnlyValue => ViewModel?.DisplayValue ?? string.Empty;

    public Visibility BooleanEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.Boolean ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TextEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.Text ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NumberEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.Number ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EnumEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.Enum ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ReadOnlyEditorVisibility => ViewModel == null || ViewModel.EditorKind == PropertyEditorKind.ReadOnly ? Visibility.Visible : Visibility.Collapsed;

    void SetFromEditor(Action update)
    {
        _updatingFromEditor = true;
        try
        {
            update();
        }
        finally
        {
            _updatingFromEditor = false;
        }
    }

    void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
