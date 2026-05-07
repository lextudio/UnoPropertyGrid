namespace UnoPropertyGrid;

public enum PropertyEditorKind
{
    Boolean,
    Text,
    Number,
    Enum,
    ReadOnly
}

public static class PropertyEditorKindExtensions
{
    public static PropertyEditorKind FromType(Type type, bool isReadOnly)
    {
        if (isReadOnly)
            return PropertyEditorKind.ReadOnly;

        var baseType = Nullable.GetUnderlyingType(type) ?? type;
        if (baseType == typeof(bool))
            return PropertyEditorKind.Boolean;
        if (baseType == typeof(string) || baseType == typeof(char))
            return PropertyEditorKind.Text;
        if (baseType.IsEnum)
            return PropertyEditorKind.Enum;
        if (IsNumeric(baseType))
            return PropertyEditorKind.Number;

        return PropertyEditorKind.ReadOnly;
    }

    static bool IsNumeric(Type type)
    {
        return type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }
}
