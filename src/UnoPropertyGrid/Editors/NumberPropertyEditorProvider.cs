using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UnoPropertyGrid;

sealed class NumberPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.Number;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var textBox = new TextBox
        {
            Text = context.Value?.ToString() ?? string.Empty,
            InputScope = new InputScope
            {
                Names = { new InputScopeName(InputScopeNameValue.Number) }
            }
        };
        textBox.TextChanged += (_, _) => PropertyGridEditorProviderUtilities.Commit(context, string.IsNullOrWhiteSpace(textBox.Text) ? null : textBox.Text);
        return textBox;
    }
}
