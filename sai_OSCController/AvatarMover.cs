using SharpOSC;
using Cysharp.Threading.Tasks;

public class AvatarMover
{
    public class MoveData
    {
        public InputType Type { get; set; }
        public bool IsMoving { get; set; }

        public MoveData(InputType type, bool isMoving)
        {
            Type = type;
            IsMoving = isMoving;
        }
    }


    OSCSender? oscSender = null;

    public enum InputType
    {
        // 移動
        MoveForward,
        MoveBackward,
        MoveRight,
        MoveLeft,
        Jump,

        // 視点の向き
        LookRight,
        LookLeft,
        ComfortRight, // VR Only
        ComfortLeft, // VR Only

        Voice,

        Unknown,
    }

    readonly Dictionary<InputType, string> inputTypeAddress = new()
    {
        { InputType.MoveForward, "/input/MoveForward" },
        { InputType.MoveBackward, "/input/MoveBackward" },
        { InputType.MoveRight, "/input/MoveRight" },
        { InputType.MoveLeft, "/input/MoveLeft" },

        { InputType.Jump, "/input/Jump" },

        { InputType.LookRight, "/input/LookRight" },
        { InputType.LookLeft, "/input/LookLeft" },
        { InputType.ComfortRight, "/input/ComfortRight" }, // VR Only
        { InputType.ComfortLeft, "/input/ComfortLeft" },  // VR Only

        { InputType.Voice, "/input/Voice" },
    };

    bool allStopFlag = false;

    List<MoveData> moveDatas = new();

    public AvatarMover(OSCSender sender)
    {
        oscSender = sender;

        foreach (var i in inputTypeAddress.Keys)
        {
            moveDatas.Add(new MoveData(i, false));
        }
    }

    public void Move(InputType type, float value)
    {
        if (type == InputType.Unknown) return;

        if (type is InputType.Jump or InputType.Voice)
        {
            OnceAction(type).Forget();
            return;
        }

        // 動いているかどうかを設定
        var data = moveDatas.Find(x => x.Type == type);
        data.IsMoving = value == 1 ? true : false;

        if (value == 1)
        {
            MoveAsync(data).Forget();
        }
    }

    async UniTask MoveAsync(MoveData data)
    {
        List<OscMessage> sendData = new();
        sendData.Add(new OscMessage(inputTypeAddress[data.Type], 1));

        while (data.IsMoving && !allStopFlag)
        {
            oscSender.Send(sendData);

            await UniTask.Yield();
        }

        sendData[0].Arguments[0] = 0;
        oscSender.Send(sendData);
    }

    async UniTask OnceAction(InputType type)
    {
        List<OscMessage> oscMessages = new();
        oscMessages.Add(new OscMessage(inputTypeAddress[type], 1));
        oscSender.Send(oscMessages);

        await Task.Delay(1000);

        oscMessages.Clear();
        oscMessages.Add(new OscMessage(inputTypeAddress[type], 0));
        oscSender.Send(oscMessages);
    }

    /// <summary>
    /// 全部の入力を停止する
    /// </summary>
    public async UniTask Stop()
    {
        allStopFlag = true;

        List<OscMessage> sendData = new();

        foreach(var i in inputTypeAddress.Values)
        {
            // Voiceは除外
            if (i == inputTypeAddress[InputType.Voice]) continue;

            sendData.Add(new OscMessage(i, 0));
        }

        oscSender.Send(sendData);

        await UniTask.Yield();

        allStopFlag = false;
    }
}
