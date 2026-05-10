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

namespace UnoPropertyGrid;

public sealed partial class PropertyGridControl : UserControl, INotifyPropertyChanged
{
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
            new PropertyMetadata(180d));

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
    readonly IPropertyGridPropertyProvider _propertyProvider = new TypeDescriptorPropertyProvider();
    readonly IPropertyGridEventProvider _eventProvider = new ReflectionEventProvider();
    readonly Dictionary<string, bool> _categoryExpansion = new(StringComparer.Ordinal);
    bool _categorizedRowsDirty = true;
    bool _flatRowsDirty = true;
    bool _eventRowsDirty = true;
    SolidColorBrush _backgroundBrush = new(Colors.White);
    SolidColorBrush _panelBrush = new(Colors.White);
    SolidColorBrush _categoryBrush = new(Colors.White);
    SolidColorBrush _borderBrush = new(Colors.LightGray);
    SolidColorBrush _foregroundBrush = new(Colors.Black);
    SolidColorBrush _mutedForegroundBrush = new(Colors.Gray);
    SolidColorBrush _overrideIndicatorBrush = new(Colors.Black);
    string _searchText = string.Empty;

    public PropertyGridControl()
    {
        InitializeComponent();
        ApplyThemeBrushes();
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
    }

    void BuildCategorizedRows()
    {
        CategorizedRowsPanel.Children.Clear();
        foreach (var category in _categories)
            CategorizedRowsPanel.Children.Add(CreateCategoryHeader(category));
        _categorizedRowsDirty = false;
    }

    void BuildFlatRows()
    {
        FlatRowsPanel.Children.Clear();
        foreach (var property in _flatProperties)
            FlatRowsPanel.Children.Add(CreatePropertyRow(property));
        _flatRowsDirty = false;
    }

    void BuildEventRows()
    {
        EventRowsPanel.Children.Clear();
        foreach (var @event in _visibleEvents)
            EventRowsPanel.Children.Add(CreateEventRow(@event));
        _eventRowsDirty = false;
    }

    FrameworkElement CreateCategoryHeader(PropertyGridCategoryViewModel category)
    {
        var container = new StackPanel();
        var childrenPanel = new StackPanel
        {
            Visibility = category.IsExpanded ? Visibility.Visible : Visibility.Collapsed
        };

        container.Children.Add(CreateCategoryToggle(category, childrenPanel));

        foreach (var property in category.Rows)
            childrenPanel.Children.Add(CreatePropertyRow(property));

        container.Children.Add(childrenPanel);
        return container;
    }

    Button CreateCategoryToggle(PropertyGridCategoryViewModel category, StackPanel childrenPanel)
    {
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = _categoryBrush,
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0),
            MinHeight = 24,
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
            FontWeight = new FontWeight { Weight = 600 },
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

    FrameworkElement CreatePropertyRow(PropertyGridPropertyViewModel property)
    {
        var row = CreateRowGrid();
        row.Children.Add(CreateNameCell(property.DisplayName));

        var editorBorder = CreateCellBorder(1);
        editorBorder.Padding = new Thickness(4, 1, 4, 1);
        editorBorder.Child = new PropertyEditorControl { ViewModel = property };
        row.Children.Add(editorBorder);

        row.Children.Add(CreateIndicatorCell(2, property));
        var outer = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = _backgroundBrush,
            Child = row
        };
        return outer;
    }

    FrameworkElement CreateEventRow(PropertyGridEventViewModel @event)
    {
        var row = CreateRowGrid(includeIndicatorColumn: false);
        row.Children.Add(CreateNameCell(@event.DisplayName));

        var textBox = new TextBox
        {
            Text = @event.HandlerName,
            FontSize = 12,
            Padding = new Thickness(4, 2, 4, 2),
            MinHeight = 24
        };
        textBox.TextChanged += (_, _) => @event.HandlerName = textBox.Text;
        Grid.SetColumn(textBox, 1);
        row.Children.Add(textBox);
        var outer = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = _backgroundBrush,
            Child = row
        };
        return outer;
    }

    Grid CreateRowGrid(bool includeIndicatorColumn = true)
    {
        var row = new Grid
        {
            MinHeight = 24,
            Background = _backgroundBrush,
            ColumnSpacing = 0
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NameColumnWidth) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (includeIndicatorColumn)
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        return row;
    }

    Border CreateNameCell(string text)
    {
        var border = CreateCellBorder(0);
        border.Padding = new Thickness(20, 2, 4, 2);
        border.Child = new TextBlock
        {
            Text = text,
            Foreground = _foregroundBrush,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        return border;
    }

    Border CreateIndicatorCell(int column, PropertyGridPropertyViewModel property)
    {
        var border = CreateCellBorder(column);
        border.Padding = new Thickness(3, 0, 3, 0);
        var indicator = new Rectangle
        {
            Width = 6,
            Height = 6,
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
        var theme = PropertyGridTheme == ElementTheme.Default
            ? Application.Current?.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark
            : PropertyGridTheme;

        if (theme == ElementTheme.Light)
        {
            _backgroundBrush = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243));
            _panelBrush = new SolidColorBrush(Colors.White);
            _categoryBrush = new SolidColorBrush(Color.FromArgb(255, 232, 232, 232));
            _borderBrush = new SolidColorBrush(Color.FromArgb(255, 208, 208, 208));
            _foregroundBrush = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
            _mutedForegroundBrush = new SolidColorBrush(Color.FromArgb(255, 95, 95, 95));
            _overrideIndicatorBrush = new SolidColorBrush(Color.FromArgb(255, 0, 122, 204));
        }
        else
        {
            _backgroundBrush = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
            _panelBrush = new SolidColorBrush(Color.FromArgb(255, 37, 37, 38));
            _categoryBrush = new SolidColorBrush(Color.FromArgb(255, 37, 37, 38));
            _borderBrush = new SolidColorBrush(Color.FromArgb(255, 63, 63, 70));
            _foregroundBrush = new SolidColorBrush(Color.FromArgb(255, 241, 241, 241));
            _mutedForegroundBrush = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            _overrideIndicatorBrush = new SolidColorBrush(Color.FromArgb(255, 0, 122, 204));
        }

        if (RootControl is null)
            return;

        RootControl.Background = _backgroundBrush;
        HeaderPanel.Background = _panelBrush;
        ArrangeByPanel.Background = _panelBrush;
        ObjectGlyph.Foreground = _mutedForegroundBrush;
        NameLabel.Foreground = _foregroundBrush;
        TypeLabel.Foreground = _foregroundBrush;
        ObjectTypeTextBlock.Foreground = _foregroundBrush;
        SearchGlyph.Foreground = _mutedForegroundBrush;
    }

    void OnViewModeChecked(object sender, RoutedEventArgs e)
    {
        if (sender == EventsButton)
            ViewMode = PropertyGridViewMode.Events;
        else if (sender == PropertiesButton)
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
