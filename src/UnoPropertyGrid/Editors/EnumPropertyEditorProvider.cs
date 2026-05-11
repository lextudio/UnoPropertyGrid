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
        var comboBox = new ComboBox
        {
            ItemsSource = Enum.GetValues(type).Cast<object>().ToArray(),
            SelectedItem = context.Value,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        comboBox.SelectionChanged += (_, _) => PropertyGridEditorProviderUtilities.Commit(context, comboBox.SelectedItem);
        return comboBox;
    }
}
