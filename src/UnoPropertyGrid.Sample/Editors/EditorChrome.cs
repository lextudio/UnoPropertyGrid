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

static class EditorChrome
{
    public static Button CreatePickerButton(string glyph, string tooltip)
    {
        var button = new Button
        {
            Width = 30,
            Height = 30,
            Padding = new Thickness(0),
            Content = new FontIcon
            {
                Glyph = glyph,
                FontSize = 14
            }
        };
        ToolTipService.SetToolTip(button, tooltip);
        return button;
    }
}
