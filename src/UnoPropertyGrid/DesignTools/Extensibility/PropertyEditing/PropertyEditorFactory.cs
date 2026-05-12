using UnoPropertyGrid;

namespace LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing;

public static class PropertyEditorFactory
{
    public static IPropertyGridEditorProvider? CreateEditorProvider(IEnumerable<Attribute> attributes)
    {
        var editorAttribute = attributes.OfType<EditorAttribute>().FirstOrDefault();
        if (editorAttribute == null)
            return null;

        if (!typeof(IPropertyGridEditorProvider).IsAssignableFrom(editorAttribute.BaseEditorType))
            return null;

        if (!typeof(IPropertyGridEditorProvider).IsAssignableFrom(editorAttribute.EditorType))
            return null;

        return Activator.CreateInstance(editorAttribute.EditorType, nonPublic: true) as IPropertyGridEditorProvider;
    }
}
