// server.js
const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 3000 });

console.log("=== スマート・シグナリングサーバー起動 (Port: 3000) ===");

// 接続しているクライアントのリスト { "P1": ws, "host": ws, ... }
let clients = {};

wss.on('connection', function connection(ws) {
    console.log(">> 新しい接続がありました");

    ws.on('message', function incoming(message) {
        let data;
        try {
            data = JSON.parse(message);
        } catch (e) {
            console.error("JSONパースエラー:", message);
            return;
        }

        // --- A. ログイン処理 (接続時に「私はP1です」と名乗る) ---
        if (data.type === "login_notify") {
            // IDを登録 (例: "P1", "host")
            const myId = data.id || "unknown";
            ws.myId = myId; // ソケット自体にIDをメモしておく
            clients[myId] = ws; 
            console.log(`✅ 登録完了: ${myId}`);
            
            // もしPC(host)への通知なら、転送してあげる
            if (clients["host"] && clients["host"].readyState === WebSocket.OPEN) {
                 // "from" を付けて転送
                data.from = myId;
                clients["host"].send(JSON.stringify(data));
            }
            return;
        }

        // --- B. メッセージ転送処理 (Offer/Answer/Candidate) ---
        // 宛先 (target) が指定されている場合
        if (data.target && clients[data.target]) {
            const targetWs = clients[data.target];
            
            if (targetWs.readyState === WebSocket.OPEN) {
                // ★重要: 「誰から来たか (from)」を付与して転送！
                // これでPC側が "from": "P1" を見て判断できるようになる
                data.from = ws.myId; 
                
                targetWs.send(JSON.stringify(data));
                console.log(`📩 転送: ${ws.myId} -> ${data.target} (${data.type})`);
            }
        } 
        // 宛先不明の場合 (PCへの返信とみなす簡易処理)
        else if (clients["host"] && ws !== clients["host"]) {
             data.from = ws.myId;
             clients["host"].send(JSON.stringify(data));
        }
    });

    ws.on('close', () => {
        if (ws.myId) {
            console.log(`❌ 切断: ${ws.myId}`);
            delete clients[ws.myId];
        }
    });
});
