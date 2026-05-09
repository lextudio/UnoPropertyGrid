namespace UnoPropertyGrid;

public enum PropertyEditorKind
{
    Boolean,
    Text,
    Number,
    Enum,
    Brush,
    FontFamily,
    FontWeight,
    FontStyle,
    FontStretch,
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
        if (typeof(Microsoft.UI.Xaml.Media.Brush).IsAssignableFrom(baseType))
            return PropertyEditorKind.Brush;
        if (baseType == typeof(Microsoft.UI.Xaml.Media.FontFamily))
            return PropertyEditorKind.FontFamily;
        if (baseType == typeof(Windows.UI.Text.FontWeight))
            return PropertyEditorKind.FontWeight;
        if (baseType == typeof(Windows.UI.Text.FontStyle))
            return PropertyEditorKind.FontStyle;
        if (baseType == typeof(Windows.UI.Text.FontStretch))
            return PropertyEditorKind.FontStretch;
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
