using UnityEngine;

[System.Serializable]
public struct TabletInputData
{
    public Vector2 stick;
    public bool buttonA;
    public Vector3 gyro;
    public float touchX;
    public float touchY;
    public bool touchPress;
}

[System.Serializable]
public struct TabletPacket
{
    public int playerId;
    public TabletInputData data;
}

[System.Serializable]
public class SignalingMessage
{
    public string type; // "offer", "answer", "candidate", "login_notify"
    public string id;   // "P1" or "P2"
    public string from; // ‘—MŒ³
    public string target; // ‘—Mæ
    public string sdp;
    public string candidate;
    public string sdpMid;
    public int sdpMLineIndex;
}