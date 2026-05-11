using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnoPropertyGrid.Sample;

public sealed class ExperienceSettings : INotifyPropertyChanged
{
    DateTimeOffset _date = DateTimeOffset.Now.Date.AddDays(3);
    TimeSpan _time = new(18, 30, 0);
    double _volume = 42;
    string _city = "Toronto";

    public event PropertyChangedEventHandler? PropertyChanged;

    [Category("Schedule")]
    [Description("Uses a custom DatePicker editor.")]
    public DateTimeOffset Date
    {
        get => _date;
        set => SetField(ref _date, value);
    }

    [Category("Schedule")]
    [Description("Uses a custom TimePicker editor.")]
    public TimeSpan Time
    {
        get => _time;
        set => SetField(ref _time, value);
    }

    [Category("Playback")]
    [Description("Uses a slider editor with a live percentage readout.")]
    public double Volume
    {
        get => _volume;
        set => SetField(ref _volume, value);
    }

    [Category("Location")]
    [Description("Uses a compact map-style city selector.")]
    public string City
    {
        get => _city;
        set => SetField(ref _city, value);
    }

    bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
