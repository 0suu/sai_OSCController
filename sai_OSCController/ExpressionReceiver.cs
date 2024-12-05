using System.Net.Sockets;
using System.Net;
using System.Text;

namespace sai_OSCController
{
    public class ExpressionReceiver
    {
        UdpClient udpClient;
        int port = 31313;
        string receivedString = "";

        FacialManager? facialManager = null;

        public Dictionary<string, float> ReceivedValue = new()
        {
            { "browDown_L", 0f },
            { "browDown_R", 0f },
            { "browInnerUp_L", 0f },
            { "browInnerUp_R", 0f },
            { "browOuterUp_L", 0f },
            { "browOuterUp_R", 0f },
            { "cheekPuff_L", 0f },
            { "cheekPuff_R", 0f },
            { "cheekSquint_L", 0f },
            { "cheekSquint_R", 0f },
            { "eyeBlink_L", 0f },
            { "eyeBlink_R", 0f },
            { "eyeLookDown_L", 0f },
            { "eyeLookDown_R", 0f },
            { "eyeLookIn_L", 0f },
            { "eyeLookIn_R", 0f },
            { "eyeLookOut_L", 0f },
            { "eyeLookOut_R", 0f },
            { "eyeLookUp_L", 0f },
            { "eyeLookUp_R", 0f },
            { "eyeSquint_L", 0f },
            { "eyeSquint_R", 0f },
            { "eyeWide_L", 0f },
            { "eyeWide_R", 0f },
            { "jawForward",0f},
            { "jawLeft", 0f},
            { "jawOpen", 0f},
            { "jawRight", 0f},
            { "mouthClose", 0f},
            { "mouthDimple_L", 0f },
            { "mouthDimple_R", 0f },
            { "mouthFrown_L", 0f },
            { "mouthFrown_R", 0f },
            { "mouthFunnel", 0f},
            { "mouthLeft", 0f},
            { "mouthLowerDown_L", 0f},
            { "mouthLowerDown_R", 0f},
            { "mouthPress_L", 0f },
            { "mouthPress_R", 0f },
            { "mouthPucker", 0f},
            { "mouthRight", 0f},
            { "mouthRollLower", 0f},
            { "mouthRollUpper", 0f},
            { "mouthShrugLowe", 0f},
            { "mouthShrugUppe", 0f},
            { "mouthSmile_L", 0f },
            { "mouthSmile_R", 0f },
            { "mouthStretch_L", 0f },
            { "mouthStretch_R", 0f },
            { "mouthUpperUp_L", 0f },
            { "mouthUpperUp_R", 0f },
            { "noseSneer_L", 0f },
            { "noseSneer_R", 0f },
            { "Rotation_X", 0f },
            { "Rotation_Y", 0f },
            { "Rotation_Z", 0f },
            { "Rotation_W", 0f },
            { "TransrationX", 0f },
            { "TransrationY", 0f },
            { "TransrationZ", 0f },
        };

        public ExpressionReceiver()
        {
            Console.WriteLine("Start ExpressionReceiver");

            facialManager = new();

            udpClient = new UdpClient(port);

            udpClient.BeginReceive(ReceiveCallback, null);
        }

        void Update()
        {
            if (receivedString != "")
            {
                var stringArr = receivedString.Split(',');

                var floatArr = Array.ConvertAll(stringArr, float.Parse);

                for (int i = 0; i < ReceivedValue.Keys.Count; i++)
                {
                    var key = ReceivedValue.Keys.ElementAt(i);
                    ReceivedValue[key] = floatArr[i];
                }

                facialManager.Update(ReceivedValue);

                receivedString = "";
            }
        }

        public void OnClosed()
        {
            udpClient.Close();
            Console.WriteLine("Close udp");
        }

        void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                IPEndPoint remoteEP = null;
                byte[] rcvBytes = udpClient.EndReceive(ar, ref remoteEP);

                byte[] lenBytes = new byte[4];
                Array.Copy(rcvBytes, 0, lenBytes, 0, 4);
                byte[] strBytes = new byte[rcvBytes.Length - 4];
                Array.Copy(rcvBytes, 4, strBytes, 0, rcvBytes.Length - 4);

                uint len = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenBytes));

                string str = Encoding.UTF8.GetString(strBytes);

                receivedString = str;
                Update();

                udpClient.BeginReceive(ReceiveCallback, null);
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
            }
        }
    }
}