using System.Reflection;

namespace UnoPropertyGrid;

public sealed class PropertyGridEventDescriptor
{
    readonly object _component;
    readonly EventInfo _event;

    public PropertyGridEventDescriptor(object component, EventInfo @event)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _event = @event ?? throw new ArgumentNullException(nameof(@event));
    }

    public string Name => _event.Name;
    public string DisplayName => _event.Name;
    public string Category => "Events";
    public string Description => string.Empty;
    public Type? HandlerType => _event.EventHandlerType;
    public Type? DeclaringType => _event.DeclaringType;
    public EventInfo EventInfo => _event;
    public object Component => _component;
}
