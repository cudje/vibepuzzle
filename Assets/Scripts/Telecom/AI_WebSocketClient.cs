using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;
using TMPro;
using System.Collections.Generic;
using System.Collections;

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
    //public string wsaddress = "ws://192.168.178.134:8002/ws";
    public string wsaddress;
    public TMP_InputField userIdField;                  // 유저 ID 입력란
    public TMP_InputField stageIdField;                 // 스테이지 ID 입력란
    public TMP_InputField promptField;                  // 프롬프트 입력란
    public Button sendButton;                       // “전송” 버튼
    public TMP_Text logText;                            // 로그 출력 UI
    public PromptInterpreter promptInterpreter;

    public Animator animator;                       // Animator (Trigger: "Speak")
    public AudioSource speakSource;                 // 재생할 AudioSource
    public AudioClip speakClip;                     // (선택) 특정 클립 고정 재생
    public float speakDelay = 1.5f;                 // 재생 전 대기
    public float speakDuration = 6f;                // 재생 시간(초)
    private Coroutine speakRoutine;                 // 중복 재생 방지용
    public Image spinLoading;
    public float spinSpeed = 30f;

    public GameObject executeWarn;
    public float busyNoticeDuration = 1.5f;
    public bool isBusy = false;

    // ── 네트워크 객체 ──
    private WebSocket ws = null;                           // WebSocket 세션

    // Main Thread 디스패처 (UI 업데이트)
    private readonly System.Action<System.Action> EnqueueOnMain =
        action => UnityMainThreadDispatcher.Instance().Enqueue(action);

    // ──────────────────
    // 1) 연결 및 이벤트 바인딩
    // ──────────────────
    void Start()
    {
        // ws = new WebSocket("ws://192.168.55.82:8002/ws");     // AI 서버 주소
        if (wsaddress == "") {
            wsaddress = "ws://192.168.179.56:8002/ws";
        }
        ws = new WebSocket(wsaddress);
        ws.OnOpen    += (_, __) => Debug.Log("Connected to AI WebSocket Server");
        ws.OnMessage += (_,  e) => HandleServerMessage(e.Data);
        ws.OnError   += (_,  e) => Debug.LogError($"WebSocket Error: {e.Message}");
        ws.OnClose   += (_,  e) => Debug.Log($"Disconnected: {e.Reason}");
        ws.Compression = CompressionMethod.None;     // 메시지 압축 미사용(조사/테스트용)
        ws.ConnectAsync();

        sendButton.onClick.AddListener(SendPrompt);
    }

    // ──────────────────
    // 2) 프롬프트 전송
    // ──────────────────
    private void SendPrompt()
    {
        if (isBusy)
        {
            StartCoroutine(CoShowBusyNotice("이미 명령이 실행 중입니다."));
            return;
        }

        if (ws.ReadyState != WebSocketState.Open)
        {
            StartCoroutine(CoShowBusyNotice("서버에 연결되어 있지 않습니다."));
            logText.text = "서버에 연결되어 있지 않습니다.";
            return;
        }

        string userId  = userIdField.text.Trim();
        string stageId = stageIdField.text.Trim();
        string prompt  = promptField.text.Trim();

        if (string.IsNullOrEmpty(userId))
        {
            logText.text = "유저 ID를 입력하세요.";
            return;
        }
        if (string.IsNullOrEmpty(stageId))
        {
            logText.text = "Stage를 입력하세요.";
            return;
        }
        if (string.IsNullOrEmpty(prompt))
        {
            logText.text = "프롬프트를 입력하세요.";
            return;
        }

        isBusy = true;

        StartCoroutine(SpinUntilDone());

        PromptRequest req = new PromptRequest
        {
            userId  = userId,
            stageId = string.IsNullOrEmpty(stageId) ? null : stageId,
            prompt  = prompt
        };

        ws.Send(JsonUtility.ToJson(req));           // 직렬화 후 전송

        if (animator != null)
            animator.SetTrigger("Speak");

        // (2) 1.5초 뒤 사운드 재생 시작 → 6초 뒤 강제 정지
        if (speakRoutine != null) StopCoroutine(speakRoutine);
        speakRoutine = StartCoroutine(PlayVoiceWithDelay(speakDelay, speakDuration));
    }

    private IEnumerator CoShowBusyNotice(string s)
    {
        executeWarn.GetComponent<TextMeshProUGUI>().text = s;
        executeWarn.SetActive(true);
        yield return new WaitForSeconds(busyNoticeDuration);
        executeWarn.SetActive(false);
    }

    private IEnumerator SpinUntilDone()
    {
        var rt = spinLoading.rectTransform;
        spinLoading.gameObject.SetActive(true);

        while (isBusy)
        {
            float dt = Time.deltaTime;
            rt.Rotate(0f, 0f, -spinSpeed * dt); // 시계방향(-), 반시계는 +로
            yield return null; // 매 프레임
        }

        // 끝나면 숨기고 각도 원복(원하면)
        spinLoading.gameObject.SetActive(false);
        rt.localRotation = Quaternion.identity;
    }

    // 1.5초 대기 후 재생 → 6초 뒤 정지
    private IEnumerator PlayVoiceWithDelay(float delay, float duration)
    {
        if (speakSource == null) yield break;

        yield return new WaitForSeconds(delay);

        // 재생 준비
        if (speakClip != null) speakSource.clip = speakClip;
        speakSource.Stop();
        speakSource.time = 0f;
        speakSource.Play();

        // 지정 시간 재생
        yield return new WaitForSeconds(duration);

        // 강제 정지
        speakSource.loop = false;
        speakSource.Stop();
    }

    // ──────────────────
    // 3) 서버 응답 처리 ★
    // ──────────────────
    private void HandleServerMessage(string json)
    {
        var res = JsonUtility.FromJson<ActionResponse>(json); // 단순 구조

        EnqueueOnMain(() =>
        {
            if (!string.IsNullOrEmpty(res.error))
            {
                logText.text = $"에러: {res.error}";
                return;
            }

            GameData.promptLen = res.promptLen;

            // 로그 예시 출력
            logText.text =
                //$"동작: {res.code}\n" +
                $"프롬프트 길이: {GameData.promptLen}";

            promptInterpreter.fusion_start(res.code);
        });
    }

    void OnDestroy()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.CloseAsync();
            ws = null;
        }
    }

    // ──────────────────
    // 4) 종료 시 연결 정리
    // ──────────────────
    void OnApplicationQuit()
    {
        ws?.CloseAsync();
        ws = null;
    }
}