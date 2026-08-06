using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Globalization;
using Windows.System;

namespace UnoPropertyGrid;

sealed class ThicknessPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context) =>
        context.Descriptor.PropertyType == typeof(Thickness) && !context.Descriptor.IsReadOnly;

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var t = context.Descriptor.GetValue() is Thickness v ? v : new Thickness();
        return Build(t, val => context.SetValue?.Invoke(val));
    }

    static FrameworkElement Build(Thickness initial, Action<Thickness> commit)
    {
        // Outer 5-row × 5-col grid:
        //   row0=top box, row1=spacer, row2=center diagram, row3=spacer, row4=bottom box
        //   col0=left box, col1=spacer, col2=center, col3=spacer, col4=right box
        const double boxW = 52, gap = 4, diag = 60;

        var outer = new Grid();
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(gap) });
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(diag) });
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(gap) });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(boxW) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(boxW) });

        var leftBox   = MakeBox(initial.Left);
        var topBox    = MakeBox(initial.Top);
        var rightBox  = MakeBox(initial.Right);
        var bottomBox = MakeBox(initial.Bottom);

        // Top box — row 0, col 2 (centered above diagram)
        Grid.SetRow(topBox, 0); Grid.SetColumn(topBox, 2);
        // Bottom box — row 4, col 2
        Grid.SetRow(bottomBox, 4); Grid.SetColumn(bottomBox, 2);
        // Left box — row 2, col 0 (vertically centered next to diagram)
        Grid.SetRow(leftBox, 2); Grid.SetColumn(leftBox, 0);
        // Right box — row 2, col 4
        Grid.SetRow(rightBox, 2); Grid.SetColumn(rightBox, 4);

        leftBox.VerticalAlignment   = VerticalAlignment.Center;
        rightBox.VerticalAlignment  = VerticalAlignment.Center;
        topBox.HorizontalAlignment  = HorizontalAlignment.Center;
        bottomBox.HorizontalAlignment = HorizontalAlignment.Center;

        // Diagram rectangle — row 2, col 2
        var diagram = new Border
        {
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(30, 128, 128, 128)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(2)
        };
        Grid.SetRow(diagram, 2); Grid.SetColumn(diagram, 2);

        outer.Children.Add(topBox);
        outer.Children.Add(leftBox);
        outer.Children.Add(diagram);
        outer.Children.Add(rightBox);
        outer.Children.Add(bottomBox);

        void Commit()
        {
            commit(new Thickness(ReadBox(leftBox), ReadBox(topBox), ReadBox(rightBox), ReadBox(bottomBox)));
        }

        Wire(leftBox,   Commit);
        Wire(topBox,    Commit);
        Wire(rightBox,  Commit);
        Wire(bottomBox, Commit);

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
