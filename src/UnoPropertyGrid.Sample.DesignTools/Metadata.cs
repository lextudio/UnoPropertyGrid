using System.ComponentModel;
using LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.Metadata;
using LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing;
using EditorAttribute = LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing.EditorAttribute;

[assembly: ProvideMetadata(typeof(UnoPropertyGrid.Sample.DesignTools.Metadata))]

namespace UnoPropertyGrid.Sample.DesignTools;

public sealed class Metadata : IProvideAttributeTable
{
    public AttributeTable AttributeTable
    {
        get
        {
            var builder = new AttributeTableBuilder();

            builder.AddCustomAttributes(
                "UnoPropertyGrid.Sample.ExperienceSettings",
                "Date",
                new CategoryAttribute("Schedule"),
                new DescriptionAttribute("Uses a metadata-registered date editor."),
                new EditorAttribute(typeof(DateEditorProvider), typeof(IPropertyGridEditorProvider)));

            builder.AddCustomAttributes(
                "UnoPropertyGrid.Sample.ExperienceSettings",
                "Time",
                new CategoryAttribute("Schedule"),
                new DescriptionAttribute("Uses a metadata-registered time editor."),
                new EditorAttribute(typeof(TimeEditorProvider), typeof(IPropertyGridEditorProvider)));

            builder.AddCustomAttributes(
                "UnoPropertyGrid.Sample.ExperienceSettings",
                "Volume",
                new EditorAttribute(typeof(VolumeEditorProvider), typeof(IPropertyGridEditorProvider)));

            builder.AddCustomAttributes(
                "UnoPropertyGrid.Sample.ExperienceSettings",
                "City",
                new EditorAttribute(typeof(CityMapEditorProvider), typeof(IPropertyGridEditorProvider)));

            return builder.CreateTable();
        }
    }
}
