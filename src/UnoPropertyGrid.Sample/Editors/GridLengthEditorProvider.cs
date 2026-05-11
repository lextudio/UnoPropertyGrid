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

sealed class GridLengthEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return context.Descriptor.PropertyType == typeof(GridLength)
            && !context.Descriptor.IsReadOnly;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var current = context.Descriptor.GetValue() is GridLength length ? length : new GridLength(1, GridUnitType.Star);
        var panel = new Grid { ColumnSpacing = 4 };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });

        var valueBox = new TextBox
        {
            Text = current.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            MinWidth = 64
        };
        panel.Children.Add(valueBox);

        var typeBox = new ComboBox
        {
            ItemsSource = new[] { "Pixel", "Star", "Auto" },
            SelectedItem = current.IsAuto ? "Auto" : current.IsStar ? "Star" : "Pixel"
        };
        Grid.SetColumn(typeBox, 1);
        panel.Children.Add(typeBox);

        valueBox.LostFocus += (_, _) => Apply();
        typeBox.SelectionChanged += (_, _) => Apply();
        return panel;

        void Apply()
        {
            var kind = typeBox.SelectedItem as string ?? "Star";
            if (kind == "Auto")
            {
                context.SetValue?.Invoke(GridLength.Auto);
                return;
            }

            var number = double.TryParse(valueBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 1;
            context.SetValue?.Invoke(new GridLength(number, kind == "Pixel" ? GridUnitType.Pixel : GridUnitType.Star));
        }
    }
}
