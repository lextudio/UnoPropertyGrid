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
            MinHeight = 30,
            BorderThickness = new Thickness(0)
        };
        Grid.SetColumn(comboBox, 1);
        panel.Children.Add(comboBox);
        comboBox.DropDownOpened += (_, _) => ApplyComboBoxBackground(comboBox);
        comboBox.DropDownClosed += (_, _) => ApplyComboBoxBackground(comboBox);

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
        void ApplyTheme(ElementTheme? t = null)
        {
            var theme = t ?? EditorChrome.GetEffectiveTheme(comboBox);
            ApplyComboBoxTheme(comboBox, theme);
            ApplyMapTheme(canvas, map, theme);
        }
        comboBox.Loaded += (_, _) => ApplyTheme();
        comboBox.ActualThemeChanged += (_, _) => ApplyTheme();
        context.ThemeChanged += t => ApplyTheme(t);

        var flyout = new Flyout { Content = canvas };
        var chooseButton = EditorChrome.CreatePickerButton("\uE707", "Choose city", context);
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
            button.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Color.FromArgb(255, 0x00, 0x78, 0xD4));
            button.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Color.FromArgb(255, 0x00, 0x66, 0xB4));
            button.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Colors.Transparent);
            button.Resources["ButtonBorderBrushPressed"] = new SolidColorBrush(Colors.Transparent);
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
            ApplyComboBoxBackground(comboBox);
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

    static void ApplyMapTheme(Canvas canvas, Image map, ElementTheme theme)
    {
        var isDark = theme == ElementTheme.Dark;
        canvas.Background = new SolidColorBrush(isDark
            ? Color.FromArgb(255, 0x1A, 0x27, 0x44)
            : Color.FromArgb(255, 225, 238, 247));
        map.Source = new SvgImageSource(new Uri(isDark
            ? "ms-appx:///Assets/Svg/world_map_dark.svg"
            : "ms-appx:///Assets/Svg/world_map.svg"));
    }

    static void ApplyComboBoxTheme(ComboBox comboBox, ElementTheme? theme = null)
    {
        var isDark = (theme ?? EditorChrome.GetEffectiveTheme(comboBox)) == ElementTheme.Dark;
        var fg = new SolidColorBrush(isDark ? Color.FromArgb(255, 0xD4, 0xD4, 0xD4) : Color.FromArgb(255, 0x1E, 0x1E, 0x1E));
        var bg = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x25, 0x25, 0x26) : Color.FromArgb(255, 0xF3, 0xF3, 0xF3));
        var hover = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x2D, 0x2D, 0x30) : Color.FromArgb(255, 0xE8, 0xE8, 0xE8));
        var border = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x3F, 0x3F, 0x46) : Color.FromArgb(255, 0xCC, 0xCC, 0xCC));
        var muted = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x6B, 0x6B, 0x6B) : Color.FromArgb(255, 0x5F, 0x5F, 0x5F));
        comboBox.Background = bg;
        comboBox.Foreground = fg;
        comboBox.BorderBrush = border;
        comboBox.Resources["ComboBoxBackground"] = bg;
        comboBox.Resources["ComboBoxBackgroundPointerOver"] = hover;
        comboBox.Resources["ComboBoxBackgroundPressed"] = hover;
        comboBox.Resources["ComboBoxBackgroundFocused"] = bg;
        comboBox.Resources["ComboBoxBackgroundFocusedPointerOver"] = hover;
        comboBox.Resources["ComboBoxBackgroundFocusedPressed"] = hover;
        comboBox.Resources["ComboBoxBackgroundOpen"] = bg;
        comboBox.Resources["ComboBoxBackgroundOpenPointerOver"] = hover;
        comboBox.Resources["ComboBoxBackgroundOpenPressed"] = hover;
        comboBox.Resources["ComboBoxForeground"] = fg;
        comboBox.Resources["ComboBoxForegroundPointerOver"] = fg;
        comboBox.Resources["ComboBoxForegroundPressed"] = fg;
        comboBox.Resources["ComboBoxForegroundFocused"] = fg;
        comboBox.Resources["ComboBoxForegroundFocusedPointerOver"] = fg;
        comboBox.Resources["ComboBoxForegroundOpen"] = fg;
        comboBox.Resources["ComboBoxBorderBrush"] = border;
        comboBox.Resources["ComboBoxBorderBrushPointerOver"] = border;
        comboBox.Resources["ComboBoxBorderBrushPressed"] = border;
        comboBox.Resources["ComboBoxBorderBrushFocused"] = border;
        comboBox.Resources["ComboBoxBorderBrushFocusedPointerOver"] = border;
        comboBox.Resources["ComboBoxBorderBrushOpen"] = border;
        comboBox.Resources["ComboBoxDropDownBackground"] = bg;
        comboBox.Resources["ComboBoxDropDownBorderBrush"] = border;
        comboBox.Resources["ComboBoxDropDownForeground"] = fg;
        comboBox.Resources["ComboBoxItemBackground"] = bg;
        comboBox.Resources["ComboBoxItemBackgroundPointerOver"] = hover;
        comboBox.Resources["ComboBoxItemBackgroundPressed"] = hover;
        comboBox.Resources["ComboBoxItemBackgroundSelected"] = hover;
        comboBox.Resources["ComboBoxItemBackgroundSelectedPointerOver"] = hover;
        comboBox.Resources["ComboBoxItemForeground"] = fg;
        comboBox.Resources["ComboBoxItemForegroundPointerOver"] = fg;
        comboBox.Resources["ComboBoxItemForegroundSelected"] = fg;
        comboBox.Resources["ComboBoxDropDownGlyphForeground"] = fg;
        comboBox.Resources["ComboBoxDropDownGlyphForegroundPointerOver"] = fg;
        comboBox.Resources["ComboBoxDropDownGlyphForegroundPressed"] = fg;
        comboBox.Resources["ComboBoxDropDownGlyphForegroundFocused"] = fg;
        comboBox.Resources["ComboBoxDropDownGlyphForegroundFocusedPointerOver"] = fg;
        comboBox.Resources["ComboBoxDropDownGlyphForegroundOpen"] = fg;
        comboBox.Resources["TextControlBackground"] = bg;
        comboBox.Resources["TextControlBackgroundPointerOver"] = bg;
        comboBox.Resources["TextControlBackgroundFocused"] = bg;
        comboBox.Resources["TextControlForeground"] = fg;
        comboBox.Resources["TextControlForegroundFocused"] = fg;
        comboBox.Resources["TextControlPlaceholderForeground"] = muted;
        SetPlaceholderForeground(comboBox, muted);
    }

    static void ApplyComboBoxBackground(ComboBox comboBox)
    {
        var isDark = EditorChrome.GetEffectiveTheme(comboBox) == ElementTheme.Dark;
        var bg = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x25, 0x25, 0x26) : Color.FromArgb(255, 0xF3, 0xF3, 0xF3));
        PatchBackground(comboBox, bg);
    }

    static void PatchBackground(DependencyObject root, Brush brush)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Border b && b.Name == "BackgroundElement")
                b.Background = brush;
            else
                PatchBackground(child, brush);
        }
    }

    static void SetPlaceholderForeground(DependencyObject root, Brush brush)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock tb && (tb.Name == "PlaceholderTextContentPresenter" || tb.Name == "PlaceholderTextBlock"))
                tb.Foreground = brush;
            else
                SetPlaceholderForeground(child, brush);
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
