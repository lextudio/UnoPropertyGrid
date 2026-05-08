using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoPropertyGrid;

public sealed class PropertyItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? GroupHeaderTemplate { get; set; }
    public DataTemplate? PropertyTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
        => item is PropertyGroupHeader ? GroupHeaderTemplate! : PropertyTemplate!;

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        => SelectTemplateCore(item);
}
