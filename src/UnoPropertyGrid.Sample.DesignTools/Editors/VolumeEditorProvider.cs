using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.System;
using Windows.UI;

namespace UnoPropertyGrid.Sample.DesignTools;

sealed class VolumeEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return !context.Descriptor.IsReadOnly
            && context.Descriptor.PropertyType == typeof(double)
            && (context.Component is Slider && context.Descriptor.Name == nameof(Slider.Value)
                || context.Descriptor.Name.Contains("Volume", StringComparison.OrdinalIgnoreCase));
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var value = Clamp(context.Descriptor.GetValue() is double current ? current : 0);
        const double trackLeft = 8;
        const double trackRight = 172;
        const double trackBottom = 34;
        const double trackTop = 8;

        var panel = new Grid
        {
            Width = 220,
            Height = 46
        };

        const int segmentCount = 7;
        const double gap = 4;
        var activeBrush = new SolidColorBrush();
        var inactiveBrush = new SolidColorBrush();
        var segments = new List<VolumeSegment>(segmentCount);
        double TopAt(double x) => trackBottom - (trackBottom - trackTop) * (x - trackLeft) / (trackRight - trackLeft);
        for (var i = 0; i < segmentCount; i++)
        {
            var x0 = trackLeft + (trackRight - trackLeft) * i / segmentCount + gap / 2;
            var x1 = trackLeft + (trackRight - trackLeft) * (i + 1) / segmentCount - gap / 2;
            var y0 = TopAt(x0);
            var y1 = TopAt(x1);
            var inactiveSegment = CreateVolumeSegment(x0, x1, y0, y1, inactiveBrush);
            var activeSegment = CreateVolumeSegment(x0, x1, y0, y1, activeBrush);
            var activeClip = new RectangleGeometry();
            activeSegment.Clip = activeClip;
            segments.Add(new VolumeSegment(x0, x1, inactiveSegment, activeSegment, activeClip));
            panel.Children.Add(inactiveSegment);
            panel.Children.Add(activeSegment);
        }

        static Polygon CreateVolumeSegment(double x0, double x1, double y0, double y1, Brush fill)
        {
            var segment = new Polygon
            {
                Fill = fill
            };
            segment.Points.Add(new Windows.Foundation.Point(x0, trackBottom));
            segment.Points.Add(new Windows.Foundation.Point(x1, trackBottom));
            segment.Points.Add(new Windows.Foundation.Point(x1, y1));
            segment.Points.Add(new Windows.Foundation.Point(x0, y0));
            return segment;
        }

        var label = new TextBlock
        {
            Text = $"{value:0}%",
            Width = 42,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextAlignment = TextAlignment.Right
        };
        panel.Children.Add(label);
        void ApplyVolumeTheme(ElementTheme? t = null)
        {
            var isDark = (t ?? EditorChrome.GetEffectiveTheme(label)) == ElementTheme.Dark;
            label.Foreground = new SolidColorBrush(isDark
                ? Color.FromArgb(255, 0xD4, 0xD4, 0xD4)
                : Color.FromArgb(255, 0x1E, 0x1E, 0x1E));
            activeBrush.Color = isDark
                ? Color.FromArgb(255, 0xD7, 0xBA, 0x7D)
                : Color.FromArgb(255, 0xA6, 0x5F, 0x00);
            inactiveBrush.Color = isDark
                ? Color.FromArgb(255, 0x5A, 0x5A, 0x5A)
                : Color.FromArgb(255, 0xB4, 0xBA, 0xC4);
        }
        label.Loaded += (_, _) => ApplyVolumeTheme();
        context.ThemeChanged += t => ApplyVolumeTheme(t);
        ApplyVolumeTheme();

        SetSegments(value);

        panel.PointerPressed += OnPointer;
        panel.PointerMoved += OnPointer;
        panel.PointerReleased += (_, args) =>
        {
            panel.ReleasePointerCapture(args.Pointer);
        };

        return panel;

        void OnPointer(object sender, PointerRoutedEventArgs args)
        {
            if (args.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse
                && !args.GetCurrentPoint(panel).Properties.IsLeftButtonPressed)
                return;

            panel.CapturePointer(args.Pointer);
            var point = args.GetCurrentPoint(panel).Position;
            var rounded = Math.Round(Clamp((point.X - trackLeft) / (trackRight - trackLeft) * 100));
            SetSegments(rounded);
            label.Text = $"{rounded:0}%";
            context.SetValue?.Invoke(rounded);
        }

        void SetSegments(double percent)
        {
            var activeX = trackLeft + (trackRight - trackLeft) * percent / 100d;
            foreach (var segment in segments)
            {
                var width = Math.Max(0, Math.Min(segment.Right, activeX) - segment.Left);
                segment.ActiveClip.Rect = new Windows.Foundation.Rect(segment.Left, 0, width, trackBottom);
            }
        }

        static double Clamp(double input) => Math.Max(0, Math.Min(100, input));
    }

    sealed record VolumeSegment(double Left, double Right, Polygon Inactive, Polygon Active, RectangleGeometry ActiveClip);
}
