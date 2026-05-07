using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoPropertyGrid;

public sealed partial class PropertyGridControl : UserControl
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

    readonly ObservableCollection<PropertyGridPropertyViewModel> _properties = new();
    readonly ObservableCollection<PropertyGridPropertyViewModel> _visibleProperties = new();
    readonly IPropertyGridPropertyProvider _propertyProvider = new TypeDescriptorPropertyProvider();
    string _searchText = string.Empty;

    public PropertyGridControl()
    {
        InitializeComponent();
        PropertiesItemsControl.ItemsSource = _visibleProperties;
        CategoryComboBox.Items.Add("All");
        CategoryComboBox.SelectedIndex = 0;
    }

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

    static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PropertyGridControl)d).RefreshProperties(e.NewValue);
    }

    static void OnFilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PropertyGridControl)d).ApplyFilters();
    }

    public void Refresh()
    {
        RefreshProperties(SelectedObject);
    }

    void RefreshProperties(object? selectedObject)
    {
        _properties.Clear();
        _visibleProperties.Clear();

        if (selectedObject == null)
        {
            PopulateCategories();
            return;
        }

        foreach (var property in _propertyProvider.GetProperties(selectedObject))
        {
            _properties.Add(new PropertyGridPropertyViewModel(property));
        }

        PopulateCategories();
        ApplyFilters();
    }

    void PopulateCategories()
    {
        var selected = CategoryComboBox.SelectedItem as string;
        CategoryComboBox.Items.Clear();
        CategoryComboBox.Items.Add("All");

        foreach (var category in _properties.Select(p => p.Category).Distinct().OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase))
        {
            CategoryComboBox.Items.Add(category);
        }

        CategoryComboBox.SelectedItem = selected != null && CategoryComboBox.Items.Contains(selected) ? selected : "All";
    }

    void ApplyFilters()
    {
        var category = CategoryComboBox.SelectedItem as string;
        IEnumerable<PropertyGridPropertyViewModel> query = _properties;

        if (!ShowReadOnlyProperties)
            query = query.Where(p => !p.IsReadOnly);

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(p => string.Equals(p.Category, category, StringComparison.CurrentCultureIgnoreCase));

        if (!string.IsNullOrWhiteSpace(_searchText))
            query = query.Where(p => p.DisplayName.Contains(_searchText, StringComparison.CurrentCultureIgnoreCase)
                                  || p.Name.Contains(_searchText, StringComparison.CurrentCultureIgnoreCase));

        query = SortMode == PropertyGridSortMode.Categorized
            ? query.OrderBy(p => p.Category, StringComparer.CurrentCultureIgnoreCase).ThenBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            : query.OrderBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase);

        _visibleProperties.Clear();
        foreach (var property in query)
            _visibleProperties.Add(property);
    }

    void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text ?? string.Empty;
        ApplyFilters();
    }

    void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }
}
