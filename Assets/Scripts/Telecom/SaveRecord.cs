using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
    public Leaderboards leaderboards;
}

[Serializable]
public class Leaderboards
{
    public List<LeaderboardEntry> prompt_top10;
    public List<LeaderboardEntry> time_top10;
}

[Serializable]
public class LeaderboardEntry
{
    public string user_id;
    public int prompt_length;
    public int clear_time_ms;
    public int profile_image;

    public LeaderboardEntry(string userId, int promptLength, int clearTimeMs, int profileImage)
    {
        this.user_id = userId;
        this.prompt_length = promptLength;
        this.clear_time_ms = clearTimeMs;
        this.profile_image = profileImage;
    }
}

public class SaveRecord : MonoBehaviour
{
    [Header("서버 주소")]
    public string restBaseUrl;

    void Start()
    {
        if (restBaseUrl == "")
        {
            restBaseUrl = "https://" + GameData.serverurl + GameData.serverPort;
        }
    }

    public void SendRunLog()
    {
        GameData.serverAck = false;

        var payload = new RunLog
        {
            user_id = GameData.GetUserText(),
            stage_code = GameData.GetRecentStage(),
            prompt_length = GameData.promptLen,
            clear_time_ms = GameData.moveCount
        };

        StartCoroutine(PostRunLog(payload));
    }

    private IEnumerator PostRunLog(RunLog data)
    {
        string url = $"{restBaseUrl}/run-logs";

        string json = JsonUtility.ToJson(data);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;

            // 로컬/개발 HTTPS 자체서명 인증서 우회(필요 시)
            if (IsHttps(url)) req.certificateHandler = new DevCertBypass();

            Debug.Log($"[POST] {url} body={json}");
            yield return req.SendWebRequest();

            long code = req.responseCode;
            string respText = req.downloadHandler != null ? req.downloadHandler.text : "";

            bool ok = req.result == UnityWebRequest.Result.Success &&
                      code >= 200 && code < 300;

            if (ok)
            {
                var result = ParseGameResult(respText);
                if (result != null && result.ack)
                {
                    Debug.Log(FormatGameResult(result));
                    GameData.serverAck = true;
                }
                else
                {
                    Debug.LogWarning("[SaveRecord] 응답 파싱 실패 또는 ack=false :: " + respText);
                }
            }
            else
            {
                Debug.LogError($"[SaveRecord] POST 실패 (HTTP {code}) {req.error} :: {respText}");
            }
        }
    }

    private bool IsHttps(string url) => url.StartsWith("https://");

    // 개발용(자체서명 인증서) 우회 핸들러 – 이미 프로젝트에 있다면 이 중복 정의는 제거
    private class DevCertBypass : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
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

        GameData.prompt_top10 = gr.leaderboards.prompt_top10;
        GameData.time_top10 = gr.leaderboards.time_top10;

        return
            $"유저ID: {gr.user_id}\n" +
            $"스테이지: {gr.stage}\n" +
            $"클리어타임: 상위 {gr.rank_clear_time_percent:F1}% · {gr.rank_clear_time}위\n" +
            $"단어수:     상위 {gr.rank_tokens_percent:F1}% · {gr.rank_tokens}위\n";
    }
}
