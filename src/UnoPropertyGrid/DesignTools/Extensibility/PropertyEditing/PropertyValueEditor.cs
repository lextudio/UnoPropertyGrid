using Microsoft.UI.Xaml;
using UnoPropertyGrid;

namespace LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing;

public abstract class PropertyValueEditor : IPropertyGridEditorProvider
{
    public virtual bool CanEdit(PropertyGridEditorContext context)
    {
        return !context.Descriptor.IsReadOnly;
    }

    public abstract FrameworkElement CreateEditor(PropertyGridEditorContext context);
}
