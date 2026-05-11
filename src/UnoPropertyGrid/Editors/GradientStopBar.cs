using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace UnoPropertyGrid;

sealed class GradientStopBar : UserControl
{
    const double BarH = 20;
    const double MarkerH = 12;
    const double MarkerW = 12;

    readonly Canvas _canvas;
    readonly List<GradientStop> _stops = new();
    GradientStop? _selected;
    bool _dragging;

    public event Action<GradientStop?>? StopSelectionChanged;
    public event Action? StopsChanged;

    public GradientStopBar()
    {
        _canvas = new Canvas { Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
        Content = _canvas;
        MinHeight = BarH + MarkerH + 4;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        SizeChanged += (_, e) =>
        {
            _canvas.Width = e.NewSize.Width;
            _canvas.Height = e.NewSize.Height;
            Render();
        };
        _canvas.PointerPressed += OnPressed;
        _canvas.PointerMoved += OnMoved;
        _canvas.PointerReleased += OnReleased;
    }

    public GradientStop? SelectedStop => _selected;
    public IReadOnlyList<GradientStop> Stops => _stops.AsReadOnly();

    public void SetStops(IEnumerable<GradientStop> stops)
    {
        _stops.Clear();
        _stops.AddRange(stops.Select(s => new GradientStop { Color = s.Color, Offset = s.Offset }));
        _stops.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        _selected = _stops.Count > 0 ? _stops[0] : null;
        Render();
    }

    public void UpdateSelectedStopColor(Windows.UI.Color color)
    {
        if (_selected == null) return;
        _selected.Color = color;
        Render();
        StopsChanged?.Invoke();
    }

    public void RemoveSelectedStop()
    {
        if (_selected == null || _stops.Count <= 2) return;
        _stops.Remove(_selected);
        _selected = _stops[0];
        Render();
        StopSelectionChanged?.Invoke(_selected);
        StopsChanged?.Invoke();
    }

    void Render()
    {
        var w = ActualWidth;
        if (w <= 0) return;
        _canvas.Children.Clear();

        // Light grey checkerboard stand-in to show transparency
        _canvas.Children.Add(new Rectangle
        {
            Width = w,
            Height = BarH,
            Fill = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
        });

        // Gradient fill
        var gradBrush = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
        foreach (var s in _stops.OrderBy(s => s.Offset))
            gradBrush.GradientStops.Add(new GradientStop { Color = s.Color, Offset = s.Offset });
        _canvas.Children.Add(new Rectangle { Width = w, Height = BarH, Fill = gradBrush });

        // Bar border
        _canvas.Children.Add(new Rectangle
        {
            Width = w,
            Height = BarH,
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            StrokeThickness = 1
        });

        // Stop markers — upward-pointing triangles below the bar
        foreach (var stop in _stops)
        {
            var x = stop.Offset * w;
            var isSelected = stop == _selected;
            var poly = new Polygon
            {
                Fill = new SolidColorBrush(stop.Color),
                Stroke = new SolidColorBrush(isSelected ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.DarkGray),
                StrokeThickness = isSelected ? 2 : 1,
                Tag = stop
            };
            poly.Points.Add(new Point(MarkerW / 2, 0));
            poly.Points.Add(new Point(0, MarkerH));
            poly.Points.Add(new Point(MarkerW, MarkerH));
            Canvas.SetLeft(poly, x - MarkerW / 2);
            Canvas.SetTop(poly, BarH + 2);
            _canvas.Children.Add(poly);
        }
    }

    void OnPressed(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(_canvas).Position;

        if (pt.Y <= BarH)
        {
            // Click on gradient bar → add a new stop interpolated at that offset
            var offset = Math.Clamp(pt.X / ActualWidth, 0.0, 1.0);
            var newStop = new GradientStop { Color = Interpolate(offset), Offset = offset };
            _stops.Add(newStop);
            _stops.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            _selected = newStop;
        }
        else
        {
            // Click in marker area → select nearest stop for dragging
            var hit = FindNearest(pt.X);
            if (hit != null) _selected = hit;
        }

        _dragging = true;
        _canvas.CapturePointer(e.Pointer);
        Render();
        StopSelectionChanged?.Invoke(_selected);
        StopsChanged?.Invoke();
    }

    void OnMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging || _selected == null) return;
        _selected.Offset = Math.Clamp(e.GetCurrentPoint(_canvas).Position.X / ActualWidth, 0.0, 1.0);
        Render();
        StopsChanged?.Invoke();
    }

    void OnReleased(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false;
        _canvas.ReleasePointerCapture(e.Pointer);
    }

    GradientStop? FindNearest(double x)
    {
        GradientStop? best = null;
        var bestDist = MarkerW + 4;
        foreach (var s in _stops)
        {
            var d = Math.Abs(s.Offset * ActualWidth - x);
            if (d < bestDist) { bestDist = d; best = s; }
        }
        return best;
    }

    Windows.UI.Color Interpolate(double offset)
    {
        if (_stops.Count == 0) return Microsoft.UI.Colors.White;
        var sorted = _stops.OrderBy(s => s.Offset).ToList();
        if (offset <= sorted[0].Offset) return sorted[0].Color;
        if (offset >= sorted[^1].Offset) return sorted[^1].Color;
        for (var i = 0; i < sorted.Count - 1; i++)
        {
            if (offset >= sorted[i].Offset && offset <= sorted[i + 1].Offset)
            {
                var t = (offset - sorted[i].Offset) / (sorted[i + 1].Offset - sorted[i].Offset);
                return LerpColor(sorted[i].Color, sorted[i + 1].Color, t);
            }
        }
        return Microsoft.UI.Colors.White;
    }

    static Windows.UI.Color LerpColor(Windows.UI.Color a, Windows.UI.Color b, double t) =>
        Windows.UI.Color.FromArgb(
            (byte)(a.A + (b.A - a.A) * t),
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
}
