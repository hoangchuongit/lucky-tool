using AdvancedSharpAdbClient.Models;
using System.ComponentModel;

public class Device : INotifyPropertyChanged
{
    private string serial;
    private string proxy;
    private string message;
    private DeviceState status;
    private bool isRunning;

    public string Serial { get => serial; set { if (serial != value) { serial = value; OnPropertyChanged(nameof(Serial)); } } }

    public string Proxy { get => proxy; set { if (proxy != value) { proxy = value; OnPropertyChanged(nameof(Proxy)); } } }

    public string Message { get => message; set { if (message != value) { message = value; OnPropertyChanged(nameof(Message)); } } }

    public DeviceState Status { get => status; set { if (status != value) { status = value; OnPropertyChanged(nameof(Status)); } } }

    public bool IsRunning { get => isRunning; set { if (isRunning != value) { isRunning = value; OnPropertyChanged(nameof(IsRunning)); } } }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
