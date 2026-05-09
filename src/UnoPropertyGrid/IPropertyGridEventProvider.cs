namespace UnoPropertyGrid;

public interface IPropertyGridEventProvider
{
    IEnumerable<PropertyGridEventDescriptor> GetEvents(object component);
}
