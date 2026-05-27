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

sealed class TimeEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return context.Descriptor.PropertyType == typeof(TimeSpan)
            && !context.Descriptor.IsReadOnly;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var panel = new Grid { ColumnSpacing = 6 };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var initial = context.Descriptor.GetValue() is TimeSpan value ? value : TimeSpan.Zero;
        var textBox = new TextBox
        {
            Text = initial.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
            PlaceholderText = "HH:mm",
            MinHeight = 30,
            CornerRadius = new CornerRadius(0),
            BorderThickness = new Thickness(1)
        };
        Grid.SetColumn(textBox, 1);
        panel.Children.Add(textBox);
        textBox.Loaded += (_, _) => EditorChrome.ApplyTextBoxTheme(textBox);
        context.ThemeChanged += t => EditorChrome.ApplyTextBoxTheme(textBox, t);

        var selectedTime = initial;
        SpinPickerParts? hourPicker = null;
        SpinPickerParts? minutePicker = null;
        var timePickerPanel = new Grid
        {
            Width = 240,
            Height = 198,
            Padding = new Thickness(12),
            ColumnSpacing = 10
        };
        timePickerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        timePickerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timePickerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        hourPicker = CreateSpinPicker(
            24,
            selectedTime.Hours,
            value => selectedTime = new TimeSpan(value, selectedTime.Minutes, 0),
            ApplyPicker);
        timePickerPanel.Children.Add(hourPicker.Root);
        var separator = new TextBlock
        {
            Text = ":",
            FontSize = 26,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(separator, 1);
        timePickerPanel.Children.Add(separator);
        minutePicker = CreateSpinPicker(
            60,
            selectedTime.Minutes,
            value => selectedTime = new TimeSpan(selectedTime.Hours, value, 0),
            ApplyPicker);
        Grid.SetColumn(minutePicker.Root, 2);
        timePickerPanel.Children.Add(minutePicker.Root);

        var flyout = new Flyout { Content = timePickerPanel };
        var button = EditorChrome.CreatePickerButton("\uE823", "Choose time", context);
        button.Flyout = flyout;
        panel.Children.Add(button);
        button.Loaded += (_, _) => timePickerPanel.RequestedTheme = EditorChrome.GetEffectiveTheme(button);
        button.ActualThemeChanged += (_, _) => timePickerPanel.RequestedTheme = EditorChrome.GetEffectiveTheme(button);
        context.ThemeChanged += t => timePickerPanel.RequestedTheme = t;

        flyout.Opening += (_, _) =>
        {
            selectedTime = context.Descriptor.GetValue() is TimeSpan current ? current : initial;
            hourPicker?.SetValue(selectedTime.Hours);
            minutePicker?.SetValue(selectedTime.Minutes);
        };

        void ApplyPicker()
        {
            selectedTime = NormalizeTime(selectedTime);
            hourPicker?.SetValue(selectedTime.Hours);
            minutePicker?.SetValue(selectedTime.Minutes);
            textBox.Text = selectedTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            context.SetValue?.Invoke(selectedTime);
        }

        textBox.LostFocus += (_, _) => ApplyText();
        textBox.KeyDown += (_, args) =>
        {
            if (args.Key == Windows.System.VirtualKey.Enter)
                ApplyText();
        };

        return panel;

        void ApplyText()
        {
            if (!TimeSpan.TryParseExact(textBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var parsed))
            {
                var current = context.Descriptor.GetValue() is TimeSpan currentValue ? currentValue : initial;
                textBox.Text = current.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
                return;
            }

            selectedTime = parsed;
            ApplyPicker();
        }
    }

    static SpinPickerParts CreateSpinPicker(int modulus, int initialValue, Action<int> onValueChanged, Action apply)
    {
        const double rowHeight = 30;
        var root = new Grid
        {
            Width = 82,
            Height = rowHeight * 5
        };
        for (var i = 0; i < 5; i++)
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowHeight) });

        var highlight = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240))
        };
        Grid.SetRow(highlight, 2);
        root.Children.Add(highlight);

        var textBlocks = new List<TextBlock>(5);
        for (var i = 0; i < 5; i++)
        {
            var text = new TextBlock
            {
                FontSize = i == 2 ? 28 : 18,
                Opacity = i == 2 ? 1 : (i == 1 || i == 3 ? 0.65 : 0.35),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(text, i);
            textBlocks.Add(text);
            root.Children.Add(text);
        }

        var currentValue = Normalize(initialValue);
        var dragAccumulator = 0d;

        UpdateVisuals();

        root.PointerWheelChanged += (_, args) =>
        {
            if (args.GetCurrentPoint(root).Properties.MouseWheelDelta > 0)
                Step(-1);
            else
                Step(1);
            apply();
        };
        root.ManipulationMode = ManipulationModes.TranslateY;
        root.ManipulationDelta += (_, args) =>
        {
            dragAccumulator += args.Delta.Translation.Y;
            while (dragAccumulator >= rowHeight)
            {
                Step(-1);
                dragAccumulator -= rowHeight;
                apply();
            }
            while (dragAccumulator <= -rowHeight)
            {
                Step(1);
                dragAccumulator += rowHeight;
                apply();
            }
        };
        root.ManipulationCompleted += (_, _) => dragAccumulator = 0;
        root.KeyDown += (_, args) =>
        {
            if (args.Key == VirtualKey.Up)
            {
                Step(-1);
                apply();
            }
            else if (args.Key == VirtualKey.Down)
            {
                Step(1);
                apply();
            }
        };

        return new SpinPickerParts(root, SetValue);

        void Step(int delta)
        {
            currentValue = Normalize(currentValue + delta);
            onValueChanged(currentValue);
            UpdateVisuals();
        }

        void SetValue(int value)
        {
            currentValue = Normalize(value);
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            for (var offset = -2; offset <= 2; offset++)
            {
                var index = offset + 2;
                var value = Normalize(currentValue + offset);
                textBlocks[index].Text = value.ToString("00", CultureInfo.InvariantCulture);
            }
        }

        int Normalize(int value)
        {
            return ((value % modulus) + modulus) % modulus;
        }
    }

    static TimeSpan NormalizeTime(TimeSpan value)
    {
        var totalMinutes = ((int)value.TotalMinutes % (24 * 60) + (24 * 60)) % (24 * 60);
        return TimeSpan.FromMinutes(totalMinutes);
    }

    sealed class SpinPickerParts
    {
        public SpinPickerParts(Grid root, Action<int> setValue)
        {
            Root = root;
            SetValue = setValue;
        }

        public Grid Root { get; }
        public Action<int> SetValue { get; }
    }
}
