using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.UI;

namespace UnoPropertyGrid;

static class BrushEditorContent
{
    enum BrushMode { None, Solid, Gradient, Image }

    static List<GradientStop> DefaultStops() =>
    [
        new() { Color = Microsoft.UI.Colors.White, Offset = 0 },
        new() { Color = Microsoft.UI.Colors.Black, Offset = 1 }
    ];

    public static FrameworkElement Create(Brush? initial, Action<Brush?> onChange)
    {
        var initialMode = DetectMode(initial);
        var solidColor = (initial as SolidColorBrush)?.Color ?? Microsoft.UI.Colors.White;
        var gradientStops = ExtractGradientStops(initial) ?? DefaultStops();
        var gradientAngle = initial is LinearGradientBrush lgbInit ? ComputeAngle(lgbInit) : 0.0;

        var noneTab     = MakeTab(NoBrushIcon(),    "No brush",    BuildNoneContent());
        var solidTab    = MakeTab(SolidColorIcon(), "Solid color", BuildSolidContent(solidColor, c => { solidColor = c; onChange(new SolidColorBrush(c)); }));
        var gradientTab = MakeTab(GradientIcon(),   "Gradient",    BuildGradientContent(gradientStops, gradientAngle,
                              (s, a) => { gradientStops = s; gradientAngle = a; onChange(BuildLinearBrush(s, a)); }));
        var imageTab    = MakeTab(ImageBrushIcon(), "Image",       BuildImageContent(initial as ImageBrush, onChange));

        var tabView = new TabView
        {
            IsAddTabButtonVisible = false,
            TabWidthMode = TabViewWidthMode.SizeToContent
        };
        tabView.TabItems.Add(noneTab);
        tabView.TabItems.Add(solidTab);
        tabView.TabItems.Add(gradientTab);
        tabView.TabItems.Add(imageTab);

        // Set initial selection before attaching SelectionChanged to avoid spurious onChange call
        tabView.SelectedIndex = (int)initialMode;

        tabView.SelectionChanged += (_, _) =>
        {
            // Image tab manages its own onChange calls; other tabs emit immediately on switch
            if ((BrushMode)tabView.SelectedIndex == BrushMode.Image)
                return;
            onChange((BrushMode)tabView.SelectedIndex switch
            {
                BrushMode.None     => null,
                BrushMode.Solid    => new SolidColorBrush(solidColor),
                BrushMode.Gradient => BuildLinearBrush(gradientStops, gradientAngle),
                _                  => null
            });
        };

        return tabView;
    }

    // TabViewItem.Header accepts any UIElement — use it to host hand-drawn canvas icons.
    // IconSource is left null; the Canvas in Header fills that role.
    static TabViewItem MakeTab(UIElement icon, string tooltip, object content)
    {
        var item = new TabViewItem
        {
            Header = icon,
            IconSource = null,
            IsClosable = false,
            Content = content,
            MinWidth = 20,
            MaxWidth = 44,
            Padding = new Thickness(2, 2, 2, 2),
            Margin = new Thickness(0)
        };
        ToolTipService.SetToolTip(item, tooltip);
        return item;
    }

    // Shared stroke color — visible on both light and dark themes
    static SolidColorBrush IconStroke() => new(Microsoft.UI.ColorHelper.FromArgb(255, 140, 140, 140));
    static SolidColorBrush IconFill()   => new(Microsoft.UI.ColorHelper.FromArgb(255, 140, 140, 140));
    static SolidColorBrush Transparent() => new(Microsoft.UI.Colors.Transparent);

    // Outer 16×10 rectangle, stroke only — shared scaffold for all icons
    static Canvas IconCanvas()
    {
        var c = new Canvas { Width = 16, Height = 10 };
        c.Children.Add(new Rectangle
        {
            Width = 16, Height = 10,
            Stroke = IconStroke(),
            StrokeThickness = 1,
            Fill = Transparent()
        });
        return c;
    }

    // No brush: outer rect + diagonal × lines
    static UIElement NoBrushIcon()
    {
        var c = IconCanvas();
        c.Children.Add(new Line { X1 = 2, Y1 = 2, X2 = 14, Y2 = 8, Stroke = IconStroke(), StrokeThickness = 1.2 });
        c.Children.Add(new Line { X1 = 14, Y1 = 2, X2 = 2, Y2 = 8, Stroke = IconStroke(), StrokeThickness = 1.2 });
        return c;
    }

    // Solid color: outer rect + smaller filled inner rect
    static UIElement SolidColorIcon()
    {
        var c = IconCanvas();
        var inner = new Rectangle { Width = 8, Height = 4, Fill = IconFill() };
        Canvas.SetLeft(inner, 4);
        Canvas.SetTop(inner, 3);
        c.Children.Add(inner);
        return c;
    }

    // Gradient: outer rect + inner rect with white-to-dark horizontal gradient
    static UIElement GradientIcon()
    {
        var c = IconCanvas();
        var gradBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint   = new Point(1, 0.5)
        };
        gradBrush.GradientStops.Add(new GradientStop { Color = Microsoft.UI.Colors.White, Offset = 0 });
        gradBrush.GradientStops.Add(new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(255, 60, 60, 60), Offset = 1 });
        var inner = new Rectangle { Width = 8, Height = 4, Fill = gradBrush };
        Canvas.SetLeft(inner, 4);
        Canvas.SetTop(inner, 3);
        c.Children.Add(inner);
        return c;
    }

    // Image brush: outer rect + mountain triangle + sun ellipse
    static UIElement ImageBrushIcon()
    {
        var c = IconCanvas();
        var mountain = new Polygon { Fill = IconFill() };
        mountain.Points.Add(new Point(2,  9));
        mountain.Points.Add(new Point(8,  3));
        mountain.Points.Add(new Point(14, 9));
        c.Children.Add(mountain);
        var sun = new Ellipse { Width = 3, Height = 3, Fill = IconFill() };
        Canvas.SetLeft(sun, 10);
        Canvas.SetTop(sun, 1.5);
        c.Children.Add(sun);
        return c;
    }

    static FrameworkElement BuildNoneContent() =>
        new TextBlock
        {
            Text = "No brush",
            FontSize = 12,
            Opacity = 0.6,
            Margin = new Thickness(0, 4, 0, 0)
        };

    static FrameworkElement BuildSolidContent(Color initialColor, Action<Color> onChanged)
    {
        var picker = new ColorPicker
        {
            Color = initialColor,
            IsAlphaEnabled = true,
            IsHexInputVisible = true
        };
        picker.ColorChanged += (_, e) => onChanged(e.NewColor);
        return picker;
    }

    static FrameworkElement BuildGradientContent(
        List<GradientStop> initialStops,
        double initialAngle,
        Action<List<GradientStop>, double> onChanged)
    {
        var angle = initialAngle;
        var stopBar = new GradientStopBar();
        stopBar.SetStops(initialStops);

        var removeBtn = new Button
        {
            Content = "Remove stop",
            FontSize = 11,
            IsEnabled = initialStops.Count > 2
        };
        var hint = new TextBlock
        {
            Text = "Click bar to add stop",
            FontSize = 11,
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center
        };

        var stopActionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 4, 0, 4)
        };
        stopActionRow.Children.Add(removeBtn);
        stopActionRow.Children.Add(hint);

        var stopColorPicker = new ColorPicker
        {
            Color = stopBar.SelectedStop?.Color ?? Microsoft.UI.Colors.White,
            IsAlphaEnabled = true,
            IsHexInputVisible = true
        };

        List<GradientStop> CurrentStopList() =>
            stopBar.Stops.Select(s => new GradientStop { Color = s.Color, Offset = s.Offset }).ToList();

        bool syncing = false;

        stopBar.StopSelectionChanged += sel =>
        {
            if (sel == null) return;
            syncing = true;
            stopColorPicker.Color = sel.Color;
            syncing = false;
            removeBtn.IsEnabled = stopBar.Stops.Count > 2;
        };

        stopBar.StopsChanged += () =>
        {
            removeBtn.IsEnabled = stopBar.Stops.Count > 2;
            onChanged(CurrentStopList(), angle);
        };

        stopColorPicker.ColorChanged += (_, e) =>
        {
            if (syncing) return;
            stopBar.UpdateSelectedStopColor(e.NewColor);
        };

        removeBtn.Click += (_, _) => stopBar.RemoveSelectedStop();

        var content = new StackPanel { Spacing = 2 };
        content.Children.Add(stopBar);
        content.Children.Add(stopActionRow);
        content.Children.Add(new TextBlock { Text = "Angle", FontSize = 12, Margin = new Thickness(0, 6, 0, 2) });
        content.Children.Add(BuildAngleEditor(angle, a =>
        {
            angle = a;
            onChanged(CurrentStopList(), angle);
        }));
        content.Children.Add(new TextBlock { Text = "Stop color", FontSize = 12, Margin = new Thickness(0, 6, 0, 2) });
        content.Children.Add(stopColorPicker);

        return content;
    }

    static FrameworkElement BuildImageContent(ImageBrush? initial, Action<Brush?> onChange)
    {
        var initialUri = GetImageUri(initial);

        var uriBox = new TextBox
        {
            Text = initialUri,
            PlaceholderText = "Image URI or file path",
            FontSize = 12
        };

        var previewImage = new Microsoft.UI.Xaml.Controls.Image
        {
            MaxHeight = 100,
            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
            Margin = new Thickness(0, 6, 0, 0)
        };

        if (!string.IsNullOrEmpty(initialUri))
            TrySetImageSource(previewImage, initialUri);

        void Apply()
        {
            var text = uriBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                previewImage.Source = null;
                onChange(null);
                return;
            }
            var uri = ToUri(text);
            if (uri == null) return;
            previewImage.Source = new BitmapImage(uri);
            onChange(new ImageBrush { ImageSource = new BitmapImage(uri) });
        }

        uriBox.KeyDown += (_, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                Apply();
        };

        var applyBtn = new Button { Content = "Apply", FontSize = 12 };
        applyBtn.Click += (_, _) => Apply();

        var browseBtn = new Button { Content = "Browse…", FontSize = 12 };
        browseBtn.Click += async (_, _) =>
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".webp");
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            uriBox.Text = file.Path;
            Apply();
        };

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 6, 0, 0) };
        btnRow.Children.Add(browseBtn);
        btnRow.Children.Add(applyBtn);

        var content = new StackPanel { Spacing = 2, Padding = new Thickness(0, 4, 0, 0) };
        content.Children.Add(new TextBlock { Text = "Image source", FontSize = 12, Margin = new Thickness(0, 0, 0, 2) });
        content.Children.Add(uriBox);
        content.Children.Add(btnRow);
        content.Children.Add(previewImage);
        return content;
    }

    static string GetImageUri(ImageBrush? brush)
    {
        if (brush?.ImageSource is BitmapImage bmp)
            return bmp.UriSource?.ToString() ?? string.Empty;
        return string.Empty;
    }

    static void TrySetImageSource(Microsoft.UI.Xaml.Controls.Image img, string text)
    {
        var uri = ToUri(text);
        if (uri != null) img.Source = new BitmapImage(uri);
    }

    static Uri? ToUri(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri)) return uri;
        // Treat as local file path
        if (System.IO.File.Exists(text))
            return new Uri("file:///" + text.Replace('\\', '/'));
        return null;
    }

    static FrameworkElement BuildAngleEditor(double initialAngle, Action<double> onChanged)
    {
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });

        var slider = new Slider { Minimum = 0, Maximum = 360, Value = initialAngle, StepFrequency = 1 };
        var box = new TextBox
        {
            Text = initialAngle.ToString("0"),
            Width = 52,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(box, 1);
        grid.Children.Add(slider);
        grid.Children.Add(box);

        bool syncing = false;
        slider.ValueChanged += (_, e) =>
        {
            if (syncing) return;
            syncing = true;
            box.Text = e.NewValue.ToString("0");
            syncing = false;
            onChanged(e.NewValue);
        };
        box.LostFocus += (_, _) =>
        {
            if (!double.TryParse(box.Text, out var v)) return;
            v = Math.Clamp(v, 0, 360);
            syncing = true;
            slider.Value = v;
            box.Text = v.ToString("0");
            syncing = false;
            onChanged(v);
        };

        return grid;
    }

    static BrushMode DetectMode(Brush? brush) => brush switch
    {
        null                                   => BrushMode.None,
        LinearGradientBrush or RadialGradientBrush => BrushMode.Gradient,
        ImageBrush                             => BrushMode.Image,
        _                                      => BrushMode.Solid
    };

    static List<GradientStop>? ExtractGradientStops(Brush? brush) => brush switch
    {
        LinearGradientBrush lgb => lgb.GradientStops.Select(s => new GradientStop { Color = s.Color, Offset = s.Offset }).ToList(),
        RadialGradientBrush rgb => rgb.GradientStops.Select(s => new GradientStop { Color = s.Color, Offset = s.Offset }).ToList(),
        _ => null
    };

    static double ComputeAngle(LinearGradientBrush brush)
    {
        var dx = brush.EndPoint.X - brush.StartPoint.X;
        var dy = brush.EndPoint.Y - brush.StartPoint.Y;
        return (Math.Atan2(-dy, dx) * 180.0 / Math.PI + 360) % 360;
    }

    static LinearGradientBrush BuildLinearBrush(List<GradientStop> stops, double angle)
    {
        var rad = angle * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0.5 - cos * 0.5, 0.5 + sin * 0.5),
            EndPoint   = new Point(0.5 + cos * 0.5, 0.5 - sin * 0.5)
        };
        foreach (var stop in stops.OrderBy(s => s.Offset))
            brush.GradientStops.Add(new GradientStop { Color = stop.Color, Offset = stop.Offset });
        return brush;
    }

}
