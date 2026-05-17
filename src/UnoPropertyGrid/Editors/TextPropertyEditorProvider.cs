using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TextBox = LeXtudio.UI.Controls.TextBox;

namespace UnoPropertyGrid;

sealed class TextPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.Text;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var textBox = new TextBox
        {
            Text = context.Value?.ToString() ?? string.Empty
        };
        textBox.TextChanged += (_, _) => PropertyGridEditorProviderUtilities.Commit(context, textBox.Text);
        return textBox;
    }
}
