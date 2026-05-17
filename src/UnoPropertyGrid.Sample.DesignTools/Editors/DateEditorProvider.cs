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
using TextBox = LeXtudio.UI.Controls.TextBox;

namespace UnoPropertyGrid.Sample.DesignTools;

sealed class DateEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return context.Descriptor.PropertyType == typeof(DateTimeOffset)
            && !context.Descriptor.IsReadOnly;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var panel = new Grid { ColumnSpacing = 6 };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var initial = context.Descriptor.GetValue() is DateTimeOffset value ? value : DateTimeOffset.Now;
        var textBox = new TextBox
        {
            Text = initial.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            PlaceholderText = "yyyy-MM-dd",
            MinHeight = 30
        };
        Grid.SetColumn(textBox, 1);
        panel.Children.Add(textBox);

        var calendar = new CalendarView
        {
            SelectedDates = { initial },
            MinWidth = 280
        };
        var flyout = new Flyout { Content = calendar };
        var button = EditorChrome.CreatePickerButton("\uE787", "Choose date");
        button.Flyout = flyout;
        panel.Children.Add(button);

        calendar.SelectedDatesChanged += (_, args) =>
        {
            if (args.AddedDates.Count == 0)
                return;

            var selected = args.AddedDates[0];
            textBox.Text = selected.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            context.SetValue?.Invoke(selected);
            flyout.Hide();
        };
        textBox.LostFocus += (_, _) => ApplyText();
        textBox.KeyDown += (_, args) =>
        {
            if (args.Key == Windows.System.VirtualKey.Enter)
                ApplyText();
        };

        return panel;

        void ApplyText()
        {
            if (!DateTimeOffset.TryParseExact(textBox.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            {
                var current = context.Descriptor.GetValue() is DateTimeOffset currentValue ? currentValue : initial;
                textBox.Text = current.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return;
            }

            calendar.SelectedDates.Clear();
            calendar.SelectedDates.Add(parsed);
            context.SetValue?.Invoke(parsed);
        }
    }
}
