using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#if DEBUG
using LeXtudio.DevFlow.Agent.Uno;
using Microsoft.Maui.DevFlow.Agent.Core;
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
        _devFlowAgent = new UnoAgentService(new AgentOptions { Port = 5500 });
        _devFlowAgent.Start();
#endif
        var window = new Window();
        window.Content = new Frame();

        if (window.Content is Frame frame)
            frame.Navigate(typeof(MainPage));

        window.Activate();
    }
}
