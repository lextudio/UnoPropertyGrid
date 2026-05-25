using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoPropertyGrid.Sample.Aot;

public sealed partial class MainPage : Page
{
    readonly DeviceSettings _device = new();
    readonly NetworkConfig _network = new();

    public MainPage()
    {
        InitializeComponent();

        // Replace the default reflection-based provider with the source-generated
        // one. GeneratedPropertyGridDescriptors is emitted by UnoPropertyGrid.Generator
        // and contains typed lambda accessors — no reflection at property-discovery time.
        PropertyGrid.PropertyProvider = GeneratedPropertyGridDescriptors.CreateProvider();

        ObjectSelector.ItemsSource = new[]
        {
            new SampleObject("Device Settings", _device),
            new SampleObject("Network Config", _network),
        };
        ObjectSelector.SelectedIndex = 0;
    }

    void ObjectSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ObjectSelector.SelectedItem is SampleObject item)
            PropertyGrid.SelectedObject = item.Value;
    }

    sealed record SampleObject(string Name, object Value)
    {
        public override string ToString() => Name;
    }
}
