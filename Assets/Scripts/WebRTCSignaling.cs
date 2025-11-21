using UnityEngine;
using NativeWebSocket;
using System;

public class WebRTCSignaling : MonoBehaviour
{
    [Header("Settings")]
    public string serverUrl = "ws://localhost:3000"; // タブレット側ではIPアドレスに変更！
    public bool autoConnect = true;

    private WebSocket websocket;

    // メッセージ受信イベント
    public event Action<string> OnMessageReceived;

    async void Start()
    {
        if (autoConnect) await Connect();
    }

    public async System.Threading.Tasks.Task Connect()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Send(new SignalingMessage { type = "login_notify", id = "host" });
            Debug.Log("サーバーに Host としてログインしました");
        };
        websocket.OnError += (e) => Debug.LogError("WS Error: " + e);
        websocket.OnClose += (e) => Debug.Log("WS Closed");

        // メッセージが来たらイベント発火
        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            OnMessageReceived?.Invoke(message);
        };

        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null) websocket.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null) await websocket.Close();
    }

    // メッセージ送信関数
    public void Send(object data)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            string json = JsonUtility.ToJson(data);
            websocket.SendText(json);
        }
    }
}