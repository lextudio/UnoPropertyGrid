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

namespace UnoPropertyGrid.Sample.DesignTools;

static class EditorChrome
{
    static void ApplyButtonTheme(Button button, ElementTheme? theme = null)
    {
        var isDark = (theme ?? GetEffectiveTheme(button)) == ElementTheme.Dark;
        button.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(isDark
            ? Color.FromArgb(255, 0x2D, 0x2D, 0x30)
            : Color.FromArgb(255, 0xE8, 0xE8, 0xE8));
        button.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(isDark
            ? Color.FromArgb(255, 0x3F, 0x3F, 0x46)
            : Color.FromArgb(255, 0xD0, 0xD0, 0xD0));
        button.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Colors.Transparent);
    }

    public static void ApplyTextBoxTheme(Control textBox, ElementTheme? theme = null)
    {
        var isDark = (theme ?? GetEffectiveTheme(textBox)) == ElementTheme.Dark;
        var fg = new SolidColorBrush(isDark ? Color.FromArgb(255, 0xD4, 0xD4, 0xD4) : Color.FromArgb(255, 0x1E, 0x1E, 0x1E));
        var bg = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x25, 0x25, 0x26) : Color.FromArgb(255, 0xFF, 0xFF, 0xFF));
        var border = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x3F, 0x3F, 0x46) : Color.FromArgb(255, 0xCC, 0xCC, 0xCC));
        var muted = new SolidColorBrush(isDark ? Color.FromArgb(255, 0x8A, 0x8A, 0x8A) : Color.FromArgb(255, 0x5F, 0x5F, 0x5F));
        textBox.Foreground = fg;
        textBox.Background = bg;
        textBox.BorderBrush = border;
        textBox.Resources["TextControlBackground"] = bg;
        textBox.Resources["TextControlBackgroundPointerOver"] = bg;
        textBox.Resources["TextControlBackgroundFocused"] = bg;
        textBox.Resources["TextControlForeground"] = fg;
        textBox.Resources["TextControlForegroundPointerOver"] = fg;
        textBox.Resources["TextControlForegroundFocused"] = fg;
        textBox.Resources["TextControlPlaceholderForeground"] = muted;
        textBox.Resources["TextControlBorderBrush"] = border;
        textBox.Resources["TextControlBorderBrushPointerOver"] = border;
        textBox.Resources["TextControlBorderBrushFocused"] = new SolidColorBrush(Color.FromArgb(255, 0x00, 0x78, 0xD4));
    }

    public static ElementTheme GetEffectiveTheme(FrameworkElement element)
    {
        DependencyObject? node = element;
        while (node != null)
        {
            if (node is FrameworkElement fe && fe.RequestedTheme != ElementTheme.Default)
                return fe.RequestedTheme;
            node = VisualTreeHelper.GetParent(node);
        }
        return Application.Current.RequestedTheme == ApplicationTheme.Dark
            ? ElementTheme.Dark : ElementTheme.Light;
    }

    public static Button CreatePickerButton(string glyph, string tooltip, PropertyGridEditorContext? context = null)
    {
        var button = new Button
        {
            Width = 30,
            Height = 30,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Content = new FontIcon
            {
                Glyph = glyph,
                FontSize = 14
            }
        };
        ToolTipService.SetToolTip(button, tooltip);
        button.Loaded += (_, _) => ApplyButtonTheme(button);
        button.ActualThemeChanged += (_, _) => ApplyButtonTheme(button);
        if (context != null)
            context.ThemeChanged += t => ApplyButtonTheme(button, t);
        return button;
    }
}
