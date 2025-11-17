using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets; // これを追加

public class ServerLauncher : MonoBehaviour
{
    private Process serverProcess;

    // server.jsで設定しているポート番号（デフォルト3000など）に合わせてください
    private const int SERVER_PORT = 3000;
    private const string SERVER_HOST = "127.0.0.1";

    void Start()
    {
        // まずサーバーが既に動いているか確認する
        if (IsServerRunning(SERVER_HOST, SERVER_PORT))
        {
            UnityEngine.Debug.Log("サーバーは既に稼働中です。既存のサーバーを使用します。");
        }
        else
        {
            UnityEngine.Debug.Log("サーバーが見つかりません。新規に起動します...");
            StartServer();
        }
    }

    // ポートが開いているかチェックする関数
    bool IsServerRunning(string host, int port)
    {
        try
        {
            using (var client = new TcpClient())
            {
                // 接続できればサーバーがいるということ
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(System.TimeSpan.FromMilliseconds(500)); // 0.5秒待つ
                if (success)
                {
                    client.EndConnect(result);
                    return true;
                }
            }
        }
        catch
        {
            // 接続エラー＝サーバーはいない
        }
        return false;
    }

    void StartServer()
    {
        string folderPath = Application.streamingAssetsPath;
        string nodePath = Path.Combine(folderPath, "node.exe");
        string scriptPath = Path.Combine(folderPath, "server.js");

        // Node.exeがあるか念のため確認
        if (!File.Exists(nodePath))
        {
            UnityEngine.Debug.LogError("node.exeが見つかりません！ StreamingAssetsを確認してください。");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = nodePath,
            Arguments = scriptPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true // 本番はtrue推奨
        };

        try
        {
            serverProcess = Process.Start(startInfo);
            UnityEngine.Debug.Log("サーバーを自動起動しました。 PID: " + serverProcess.Id);

            serverProcess.OutputDataReceived += (sender, e) =>
            { if (e.Data != null) UnityEngine.Debug.Log("[Node] " + e.Data); };
            serverProcess.BeginOutputReadLine();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("サーバー起動失敗: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        // 自分で起動したサーバープロセスだけを終了させる
        // （もともと起動していたサーバーは殺さない）
        if (serverProcess != null && !serverProcess.HasExited)
        {
            serverProcess.Kill();
            serverProcess.Dispose();
            UnityEngine.Debug.Log("自動起動したサーバーを停止しました。");
        }
    }
}