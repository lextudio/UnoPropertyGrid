using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Globalization;
using Windows.System;

namespace UnoPropertyGrid;

sealed class CornerRadiusPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context) =>
        context.Descriptor.PropertyType == typeof(CornerRadius) && !context.Descriptor.IsReadOnly;

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var r = context.Descriptor.GetValue() is CornerRadius v ? v : new CornerRadius();
        return Build(r, val => context.SetValue?.Invoke(val));
    }

    static FrameworkElement Build(CornerRadius initial, Action<CornerRadius> commit)
    {
        // 3-row × 3-col grid:
        //   corners at (0,0) TopLeft, (0,2) TopRight, (2,0) BottomLeft, (2,2) BottomRight
        //   diagram fills (0-2, 0-2) via RowSpan/ColSpan with ZIndex behind boxes
        const double boxW = 52, diag = 80;

        var outer = new Grid();
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(diag) });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(boxW) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(boxW) });

        var tlBox = MakeBox(initial.TopLeft);
        var trBox = MakeBox(initial.TopRight);
        var blBox = MakeBox(initial.BottomLeft);
        var brBox = MakeBox(initial.BottomRight);

        Grid.SetRow(tlBox, 0); Grid.SetColumn(tlBox, 0);
        Grid.SetRow(trBox, 0); Grid.SetColumn(trBox, 2);
        Grid.SetRow(blBox, 2); Grid.SetColumn(blBox, 0);
        Grid.SetRow(brBox, 2); Grid.SetColumn(brBox, 2);

        // Live-updating rounded-corner preview border behind boxes
        var preview = new Border
        {
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1.5),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(30, 128, 128, 128)),
            CornerRadius = initial,
            Margin = new Thickness(6),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRowSpan(preview, 3);
        Grid.SetColumnSpan(preview, 3);

        outer.Children.Add(preview);
        outer.Children.Add(tlBox);
        outer.Children.Add(trBox);
        outer.Children.Add(blBox);
        outer.Children.Add(brBox);

        void Commit()
        {
            var cr = new CornerRadius(ReadBox(tlBox), ReadBox(trBox), ReadBox(brBox), ReadBox(blBox));
            preview.CornerRadius = cr;
            commit(cr);
        }

        Wire(tlBox, Commit);
        Wire(trBox, Commit);
        Wire(blBox, Commit);
        Wire(brBox, Commit);

        return outer;
    }

    static TextBox MakeBox(double value) => new()
    {
        Text = value.ToString("0.##", CultureInfo.InvariantCulture),
        MinWidth = 48,
        MaxWidth = 52,
        FontSize = 11,
        Padding = new Thickness(4, 2, 4, 2),
        TextAlignment = TextAlignment.Center
    };

    static void Wire(TextBox box, Action commit)
    {
        box.LostFocus += (_, _) => commit();
        box.KeyDown += (_, e) => { if (e.Key == VirtualKey.Enter) commit(); };
    }

    static double ReadBox(TextBox box) =>
        double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
}
