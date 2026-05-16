using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System.Linq;
using Windows.Foundation;

namespace UnoPropertyGrid;

sealed class BrushPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context) =>
        PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.Brush;

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var brush = context.Value as Brush;
        var previewBrush = brush ?? PropertyGridEditorProviderUtilities.GetBrushPreview(context.Value);

        // Checkerboard backdrop makes transparency and "no brush" visible
        var backdrop = CreateCheckerboardElement();
        var fill = new Rectangle { Fill = BrushOrTransparent(previewBrush) };

        var preview = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 18
        };
        // Label shown when there is explicitly "no brush" (null)
        var noBrushLabel = new TextBlock
        {
            Text = "No brush",
            FontSize = 12,
            Opacity = 0.75,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        // Frame border that contains the preview content; children are inset
        var frame = new Border
        {
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(1)
        };

        var inner = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };

        // Show checkerboard only when the brush is effectively transparent
        var isTransparent = IsEffectivelyTransparent(brush);
        backdrop.Visibility = isTransparent ? Visibility.Visible : Visibility.Collapsed;

        inner.Children.Add(backdrop);
        inner.Children.Add(fill);
        noBrushLabel.Visibility = brush == null ? Visibility.Visible : Visibility.Collapsed;
        inner.Children.Add(noBrushLabel);

        frame.Child = inner;
        preview.Children.Add(frame);

        var flyout = new Flyout
        {
#if WINDOWS_APP_SDK
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
#else
            Placement = FlyoutPlacementMode.Auto,
#endif
            // Cap the presenter height so it never overflows the window on any platform;
            // the ScrollViewer inside then handles the overflow.
            FlyoutPresenterStyle = new Style(typeof(FlyoutPresenter))
            {
                Setters = { new Setter(FrameworkElement.MaxHeightProperty, 500.0) }
            }
        };
        var editorContent = BrushEditorContent.Create(brush, b =>
        {
            PropertyGridEditorProviderUtilities.Commit(context, b);
            var descriptorValue = context.Descriptor.GetValue();
            var newBrush = PropertyGridEditorProviderUtilities.GetBrushPreview(descriptorValue);
            fill.Fill = BrushOrTransparent(newBrush);
            var isNowTransparent = IsEffectivelyTransparent(newBrush);
            noBrushLabel.Visibility = descriptorValue == null ? Visibility.Visible : Visibility.Collapsed;
            backdrop.Visibility = isNowTransparent ? Visibility.Visible : Visibility.Collapsed;
        });

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = new Border
            {
                Padding = new Thickness(8),
                MaxWidth = 640,
                Child = editorContent
            }
        };

        flyout.Content = scroll;

#if !WINDOWS_APP_SDK
        // FlyoutPresenterStyle MaxHeight is ignored by Uno; constrain the presenter directly
        // after it is created so the ScrollViewer can scroll the overflow.
        flyout.Opened += (_, _) =>
        {
            if (scroll.Parent is FlyoutPresenter fp)
            {
                fp.MaxHeight = 480;
                fp.VerticalAlignment = VerticalAlignment.Top;
            }
        };
#endif

        preview.Tapped += (_, _) =>
        {
            if (!preview.IsLoaded || flyout.IsOpen)
                return;

            flyout.ShowAt(preview);
        };

        return preview;
    }

    static Brush BrushOrTransparent(Brush? brush) =>
        brush ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);

    static FrameworkElement CreateCheckerboardElement()
    {
        const double squareSize = 6.0;
        var lightBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
        var darkBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 200, 200, 200));

        var canvas = new Canvas { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };

        void Redraw(Size sz)
        {
            canvas.Children.Clear();
            if (sz.Width <= 0 || sz.Height <= 0) return;
            var cols = (int)Math.Ceiling(sz.Width / squareSize);
            var rows = (int)Math.Ceiling(sz.Height / squareSize);
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var rect = new Rectangle
                {
                    Width = squareSize,
                    Height = squareSize,
                    Fill = ((r + c) & 1) == 0 ? lightBrush : darkBrush,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(rect, c * squareSize);
                Canvas.SetTop(rect, r * squareSize);
                canvas.Children.Add(rect);
            }
        }

        canvas.SizeChanged += (_, e) =>
        {
            Redraw(e.NewSize);
            canvas.Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
        };
        return canvas;
    }

    static bool IsEffectivelyTransparent(Brush? brush)
    {
        if (brush == null) return false;
        if (Math.Abs(brush.Opacity) < 1e-6) return true;
        if (brush is SolidColorBrush scb) return scb.Color.A == 0;
        if (brush is LinearGradientBrush lgb)
            return lgb.GradientStops.Count > 0 && lgb.GradientStops.All(s => s.Color.A == 0);
        if (brush is RadialGradientBrush rgb)
            return rgb.GradientStops.Count > 0 && rgb.GradientStops.All(s => s.Color.A == 0);
        if (brush is ImageBrush ib) return Math.Abs(ib.Opacity) < 1e-6;
        return false;
    }
}
