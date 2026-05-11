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
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.FontWeight;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = PropertyGridEditorProviderUtilities.FontWeights,
            SelectedItem = PropertyGridEditorProviderUtilities.GetFontWeightName(context.Value),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        comboBox.SelectionChanged += (_, _) => PropertyGridEditorProviderUtilities.Commit(context, comboBox.SelectedItem);
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
