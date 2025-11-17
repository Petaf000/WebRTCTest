using System.Linq;
using UnityEngine;
using Unity.WebRTC;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class MasterInputRouter : MonoBehaviour
{
    [Header("Target Players")]
    [SerializeField] private Player player1;
    [SerializeField] private Player player2;

    [Header("WebRTC Signaling")]
    [SerializeField] private WebRTCSignaling signaling;

    [Header("Render Textures")]
    [SerializeField] private RenderTexture rt_p1;
    [SerializeField] private RenderTexture rt_p2;

    // --- 3つの入力パイプライン ---
    private Controller_Actions p1_Actions;
    private Controller_Actions p2_Actions;
    private Controller_Actions debug_Actions;

    private Player hotseatActivePlayer = null;

    // WebRTC Connections
    private RTCPeerConnection p1_Connection;
    private RTCPeerConnection p2_Connection;

    void Start()
    {
        if (Display.displays.Length > 1) Display.displays[1].Activate();

        SetupInputPipelines();
        StartCoroutine(WebRTC.Update());

        signaling.OnMessageReceived += HandleSignalingMessage;
        StartCoroutine(LoginAsHost());
    }

    void OnDisable()
    {
        p1_Connection?.Close();
        p2_Connection?.Close();
        p1_Actions?.Disable();
        p2_Actions?.Disable();
        debug_Actions?.Disable();
    }

    void SetupInputPipelines()
    {
        // P1 (Tablet)
        p1_Actions = new Controller_Actions();
        p1_Actions.devices = new[] { TabletInputDriver.Instance.DeviceP1 };
        p1_Actions.P1_Input.Enable();
        BindActions(p1_Actions.P1_Input, player1);

        // P2 (Tablet)
        p2_Actions = new Controller_Actions();
        p2_Actions.devices = new[] { TabletInputDriver.Instance.DeviceP2 };
        p2_Actions.P2_Input.Enable();
        BindActions(p2_Actions.P2_Input, player2);

        // Debug (PC)
        debug_Actions = new Controller_Actions();
        debug_Actions.devices = InputSystem.devices.Where(d => !(d is TabletDevice)).ToArray();
        debug_Actions.Debug_Input.Enable();

        debug_Actions.Debug_Input.Move.performed += ctx => hotseatActivePlayer?.Move(ctx.ReadValue<Vector2>());
        debug_Actions.Debug_Input.Move.canceled += ctx => hotseatActivePlayer?.Move(Vector2.zero);
        debug_Actions.Debug_Input.Jump.performed += ctx => hotseatActivePlayer?.Jump();
    }

    void HandleSignalingMessage(string json)
    {
        // Debug.Log("★ メッセージ受信: " + json); // ログがうるさい場合はコメントアウト
        var msg = JsonUtility.FromJson<SignalingMessage>(json);
        string senderId = !string.IsNullOrEmpty(msg.from) ? msg.from : msg.id;

        if (msg.type == "login_notify")
        {
            if (msg.id == "P1") StartConnection(0);
            if (msg.id == "P2") StartConnection(1);
        }
        else if (msg.type == "answer")
        {
            var desc = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = msg.sdp };
            var targetPC = (senderId == "P1") ? p1_Connection : p2_Connection;

            if (targetPC != null)
            {
                StartCoroutine(OnAnswerReceived(targetPC, desc));
            }
            else
            {
                Debug.LogError($"Unknown sender for Answer: {senderId}");
            }
        }
        else if (msg.type == "candidate")
        {
            var candidate = new RTCIceCandidate(new RTCIceCandidateInit
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex
            });

            var targetPC = (senderId == "P1") ? p1_Connection : p2_Connection;
            targetPC?.AddIceCandidate(candidate);
        }
    }

    IEnumerator LoginAsHost()
    {
        yield return new WaitForSeconds(1.0f);
        signaling.Send(new SignalingMessage { type = "login_notify", id = "host" });
        Debug.Log("サーバーに Host としてログインしました");
    }

    // 接続を開始する (Offerを作る)
    public void StartConnection(int playerId)
    {
        string targetId = (playerId == 0) ? "P1" : "P2";
        Debug.Log($"Start Connection to {targetId}");

        var config = new RTCConfiguration { iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } } };
        var pc = new RTCPeerConnection(ref config);

        // データチャネル変数
        RTCDataChannel channel = null;

        if (playerId == 0)
        {
            p1_Connection = pc;

            // 映像トラック追加
            var track = new VideoStreamTrack(rt_p1);
            p1_Connection.AddTrack(track);

            // ★修正: PC側からデータチャネルを作成する！
            channel = p1_Connection.CreateDataChannel("input");
        }
        else
        {
            p2_Connection = pc;
            var track = new VideoStreamTrack(rt_p2);
            p2_Connection.AddTrack(track);

            // ★修正: PC側からデータチャネルを作成する！
            channel = p2_Connection.CreateDataChannel("input");
        }

        // ★修正: 受信設定
        if (channel != null)
        {
            channel.OnMessage = bytes => OnWebRTCData(bytes);
            channel.OnOpen = () => Debug.Log($"DataChannel ({targetId}) Open!");
        }

        // ICE Candidate 送信
        pc.OnIceCandidate = candidate =>
        {
            signaling.Send(new SignalingMessage
            {
                type = "candidate",
                target = targetId,
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = (int)candidate.SdpMLineIndex
            });
        };

        StartCoroutine(CreateOffer(pc, targetId));
    }

    IEnumerator CreateOffer(RTCPeerConnection pc, string targetId)
    {
        var op = pc.CreateOffer();
        yield return op;
        var opdesc = op.Desc;
        yield return pc.SetLocalDescription(ref opdesc);

        signaling.Send(new SignalingMessage { type = "offer", target = targetId, sdp = op.Desc.sdp });
    }

    IEnumerator OnAnswerReceived(RTCPeerConnection pc, RTCSessionDescription desc)
    {
        yield return pc.SetRemoteDescription(ref desc);
    }

    public void OnWebRTCData(byte[] bytes)
    {
        string json = System.Text.Encoding.UTF8.GetString(bytes);
        // Debug.Log($"データ受信: {json}");
        var dataObj = JsonUtility.FromJson<TabletPacket>(json);
        TabletInputDriver.Instance.InjectData(dataObj.playerId, dataObj.data);
    }

    void BindActions(Controller_Actions.P1_InputActions map, Player targetPlayer)
    {
        map.Move.performed += ctx => targetPlayer.Move(ctx.ReadValue<Vector2>());
        map.Move.canceled += ctx => targetPlayer.Move(Vector2.zero);
        map.Jump.performed += ctx => targetPlayer.Jump();
    }
    void BindActions(Controller_Actions.P2_InputActions map, Player targetPlayer)
    {
        map.Move.performed += ctx => targetPlayer.Move(ctx.ReadValue<Vector2>());
        map.Move.canceled += ctx => targetPlayer.Move(Vector2.zero);
        map.Jump.performed += ctx => targetPlayer.Jump();
    }

    void Update()
    {
        HandleHotseatFocusByKeyboard();
    }

    private void HandleHotseatFocusByKeyboard()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame) hotseatActivePlayer = player1;
        if (Keyboard.current.f2Key.wasPressedThisFrame) hotseatActivePlayer = player2;
        if (Keyboard.current.escapeKey.wasPressedThisFrame) hotseatActivePlayer = null;
    }
}