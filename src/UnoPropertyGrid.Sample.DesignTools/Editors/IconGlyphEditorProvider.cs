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

sealed class IconGlyphEditorProvider : IPropertyGridEditorProvider
{
    static readonly GlyphOption[] Glyphs =
    [
        new("Favorite", "\uE734"),
        new("Home", "\uE80F"),
        new("Edit", "\uE70F"),
        new("Palette", "\uE790"),
        new("Settings", "\uE713")
    ];

    public bool CanEdit(PropertyGridEditorContext context)
    {
        return context.Component is FontIcon
            && context.Descriptor.Name == nameof(FontIcon.Glyph)
            && !context.Descriptor.IsReadOnly;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = Glyphs,
            DisplayMemberPath = nameof(GlyphOption.Name),
            SelectedItem = Glyphs.FirstOrDefault(g => g.Glyph == (context.Descriptor.GetValue() as string)) ?? Glyphs[0]
        };

        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is GlyphOption option)
                context.SetValue?.Invoke(option.Glyph);
        };

        return comboBox;
    }

    sealed record GlyphOption(string Name, string Glyph);
}
