namespace UnoPropertyGrid;

public interface IPropertyGridPropertyProvider
{
    IEnumerable<PropertyGridPropertyDescriptor> GetProperties(object component);
}
