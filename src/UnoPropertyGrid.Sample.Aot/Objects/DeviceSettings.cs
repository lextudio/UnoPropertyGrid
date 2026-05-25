using System.ComponentModel;
using System.Runtime.CompilerServices;

// Tell the source generator to produce AOT-safe descriptors for this type.
[assembly: UnoPropertyGrid.GeneratePropertyGridDescriptors(typeof(UnoPropertyGrid.Sample.Aot.DeviceSettings))]
[assembly: UnoPropertyGrid.GeneratePropertyGridDescriptors(typeof(UnoPropertyGrid.Sample.Aot.NetworkConfig))]

namespace UnoPropertyGrid.Sample.Aot;

public enum ConnectionProtocol { Http, Https, WebSocket }

public sealed class DeviceSettings : INotifyPropertyChanged
{
    string _hostName = "localhost";
    int _port = 443;
    bool _useTls = true;
    string _description = string.Empty;
    ConnectionProtocol _protocol = ConnectionProtocol.Https;
    double _timeout = 30.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    [Category("Network")]
    [Description("The hostname or IP address of the target device.")]
    public string HostName
    {
        get => _hostName;
        set => SetField(ref _hostName, value);
    }

    [Category("Network")]
    [Description("The TCP port number (1–65535).")]
    public int Port
    {
        get => _port;
        set => SetField(ref _port, value);
    }

    [Category("Network")]
    [Description("Enables TLS encryption for the connection.")]
    [DefaultValue(true)]
    public bool UseTls
    {
        get => _useTls;
        set => SetField(ref _useTls, value);
    }

    [Category("Network")]
    [Description("The transport protocol used to communicate with the device.")]
    [DefaultValue(ConnectionProtocol.Https)]
    public ConnectionProtocol Protocol
    {
        get => _protocol;
        set => SetField(ref _protocol, value);
    }

    [Category("Timing")]
    [Description("Request timeout in seconds.")]
    [DefaultValue(30.0)]
    public double Timeout
    {
        get => _timeout;
        set => SetField(ref _timeout, value);
    }

    [Category("General")]
    [Description("A free-form description for this device entry.")]
    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    [Category("General")]
    [Description("Auto-assigned serial number. Read-only.")]
    [ReadOnly(true)]
    public string SerialNumber { get; } = $"SN-{System.Guid.NewGuid():N}"[..12].ToUpperInvariant();

    [Browsable(false)]
    public string InternalToken { get; set; } = string.Empty;

    bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class NetworkConfig : INotifyPropertyChanged
{
    string _subnet = "192.168.1.0/24";
    int _maxConnections = 10;
    bool _enableDhcp = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    [Category("Addressing")]
    [Description("CIDR subnet for this network segment.")]
    public string Subnet
    {
        get => _subnet;
        set => SetField(ref _subnet, value);
    }

    [Category("Capacity")]
    [Description("Maximum concurrent connections allowed.")]
    [DefaultValue(10)]
    public int MaxConnections
    {
        get => _maxConnections;
        set => SetField(ref _maxConnections, value);
    }

    [Category("Addressing")]
    [Description("Enables DHCP address assignment.")]
    [DefaultValue(true)]
    public bool EnableDhcp
    {
        get => _enableDhcp;
        set => SetField(ref _enableDhcp, value);
    }

    bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
