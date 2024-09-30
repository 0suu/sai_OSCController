using Valve.VR;
using System.Windows.Threading;
using SharpOSC;
using System.Windows.Controls;
using System.Windows;
using sai_OSCController;

public class BatteryDataReceiver
{
    public class Device(string name, string id, float battery)
    {
        public string Name = name;
        public string ID = id;
        public float Battery = battery;
    }

    List<Device> Devices = new();

    private DispatcherTimer _timer1 = null;

    private int waitClickSlot = -1;

    const int slotCount = 14; // スロットリストのボタン数

    const string batteryAddress = "/avatar/parameters/BatteryFloat";
    const int updateDeviceInterval = 10; // 10秒ごと

    List<Button> slotButtons = new();

    List<string> sendDeviceIDList;

    public List<OscMessage>? GetSendData()
    {
        List<OscMessage> messages = new();

        if (sendDeviceIDList.Count == 0)
        {
            return messages;
        }

        for (int i = 0; i < sendDeviceIDList.Count; i++)
        {
            var deviceID = sendDeviceIDList[i];
            var foundDevice = Devices.Find(x => x.ID == deviceID);

            if (foundDevice == null)
            {
                Console.WriteLine($"デバイス {deviceID} が見つからないので処理しない");
                continue;
            }

            float value = 1 - foundDevice.Battery;

            var tempBatteryAddress = batteryAddress + i.ToString("D2");

            messages.Add(new OscMessage(tempBatteryAddress, value));
        }

        return messages;
        
    }

    public BatteryDataReceiver()
    {
        FirstUpdateTimer();

        sendDeviceIDList = new();
        for (int i = 0; i < slotCount; i++)
        {
            sendDeviceIDList.Add("");
        }

        // スロットリストのボタン作成
        CreateSlotButtons();
    }

    void FirstUpdateTimer()
    {
        DispatcherTimer initialTimer = new();
        initialTimer.Interval = TimeSpan.FromSeconds(1); // 1秒後に実行
        initialTimer.Tick += (sender, e) =>
        {
            UpdateDevice();
            initialTimer.Stop();
            UpdateTimer();
        };
        initialTimer.Start();
    }

    void UpdateTimer()
    {
        _timer1 = new();
        _timer1.Interval = TimeSpan.FromSeconds(updateDeviceInterval);
        _timer1.Tick += (sender, e) =>
        {
            UpdateDevice();
        };
        _timer1.Start();
    }

    void UpdateDevice()
    {
        Console.WriteLine("デバイスを更新");

        // デバイスリスト更新
        Devices.Clear();
        Devices = ListDeviceBatteryStatus();

        for (int i = 0; i < slotCount; i++)
        {
            if (!TryGetPropertyValue($"slot{i}", out var value)) continue;

            Console.WriteLine($"slot{i}に保存値 [{value}] を発見");

            // 既にスロットにデバイスがセットされている場合はスキップ null or empty
            if (sendDeviceIDList[i] != "")
            {
                // Console.WriteLine($"スロット {i} にデバイスがセットされているのでスキップ");
                continue;
            }

            var foundDevice = Devices.Find(x => x.ID == value.ToString());

            //　デバイスが見つからない場合はスキップ
            if (foundDevice == null)
            {
                // Console.WriteLine($"デバイス {value} が見つからないのでスキップ");
                continue;
            }

            // スロットにデバイスをセット
            SetDeviceInSlot(i, foundDevice);
        }

        CreateDeviceButton();
    }

    void CreateSlotButtons()
    {
        // クリア
        ClearSlotButtonGrid();

        slotButtons = new List<Button>();

        for (int i = 0; i < slotCount; i++)
        {
            Button newButton = new();

            newButton.Content = $"Slot {i}";

            newButton.FontSize = 12;
            newButton.Margin = new Thickness(5); // ボタンの周囲に5ピクセルのマージンを追加
            newButton.Padding = new Thickness(0); // ボタン内のテキストパディングを0に設定
            newButton.Width = 175;
            newButton.Height = 40;
            newButton.HorizontalAlignment = HorizontalAlignment.Center;
            newButton.HorizontalContentAlignment = HorizontalAlignment.Center;
            newButton.Tag = i; // ボタンにタグを追加

            slotButtons.Add(newButton);
        }

        // ボタンをWrapPanelに追加
        if (slotButtons.Count == 0) return;

        // WrapPanelを作成
        WrapPanel wrapPanel = new();
        wrapPanel.Orientation = Orientation.Vertical;

        foreach (var button in slotButtons)
        {
            button.Click += new RoutedEventHandler(OnClickSlotButton);
            wrapPanel.Children.Add(button); // WrapPanelにボタンを追加
        }

        // ボタンが格納されたWrapPanelをUIに追加します。
        MainWindow.Instance.SlotGrid.Children.Add(wrapPanel);
    }

    void ClearDeviceListGrid()
    {
        // クリア
        MainWindow.Instance.DeviceListGrid.Children.Clear();
    }

    void ClearSlotButtonGrid()
    {
        // クリア
        MainWindow.Instance.SlotGrid.Children.Clear();
    }

    void CreateDeviceButton()
    {
        var buttons = new List<Button>();

        for (int i = 0; i < Devices.Count; i++)
        {
            Button newButton = new Button();
            Console.WriteLine(Devices[i].Name);

            // ボタンについての設定
            try
            {
                newButton.Content = $"{Devices[i].Name}\n{Devices[i].ID}";

                newButton.FontSize = 12;
                newButton.Margin = new Thickness(5); // ボタンの周囲に5ピクセルのマージンを追加
                newButton.Padding = new Thickness(0); // ボタン内のテキストパディングを0に設定
                newButton.Width = 110;
                newButton.Height = 40;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("ArgumentException caught!");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"ParamName: {ex.ParamName}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            Console.WriteLine("ボタン作成");

            buttons.Add(newButton);
        }

        AddDeviceButton(buttons);
    }

    void AddDeviceButton(List<Button> newButtons)
    {
        ClearDeviceListGrid();

        if (newButtons.Count == 0) return;

        // WrapPanelを作成
        WrapPanel wrapPanel = new WrapPanel();
        wrapPanel.Orientation = Orientation.Horizontal;

        foreach (var button in newButtons)
        {
            button.Click += new RoutedEventHandler(OnClickDeviceButton);
            wrapPanel.Children.Add(button); // WrapPanelにボタンを追加
        }

        // ボタンが格納されたWrapPanelをUIに追加
        MainWindow.Instance.DeviceListGrid.Children.Add(wrapPanel);
    }

    void OnClickDeviceButton(object sender, RoutedEventArgs e)
    {
        // ボタンがクリックされたときの処理
        Button clickedButton = (Button)sender;

        // スロットが入力待機中ではないなら処理しない
        if (waitClickSlot == -1) return;

        // スロットにデバイスをセット
        SetDeviceInSlot(waitClickSlot, Devices.Find(x => clickedButton.Content.ToString().Contains(x.ID)));

        Console.WriteLine($"デバイスボタン {clickedButton.Content} をクリック");
    }

    void OnClickSlotButton(object sender, RoutedEventArgs e)
    {
        // クリックされたボタンを取得
        Button clickedButton = (Button)sender;

        int index = (int)clickedButton.Tag;

        if (waitClickSlot == index)
        {
            // 二連続でクリックされた スロットを空にする
            RemoveDeviceInSlot(index);
            waitClickSlot = -1;

            return;
        }

        // TextBlockを使わないと中央ぞろえできない
        {
            TextBlock textBlock = new();

            textBlock.Text = "デバイスをクリック\nもう一度クリックで空にする";
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;

            clickedButton.Content = textBlock;
        }

        // ボタン名のデバイスのindexをセット
        waitClickSlot = index;

        Console.WriteLine($"{clickedButton.Content} index: {index} をクリック");
    }

    void SetDeviceInSlot(int slotNum, Device device)
    {
        Console.WriteLine($"スロット {slotNum} にデバイス {device.Name} をセット");

        slotButtons[slotNum].Content = $"{device.Name}\n{device.ID}";

        sendDeviceIDList[slotNum] = device.ID;

        // 保存
        SaveDeviceIDToSettings(slotNum, device.ID);

        // 待機状態を解除
        waitClickSlot = -1;
    }

    string? GetDeviceInSlot(int slotNum)
    {
        return slotButtons[slotNum].Content.ToString();
    }

    /// <summary>
    /// 指定されたスロット番号にデバイスIDを設定し、設定を保存します。
    /// </summary>
    /// <param name="slotNum">スロット番号</param>
    /// <param name="deviceID">デバイスID</param>
    void SaveDeviceIDToSettings(int slotNum, string deviceID)
    {
        try
        {
            string propertyName = $"slot{slotNum}";
            var settings = sai_OSCController.Properties.Settings.Default;
            var propertyInfo = typeof(sai_OSCController.Properties.Settings).GetProperty(propertyName);

            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(settings, deviceID);
                settings.Save();
                Console.WriteLine($"設定に {propertyName} = {deviceID} を保存");
            }
            else
            {
                Console.WriteLine($"プロパティ {propertyName} が存在しないか、書き込み不可");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定の保存中にエラーが発生しました: {ex.Message}");
        }
    }

    void RemoveDeviceInSlot(int slotNum)
    {
        slotButtons[slotNum].Content = $"Slot {slotNum}";

        sendDeviceIDList[slotNum] = "";

        // 保存
        SaveDeviceIDToSettings(slotNum, "");
    }

    List<Device> ListDeviceBatteryStatus()
    {
        List<Device> devices = new();

        if (OpenVR.System == null)
        {
            Console.WriteLine("OpenVR.System is null");
            return devices;
        }

        for (int i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            ETrackedDeviceClass deviceClass = OpenVR.System.GetTrackedDeviceClass((uint)i);
            float batteryPercentage = GetTrackedDevicePropertyFloat((uint)i, ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float);
            string aaa = GetTrackedDevicePropertyString((uint)i, ETrackedDeviceProperty.Prop_SerialNumber_String);
            Console.WriteLine("        Device " + i + ": " + aaa + " (" + deviceClass.ToString() + "), Battery: " + (batteryPercentage * 100).ToString("F0") + "%");
            if (batteryPercentage >= 0)
            {
                if (batteryPercentage >= 0)
                {
                    string deviceName = GetTrackedDevicePropertyString((uint)i, ETrackedDeviceProperty.Prop_ModelNumber_String);
                    string deviceID = GetTrackedDevicePropertyString((uint)i, ETrackedDeviceProperty.Prop_SerialNumber_String);
                    Console.WriteLine("Device " + i + ": " + deviceID + " (" + deviceClass.ToString() + "), Battery: " + (batteryPercentage * 100).ToString("F0") + "%");

                    // デバイスの情報をリストに追加
                    devices.Add(new(deviceName, deviceID, batteryPercentage));
                }
            }
        }

        return devices;
    }

    float GetTrackedDevicePropertyFloat(uint deviceId, ETrackedDeviceProperty prop)
    {
        ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
        float result = OpenVR.System.GetFloatTrackedDeviceProperty(deviceId, prop, ref error);
        if (error == ETrackedPropertyError.TrackedProp_Success)
        {
            return result;
        }
        return -1;
    }

    string GetTrackedDevicePropertyString(uint deviceId, ETrackedDeviceProperty prop)
    {
        ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
        uint bufferSize = OpenVR.System.GetStringTrackedDeviceProperty(deviceId, prop, null, 0, ref error);
        if (bufferSize > 1)
        {
            System.Text.StringBuilder buffer = new System.Text.StringBuilder((int)bufferSize);
            OpenVR.System.GetStringTrackedDeviceProperty(deviceId, prop, buffer, bufferSize, ref error);
            return buffer.ToString();
        }
        return null;
    }

    bool TryGetPropertyValue(string propertyName, out object value)
    {
        var propertyInfo = typeof(sai_OSCController.Properties.Settings).GetProperty(propertyName);
        if (propertyInfo == null)
        {
            value = null;
            return false;
        }

        value = propertyInfo.GetValue(sai_OSCController.Properties.Settings.Default);
        return value != null;
    }
}
