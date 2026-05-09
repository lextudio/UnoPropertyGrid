using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

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
        PropertyGridLogger.Log($"Editor [{ViewModel?.Name}]: OnViewModelPropertyChanged prop={e.PropertyName}, _updatingFromEditor={_updatingFromEditor}");
        if (!_updatingFromEditor)
            NotifyBindingsChanged();
    }

    void NotifyBindingsChanged()
    {
        PropertyGridLogger.Log($"Editor [{ViewModel?.Name}]: NotifyBindingsChanged, BooleanValue={BooleanValue}");
        OnPropertyChanged(nameof(BooleanValue));
        OnPropertyChanged(nameof(TextValue));
        OnPropertyChanged(nameof(NumberValue));
        OnPropertyChanged(nameof(EnumValue));
        OnPropertyChanged(nameof(EnumValues));
        OnPropertyChanged(nameof(BrushValue));
        OnPropertyChanged(nameof(BrushPreview));
        OnPropertyChanged(nameof(CommonBrushes));
        OnPropertyChanged(nameof(FontFamilyValue));
        OnPropertyChanged(nameof(FontFamilies));
        OnPropertyChanged(nameof(FontWeightValue));
        OnPropertyChanged(nameof(FontWeights));
        OnPropertyChanged(nameof(FontStyleValue));
        OnPropertyChanged(nameof(FontStyles));
        OnPropertyChanged(nameof(FontStretchValue));
        OnPropertyChanged(nameof(FontStretches));
        OnPropertyChanged(nameof(ReadOnlyValue));
        OnPropertyChanged(nameof(BooleanEditorVisibility));
        OnPropertyChanged(nameof(TextEditorVisibility));
        OnPropertyChanged(nameof(NumberEditorVisibility));
        OnPropertyChanged(nameof(EnumEditorVisibility));
        OnPropertyChanged(nameof(BrushEditorVisibility));
        OnPropertyChanged(nameof(FontFamilyEditorVisibility));
        OnPropertyChanged(nameof(FontWeightEditorVisibility));
        OnPropertyChanged(nameof(FontStyleEditorVisibility));
        OnPropertyChanged(nameof(FontStretchEditorVisibility));
        OnPropertyChanged(nameof(ReadOnlyEditorVisibility));
    }

    public bool? BooleanValue
    {
        get
        {
            var v = ViewModel?.BooleanValue;
            PropertyGridLogger.Log($"Editor [{ViewModel?.Name}]: BooleanValue GET => {v}");
            return v;
        }
        set
        {
            PropertyGridLogger.Log($"Editor [{ViewModel?.Name}]: BooleanValue SET => {value}");
            SetFromEditor(() => { if (ViewModel != null) ViewModel.BooleanValue = value; });
        }
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
    public IReadOnlyList<string> CommonBrushes { get; } = new[] { "No brush", "Transparent", "Black", "White", "Gray", "Red", "Green", "Blue", "Yellow" };
    public IReadOnlyList<string> FontFamilies { get; } = new[] { "Segoe UI", "Arial", "Calibri", "Cambria", "Consolas", "Courier New", "Georgia", "Tahoma", "Times New Roman", "Verdana" };
    public IReadOnlyList<string> FontWeights { get; } = new[] { "Thin", "ExtraLight", "Light", "Normal", "Medium", "SemiBold", "Bold", "ExtraBold", "Black" };
    public IReadOnlyList<object> FontStyles { get; } = Enum.GetValues(typeof(FontStyle)).Cast<object>().ToArray();
    public IReadOnlyList<object> FontStretches { get; } = Enum.GetValues(typeof(FontStretch)).Cast<object>().ToArray();
    public string ReadOnlyValue => ViewModel?.DisplayValue ?? string.Empty;

    public string BrushValue
    {
        get => ViewModel?.BrushValue ?? "No brush";
        set => SetFromEditor(() => { if (ViewModel != null) ViewModel.BrushValue = value; });
    }

    public Brush BrushPreview => ViewModel?.BrushPreview ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);

    public string FontFamilyValue
    {
        get => ViewModel?.FontFamilyValue ?? "Segoe UI";
        set => SetFromEditor(() => { if (ViewModel != null) ViewModel.FontFamilyValue = value; });
    }

    public string FontWeightValue
    {
        get => ViewModel?.FontWeightValue ?? "Normal";
        set => SetFromEditor(() => { if (ViewModel != null) ViewModel.FontWeightValue = value; });
    }

    public object? FontStyleValue
    {
        get => ViewModel?.FontStyleValue;
        set => SetFromEditor(() => { if (ViewModel != null) ViewModel.FontStyleValue = value; });
    }

    public object? FontStretchValue
    {
        get => ViewModel?.FontStretchValue;
        set => SetFromEditor(() => { if (ViewModel != null) ViewModel.FontStretchValue = value; });
    }

    public Visibility BooleanEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.Boolean ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TextEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.Text ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NumberEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.Number ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EnumEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.Enum ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BrushEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.Brush ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FontFamilyEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.FontFamily ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FontWeightEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.FontWeight ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FontStyleEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.FontStyle ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FontStretchEditorVisibility => ViewModel?.EditorKind == PropertyEditorKind.FontStretch ? Visibility.Visible : Visibility.Collapsed;
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
