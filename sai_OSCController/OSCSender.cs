using SharpOSC;

public class OSCSender
{
    const string ip = "127.0.0.1"; // VRChatのIPアドレス
    const int port = 9000; // VRChatのOSC受信用ポート

    UDPSender oscSender;

    public OSCSender()
	{
        oscSender = new UDPSender(ip, port);
    }

	public void Send(List<OscMessage> messages)
	{
        foreach(var i in messages)
        {
            oscSender.Send(i);

            Console.WriteLine($"Send OSC : {i.Address} : {i.Arguments[0]}");
        }
    }

    public void Dispose()
    {
        oscSender.Close();
    }
}
