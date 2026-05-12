using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.System;
using Windows.UI;

namespace UnoPropertyGrid.Sample.DesignTools;

sealed class CityMapEditorProvider : IPropertyGridEditorProvider
{
    const double MapWidth = 520;
    const double MapHeight = 254;
    const double EqualEarthLimit = 74d;
    const double EqualEarthTop = 15.5d;
    const double EqualEarthBottom = 238.5d;

    static readonly CityOption[] Cities =
    [
        new("Vancouver", -123.1207, 49.2827),
        new("New York", -74.0060, 40.7128),
        new("London", -0.1276, 51.5072),
        new("Tokyo", 139.6503, 35.6762)
    ];

    public bool CanEdit(PropertyGridEditorContext context)
    {
        return context.Descriptor.PropertyType == typeof(string)
            && context.Descriptor.Name.Contains("City", StringComparison.OrdinalIgnoreCase)
            && !context.Descriptor.IsReadOnly;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var panel = new Grid { ColumnSpacing = 6 };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var initial = context.Descriptor.GetValue() as string ?? string.Empty;
        var comboBox = new ComboBox
        {
            IsEditable = true,
            ItemsSource = Cities.Select(city => city.Name).ToArray(),
            Text = initial,
            PlaceholderText = "City",
            MinHeight = 30
        };
        Grid.SetColumn(comboBox, 1);
        panel.Children.Add(comboBox);

        var canvas = new Canvas
        {
            Width = MapWidth,
            Height = MapHeight,
            Background = new SolidColorBrush(Color.FromArgb(255, 225, 238, 247)),
        };

        var map = new Image
        {
            Width = MapWidth,
            Height = MapHeight,
            Stretch = Stretch.Fill,
            Source = new SvgImageSource(new Uri("ms-appx:///Assets/Svg/world_map.svg"))
        };
        canvas.Children.Add(map);

        var flyout = new Flyout { Content = canvas };
        var chooseButton = EditorChrome.CreatePickerButton("\uE707", "Choose city");
        chooseButton.Flyout = flyout;
        panel.Children.Add(chooseButton);

        foreach (var city in Cities)
        {
            var point = Project(city.Longitude, city.Latitude);
            var button = new Button
            {
                Width = 22,
                Height = 22,
                Padding = new Thickness(0),
                Content = "•",
                FontSize = 18,
                Tag = city
            };
            ToolTipService.SetToolTip(button, city.Name);
            Canvas.SetLeft(button, point.X - 11);
            Canvas.SetTop(button, point.Y - 11);
            button.Click += (_, _) =>
            {
                comboBox.Text = city.Name;
                comboBox.SelectedItem = city.Name;
                context.SetValue?.Invoke(city.Name);
                flyout.Hide();
            };
            canvas.Children.Add(button);
        }

        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is string city)
                context.SetValue?.Invoke(city);
        };
        comboBox.LostFocus += (_, _) => ApplyText();
        comboBox.KeyDown += (_, args) =>
        {
            if (args.Key == Windows.System.VirtualKey.Enter)
                ApplyText();
        };

        return panel;

        void ApplyText()
        {
            context.SetValue?.Invoke(comboBox.Text.Trim());
        }
    }

    static Windows.Foundation.Point Project(double longitude, double latitude)
    {
        var projected = EqualEarthProject(longitude, Math.Max(-EqualEarthLimit, Math.Min(EqualEarthLimit, latitude)));
        var left = EqualEarthProject(-180, 0).X;
        var right = EqualEarthProject(180, 0).X;
        var top = EqualEarthProject(0, EqualEarthLimit).Y;
        var bottom = EqualEarthProject(0, -EqualEarthLimit).Y;

        var x = (projected.X - left) / (right - left) * MapWidth;
        var y = EqualEarthTop + (projected.Y - top) / (bottom - top) * (EqualEarthBottom - EqualEarthTop);
        return new Windows.Foundation.Point(x, y);
    }

    static Windows.Foundation.Point EqualEarthProject(double longitude, double latitude)
    {
        const double a1 = 1.340264;
        const double a2 = -0.081106;
        const double a3 = 0.000893;
        const double a4 = 0.003796;
        var m = Math.Sqrt(3) / 2d;

        var lambda = longitude * Math.PI / 180d;
        var phi = latitude * Math.PI / 180d;
        var theta = Math.Asin(m * Math.Sin(phi));
        var theta2 = theta * theta;
        var theta6 = theta2 * theta2 * theta2;
        var x = 2 * Math.Sqrt(3) * lambda * Math.Cos(theta)
            / (3 * (9 * a4 * theta6 + 7 * a3 * theta2 * theta2 + 3 * a2 * theta2 + a1));
        var y = a4 * theta * theta6 + a3 * theta * theta2 * theta2 + a2 * theta * theta2 + a1 * theta;
        return new Windows.Foundation.Point(x, -y);
    }

    static Windows.Foundation.Point EquirectangularProject(double longitude, double latitude)
    {
        var x = (longitude + 180d) / 360d * MapWidth;
        var y = (90d - latitude) / 180d * MapHeight;
        return new Windows.Foundation.Point(x, y);
    }

    sealed record CityOption(string Name, double Longitude, double Latitude);
}
