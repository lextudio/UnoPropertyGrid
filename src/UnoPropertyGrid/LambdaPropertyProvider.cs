namespace UnoPropertyGrid;

/// <summary>
/// AOT-safe property provider that uses pre-registered typed lambda factories
/// instead of runtime reflection. Register one factory per component type, then
/// set this as <see cref="PropertyGridControl.PropertyProvider"/>.
/// </summary>
public sealed class LambdaPropertyProvider : IPropertyGridPropertyProvider
{
    readonly Dictionary<Type, Func<object, IEnumerable<PropertyGridPropertyDescriptor>>> _factories = new();

    /// <summary>
    /// Registers a descriptor factory for <typeparamref name="T"/>.
    /// Returns <c>this</c> so calls can be chained.
    /// </summary>
    public LambdaPropertyProvider Register<T>(Func<T, IEnumerable<PropertyGridPropertyDescriptor>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[typeof(T)] = component => factory((T)component);
        return this;
    }

    /// <inheritdoc/>
    public IEnumerable<PropertyGridPropertyDescriptor> GetProperties(object component)
    {
        ArgumentNullException.ThrowIfNull(component);
        return _factories.TryGetValue(component.GetType(), out var factory)
            ? factory(component)
            : [];
    }
}
