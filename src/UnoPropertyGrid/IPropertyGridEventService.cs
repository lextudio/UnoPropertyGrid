using System.Reflection;

namespace UnoPropertyGrid;

public interface IPropertyGridEventService
{
    string? GetHandlerName(object component, EventInfo eventInfo);
    Task SetHandlerNameAsync(object component, EventInfo eventInfo, string? handlerName);
    Task NavigateToHandlerAsync(object component, EventInfo eventInfo);
}
