using SB;
using SharpOSC;
using System.Windows.Threading;

public class MeterDataReceiver
{
    event Action<string> OnReciveOutputEvent;

    public string DeviceID => Environment.GetEnvironmentVariable("sai-osc-SBMeterID", EnvironmentVariableTarget.Machine);

    DispatcherTimer _timer1 = null;

    const int updateDeviceInterval = 60; // 30秒ごと

    SwitchBotAPICall switchBotAPICall = null;

    const string humidityAddress = "/avatar/parameters/Humidity";
    const string temperatureAddress = "/avatar/parameters/Temperature";

    float tempMin = 0;
    float tempMax = 50;

    int humidity = 0;
    float temperature = 0;

    public MeterDataReceiver()
    {
        Start();
    }

    public List<OscMessage>? GetSendData()
    {
        if(humidity == 0 || temperature == 0)
        {
            return null;
        }

        List<OscMessage> sendData = new();

        sendData.Add(new OscMessage(humidityAddress, (float)humidity / 100));
        sendData.Add(new OscMessage(temperatureAddress, (float)(temperature / 50)));

        return sendData;
    }

    void GetData()
    {
        switchBotAPICall.GetDeviceStatus(DeviceID, OnReciveOutputEvent);
    }

    void Start()
    {
        if(DeviceID == null || DeviceID.Length == 0)
        {
            Console.WriteLine("MeterIDは Null or Empty");
            return;
        }

        switchBotAPICall = new();

        OnReciveOutputEvent += OnReceveOutput;

        // 初回取得
        GetData();

        _timer1 = new();
        _timer1.Interval = TimeSpan.FromSeconds(updateDeviceInterval);
        _timer1.Tick += (sender, e) =>
        {
            GetData();
        };
        _timer1.Start();
    }

    void OnReceveOutput(string output)
    {
        var humidity = output.Substring(output.IndexOf("humidity") + 10, 2);
        var temperature = output.Substring(output.IndexOf("temperature") + 13, 4);

        Console.WriteLine("humidity : " + humidity);
        Console.WriteLine("temperature : " + temperature);

        this.humidity = int.Parse(humidity);
        this.temperature = float.Parse(temperature);
    }
}
