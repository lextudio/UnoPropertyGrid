using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace UnoPropertyGrid;

sealed class BrushPropertyEditorProvider : IPropertyGridEditorProvider
{
    public bool CanEdit(PropertyGridEditorContext context)
    {
        return PropertyEditorKindExtensions.FromType(context.Descriptor.PropertyType, context.Descriptor.IsReadOnly) == PropertyEditorKind.Brush;
    }

    public FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var grid = new Grid { ColumnSpacing = 6 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var preview = new Border
        {
            Width = 18,
            Height = 18,
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            Child = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Fill = PropertyGridEditorProviderUtilities.GetBrushPreview(context.Value)
            }
        };
        grid.Children.Add(preview);

        var comboBox = new ComboBox
        {
            ItemsSource = PropertyGridEditorProviderUtilities.CommonBrushes,
            SelectedItem = PropertyGridEditorProviderUtilities.GetBrushName(context.Value),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetColumn(comboBox, 1);
        grid.Children.Add(comboBox);

        comboBox.SelectionChanged += (_, _) =>
        {
            var value = comboBox.SelectedItem as string;
            PropertyGridEditorProviderUtilities.Commit(context, value);
            if (preview.Child is Microsoft.UI.Xaml.Shapes.Rectangle rectangle)
                rectangle.Fill = PropertyGridEditorProviderUtilities.GetBrushPreview(context.Descriptor.GetValue());
        };
        return grid;
    }
}
