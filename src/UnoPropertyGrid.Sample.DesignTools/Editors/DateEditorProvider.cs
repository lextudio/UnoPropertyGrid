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

    static void ApplyCalendarTheme(CalendarView calendar, ElementTheme theme)
    {
        var isDark = theme == ElementTheme.Dark;
        var bg = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x25, 0x25, 0x26) : Color.FromArgb(255, 0xFF, 0xFF, 0xFF));
        var fg = new SolidColorBrush(isDark ? Color.FromArgb(255, 0xD4, 0xD4, 0xD4) : Color.FromArgb(255, 0x1E, 0x1E, 0x1E));
        var hover = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x55, 0x55, 0x58) : Color.FromArgb(255, 0xD0, 0xD0, 0xD0));
        var selected = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x00, 0x78, 0xD4) : Color.FromArgb(255, 0x00, 0x78, 0xD4));
        var today = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x09, 0x47, 0x71) : Color.FromArgb(255, 0xCC, 0xE4, 0xF7));
        var muted = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x6B, 0x6B, 0x6B) : Color.FromArgb(255, 0x9E, 0x9E, 0x9E));
        var border = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x3F, 0x3F, 0x46) : Color.FromArgb(255, 0xCC, 0xCC, 0xCC));
        calendar.Background = bg;
        calendar.Foreground = fg;
        calendar.BorderBrush = border;
        calendar.CalendarItemBackground = bg;
        calendar.CalendarItemForeground = fg;
        calendar.CalendarItemBorderBrush = border;
        calendar.HoverBorderBrush = hover;
        calendar.SelectedHoverBorderBrush = selected;
        calendar.SelectedPressedBorderBrush = selected;
        calendar.SelectedBorderBrush = selected;
        calendar.PressedBorderBrush = hover;
        calendar.TodayBackground = today;
        calendar.TodayForeground = fg;
        calendar.SelectedForeground = fg;
        calendar.TodayBlackoutBackground = today;
        calendar.BlackoutBackground = hover;
        calendar.OutOfScopeBackground = bg;
        calendar.OutOfScopeForeground = muted;
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
        textBox.Loaded += (_, _) => EditorChrome.ApplyTextBoxTheme(textBox);
        context.ThemeChanged += t => EditorChrome.ApplyTextBoxTheme(textBox, t);

        var calendar = new CalendarView
        {
            SelectedDates = { initial },
            MinWidth = 280
        };
        var flyout = new Flyout { Content = calendar };
        var button = EditorChrome.CreatePickerButton("\uE787", "Choose date", context);
        button.Flyout = flyout;
        panel.Children.Add(button);
        ElementTheme _calTheme = ElementTheme.Default;
        void ApplyDateTheme(ElementTheme? t = null)
        {
            _calTheme = t ?? EditorChrome.GetEffectiveTheme(button);
            calendar.RequestedTheme = _calTheme;
        }
        button.Loaded += (_, _) => ApplyDateTheme();
        button.ActualThemeChanged += (_, _) => ApplyDateTheme();
        flyout.Opening += (_, _) => ApplyCalendarTheme(calendar, _calTheme == ElementTheme.Default ? EditorChrome.GetEffectiveTheme(button) : _calTheme);
        context.ThemeChanged += t => ApplyDateTheme(t);

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
