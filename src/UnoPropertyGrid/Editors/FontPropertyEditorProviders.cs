using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoPropertyGrid;

sealed class FontFamilyPropertyEditorProvider : IPropertyGridEditorProvider
{
    readonly IReadOnlyList<string> _fontFamilies = PropertyGridEditorProviderUtilities.LoadSystemFontFamilies();

    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.FontFamily;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = _fontFamilies,
            SelectedItem = context.Value is Microsoft.UI.Xaml.Media.FontFamily family ? family.Source : context.Value?.ToString() ?? _fontFamilies.FirstOrDefault(),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        comboBox.SelectionChanged += (_, _) => PropertyGridEditorProviderUtilities.Commit(context, comboBox.SelectedItem);
        return comboBox;
    }
}

sealed class FontWeightPropertyEditorProvider : IPropertyGridEditorProvider
{
    static readonly (string Name, ushort Weight)[] _entries =
    [
        ("Thin",       100),
        ("ExtraLight", 200),
        ("Light",      300),
        ("Normal",     400),
        ("Medium",     500),
        ("SemiBold",   600),
        ("Bold",       700),
        ("ExtraBold",  800),
        ("Black",      900),
    ];

    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.FontWeight;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var currentName = PropertyGridEditorProviderUtilities.GetFontWeightName(context.Value);
        var comboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };

        foreach (var (name, weight) in _entries)
        {
            var item = new ComboBoxItem
            {
                Content = name,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = weight },
                Tag = name
            };
            comboBox.Items.Add(item);
            if (name == currentName)
            {
                comboBox.SelectedItem = item;
                comboBox.FontWeight = item.FontWeight;
            }
        }

        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is ComboBoxItem selected)
            {
                comboBox.FontWeight = selected.FontWeight;
                PropertyGridEditorProviderUtilities.Commit(context, selected.Tag);
            }
        };
        return comboBox;
    }
}

sealed class FontStylePropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.FontStyle;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = PropertyGridEditorProviderUtilities.FontStyles,
            SelectedItem = context.Value,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        comboBox.SelectionChanged += (_, _) => PropertyGridEditorProviderUtilities.Commit(context, comboBox.SelectedItem);
        return comboBox;
    }
}

sealed class FontStretchPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.FontStretch;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = PropertyGridEditorProviderUtilities.FontStretches,
            SelectedItem = context.Value,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        comboBox.SelectionChanged += (_, _) => PropertyGridEditorProviderUtilities.Commit(context, comboBox.SelectedItem);
        return comboBox;
    }
}
