using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class TabletInputDriver : MonoBehaviour
{
    // シーンになくても勝手に起動して常駐する
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        var obj = new GameObject("[System] TabletInputDriver");
        obj.AddComponent<TabletInputDriver>();
        DontDestroyOnLoad(obj);
    }

    // シングルトン（どこからでも呼べるように）
    public static TabletInputDriver Instance { get; private set; }

    // 仮想デバイスの実体
    public TabletDevice DeviceP1 { get; private set; }
    public TabletDevice DeviceP2 { get; private set; }

    void Awake()
    {
        Instance = this;
        // 仮想デバイスを2つ作成して接続
        DeviceP1 = InputSystem.AddDevice<TabletDevice>("TabletP1");
        DeviceP2 = InputSystem.AddDevice<TabletDevice>("TabletP2");
        Debug.Log("仮想タブレット P1/P2 を接続しました");
    }

    void OnDestroy()
    {
        if (DeviceP1 != null) InputSystem.RemoveDevice(DeviceP1);
        if (DeviceP2 != null) InputSystem.RemoveDevice(DeviceP2);
    }

    // ★ 外部（WebRTC）からこれを呼ぶだけで入力完了！
    public void InjectData(int playerId, TabletInputData data)
    {
        var targetDevice = (playerId == 0) ? DeviceP1 : DeviceP2;
        if (targetDevice == null) return;

        // InputSystem にイベントを流し込む
        using (StateEvent.From(targetDevice, out var stateEvent))
        {
            targetDevice.leftStick.WriteValueIntoEvent(data.stick, stateEvent);
            targetDevice.buttonSouth.WriteValueIntoEvent(data.buttonA ? 1f : 0f, stateEvent);
            targetDevice.gyro.WriteValueIntoEvent(data.gyro, stateEvent);

            // 正規化座標(0-1) を スクリーン座標(Pixel) に変換
            var screenPos = new Vector2(data.touchX * 1920f, data.touchY * 1080f);
            targetDevice.touchPosition.WriteValueIntoEvent(screenPos, stateEvent);
            targetDevice.touchPress.WriteValueIntoEvent(data.touchPress ? 1f : 0f, stateEvent);

            InputSystem.QueueEvent(stateEvent);

            Debug.Log("state"+ data.stick);
        }
    }
}