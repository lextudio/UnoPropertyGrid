using Microsoft.UI.Xaml;

namespace UnoPropertyGrid;

sealed class BuiltInPropertyEditorProvider : IPropertyGridEditorProvider
{
    readonly IReadOnlyList<IPropertyGridEditorProvider> _providers =
    [
        new BooleanPropertyEditorProvider(),
        new TextPropertyEditorProvider(),
        new NumberPropertyEditorProvider(),
        new ThicknessPropertyEditorProvider(),
        new EnumPropertyEditorProvider(),
        new BrushPropertyEditorProvider(),
        new CornerRadiusPropertyEditorProvider(),
        new FontFamilyPropertyEditorProvider(),
        new FontWeightPropertyEditorProvider(),
        new FontStylePropertyEditorProvider(),
        new FontStretchPropertyEditorProvider(),
        new ReadOnlyPropertyEditorProvider()
    ];

    public bool CanEdit(PropertyGridEditorContext context)
    {
        return _providers.Any(provider => provider.CanEdit(context));
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        foreach (var provider in _providers)
        {
            if (provider.CanEdit(context))
                return provider.CreateEditor(context);
        }

        return PropertyGridEditorProviderUtilities.CreateReadOnlyText(context);
    }
}
