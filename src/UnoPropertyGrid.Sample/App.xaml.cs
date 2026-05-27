using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#if DEBUG
using LeXtudio.DevFlow.Agent.Uno;
#endif

namespace UnoPropertyGrid.Sample;

public partial class App : Application
{
#if DEBUG
    private UnoAgentService? _devFlowAgent;
#endif

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
#if DEBUG
        _devFlowAgent = new UnoAgentService();
        _devFlowAgent.Start();
#endif
        var window = new Window();
        window.Content = new Frame();

        if (window.Content is Frame frame)
            frame.Navigate(typeof(MainPage));

        window.Activate();
    }
}
