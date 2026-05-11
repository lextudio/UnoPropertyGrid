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

sealed class BrushSwatchEditorProvider : IPropertyGridEditorProvider
{
    static readonly (string Name, Color Color)[] Colors =
    [
        ("Blue", Microsoft.UI.Colors.DodgerBlue),
        ("Green", Microsoft.UI.Colors.SeaGreen),
        ("Amber", Microsoft.UI.Colors.Goldenrod),
        ("Red", Microsoft.UI.Colors.IndianRed),
        ("Gray", Microsoft.UI.Colors.DimGray),
        ("White", Microsoft.UI.Colors.White)
    ];

    public bool CanEdit(PropertyGridEditorContext context)
    {
        return typeof(Brush).IsAssignableFrom(context.Descriptor.PropertyType)
            && !context.Descriptor.IsReadOnly;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        var buttons = new List<Button>();
        foreach (var option in Colors)
        {
            var button = new Button
            {
                Width = 24,
                Height = 22,
                Padding = new Thickness(0),
                Background = new SolidColorBrush(option.Color),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                Content = string.Empty
            };
            ToolTipService.SetToolTip(button, option.Name);
            button.Click += (_, _) =>
            {
                context.SetValue?.Invoke(new SolidColorBrush(option.Color));
                SelectButton(buttons, button);
            };
            buttons.Add(button);
            panel.Children.Add(button);
        }

        SelectCurrentBrush(context, buttons);
        return panel;
    }

    static void SelectCurrentBrush(PropertyGridEditorContext context, IReadOnlyList<Button> buttons)
    {
        if (context.Descriptor.GetValue() is not SolidColorBrush brush)
            return;

        for (var i = 0; i < Colors.Length && i < buttons.Count; i++)
        {
            if (Colors[i].Color == brush.Color)
            {
                SelectButton(buttons, buttons[i]);
                return;
            }
        }
    }

    static void SelectButton(IEnumerable<Button> buttons, Button selected)
    {
        foreach (var button in buttons)
            button.BorderThickness = button == selected ? new Thickness(3) : new Thickness(1);
    }
}
