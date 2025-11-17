using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

// Input Action画面で "Tablet Controller" という名前で選べるようにする
[InputControlLayout(displayName = "Tablet Controller", stateType = typeof(TabletDeviceState))]
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class TabletDevice : InputDevice
{
    // ゲームやエディタ起動時に自動登録
    static TabletDevice() { Initialize(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        InputSystem.RegisterLayout<TabletDevice>(
            matches: new InputDeviceMatcher().WithInterface("Tablet"));
    }

    // コントロールのショートカット定義
    public StickControl leftStick { get; private set; }
    public ButtonControl buttonSouth { get; private set; }
    public Vector3Control gyro { get; private set; }
    public Vector2Control touchPosition { get; private set; }
    public ButtonControl touchPress { get; private set; }

    protected override void FinishSetup()
    {
        base.FinishSetup();
        leftStick = GetChildControl<StickControl>("leftStick");
        buttonSouth = GetChildControl<ButtonControl>("buttonSouth");
        gyro = GetChildControl<Vector3Control>("gyro");
        touchPosition = GetChildControl<Vector2Control>("touchPosition");
        touchPress = GetChildControl<ButtonControl>("touchPress");
    }
}

// メモリレイアウト（データ構造）
public struct TabletDeviceState : IInputStateTypeInfo
{
    public FourCC format => new FourCC('T', 'A', 'B', 'L');

    [InputControl(name = "leftStick", layout = "Stick")]
    public Vector2 leftStick;

    [InputControl(name = "buttonSouth", layout = "Button", bit = 0)]
    public float buttonSouth; // Aボタン

    [InputControl(name = "gyro", layout = "Vector3")]
    public Vector3 gyro; // ジャイロ

    [InputControl(name = "touchPosition", layout = "Vector2")]
    public Vector2 touchPosition; // タッチ座標 (スクリーン座標)

    [InputControl(name = "touchPress", layout = "Button", bit = 1)]
    public float touchPress; // タッチ中かどうか
}