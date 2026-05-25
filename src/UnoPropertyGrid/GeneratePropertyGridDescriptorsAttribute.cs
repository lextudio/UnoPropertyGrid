namespace UnoPropertyGrid;

/// <summary>
/// Marks a type for AOT-safe property grid descriptor code generation.
/// Place this attribute at the assembly level once per target type.
/// The source generator in <c>UnoPropertyGrid.Generator</c> will emit a
/// <c>GeneratedPropertyGridDescriptors</c> class containing a
/// <c>CreateProvider()</c> factory method that returns a
/// <see cref="LambdaPropertyProvider"/> pre-configured for every annotated type.
/// </summary>
/// <example>
/// <code>
/// [assembly: GeneratePropertyGridDescriptors(typeof(MyControl))]
/// [assembly: GeneratePropertyGridDescriptors(typeof(MySettings))]
/// </code>
/// In startup:
/// <code>
/// PropertyGrid.PropertyProvider = GeneratedPropertyGridDescriptors.CreateProvider();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class GeneratePropertyGridDescriptorsAttribute : Attribute
{
    public GeneratePropertyGridDescriptorsAttribute(Type type)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>The component type for which descriptors will be generated.</summary>
    public Type Type { get; }
}
