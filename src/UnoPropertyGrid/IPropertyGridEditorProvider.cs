using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace UnoPropertyGrid;

public interface IPropertyGridEditorProvider
{
    bool CanEdit(PropertyGridEditorContext context);
    FrameworkElement CreateEditor(PropertyGridEditorContext context);
}

public sealed class PropertyGridEditorContext
{
    public required object Component { get; init; }
    public required PropertyGridPropertyDescriptor Descriptor { get; init; }
    public object? Value { get; set; }
    public BindingMode BindingMode { get; init; } = BindingMode.TwoWay;
    public IServiceProvider? Services { get; init; }
}
