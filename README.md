デバイスのバッテリー残量をOSCで送信します。
<br>
スロットが複数あり、送信先アドレスは次の通りです。
<br>
Slot 0 -> "/avatar/parameters/BatteryFloat00"
<br>
Slot 1 -> "/avatar/parameters/BatteryFloat01"
<br>
Slot 2 -> "/avatar/parameters/BatteryFloat02"
<br>


SteamVR環境で動作します。
<br>
スロットにセットしたデバイスは保存されます。次回起動時にはアプリがデバイスを検出すると自動的にセットされます。
<br>
SteamVRがデバイスを認識してから本アプリケーションがデバイスを取得できるまで時間を要することがあります。
<br>
※特にトラッカーが遅い！！
<br>

動く
<br>
・Meta Quest 3 Virtual Desktop 
<br>
・Thundra Tracker
<br>
・Vive Tracker

動かない
<br>
・Meta Quest 3 Controller
<br>
・Meta Quest 3 Quest Link (有線・無線)

多分動く
<br>
・Meta Quest 2 Virtual Desktop
<br>
・Index Controller
<br>
・Vive Controller

わからない
<br>
・上に記載のない全てのデバイス

使用例↓ SampleModel以下にサンプルモデルあります。

![SampleImage](image/SampleImage.png)
![SampleImage](image/SampleImage_02.png)

おまけ
<br>
以下の設定をするとSwitchBot温湿度計から取得した情報を送信します。 APIは1分に1回呼んでます
<br>
システム環境変数
<br>
・sai-osc-SBSecret : 「クライアントシークレット」
<br>
・sai-osc-SBToken : 「トークン」
<br>
・sai-osc-SBMeterID : 「温湿度計のデバイスID」
<br>
温度は0 ~ 50度の範囲を(float)0 ~ 1 (avatar/parameters/Humidity)
<br>
湿度は0 ~ 100%の範囲を(float)0 ~ 1 (avatar/parameters/Temperature)
