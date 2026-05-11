using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoPropertyGrid.Sample;

public sealed partial class MainPage : Page
{
    readonly ExperienceSettings _settings = new();
    readonly Border _border = new()
    {
        Padding = new Thickness(18, 12, 18, 12),
        CornerRadius = new CornerRadius(8),
        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Goldenrod),
        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkGoldenrod),
        BorderThickness = new Thickness(2)
    };
    readonly TextBlock _textBlock = new()
    {
        Text = "Editable TextBlock",
        FontSize = 24,
        TextWrapping = TextWrapping.Wrap
    };
    readonly FontIcon _fontIcon = new()
    {
        Glyph = "\uE790",
        FontSize = 32,
        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
    };
    readonly ColumnDefinition _columnDefinition = new()
    {
        Width = new GridLength(2, GridUnitType.Star)
    };

    public MainPage()
    {
        InitializeComponent();

        PropertyGrid.EditorProviders.Add(new DateEditorProvider());
        PropertyGrid.EditorProviders.Add(new TimeEditorProvider());
        PropertyGrid.EditorProviders.Add(new VolumeEditorProvider());
        PropertyGrid.EditorProviders.Add(new CityMapEditorProvider());

        ObjectSelector.ItemsSource = new[]
        {
            new SampleObject("Scenario settings", _settings),
            new SampleObject("Border", _border),
            new SampleObject("TextBlock", _textBlock),
            new SampleObject("FontIcon", _fontIcon),
            new SampleObject("ColumnDefinition", _columnDefinition)
        };
        ObjectSelector.SelectedIndex = 0;
    }

    void ObjectSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ObjectSelector.SelectedItem is SampleObject item)
            PropertyGrid.SelectedObject = item.Value;
    }

    sealed record SampleObject(string Name, object Value)
    {
        public override string ToString() => Name;
    }
}
