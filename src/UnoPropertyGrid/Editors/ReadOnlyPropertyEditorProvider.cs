using Microsoft.UI.Xaml;

namespace UnoPropertyGrid;

sealed class ReadOnlyPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.ReadOnly;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        return PropertyGridEditorProviderUtilities.CreateReadOnlyText(context);
    }
}
