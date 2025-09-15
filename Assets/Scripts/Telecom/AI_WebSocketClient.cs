using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

// ───────────────────────────────────
// ① (단일) 동작 응답 모델 ★
// ───────────────────────────────────
[System.Serializable]
public class ActionResponse
{
    public string code;                             // 동작 종류 (예: "Jump")
    public int promptLen;                           // 프롬프트 문자열 길이
    public string error;                            // 오류 메시지(null = 정상)
}

// ───────────────────────────────────
// ② 클라이언트 → AI 서버 제출 모델 (변경 없음)
// ───────────────────────────────────
[System.Serializable] public class PromptRequest
{
    public string userId;                           // 플레이어 ID
    public string stageId;                          // 스테이지 ID(옵션)
    public string prompt;                           // 프롬프트
}

public class AI_WebSocketClient : MonoBehaviour
{
    [Header("UI References")]
    // ── UI 컴포넌트 참조 ──
    //public string wsaddress;
    public string restBaseUrl;
    public PromptInterpreter promptInterpreter;
    public WarningManager warning;

    public AudioSource speakSource;                 // 재생할 AudioSource
    public AudioClip speakClip;                     // (선택) 특정 클립 고정 재생

    public bool isBusy = false;

    private float speakDelay = 0.5f;                 // 재생 전 대기
    private float speakDuration = 3.5f;                // 재생 시간(초)
    private float spinSpeed = 360f;

    private Coroutine speakRoutine;                 // 중복 재생 방지용
    private bool speakend = false;

    // ── 네트워크 객체 ──
    //private WebSocket ws = null;                           // WebSocket 세션

    // Main Thread 디스패처 (UI 업데이트)
    private readonly System.Action<System.Action> EnqueueOnMain =
        action => UnityMainThreadDispatcher.Instance().Enqueue(action);

    // ──────────────────
    // 1) 연결 및 이벤트 바인딩
    // ──────────────────
    void Start()
    {
        if (restBaseUrl == "")
        {
            restBaseUrl = "https://" + GameData.serverurl + GameData.serverPort;
        }
        
        //if (wsaddress == "") {
        //    wsaddress = "ws://" + GameData.serverurl + ":8002/ws";
        //}
        //ws = new WebSocket(wsaddress);
        //ws.OnOpen    += (_, __) => Debug.Log("Connected to AI WebSocket Server");
        //ws.OnMessage += (_,  e) => HandleServerMessage(e.Data);
        //ws.OnError   += (_,  e) => Debug.LogError($"WebSocket Error: {e.Message}");
        //ws.OnClose   += (_,  e) => Debug.Log($"Disconnected: {e.Reason}");
        //ws.Compression = CompressionMethod.None;     // 메시지 압축 미사용(조사/테스트용)
        //ws.ConnectAsync();
    }

    // ──────────────────
    // 2) 프롬프트 전송
    // ──────────────────
    public void SendPrompt()
    {
        if (isBusy)
        {
            warning.ShowBusyNotice("이미 명령이 실행 중입니다.");
            return;
        }

        //if (ws.ReadyState != WebSocketState.Open)
        //{
        //    StartCoroutine(CoShowBusyNotice("서버에 연결되어 있지 않습니다."));
        //    return;
        //}

        string prompt  = SceneHubManager.I.promptTMPInputField.text.Trim();
        if (string.IsNullOrEmpty(prompt))
        {
            return;
        }

        isBusy = true;

        StartCoroutine(SpinUntilDone());

        PromptRequest req = new PromptRequest
        {
            userId  = GameData.GetUserText(),
            stageId = string.IsNullOrEmpty(GameData.recentStage) ? null : GameData.recentStage,
            prompt  = prompt
        };

        //ws.Send(JsonUtility.ToJson(req));           // 직렬화 후 전송

        StartCoroutine(CheckServerAlive($"{restBaseUrl}/healthz", isAlive =>
        {
            if (!isAlive)
            {
                // 서버 닫힘 → 알림 코루틴 실행 후 종료
                warning.ShowBusyNotice("서버에 연결되어 있지 않습니다.");
                isBusy = false;
                return;
            }

            // 서버 열려있으면 본래 PostCommand 실행
            StartCoroutine(PostCommand(req));
            SceneHubManager.I.roboAnimator.SetTrigger("Speak");
            if (speakRoutine != null) StopCoroutine(speakRoutine);
            speakRoutine = StartCoroutine(PlayVoiceWithDelay(speakDelay, speakDuration));
        }));
    }

    public IEnumerator CheckServerAlive(string url, System.Action<bool> callback)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 3; // 3초 안에 응답 없으면 실패 처리
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && req.responseCode == 200)
            {
                callback?.Invoke(true);   // 서버 열려 있음
            }
            else
            {
                callback?.Invoke(false);  // 서버 닫힘 or 오류
            }
        }
    }

    public IEnumerator PostCommand(PromptRequest data)
    {
        string url = $"{restBaseUrl}/ai/command";

        string json = JsonUtility.ToJson(data);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 45; // 초

            // 로컬/개발 HTTPS 자체서명 인증서 우회(필요 시)
            if (IsHttps(url)) req.certificateHandler = new DevCertBypass();

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                warning.ShowBusyNotice("서버에 연결되어 있지 않습니다.");
                yield break;
            }

            var text = req.downloadHandler.text;
            var res = JsonUtility.FromJson<ActionResponse>(text);

            EnqueueOnMain(() =>
            {
                GameData.promptLen = res.promptLen;
                StartCoroutine(WaitSpeakEndThenRun(res.code));
            });
        }
    }

    private IEnumerator WaitSpeakEndThenRun(string code)
    {
        yield return new WaitUntil(() => speakend);
        promptInterpreter.fusion_start(code);
    }

    private bool IsHttps(string url) => url.StartsWith("https://");

    // 개발용(자체서명 인증서) 우회 핸들러 – 이미 프로젝트에 있다면 이 중복 정의는 제거
    private class DevCertBypass : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    private IEnumerator SpinUntilDone()
    {
        var rt = SceneHubManager.I.loadingImage.rectTransform;
        SceneHubManager.I.loadingImage.gameObject.SetActive(true);

        while (isBusy)
        {
            float dt = Time.deltaTime;
            rt.Rotate(0f, 0f, -spinSpeed * dt); // 시계방향(-), 반시계는 +로
            yield return null; // 매 프레임
        }

        // 끝나면 숨기고 각도 원복(원하면)
        SceneHubManager.I.loadingImage.gameObject.SetActive(false);
        rt.localRotation = Quaternion.identity;
    }

    // 1.5초 대기 후 재생 → 6초 뒤 정지
    private IEnumerator PlayVoiceWithDelay(float delay, float duration)
    {
        speakend = false;

        yield return new WaitForSeconds(delay);

        // 재생 준비
        if (speakClip != null) speakSource.clip = speakClip;
        speakSource.Stop();
        speakSource.time = 0f;
        speakSource.Play();

        // 지정 시간 재생
        yield return new WaitForSeconds(duration);

        // 강제 정지
        speakend = true;
        speakSource.loop = false;
        speakSource.Stop();
    }

    // ──────────────────
    // 3) 서버 응답 처리 ★
    // ──────────────────
    //private void HandleServerMessage(string json)
    //{
    //    var res = JsonUtility.FromJson<ActionResponse>(json); // 단순 구조
    //    EnqueueOnMain(() =>
    //    {
    //        if (!string.IsNullOrEmpty(res.error))
    //        {
    //            return;
    //        }
    //        GameData.promptLen = res.promptLen;
    //        promptInterpreter.fusion_start(res.code);
    //    });
    //}
    //void OnDestroy()
    //{
    //    if (ws != null && ws.ReadyState == WebSocketState.Open)
    //    {
    //        ws.CloseAsync();
    //        ws = null;
    //    }
    //}
    //void OnApplicationQuit()
    //{
    //    ws?.CloseAsync();
    //    ws = null;
    //}
}