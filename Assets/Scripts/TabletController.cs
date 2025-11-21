using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections;

public class TabletController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage remoteScreen;
    [SerializeField] private Button connectButton;

    [Header("Settings")]
    public int myPlayerId = 0; // P1=0, P2=1

    [Header("Signaling")]
    [SerializeField] private WebRTCSignaling signaling;

    private RTCPeerConnection pc;
    private RTCDataChannel dataChannel;

    // tablet input state
    private TabletInputData currentInput = new TabletInputData();

    // controller inputs
    private Controller_Actions controllerActions;

    void Start()
    {
        if (Gamepad.current == null)
        {
            InputSystem.AddDevice<Gamepad>();
        }

        EnhancedTouchSupport.Enable();
        if (SystemInfo.supportsGyroscope) Input.gyro.enabled = true;
        StartCoroutine(WebRTC.Update());

        signaling.OnMessageReceived += HandleSignalingMessage;

        connectButton.onClick.AddListener(() =>
        {
            Debug.Log("★ ボタン押下。ログイン通知送信: " + (myPlayerId == 0 ? "P1" : "P2"));
            signaling.Send(new SignalingMessage { type = "login_notify", id = (myPlayerId == 0 ? "P1" : "P2") });
            //connectButton.gameObject.SetActive(false);
        });


        // ★ 2. InputActionのセットアップとイベント購読
        controllerActions = new Controller_Actions();

        controllerActions.P1_Input.Enable();

        controllerActions.P1_Input.Move.performed += ctx => currentInput.stick = ctx.ReadValue<Vector2>();
        controllerActions.P1_Input.Move.canceled += ctx => currentInput.stick = Vector2.zero;

        controllerActions.P1_Input.Jump.performed += ctx => currentInput.buttonA = true;
        controllerActions.P1_Input.Jump.canceled += ctx => currentInput.buttonA = false;
    }

    void OnDisable()
    {
        pc?.Close();
        pc?.Dispose();
        EnhancedTouchSupport.Disable();
    }

    void HandleSignalingMessage(string json)
    {
        var msg = JsonUtility.FromJson<SignalingMessage>(json);

        if (msg.type == "offer")
        {
            StartCoroutine(CreateAnswer(msg.sdp));
        }
        else if (msg.type == "candidate")
        {
            var candidate = new RTCIceCandidate(new RTCIceCandidateInit
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex
            });
            pc?.AddIceCandidate(candidate);
        }
    }

    IEnumerator CreateAnswer(string sdp)
    {
        var config = new RTCConfiguration { iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } } };
        pc = new RTCPeerConnection(ref config);

        // ★修正: PCが作成したDataChannelを受け取る
        pc.OnDataChannel = channel =>
        {
            this.dataChannel = channel;
            Debug.Log("★ DataChannel 接続確立！");
        };

        // 映像受信設定
        pc.OnTrack = (RTCTrackEvent e) =>
        {
            if (e.Track is VideoStreamTrack videoTrack)
            {
                videoTrack.OnVideoReceived += tex => remoteScreen.texture = tex;
            }
        };

        // ICE候補の送信
        pc.OnIceCandidate = candidate =>
        {
            string myIdStr = (myPlayerId == 0) ? "P1" : "P2";
            signaling.Send(new SignalingMessage
            {
                type = "candidate",
                target = "host",
                id = myIdStr,
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = (int)candidate.SdpMLineIndex
            });
        };

        // Offerをセット
        var desc = new RTCSessionDescription { type = RTCSdpType.Offer, sdp = sdp };
        yield return pc.SetRemoteDescription(ref desc);

        // Answerを作成
        var op = pc.CreateAnswer();
        yield return op;
        var opdesc = op.Desc;
        yield return pc.SetLocalDescription(ref opdesc);

        // Answer送信
        string myIdStr2 = (myPlayerId == 0) ? "P1" : "P2";
        signaling.Send(new SignalingMessage { type = "answer", target = "host", id = myIdStr2, sdp = op.Desc.sdp });
    }

    void Update()
    {
        // チャネルチェック
        if (dataChannel == null || dataChannel.ReadyState != RTCDataChannelState.Open) return;

        // 入力収集
        if (UnityEngine.InputSystem.Gyroscope.current != null)
            currentInput.gyro = UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue();

        // --- タッチ入力の取得 (マウス対応版) ---

        // 1. まずタッチパネルを確認
        if (Touch.activeTouches.Count > 0)
        {
            var t = Touch.activeTouches[0];
            currentInput.touchX = t.screenPosition.x / Screen.width;
            currentInput.touchY = t.screenPosition.y / Screen.height;
            currentInput.touchPress = (t.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                                       t.phase == UnityEngine.InputSystem.TouchPhase.Stationary ||
                                       t.phase == UnityEngine.InputSystem.TouchPhase.Began);
        }
        // 2. タッチがない場合、マウスを確認 (PCデバッグ用)
        else if (Mouse.current != null)
        {
            // マウスの位置を正規化 (0~1)
            Vector2 mousePos = Mouse.current.position.ReadValue();
            currentInput.touchX = Mathf.Clamp01(mousePos.x / Screen.width);
            currentInput.touchY = Mathf.Clamp01(mousePos.y / Screen.height);

            // 左クリックされているか
            currentInput.touchPress = Mouse.current.leftButton.isPressed;
        }
        else
        {
            currentInput.touchPress = false;
        }

        // 送信
        TabletPacket packet = new TabletPacket { playerId = myPlayerId, data = currentInput };
        string json = JsonUtility.ToJson(packet);
        dataChannel.Send(json);
    }
}