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
    const double FlyoutWidth = 420;
    const double FlyoutHeight = 210;
    const double MarkerXOffset = -15;

    static readonly CityOption[] Cities =
    [
        new("Vancouver", -123.1207, 49.2827),
        new("Toronto", -79.3832, 43.6532),
        new("New York", -74.0060, 40.7128),
        new("London", -0.1276, 51.5072),
        new("Paris", 2.3522, 48.8566),
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
            MinHeight = EditorChrome.RowControlHeight,
            BorderThickness = new Thickness(0)
        };
        Grid.SetColumn(comboBox, 1);
        panel.Children.Add(comboBox);
        comboBox.DropDownOpened += (_, _) => ApplyComboBoxBackground(comboBox);
        comboBox.DropDownClosed += (_, _) => ApplyComboBoxBackground(comboBox);

        var mapCanvas = new Canvas
        {
            Width = MapWidth,
            Height = MapHeight,
            Background = new SolidColorBrush(Color.FromArgb(255, 225, 238, 247)),
        };
        var markerCanvas = new Canvas
        {
            Width = MapWidth,
            Height = MapHeight,
            Background = new SolidColorBrush(Colors.Transparent)
        };

        var map = new Image
        {
            Width = MapWidth,
            Height = MapHeight,
            Stretch = Stretch.Fill,
            Source = new SvgImageSource(new Uri("ms-appx:///Assets/Svg/world_map.svg"))
        };
        mapCanvas.Children.Add(map);
        void ApplyTheme(ElementTheme? t = null)
        {
            var theme = t ?? EditorChrome.GetEffectiveTheme(comboBox);
            ApplyComboBoxTheme(comboBox, theme);
            ApplyMapTheme(mapCanvas, map, theme);
        }
        comboBox.Loaded += (_, _) => ApplyTheme();
        comboBox.ActualThemeChanged += (_, _) => ApplyTheme();
        context.ThemeChanged += t => ApplyTheme(t);

        var zoomHost = new Viewbox
        {
            Stretch = Stretch.Fill,
            Width = MapWidth,
            Height = MapHeight,
            Child = mapCanvas
        };
        var contentGrid = new Grid
        {
            Width = MapWidth,
            Height = MapHeight
        };
        contentGrid.Children.Add(zoomHost);
        contentGrid.Children.Add(markerCanvas);

        var viewport = new ScrollViewer
        {
            Width = FlyoutWidth,
            Height = FlyoutHeight,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Enabled,
            VerticalScrollMode = ScrollMode.Enabled,
            ZoomMode = ZoomMode.Disabled,
            Content = contentGrid
        };
        markerCanvas.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
        var isPanning = false;
        Windows.Foundation.Point panStart = default;
        double startH = 0;
        double startV = 0;
        markerCanvas.PointerPressed += (_, args) =>
        {
            isPanning = true;
            panStart = args.GetCurrentPoint(viewport).Position;
            startH = viewport.HorizontalOffset;
            startV = viewport.VerticalOffset;
            markerCanvas.CapturePointer(args.Pointer);
        };
        markerCanvas.PointerMoved += (_, args) =>
        {
            if (!isPanning)
                return;

            var pos = args.GetCurrentPoint(viewport).Position;
            var dx = pos.X - panStart.X;
            var dy = pos.Y - panStart.Y;
            viewport.ChangeView(startH - dx, startV - dy, null, disableAnimation: true);
        };
        void EndPan(PointerRoutedEventArgs args)
        {
            if (!isPanning)
                return;
            isPanning = false;
            markerCanvas.ReleasePointerCapture(args.Pointer);
        }
        markerCanvas.PointerReleased += (_, args) => EndPan(args);
        markerCanvas.PointerCanceled += (_, args) => EndPan(args);
        markerCanvas.PointerCaptureLost += (_, args) => EndPan(args);
        var zoomFactor = 1d;
        var markerButtons = new List<(Button Button, Windows.Foundation.Point BasePoint)>();
        void ApplyZoom()
        {
            var scaledWidth = MapWidth * zoomFactor;
            var scaledHeight = MapHeight * zoomFactor;
            contentGrid.Width = scaledWidth;
            contentGrid.Height = scaledHeight;
            markerCanvas.Width = scaledWidth;
            markerCanvas.Height = scaledHeight;
            zoomHost.Width = MapWidth * zoomFactor;
            zoomHost.Height = MapHeight * zoomFactor;
            foreach (var marker in markerButtons)
            {
                Canvas.SetLeft(marker.Button, marker.BasePoint.X * zoomFactor - 11);
                Canvas.SetTop(marker.Button, marker.BasePoint.Y * zoomFactor - 11);
            }
        }

        var zoomOutButton = new Button
        {
            Content = "−",
            Width = 24,
            Height = 24,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(0)
        };
        var zoomInButton = new Button
        {
            Content = "+",
            Width = 24,
            Height = 24,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(0)
        };
        zoomOutButton.Click += (_, _) =>
        {
            zoomFactor = Math.Max(1d, zoomFactor - 0.25d);
            ApplyZoom();
        };
        zoomInButton.Click += (_, _) =>
        {
            zoomFactor = Math.Min(3.5d, zoomFactor + 0.25d);
            ApplyZoom();
        };

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 4
        };
        controls.Children.Add(zoomOutButton);
        controls.Children.Add(zoomInButton);

        var flyoutLayout = new StackPanel { Spacing = 6 };
        flyoutLayout.Children.Add(controls);
        flyoutLayout.Children.Add(viewport);

        var flyout = new Flyout { Content = flyoutLayout };
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
            markerButtons.Add((button, point));
            markerCanvas.Children.Add(button);
        }
        ApplyZoom();

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
        var projected = EqualEarthProject(longitude, Math.Clamp(latitude, -90d, 90d));
        var left = EqualEarthProject(-180, 0).X;
        var right = EqualEarthProject(180, 0).X;
        var top = EqualEarthProject(0, 90).Y;
        var bottom = EqualEarthProject(0, -90).Y;

        var x = (projected.X - left) / (right - left) * MapWidth;
        var y = (projected.Y - top) / (bottom - top) * MapHeight;
        return new Windows.Foundation.Point(x + MarkerXOffset, y);
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

    sealed record CityOption(string Name, double Longitude, double Latitude);
}
