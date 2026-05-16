using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

namespace UnoPropertyGrid;

static class PropertyGridEditorProviderUtilities
{
    public static readonly IReadOnlyList<string> CommonBrushes = ["No brush", "Transparent", "Black", "White", "Gray", "Red", "Green", "Blue", "Yellow"];
    public static readonly IReadOnlyList<string> FontWeights = ["Thin", "ExtraLight", "Light", "Normal", "Medium", "SemiBold", "Bold", "ExtraBold", "Black"];
    public static readonly IReadOnlyList<object> FontStyles = Enum.GetValues(typeof(FontStyle)).Cast<object>().ToArray();
    public static readonly IReadOnlyList<object> FontStretches = Enum.GetValues(typeof(FontStretch)).Cast<object>().ToArray();

    public static FrameworkElement CreateReadOnlyText(PropertyGridEditorContext context)
    {
        return new TextBlock
        {
            Text = context.Value?.ToString() ?? "(null)",
            Opacity = 0.9,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
    }

    public static void Commit(PropertyGridEditorContext context, object? value)
    {
        if (context.SetValue != null)
            context.SetValue(value);
        else
            context.Descriptor.SetValue(value);
        context.Value = context.Descriptor.GetValue();
    }

    public static string GetBrushName(object? value)
    {
        if (value == null)
            return "No brush";
        if (TryGetBrushColor(value, out var color))
            return GetColorName(color);
        return value.ToString() ?? "Custom brush";
    }

    public static Brush GetBrushPreview(object? value)
    {
        if (value is Brush brush)
            return brush;

        if (TryGetBrushColor(value, out var color))
            return new SolidColorBrush(color);

        return new SolidColorBrush(Colors.Transparent);
    }

    static bool TryGetBrushColor(object? value, out Color color)
    {
        color = default;
        if (value == null)
            return false;

        if (value is SolidColorBrush solidColorBrush)
        {
            color = solidColorBrush.Color;
            return true;
        }

        var type = value.GetType();
        if (type.FullName == "Windows.UI.Xaml.Media.SolidColorBrush" || type.FullName == "Microsoft.UI.Xaml.Media.SolidColorBrush")
        {
            var colorProperty = type.GetProperty("Color");
            if (colorProperty?.GetValue(value) is Color brushColor)
            {
                color = brushColor;
                return true;
            }

            if (colorProperty?.GetValue(value) is { } colorValue)
            {
                return TryGetColorFromObject(colorValue, out color);
            }
        }

        return false;
    }

    static bool TryGetColorFromObject(object value, out Color color)
    {
        color = default;
        var type = value.GetType();
        if (type.FullName == "Windows.UI.Color" || type.FullName == "Microsoft.UI.Color")
        {
            var a = (byte)(type.GetProperty("A")?.GetValue(value) ?? 0);
            var r = (byte)(type.GetProperty("R")?.GetValue(value) ?? 0);
            var g = (byte)(type.GetProperty("G")?.GetValue(value) ?? 0);
            var b = (byte)(type.GetProperty("B")?.GetValue(value) ?? 0);
            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        return false;
    }

    public static string GetFontWeightName(object? value)
    {
        if (value is FontWeight weight)
            return GetFontWeightName(weight.Weight);

        if (value?.GetType().FullName == "Microsoft.UI.Text.FontWeight")
        {
            var weightValue = (ushort?)(value.GetType().GetProperty("Weight")?.GetValue(value));
            return GetFontWeightName(weightValue ?? 400);
        }

        return "Normal";
    }

    static string GetFontWeightName(ushort weight) => weight switch
    {
        100 => "Thin",
        200 => "ExtraLight",
        300 => "Light",
        400 => "Normal",
        500 => "Medium",
        600 => "SemiBold",
        700 => "Bold",
        800 => "ExtraBold",
        900 => "Black",
        _ => weight.ToString()
    };

    public static IReadOnlyList<string> LoadSystemFontFamilies()
    {
        return TryLoadSkiaFontFamilies()
            ?? TryLoadSystemDrawingFontFamilies()
            ?? DefaultFontFamilies;
    }

    static readonly IReadOnlyList<string> DefaultFontFamilies =
    [
        "Consolas",
        "Menlo",
        "DejaVu Sans Mono",
        "Liberation Mono",
        "Monospace",
        "Courier New",
        "Courier",
        "Segoe UI",
        "Arial",
        "Calibri",
        "Georgia",
        "Tahoma",
        "Verdana",
        "Times New Roman"
    ];

    static IReadOnlyList<string>? TryLoadSkiaFontFamilies()
    {
        try
        {
            var skType = Type.GetType("SkiaSharp.SKFontManager, SkiaSharp") ?? Type.GetType("SkiaSharp.SKFontManager");
            if (skType == null)
                return null;

            var manager = skType.GetProperty("Default", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
            if (manager == null)
                return null;

            var familiesObj = skType.GetMethod("GetFontFamilies", Type.EmptyTypes)?.Invoke(manager, null)
                ?? skType.GetProperty("FontFamilies")?.GetValue(manager);
            return BuildSortedStringList(familiesObj);
        }
        catch
        {
            return null;
        }
    }

    static IReadOnlyList<string>? TryLoadSystemDrawingFontFamilies()
    {
        try
        {
            var type = Type.GetType("System.Drawing.Text.InstalledFontCollection, System.Drawing.Common")
                ?? Type.GetType("System.Drawing.Text.InstalledFontCollection");
            if (type == null)
                return null;

            var instance = Activator.CreateInstance(type);
            var families = type.GetProperty("Families")?.GetValue(instance) as Array;
            if (families == null)
                return null;

            var list = new List<string>();
            foreach (var family in families)
            {
                var name = family.GetType().GetProperty("Name")?.GetValue(family) as string;
                if (!string.IsNullOrWhiteSpace(name) && !list.Contains(name))
                    list.Add(name);
            }

            return SortOrNull(list);
        }
        catch
        {
            return null;
        }
    }

    static IReadOnlyList<string>? BuildSortedStringList(object? values)
    {
        if (values is not System.Collections.IEnumerable enumerable)
            return null;

        var list = new List<string>();
        foreach (var value in enumerable)
        {
            if (value is string text && !string.IsNullOrWhiteSpace(text) && !list.Contains(text))
                list.Add(text);
        }

        return SortOrNull(list);
    }

    static IReadOnlyList<string>? SortOrNull(List<string> list)
    {
        if (list.Count == 0)
            return null;
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }

    static string GetColorName(Color color)
    {
        if (color == Colors.Transparent)
            return "Transparent";
        if (color == Colors.Black)
            return "Black";
        if (color == Colors.White)
            return "White";
        if (color == Colors.Red)
            return "Red";
        if (color == Colors.Green)
            return "Green";
        if (color == Colors.Blue)
            return "Blue";
        if (color == Colors.Yellow)
            return "Yellow";
        if (color == Colors.Gray)
            return "Gray";
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
