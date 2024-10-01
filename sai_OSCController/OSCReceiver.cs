//using Cysharp.Threading.Tasks;
using SharpOSC;

public class OSCReceiver
{
    public OSCReceiver()
    {
        Start();
    }

    public void Start()
    {
        Console.WriteLine("Start OSCReceiver");

        //RunOSCListenerAsync().Forget();
    }
    /*
    async UniTaskVoid RunOSCListenerAsync()
    {
        HandleOscPacket callback = delegate (OscPacket packet)
        {
            HandleMessage((OscMessage)packet);
        };

        var listener = new UDPListener(9001, callback);

        Console.WriteLine("Listening for OSC messages...");
    }
    */
    void HandleMessage(OscMessage message)
    {
        if (message == null)
        {
            return;
        }

        if (!message.Address.Contains("Button"))
        {
            return;
        }

        Console.WriteLine("------------------------");

        message.Arguments.ForEach(arg =>
        {
            Console.WriteLine($"[{message.Address}] : -> {arg}");
        });
    }
}
