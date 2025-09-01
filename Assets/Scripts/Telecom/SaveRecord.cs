using TMPro;
using System;
using UnityEngine;
using WebSocketSharp;

[Serializable]
public class RunLog
{
    public string user_id;
    public string stage_code;
    public int prompt_length;
    public int clear_time_ms;
}

[Serializable]
public class GameResultResponse
{
    public bool ack;
    public string user_id;
    public string stage;
    public float rank_clear_time_percent;
    public float rank_tokens_percent;
    public int rank_clear_time;
    public int rank_tokens;
    public int total_records;
    public string received_text;
}

public class SaveRecord : MonoBehaviour
{
    [Header("서버 주소")]
    //public string wsUrl = "wss://192.168.178.134:8001/ws";
    public string wsUrl;
    public TMP_InputField recentStage;

    private WebSocket ws;

    void Start()
    {
        if (wsUrl == "")
        {
            wsUrl = "wss://192.168.179.56:8001/ws";
        }
        ws = new WebSocket(wsUrl);

        ws.OnOpen += (_, __) => Debug.Log("WS 연결됨");
        ws.OnMessage += (_, e) =>
        {
            // 결과 수신
            var result = ParseGameResult(e.Data);
            if (result != null)
            {
                Debug.Log(FormatGameResult(result));
                GameData.serverAck = true;
            }
        };
        ws.OnError += (_, e) => Debug.LogError("WS Error: " + e.Message);
        ws.OnClose += (_, e) => Debug.Log("WS Closed");

        ws.ConnectAsync();
    }

    public void SendRunLog()
    {
        if (ws == null || ws.ReadyState != WebSocketState.Open)
        {
            Debug.LogWarning("WS 미연결");
            return;
        }

        GameData.serverAck = false;

        var payload = new RunLog
        {
            user_id = GameData.userText,
            stage_code = recentStage.text,
            prompt_length = GameData.promptLen,
            clear_time_ms = GameData.moveCount
        };
        string json = JsonUtility.ToJson(payload);
        ws.Send(json);
        Debug.Log("Sent: " + json);
    }

    GameResultResponse ParseGameResult(string json)
    {
        try { return JsonUtility.FromJson<GameResultResponse>(json); }
        catch (Exception ex)
        {
            Debug.LogError("[SaveRecord] Parse 실패: " + ex);
            return null;
        }
    }

    string FormatGameResult(GameResultResponse gr)
    {
        if (gr == null || !gr.ack) return "서버 결과 없음";

        GameData.rank_clear_time_percent = gr.rank_clear_time_percent;
        GameData.rank_clear_time = gr.rank_clear_time;
        GameData.rank_tokens_percent = gr.rank_tokens_percent;
        GameData.rank_tokens = gr.rank_tokens;

        return
            $"유저ID: {gr.user_id}\n" +
            $"스테이지: {gr.stage}\n" +
            $"클리어타임: 상위 {gr.rank_clear_time_percent:F1}% · {gr.rank_clear_time}위\n" +
            $"단어수:     상위 {gr.rank_tokens_percent:F1}% · {gr.rank_tokens}위\n";
    }

    void OnApplicationQuit()
    {
        try { ws?.Close(); } catch { }
        ws = null;
    }
}
