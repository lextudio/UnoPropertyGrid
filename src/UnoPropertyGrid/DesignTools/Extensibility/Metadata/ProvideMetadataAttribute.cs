namespace LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.Metadata;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ProvideMetadataAttribute(Type metadataProviderType) : Attribute
{
    public Type MetadataProviderType { get; } = metadataProviderType;
}
