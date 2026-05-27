using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using Windows.UI.Text;
using Button = Microsoft.UI.Xaml.Controls.Button;
using TextBox = LeXtudio.UI.Controls.TextBox;
using LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing;

namespace UnoPropertyGrid;

public sealed partial class PropertyGridControl : UserControl, INotifyPropertyChanged
{
    const double DefaultNameColumnWidth = 220d;
    const double MinNameColumnWidth = 120d;
    const double MinValueColumnWidth = 140d;
    // Uniform vertical gap between rows and between a row and its section edges.
    // Matches the header panel's RowSpacing so spacing is consistent throughout.
    const double RowGap = 8d;

    public static readonly DependencyProperty SelectedObjectProperty =
        DependencyProperty.Register(
            nameof(SelectedObject),
            typeof(object),
            typeof(PropertyGridControl),
            new PropertyMetadata(null, OnSelectedObjectChanged));

    public static readonly DependencyProperty ShowReadOnlyPropertiesProperty =
        DependencyProperty.Register(
            nameof(ShowReadOnlyProperties),
            typeof(bool),
            typeof(PropertyGridControl),
            new PropertyMetadata(true, OnFilterPropertyChanged));

    public static readonly DependencyProperty SortModeProperty =
        DependencyProperty.Register(
            nameof(SortMode),
            typeof(PropertyGridSortMode),
            typeof(PropertyGridControl),
            new PropertyMetadata(PropertyGridSortMode.Categorized, OnFilterPropertyChanged));

    public static readonly DependencyProperty ViewModeProperty =
        DependencyProperty.Register(
            nameof(ViewMode),
            typeof(PropertyGridViewMode),
            typeof(PropertyGridControl),
            new PropertyMetadata(PropertyGridViewMode.Properties, OnFilterPropertyChanged));

    public static readonly DependencyProperty NameColumnWidthProperty =
        DependencyProperty.Register(
            nameof(NameColumnWidth),
            typeof(double),
            typeof(PropertyGridControl),
            new PropertyMetadata(DefaultNameColumnWidth, OnNameColumnWidthChanged));

    public static readonly DependencyProperty ShowDescriptionPaneProperty =
        DependencyProperty.Register(
            nameof(ShowDescriptionPane),
            typeof(bool),
            typeof(PropertyGridControl),
            new PropertyMetadata(false, OnDescriptionPaneChanged));

    public static readonly DependencyProperty PropertyGridThemeProperty =
        DependencyProperty.Register(
            nameof(PropertyGridTheme),
            typeof(ElementTheme),
            typeof(PropertyGridControl),
            new PropertyMetadata(ElementTheme.Default, OnPropertyGridThemeChanged));

    readonly ObservableCollection<PropertyGridPropertyViewModel> _properties = new();
    readonly ObservableCollection<PropertyGridPropertyViewModel> _flatProperties = new();
    readonly ObservableCollection<PropertyGridCategoryViewModel> _categories = new();
    readonly ObservableCollection<PropertyGridEventViewModel> _events = new();
    readonly ObservableCollection<PropertyGridEventViewModel> _visibleEvents = new();
    IPropertyGridPropertyProvider _propertyProvider = new TypeDescriptorPropertyProvider();
    readonly IPropertyGridEventProvider _eventProvider = new ReflectionEventProvider();
    readonly IPropertyGridEditorProvider _builtInEditorProvider = new BuiltInPropertyEditorProvider();
    readonly Dictionary<string, bool> _categoryExpansion = new(StringComparer.Ordinal);
    bool _applyingNameColumnWidth;
    bool _syncingNameColumnFromSplitter;
    bool _categorizedRowsDirty = true;
    bool _flatRowsDirty = true;
    bool _eventRowsDirty = true;
    SolidColorBrush _backgroundBrush = new(Colors.White);
    SolidColorBrush _panelBrush = new(Colors.White);
    SolidColorBrush _categoryBrush = new(Colors.White);
    SolidColorBrush _cellBrush = new(Colors.White);
    SolidColorBrush _borderBrush = new(Colors.LightGray);
    SolidColorBrush _foregroundBrush = new(Colors.Black);
    SolidColorBrush _mutedForegroundBrush = new(Colors.Gray);
    SolidColorBrush _overrideIndicatorBrush = new(Colors.Black);
    string _searchText = string.Empty;
    readonly List<Action<ElementTheme>> _editorThemeCallbacks = [];
    // VS-style row selection. The blue/white selection brushes are theme-independent.
    readonly SolidColorBrush _selectionBrush = new(Color.FromArgb(255, 0x00, 0x78, 0xD4));
    readonly SolidColorBrush _selectionForegroundBrush = new(Colors.White);
    Grid? _selectedRowGrid;
    TextBlock? _selectedNameText;
    TextBlock? _selectedValueText;

    public PropertyGridControl()
    {
        InitializeComponent();
        RowsHost.LayoutUpdated += OnRowsHostLayoutUpdated;
        ApplyThemeBrushes();
        ApplyNameColumnWidth(NameColumnWidth);
        // Re-apply theme brushes when the app theme changes so platform controls
        // like TextBox (placeholder color) and ComboBox pick up the correct palette.
        RootControl.ActualThemeChanged += (_, _) => ApplyThemeBrushes();
        // The TextBox ControlTemplate applies after the constructor returns (first layout pass),
        // so SetPlaceholderForeground finds no elements when called from ApplyThemeBrushes().
        // Subscribe to Loaded to fix the placeholder once the template is fully in the tree.
        SearchBox.Loaded += (_, _) => SetPlaceholderForeground(SearchBox, _mutedForegroundBrush);
        ObjectNameBox.Loaded += (_, _) => SetPlaceholderForeground(ObjectNameBox, _mutedForegroundBrush);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public object? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public bool ShowReadOnlyProperties
    {
        get => (bool)GetValue(ShowReadOnlyPropertiesProperty);
        set => SetValue(ShowReadOnlyPropertiesProperty, value);
    }

    public PropertyGridSortMode SortMode
    {
        get => (PropertyGridSortMode)GetValue(SortModeProperty);
        set => SetValue(SortModeProperty, value);
    }

    public PropertyGridViewMode ViewMode
    {
        get => (PropertyGridViewMode)GetValue(ViewModeProperty);
        set => SetValue(ViewModeProperty, value);
    }

    public double NameColumnWidth
    {
        get => (double)GetValue(NameColumnWidthProperty);
        set => SetValue(NameColumnWidthProperty, value);
    }

    public bool ShowDescriptionPane
    {
        get => (bool)GetValue(ShowDescriptionPaneProperty);
        set => SetValue(ShowDescriptionPaneProperty, value);
    }

    public ElementTheme PropertyGridTheme
    {
        get => (ElementTheme)GetValue(PropertyGridThemeProperty);
        set => SetValue(PropertyGridThemeProperty, value);
    }

    public IList<IPropertyGridEditorProvider> EditorProviders { get; } = new List<IPropertyGridEditorProvider>();

    public IPropertyGridEventService? EventService { get; set; }

    /// <summary>
    /// Overrides the default reflection-based property provider.
    /// Assign a <see cref="LambdaPropertyProvider"/> (typically produced by
    /// <c>GeneratedPropertyGridDescriptors.CreateProvider()</c>) to make the
    /// property grid AOT-safe for known component types.
    /// Setting this property refreshes the current selected object.
    /// </summary>
    public IPropertyGridPropertyProvider PropertyProvider
    {
        get => _propertyProvider;
        set
        {
            _propertyProvider = value ?? throw new ArgumentNullException(nameof(value));
            RefreshMembers(SelectedObject);
        }
    }

    public string ObjectName => GetObjectName(SelectedObject);
    public string ObjectTypeName => SelectedObject?.GetType().Name ?? string.Empty;

    static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PropertyGridControl)d;
        control.OnPropertyChanged(nameof(ObjectName));
        control.OnPropertyChanged(nameof(ObjectTypeName));
        control.RefreshMembers(e.NewValue);
    }

    static void OnFilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PropertyGridControl)d;
        control.SyncToolbarState();
        control.ApplyFilters();
    }

    static void OnDescriptionPaneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PropertyGridControl)d).SyncToolbarState();
    }

    static void OnNameColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PropertyGridControl)d).HandleNameColumnWidthChanged((double)e.NewValue);
    }

    static void OnPropertyGridThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PropertyGridControl)d;
        control.RequestedTheme = (ElementTheme)e.NewValue;
        control.RootControl.RequestedTheme = (ElementTheme)e.NewValue;
        control.ApplyThemeBrushes();
        control.MarkAllRowsDirty();
        control.ShowActiveRows();
    }

    public void Refresh()
    {
        RefreshMembers(SelectedObject);
    }

    void HandleNameColumnWidthChanged(double newWidth)
    {
        var clampedWidth = ClampNameColumnWidth(newWidth);
        if (Math.Abs(clampedWidth - newWidth) > 0.1)
        {
            _applyingNameColumnWidth = true;
            NameColumnWidth = clampedWidth;
            _applyingNameColumnWidth = false;
            return;
        }

        if (!_syncingNameColumnFromSplitter)
            ApplyNameColumnWidth(clampedWidth);

        MarkAllRowsDirty();
        ShowActiveRows();
    }

    void RefreshMembers(object? selectedObject)
    {
        _properties.Clear();
        _flatProperties.Clear();
        _categories.Clear();
        _events.Clear();
        _visibleEvents.Clear();
        MarkAllRowsDirty();

        if (selectedObject == null)
        {
            ApplyFilters();
            return;
        }

        foreach (var property in _propertyProvider.GetProperties(selectedObject))
            _properties.Add(new PropertyGridPropertyViewModel(property));

        foreach (var @event in _eventProvider.GetEvents(selectedObject))
        {
            var viewModel = new PropertyGridEventViewModel(@event);
            viewModel.HandlerName = EventService?.GetHandlerName(selectedObject, @event.EventInfo) ?? string.Empty;
            _events.Add(viewModel);
        }

        ApplyFilters();
    }

    void ApplyFilters()
    {
        if (CategorizedRowsPanel is null)
            return;

        IEnumerable<PropertyGridPropertyViewModel> propertyQuery = _properties;

        if (!ShowReadOnlyProperties)
            propertyQuery = propertyQuery.Where(p => !p.IsReadOnly);

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            propertyQuery = propertyQuery.Where(p => MatchesProperty(p, _searchText));
        }

        _flatProperties.Clear();
        _categories.Clear();
        _visibleEvents.Clear();

        if (SortMode == PropertyGridSortMode.Categorized)
        {
            foreach (var group in propertyQuery.GroupBy(p => p.Category, StringComparer.CurrentCultureIgnoreCase).OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase))
            {
                var category = new PropertyGridCategoryViewModel(group.Key);
                category.IsExpanded = ShouldExpandCategory(category.Name);
                category.PropertyChanged += OnCategoryPropertyChanged;
                foreach (var property in group.OrderBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                    category.Rows.Add(property);
                _categories.Add(category);
            }
        }
        else
        {
            foreach (var property in propertyQuery.OrderBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                _flatProperties.Add(property);
        }

        IEnumerable<PropertyGridEventViewModel> eventQuery = _events;
        if (!string.IsNullOrWhiteSpace(_searchText))
            eventQuery = eventQuery.Where(e => MatchesEvent(e, _searchText));

        foreach (var @event in eventQuery.OrderBy(e => e.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            _visibleEvents.Add(@event);

        SyncToolbarState();
        MarkAllRowsDirty();
        ShowActiveRows();
    }

    bool ShouldExpandCategory(string name)
    {
        if (!string.IsNullOrWhiteSpace(_searchText))
            return true;

        var key = GetCategoryExpansionKey(name);
        return !_categoryExpansion.TryGetValue(key, out var isExpanded) || isExpanded;
    }

    void OnCategoryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PropertyGridCategoryViewModel category || e.PropertyName != nameof(PropertyGridCategoryViewModel.IsExpanded))
            return;

        _categoryExpansion[GetCategoryExpansionKey(category.Name)] = category.IsExpanded;
    }

    string GetCategoryExpansionKey(string category)
    {
        return $"{SelectedObject?.GetType().FullName ?? string.Empty}|{ViewMode}|{category}";
    }

    static bool MatchesProperty(PropertyGridPropertyViewModel property, string searchText)
    {
        return Contains(property.DisplayName, searchText)
            || Contains(property.Name, searchText)
            || Contains(property.Category, searchText)
            || Contains(property.Description, searchText);
    }

    static bool MatchesEvent(PropertyGridEventViewModel @event, string searchText)
    {
        return Contains(@event.DisplayName, searchText)
            || Contains(@event.Name, searchText)
            || Contains(@event.HandlerName, searchText)
            || Contains(@event.HandlerTypeName, searchText);
    }

    static bool Contains(string value, string searchText)
    {
        return value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
    }

    void SyncToolbarState()
    {
        if (PropertiesButton is null || EventsButton is null)
            return;

        PropertiesButton.IsChecked = ViewMode == PropertyGridViewMode.Properties;
        EventsButton.IsChecked = ViewMode == PropertyGridViewMode.Events;

        if (ArrangeByComboBox is not null)
            ArrangeByComboBox.SelectedIndex = SortMode == PropertyGridSortMode.Alphabetical ? 1 : 0;

        ArrangeByPanel.Visibility = ViewMode == PropertyGridViewMode.Properties ? Visibility.Visible : Visibility.Collapsed;
        SearchBox.PlaceholderText = ViewMode == PropertyGridViewMode.Events ? "Search events" : "Search properties";

        if (DescriptionPane is not null)
            DescriptionPane.Visibility = ShowDescriptionPane ? Visibility.Visible : Visibility.Collapsed;
    }

    void MarkAllRowsDirty()
    {
        _categorizedRowsDirty = true;
        _flatRowsDirty = true;
        _eventRowsDirty = true;
    }

    void ShowActiveRows()
    {
        if (CategorizedRowsPanel is null)
            return;

        ApplyThemeBrushes();

        var showEvents = ViewMode == PropertyGridViewMode.Events;
        var showCategorized = ViewMode == PropertyGridViewMode.Properties && SortMode == PropertyGridSortMode.Categorized;
        var showFlat = ViewMode == PropertyGridViewMode.Properties && SortMode != PropertyGridSortMode.Categorized;

        if (showCategorized && _categorizedRowsDirty)
            BuildCategorizedRows();
        if (showFlat && _flatRowsDirty)
            BuildFlatRows();
        if (showEvents && _eventRowsDirty)
            BuildEventRows();

        CategorizedRowsPanel.Visibility = showCategorized ? Visibility.Visible : Visibility.Collapsed;
        FlatRowsPanel.Visibility = showFlat ? Visibility.Visible : Visibility.Collapsed;
        EventRowsPanel.Visibility = showEvents ? Visibility.Visible : Visibility.Collapsed;

        NotifyEditorsThemeChanged();
    }

    void BuildCategorizedRows()
    {
        _editorThemeCallbacks.Clear();
        ClearRowSelection();
        CategorizedRowsPanel.Children.Clear();
        for (var i = 0; i < _categories.Count; i++)
            CategorizedRowsPanel.Children.Add(CreateCategoryHeader(_categories[i], i == 0));
        _categorizedRowsDirty = false;
    }

    void BuildFlatRows()
    {
        _editorThemeCallbacks.Clear();
        ClearRowSelection();
        FlatRowsPanel.Children.Clear();
        foreach (var property in _flatProperties)
            FlatRowsPanel.Children.Add(CreatePropertyRow(property));
        _flatRowsDirty = false;
    }

    void BuildEventRows()
    {
        ClearRowSelection();
        EventRowsPanel.Children.Clear();
        foreach (var @event in _visibleEvents)
            EventRowsPanel.Children.Add(CreateEventRow(@event));
        _eventRowsDirty = false;
    }

    FrameworkElement CreateCategoryHeader(PropertyGridCategoryViewModel category, bool drawTopSeparator)
    {
        var container = new StackPanel();
        var childrenPanel = new StackPanel
        {
            // Top gap so the band→first-row spacing matches the uniform row gap; each row
            // contributes its own bottom gap, so band→box, box→box and box→section are equal.
            Margin = new Thickness(0, RowGap, 0, 0),
            Visibility = category.IsExpanded ? Visibility.Visible : Visibility.Collapsed
        };

        container.Children.Add(CreateCategoryToggle(category, childrenPanel));

        foreach (var property in category.Rows)
            childrenPanel.Children.Add(CreatePropertyRow(property, drawSeparator: false));

        container.Children.Add(childrenPanel);
        return new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = drawTopSeparator
                ? new Thickness(0, 1, 0, 1)
                : new Thickness(0, 0, 0, 1),
            Child = container
        };
    }

    Button CreateCategoryToggle(PropertyGridCategoryViewModel category, StackPanel childrenPanel)
    {
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = _categoryBrush,
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0, 3, 0, 3),
            MinHeight = 26,
            Content = CreateCategoryHeaderContent(category),
            Foreground = _foregroundBrush
        };
        button.Resources["ButtonBackgroundPointerOver"] = _categoryBrush;
        button.Resources["ButtonBackgroundPressed"] = _categoryBrush;
        button.Resources["ButtonBackgroundDisabled"] = _categoryBrush;
        button.Resources["ButtonForegroundPointerOver"] = _foregroundBrush;
        button.Resources["ButtonForegroundPressed"] = _foregroundBrush;
        button.Resources["ButtonForegroundDisabled"] = _foregroundBrush;
        button.Resources["ButtonBorderBrushPointerOver"] = _borderBrush;
        button.Resources["ButtonBorderBrushPressed"] = _borderBrush;
        button.Resources["ButtonBorderBrushDisabled"] = _borderBrush;

        button.Click += (_, _) => SetCategoryExpanded(category, !category.IsExpanded, button, childrenPanel);
        return button;
    }

    Grid CreateCategoryHeaderContent(PropertyGridCategoryViewModel category)
    {
        var grid = new Grid { ColumnSpacing = 4 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var glyph = new FontIcon
        {
            Glyph = category.ExpandGlyph,
            FontSize = 10,
            Foreground = _foregroundBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(glyph);

        var text = new TextBlock
        {
            Text = category.Name,
            Foreground = _foregroundBrush,
            FontSize = 12,
            FontWeight = new FontWeight { Weight = 400 },
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return grid;
    }

    void SetCategoryExpanded(PropertyGridCategoryViewModel category, bool isExpanded, Button button, StackPanel childrenPanel)
    {
        if (category.IsExpanded == isExpanded)
            return;

        category.IsExpanded = isExpanded;
        _categoryExpansion[GetCategoryExpansionKey(category.Name)] = isExpanded;
        button.Content = CreateCategoryHeaderContent(category);
        button.Background = _categoryBrush;
        button.Foreground = _foregroundBrush;
        childrenPanel.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    FrameworkElement CreatePropertyRow(PropertyGridPropertyViewModel property, bool drawSeparator = true)
    {
        var row = CreateRowGrid();
        var nameCell = CreateNameCell(property.DisplayName);
        row.Children.Add(nameCell);

        var editorBorder = CreateCellBorder(1);
        editorBorder.Padding = new Thickness(4, 0, 4, 0);
        var editor = CreateEditor(property);
        editorBorder.Child = editor;
        row.Children.Add(editorBorder);

        row.Children.Add(CreateIndicatorCell(2, property));
        var outer = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = drawSeparator ? new Thickness(0, 0, 0, 1) : new Thickness(0),
            Background = _backgroundBrush,
            // A single bottom gap per row. Combined with the row container's top gap this
            // makes the spacing uniform: band→box, box→box, and box→section are all RowGap,
            // instead of box→box being doubled by two adjacent symmetric paddings.
            Margin = drawSeparator ? new Thickness(0) : new Thickness(0, 0, 0, RowGap),
            Child = row
        };
        // VS-style selection: clicking anywhere on the row highlights it and shows the
        // property description. A plain-text value editor is recolored so it stays legible
        // on the blue fill; editors with their own background are left as-is.
        var nameText = nameCell.Child as TextBlock;
        var valueText = editor as TextBlock;
        outer.Tapped += (_, _) => SelectRow(property.DisplayName, property.Description, row, nameText, valueText);
        return outer;
    }

    void SelectRow(string title, string description, Grid row, TextBlock? nameText, TextBlock? valueText)
    {
        // Restore the previously selected row to its normal palette.
        if (_selectedRowGrid is not null)
            _selectedRowGrid.Background = _backgroundBrush;
        if (_selectedNameText is not null)
            _selectedNameText.Foreground = _foregroundBrush;
        if (_selectedValueText is not null)
            _selectedValueText.Foreground = _foregroundBrush;

        // Apply the VS blue fill + white text to the newly selected row.
        row.Background = _selectionBrush;
        if (nameText is not null)
            nameText.Foreground = _selectionForegroundBrush;
        if (valueText is not null)
            valueText.Foreground = _selectionForegroundBrush;

        _selectedRowGrid = row;
        _selectedNameText = nameText;
        _selectedValueText = valueText;

        UpdateDescription(title, description);
    }

    void ClearRowSelection()
    {
        // Rows are about to be rebuilt; drop references so we never touch detached elements.
        _selectedRowGrid = null;
        _selectedNameText = null;
        _selectedValueText = null;
        UpdateDescription(string.Empty, string.Empty);
    }

    void UpdateDescription(string title, string description)
    {
        if (DescriptionTitle is null || DescriptionText is null)
            return;
        DescriptionTitle.Text = title;
        DescriptionText.Text = description;
    }

    FrameworkElement CreateEditor(PropertyGridPropertyViewModel property)
    {
        var context = CreateEditorContext(property);

        var metadataProvider = PropertyEditorFactory.CreateEditorProvider(property.Descriptor.Attributes);
        if (metadataProvider != null && metadataProvider.CanEdit(context))
            return metadataProvider.CreateEditor(context);

        foreach (var provider in EditorProviders)
        {
            if (!provider.CanEdit(context))
                continue;

            return provider.CreateEditor(context);
        }

        return _builtInEditorProvider.CreateEditor(context);
    }

    PropertyGridEditorContext CreateEditorContext(PropertyGridPropertyViewModel property)
    {
        var context = new PropertyGridEditorContext
        {
            Component = property.Descriptor.Component,
            Descriptor = property.Descriptor,
            Value = property.Value,
            BindingMode = property.IsReadOnly ? Microsoft.UI.Xaml.Data.BindingMode.OneWay : Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
            Services = null,
            SetValue = value =>
            {
                property.Value = value;
            }
        };
        _editorThemeCallbacks.Add(t => context.RaiseThemeChanged(t));

        property.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PropertyGridPropertyViewModel.Value))
                context.Value = property.Value;
        };
        return context;
    }

    FrameworkElement CreateEventRow(PropertyGridEventViewModel @event)
    {
        // Mirror the property row layout: name / value* / trailing indicator-width column so
        // the handler box right edge aligns with property editors. The trailing column stays
        // empty (events have no override indicator).
        var row = CreateRowGrid();
        var nameCell = CreateNameCell(@event.DisplayName);
        row.Children.Add(nameCell);

        var textBox = new TextBox
        {
            Text = @event.HandlerName,
            FontSize = 12,
            Padding = new Thickness(4, 1, 4, 1),
            MinHeight = 22,
            CornerRadius = new CornerRadius(0),
            BorderThickness = new Thickness(1),
            Foreground = _foregroundBrush,
            Background = _cellBrush,
            BorderBrush = _borderBrush,
            PlaceholderForeground = _mutedForegroundBrush
        };
        ApplyTextControlResources(textBox);
        // The template applies after the row is in the tree, so the placeholder color must be
        // re-applied on Loaded (the call inside ApplyTextControlResources finds nothing yet).
        textBox.Loaded += (_, _) => SetPlaceholderForeground(textBox, _mutedForegroundBrush);
        textBox.TextChanged += (_, _) => @event.HandlerName = textBox.Text;

        var editorBorder = CreateCellBorder(1);
        editorBorder.Padding = new Thickness(4, 0, 4, 0);
        editorBorder.Child = textBox;
        row.Children.Add(editorBorder);

        var outer = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(0),
            Background = _backgroundBrush,
            // Same uniform per-row gap as property rows.
            Margin = new Thickness(0, 0, 0, RowGap),
            Child = row
        };
        var nameText = nameCell.Child as TextBlock;
        outer.Tapped += (_, _) => SelectRow(@event.DisplayName, @event.Description, row, nameText, null);
        return outer;
    }

    Grid CreateRowGrid(bool includeIndicatorColumn = true)
    {
        var row = new Grid
        {
            MinHeight = 22,
            Background = _backgroundBrush,
            ColumnSpacing = 0
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NameColumnWidth) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (includeIndicatorColumn)
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        return row;
    }

    Border CreateNameCell(string text)
    {
        var border = CreateCellBorder(0);
        border.Padding = new Thickness(12, 0, 4, 0);
        border.Child = new TextBlock
        {
            Text = text,
            Foreground = _foregroundBrush,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        ToolTipService.SetToolTip(border, text);
        return border;
    }

    void OnRowsHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var clampedWidth = ClampNameColumnWidth(NameColumnWidth);
        if (Math.Abs(clampedWidth - NameColumnWidth) > 0.1)
        {
            _applyingNameColumnWidth = true;
            NameColumnWidth = clampedWidth;
            _applyingNameColumnWidth = false;
        }
    }

    void OnRowsHostLayoutUpdated(object? sender, object e)
    {
        if (_applyingNameColumnWidth || RowsHostNameColumn is null)
            return;

        var actualWidth = ClampNameColumnWidth(RowsHostNameColumn.ActualWidth);
        if (Math.Abs(actualWidth - NameColumnWidth) <= 0.5)
            return;

        _syncingNameColumnFromSplitter = true;
        NameColumnWidth = actualWidth;
        _syncingNameColumnFromSplitter = false;
    }

    double ClampNameColumnWidth(double width)
    {
        if (RowsHost is null || RowsHost.ActualWidth <= 0)
            return Math.Max(MinNameColumnWidth, width);

        var maxWidth = Math.Max(MinNameColumnWidth, RowsHost.ActualWidth - MinValueColumnWidth - 20);
        return Math.Clamp(width, MinNameColumnWidth, maxWidth);
    }

    void ApplyNameColumnWidth(double width)
    {
        if (RowsHostNameColumn is null)
            return;

        _applyingNameColumnWidth = true;
        RowsHostNameColumn.Width = new GridLength(width);
        _applyingNameColumnWidth = false;
    }

    Border CreateIndicatorCell(int column, PropertyGridPropertyViewModel property)
    {
        var border = CreateCellBorder(column);
        border.Padding = new Thickness(4, 0, 8, 0);
        var indicator = new Rectangle
        {
            Width = 8,
            Height = 8,
            Stroke = _mutedForegroundBrush,
            StrokeThickness = 1,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        UpdateIndicator(indicator, property);
        property.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PropertyGridPropertyViewModel.IsDefaultValue))
                UpdateIndicator(indicator, property);
        };
        border.Child = indicator;
        return border;
    }

    void UpdateIndicator(Rectangle indicator, PropertyGridPropertyViewModel property)
    {
        indicator.Fill = property.IsDefaultValue
            ? new SolidColorBrush(Colors.Transparent)
            : _overrideIndicatorBrush;
    }

    Border CreateCellBorder(int column)
    {
        var border = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(0)
        };
        Grid.SetColumn(border, column);
        return border;
    }

    void ApplyThemeBrushes()
    {
        // Do not use RootControl.ActualTheme here. On Uno Platform, ActualTheme on child
        // elements reports the app/page-level theme rather than the nearest ancestor's
        // explicit RequestedTheme, so it returns Light even when RootControl.RequestedTheme
        // is Dark. ThemeResource lookups resolve correctly, but the ActualTheme API is
        // unreliable for mixed-theme subtrees on Uno.
        var theme = PropertyGridTheme == ElementTheme.Default
            ? Application.Current?.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark
            : PropertyGridTheme;

        if (theme == ElementTheme.Light)
        {
            _backgroundBrush = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243));
            _panelBrush = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243));
            _categoryBrush = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243));
            _cellBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            _borderBrush = new SolidColorBrush(Color.FromArgb(255, 208, 208, 208));
            _foregroundBrush = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
            _mutedForegroundBrush = new SolidColorBrush(Color.FromArgb(255, 95, 95, 95));
            _overrideIndicatorBrush = new SolidColorBrush(Color.FromArgb(255, 0, 122, 204));
        }
        else
        {
            _backgroundBrush = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
            _panelBrush = new SolidColorBrush(Color.FromArgb(255, 37, 37, 38));
            _categoryBrush = new SolidColorBrush(Color.FromArgb(255, 45, 45, 48));
            _cellBrush = new SolidColorBrush(Color.FromArgb(255, 37, 37, 38));
            _borderBrush = new SolidColorBrush(Color.FromArgb(255, 63, 64, 72));
            _foregroundBrush = new SolidColorBrush(Color.FromArgb(255, 212, 212, 212));
            _mutedForegroundBrush = new SolidColorBrush(Color.FromArgb(255, 138, 138, 138));
            _overrideIndicatorBrush = new SolidColorBrush(Color.FromArgb(255, 55, 148, 255));
        }

        if (RootControl is null)
            return;

        RootControl.Background = _backgroundBrush;
        HeaderPanel.Background = _panelBrush;
        ArrangeByPanel.Background = _panelBrush;
        ObjectGlyph.Foreground = _mutedForegroundBrush;
        ApplyToggleButtonTheme(PropertiesButton, theme);
        ApplyToggleButtonTheme(EventsButton, theme);
        NameLabel.Foreground = _foregroundBrush;
        TypeLabel.Foreground = _foregroundBrush;
        ObjectTypeTextBlock.Foreground = _foregroundBrush;
        SearchGlyph.Foreground = _mutedForegroundBrush;
        SearchBox.Foreground = _foregroundBrush;
        SearchBox.Background = _cellBrush;
        SearchBox.BorderBrush = _borderBrush;
        ApplyTextControlResources(SearchBox);
        ObjectNameBox.Foreground = _foregroundBrush;
        ObjectNameBox.Background = _cellBrush;
        ObjectNameBox.BorderBrush = _borderBrush;
        ApplyTextControlResources(ObjectNameBox);
        ArrangeByLabel.Foreground = _foregroundBrush;
        ArrangeByComboBox.Foreground = _foregroundBrush;
        ArrangeByComboBox.Background = _cellBrush;
        ArrangeByComboBox.BorderBrush = _borderBrush;
        ApplyComboBoxResources(ArrangeByComboBox);

        // The description pane's ThemeResource brushes resolve at app level and don't follow
        // a dark→light switch, so set them explicitly (only matters when ShowDescriptionPane).
        DescriptionPane.Background = _panelBrush;
        DescriptionPane.BorderBrush = _borderBrush;
        DescriptionTitle.Foreground = _foregroundBrush;
        DescriptionText.Foreground = _mutedForegroundBrush;
    }

    void ApplyToggleButtonTheme(ToggleButton button, ElementTheme theme)
    {
        var isDark = theme == ElementTheme.Dark;
        var fg = _foregroundBrush;
        var hover = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x2D, 0x2D, 0x30) : Color.FromArgb(255, 0xE8, 0xE8, 0xE8));
        // VS shows the selected segment with a blue outline, not a blue fill. Keep the
        // checked background the same as unchecked and signal selection via the border.
        var accent = new SolidColorBrush(Color.FromArgb(255, 0x00, 0x78, 0xD4));
        button.Background = _cellBrush;
        button.Foreground = fg;
        button.BorderBrush = _borderBrush;
        if (button.Content is FontIcon icon)
            icon.Foreground = fg;
        button.Resources["ToggleButtonForeground"] = fg;
        button.Resources["ToggleButtonForegroundDisabled"] = fg;
        button.Resources["ToggleButtonForegroundUnchecked"] = fg;
        button.Resources["ToggleButtonForegroundUncheckedPointerOver"] = fg;
        button.Resources["ToggleButtonForegroundUncheckedPressed"] = fg;
        button.Resources["ToggleButtonForegroundChecked"] = fg;
        button.Resources["ToggleButtonForegroundCheckedPointerOver"] = fg;
        button.Resources["ToggleButtonForegroundCheckedPressed"] = fg;
        button.Resources["ToggleButtonForegroundPointerOver"] = fg;
        button.Resources["ToggleButtonForegroundPressed"] = fg;
        button.Resources["ToggleButtonBackground"] = _cellBrush;
        button.Resources["ToggleButtonBackgroundDisabled"] = _cellBrush;
        button.Resources["ToggleButtonBackgroundUnchecked"] = _cellBrush;
        button.Resources["ToggleButtonBackgroundUncheckedPointerOver"] = hover;
        button.Resources["ToggleButtonBackgroundUncheckedPressed"] = hover;
        button.Resources["ToggleButtonBackgroundPointerOver"] = hover;
        button.Resources["ToggleButtonBackgroundPressed"] = hover;
        button.Resources["ToggleButtonBackgroundChecked"] = _cellBrush;
        button.Resources["ToggleButtonBackgroundCheckedPointerOver"] = hover;
        button.Resources["ToggleButtonBackgroundCheckedPressed"] = _cellBrush;
        button.Resources["ToggleButtonBorderBrush"] = _borderBrush;
        button.Resources["ToggleButtonBorderBrushDisabled"] = _borderBrush;
        button.Resources["ToggleButtonBorderBrushUnchecked"] = _borderBrush;
        button.Resources["ToggleButtonBorderBrushUncheckedPointerOver"] = _borderBrush;
        button.Resources["ToggleButtonBorderBrushUncheckedPressed"] = _borderBrush;
        button.Resources["ToggleButtonBorderBrushPointerOver"] = _borderBrush;
        button.Resources["ToggleButtonBorderBrushPressed"] = _borderBrush;
        button.Resources["ToggleButtonBorderBrushChecked"] = accent;
        button.Resources["ToggleButtonBorderBrushCheckedPointerOver"] = accent;
        button.Resources["ToggleButtonBorderBrushCheckedPressed"] = accent;
    }

    void NotifyEditorsThemeChanged()
    {
        var theme = PropertyGridTheme == ElementTheme.Default
            ? Application.Current?.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark
            : PropertyGridTheme;
        foreach (var cb in _editorThemeCallbacks)
            cb(theme);
    }

    void ApplyTextControlResources(Control control)
    {
        control.Resources["TextControlBackground"] = _backgroundBrush;
        control.Resources["TextControlBackgroundPointerOver"] = _panelBrush;
        control.Resources["TextControlBackgroundFocused"] = _backgroundBrush;
        control.Resources["TextControlForeground"] = _foregroundBrush;
        control.Resources["TextControlForegroundFocused"] = _foregroundBrush;
        control.Resources["TextControlPlaceholderForeground"] = _mutedForegroundBrush;
        control.Resources["TextControlBorderBrush"] = _borderBrush;
        control.Resources["TextControlBorderBrushPointerOver"] = _mutedForegroundBrush;
        control.Resources["TextControlBorderBrushFocused"] = _overrideIndicatorBrush;
        // On Uno, ThemeResource for placeholder text inside the TextBox template resolves at
        // app level rather than PropertyGrid scope. Walk the visual tree and set it directly.
        SetPlaceholderForeground(control, _mutedForegroundBrush);
    }

    static void SetPlaceholderForeground(DependencyObject root, Brush brush)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock tb && tb.Name == "PlaceholderTextContentPresenter")
                tb.Foreground = brush;
            else
                SetPlaceholderForeground(child, brush);
        }
    }

    void ApplyComboBoxResources(ComboBox comboBox)
    {
        comboBox.Resources["ComboBoxBackground"] = _backgroundBrush;
        comboBox.Resources["ComboBoxBackgroundPointerOver"] = _panelBrush;
        comboBox.Resources["ComboBoxBackgroundPressed"] = _panelBrush;
        comboBox.Resources["ComboBoxForeground"] = _foregroundBrush;
        comboBox.Resources["ComboBoxForegroundPointerOver"] = _foregroundBrush;
        comboBox.Resources["ComboBoxForegroundPressed"] = _foregroundBrush;
        comboBox.Resources["ComboBoxBorderBrush"] = _borderBrush;
        comboBox.Resources["ComboBoxBorderBrushPointerOver"] = _mutedForegroundBrush;
        comboBox.Resources["ComboBoxBorderBrushPressed"] = _overrideIndicatorBrush;
        comboBox.Resources["ComboBoxDropDownGlyphForeground"] = _foregroundBrush;
        comboBox.Resources["ComboBoxDropDownGlyphForegroundPointerOver"] = _foregroundBrush;
        comboBox.Resources["ComboBoxDropDownGlyphForegroundPressed"] = _foregroundBrush;
        comboBox.Resources["ComboBoxDropDownBackground"] = _cellBrush;
        comboBox.Resources["ComboBoxDropDownBorderBrush"] = _borderBrush;
        comboBox.Resources["ComboBoxDropDownForeground"] = _foregroundBrush;
        comboBox.Resources["ComboBoxItemBackground"] = _cellBrush;
        comboBox.Resources["ComboBoxItemBackgroundPointerOver"] = _panelBrush;
        comboBox.Resources["ComboBoxItemBackgroundPressed"] = _panelBrush;
        comboBox.Resources["ComboBoxItemBackgroundSelected"] = _panelBrush;
        comboBox.Resources["ComboBoxItemBackgroundSelectedPointerOver"] = _panelBrush;
        comboBox.Resources["ComboBoxItemForeground"] = _foregroundBrush;
        comboBox.Resources["ComboBoxItemForegroundPointerOver"] = _foregroundBrush;
        comboBox.Resources["ComboBoxItemForegroundSelected"] = _foregroundBrush;
    }

    void OnViewModeChecked(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, EventsButton))
            ViewMode = PropertyGridViewMode.Events;
        else if (ReferenceEquals(sender, PropertiesButton))
            ViewMode = PropertyGridViewMode.Properties;
    }

    void OnArrangeByChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ArrangeByComboBox is null)
            return;

        SortMode = ArrangeByComboBox.SelectedIndex == 1
            ? PropertyGridSortMode.Alphabetical
            : PropertyGridSortMode.Categorized;
    }

    void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text ?? string.Empty;
        ApplyFilters();
    }

    static string GetObjectName(object? selectedObject)
    {
        if (selectedObject == null)
            return string.Empty;

        string? name = null;
        try
        {
            var property = selectedObject.GetType().GetProperty("Name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            name = property?.GetValue(selectedObject)?.ToString();
        }
        catch
        {
            name = null;
        }
        return string.IsNullOrWhiteSpace(name) ? "<No Name>" : name;
    }

    void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
