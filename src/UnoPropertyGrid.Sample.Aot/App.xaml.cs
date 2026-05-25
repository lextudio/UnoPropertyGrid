using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoPropertyGrid.Sample.Aot;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new Window();
        window.Content = new Frame();

        if (window.Content is Frame frame)
            frame.Navigate(typeof(MainPage));

        window.Activate();
    }
}
