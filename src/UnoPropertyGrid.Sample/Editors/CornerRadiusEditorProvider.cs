using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.System;
using Windows.UI;

namespace UnoPropertyGrid.Sample;

sealed class CornerRadiusEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return context.Descriptor.PropertyType == typeof(CornerRadius)
            && !context.Descriptor.IsReadOnly;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var value = context.Descriptor.GetValue() is CornerRadius radius ? radius : new CornerRadius();
        return ThicknessEditorProvider.CreateFourValueEditor(value.TopLeft, value.TopRight, value.BottomRight, value.BottomLeft, SetValue);

        void SetValue(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            context.SetValue?.Invoke(new CornerRadius(topLeft, topRight, bottomRight, bottomLeft));
        }
    }
}
