using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoPropertyGrid;

sealed class EnumPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.Enum;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Descriptor.PropertyType) ?? context.Descriptor.PropertyType;
        // Use string names rather than boxed enum values: WinUI 3 marshals boxed value types
        // through IReference<T>, causing the WinRT type name to appear as the display text.
        var comboBox = new ComboBox
        {
            ItemsSource = Enum.GetNames(type),
            SelectedItem = context.Value?.ToString(),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is string name)
                PropertyGridEditorProviderUtilities.Commit(context, Enum.Parse(type, name));
        };
        return comboBox;
    }
}
