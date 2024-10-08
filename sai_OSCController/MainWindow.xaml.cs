using System.Windows;
using System.Windows.Threading;
using Valve.VR;

namespace sai_OSCController
{
    public partial class MainWindow : Window
    {
        OSCReceiver? oSCReceiver = null;

        MeterDataReceiver? meterDataReceiver = null;

        BatteryDataReceiver? batteryDataReceiver = null;

        OSCSender? oSCSender = null;

        public static MainWindow? Instance { get; set; }

        DispatcherTimer? _timer1 = null;

        int updateDeviceInterval = 10;

        bool disposed = false;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            EVRInitError error = EVRInitError.None;
            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Overlay);

            oSCReceiver = new();
            oSCSender = new();

            meterDataReceiver = new();
            batteryDataReceiver = new();

            _timer1 = new();
            _timer1.Interval = TimeSpan.FromSeconds(updateDeviceInterval);
            _timer1.Tick += (sender, e) =>
            {
                OSCSend();
            };
            _timer1.Start();

            this.Closed += OnWindowClosed;
        }

        void OSCSend()
        {
            var batteryData = batteryDataReceiver.GetSendData();
            var meterData = meterDataReceiver.GetSendData();
            oSCSender.Send(batteryData);

            if (meterData != null)
            {
                oSCSender.Send(meterData);
            }
        }

        public void OnWindowClosed(object? sender, EventArgs? e)
        {
            if (!disposed)
            {
                Console.WriteLine("Dispose");
                oSCSender.Dispose();
                OpenVR.Shutdown();
                disposed = true;
            }
        }
    }
}