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

namespace UnoPropertyGrid.Sample;

sealed class ThicknessEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return context.Descriptor.PropertyType == typeof(Thickness)
            && !context.Descriptor.IsReadOnly;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var value = context.Descriptor.GetValue() is Thickness thickness ? thickness : new Thickness();
        return CreateFourValueEditor(value.Left, value.Top, value.Right, value.Bottom, SetValue);

        void SetValue(double left, double top, double right, double bottom)
        {
            context.SetValue?.Invoke(new Thickness(left, top, right, bottom));
        }
    }

    internal static Grid CreateFourValueEditor(double left, double top, double right, double bottom, Action<double, double, double, double> setValue)
    {
        var grid = new Grid { ColumnSpacing = 4 };
        for (var i = 0; i < 4; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var boxes = new[]
        {
            CreateBox(left, "Left"),
            CreateBox(top, "Top"),
            CreateBox(right, "Right"),
            CreateBox(bottom, "Bottom")
        };

        for (var i = 0; i < boxes.Length; i++)
        {
            Grid.SetColumn(boxes[i], i);
            grid.Children.Add(boxes[i]);
        }

        foreach (var box in boxes)
        {
            box.LostFocus += (_, _) =>
            {
                setValue(Read(boxes[0]), Read(boxes[1]), Read(boxes[2]), Read(boxes[3]));
            };
        }

        return grid;
    }

    static TextBox CreateBox(double value, string header)
    {
        return new TextBox
        {
            Text = value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            Header = header,
            MinWidth = 48
        };
    }

    static double Read(TextBox box)
    {
        return double.TryParse(box.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }
}
