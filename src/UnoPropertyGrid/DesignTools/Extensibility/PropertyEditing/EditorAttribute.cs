namespace LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EditorAttribute(Type editorType, Type baseEditorType) : Attribute
{
    public Type EditorType { get; } = editorType;
    public Type BaseEditorType { get; } = baseEditorType;
}
