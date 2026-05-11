using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoPropertyGrid;

sealed class BooleanPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) is PropertyEditorKind.Boolean or PropertyEditorKind.ReadOnly
            && (Nullable.GetUnderlyingType(context.Descriptor.PropertyType) ?? context.Descriptor.PropertyType) == typeof(bool);
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var checkBox = new CheckBox
        {
            IsChecked = context.Value is bool value ? value : null,
            IsEnabled = !context.Descriptor.IsReadOnly
        };
        if (!context.Descriptor.IsReadOnly)
            checkBox.Checked += (_, _) => PropertyGridEditorProviderUtilities.Commit(context, true);
        if (!context.Descriptor.IsReadOnly)
            checkBox.Unchecked += (_, _) => PropertyGridEditorProviderUtilities.Commit(context, false);
        return checkBox;
    }
}
