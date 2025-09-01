using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// ===============================
// ====== Data classes ===========
// ===============================

// 서버에서 내려주는 진행 조회 응답(JSON 매핑)
[Serializable]
public class ProgressResponse
{
    public string user_id;
    public System.Collections.Generic.List<StageProgress> stages;
}

// 스테이지별 진행 상태
[Serializable]
public class StageProgress
{
    public string code;
    public bool unlocked;
    public bool cleared;
    public int prompt_length;
    public int clear_time_ms;
    public string cleared_at;   // ISO8601 문자열
}

// 유저 생성 요청(JSON 바디)
[Serializable]
public class CreateUserReq
{
    public string user_id;
}

// ===============================
// ====== Main Loader Class ======
// ===============================
public class LoadID : MonoBehaviour
{
    [Header("API Base")]
    //public string restBaseUrl = "https://192.168.178.134:8001";
    public string restBaseUrl;
    public SceneLoadManager sceneLoadManager;
    public GameObject executeWarn;
    public float busyNoticeDuration = 1.5f;

    private const string PREF_USER_ID = "user_id";

    private void Start()
    {
        if (restBaseUrl == "") {
            restBaseUrl = "https://192.168.179.56:8001";
        }
    }

    // 로컬 저장소(PlayerPrefs)에 user_id 저장
    void SaveUserId(string uid) => PlayerPrefs.SetString(PREF_USER_ID, uid);

    // 로컬에서 user_id 불러오기
    string LoadUserIdLocal() => PlayerPrefs.GetString(PREF_USER_ID, string.Empty);

    // HTTPS 인증서 무시 핸들러 (개발용)
    class DevCertBypass : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certData) => true;
    }

    bool IsHttps(string url) => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    // ==================================
    // 외부에서 호출할 메인 메소드
    // ==================================
    public void LoadOrCreateAndFetch()
    {
        string uid = GameData.userText;   // 외부 입력 값 사용

        if (string.IsNullOrEmpty(uid))
        {
            Debug.Log("ID를 입력하세요.");
            return;
        }

        StartCoroutine(FetchProgressOrCreate(uid));
    }

    // ==================================
    // 진행 조회 시도 → 실패하면 신규 생성 후 재조회
    // ==================================
    IEnumerator FetchProgressOrCreate(string uid)
    {
        StartCoroutine(CoShowBusyNotice("로그인 중 입니다..."));
        string url = $"{restBaseUrl}/progress/{UnityWebRequest.EscapeURL(uid)}";
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 5;
            if (IsHttps(url)) req.certificateHandler = new DevCertBypass();

            yield return req.SendWebRequest();

            long code = (long)req.responseCode;
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (req.result == UnityWebRequest.Result.Success)
            {
                SaveUserId(uid);

                // JSON 파싱
                ProgressResponse resp = JsonUtility.FromJson<ProgressResponse>(body);
                if (resp == null || resp.stages == null || resp.stages.Count == 0)
                {
                    Debug.Log("[LoadID] 진행 데이터 없음 → 신규 ID 생성");
                    yield return CreateNewId(uid);

                    // 신규 생성 후 다시 진행 조회
                    yield return FetchProgressOnly(uid);
                    sceneLoadManager.load(0);
                    yield break;
                }

                // 조회 성공 시 출력
                PrintStages(resp);
                sceneLoadManager.load(0);
            }
            else
            {
                // 서버가 꺼져있는 경우
                StartCoroutine(CoShowBusyNotice("서버에 연결되어 있지 않습니다..."));
                Debug.LogWarning($"[LoadID] 진행 조회 실패 (HTTP {code}) {req.error} :: {body}");
                GameData.stageClear[0] = true;
                sceneLoadManager.load(0);
            }
        }
    }

    // ==================================
    // 신규 ID 생성 요청
    // ==================================
    IEnumerator CreateNewId(string uid)
    {
        string json = JsonUtility.ToJson(new CreateUserReq { user_id = uid });

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest($"{restBaseUrl}/users", UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;

            if (IsHttps(restBaseUrl)) req.certificateHandler = new DevCertBypass();

            yield return req.SendWebRequest();

            long code = (long)req.responseCode;
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[LoadID] ID 생성 완료 (HTTP {code}) :: {body}");
                SaveUserId(uid);
            }
            else
            {
                Debug.LogError($"[LoadID] ID 생성 실패 (HTTP {code}) {req.error} :: {body}");
            }
        }
    }

    // ==================================
    // 단순 조회 + 출력 (생성 이후 재조회용)
    // ==================================
    IEnumerator FetchProgressOnly(string uid)
    {
        string url = $"{restBaseUrl}/progress/{UnityWebRequest.EscapeURL(uid)}";
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 5;
            if (IsHttps(url)) req.certificateHandler = new DevCertBypass();

            yield return req.SendWebRequest();

            string body = req.downloadHandler != null ? req.downloadHandler.text : "";
            ProgressResponse resp = JsonUtility.FromJson<ProgressResponse>(body);

            if (resp != null && resp.stages != null)
                PrintStages(resp);
            else
                Debug.LogWarning("[LoadID] 재조회했지만 데이터 없음");
        }
    }

    // ==================================
    // Stage 리스트 출력 (Unlocked=true만)
    // ==================================
    void PrintStages(ProgressResponse resp)
    {
        int n = 0;
        foreach (var stage in resp.stages)
        {
            if (stage.unlocked)
            {
                Debug.Log($"[LoadID] Stage={stage.code}, " +
                          $"Unlocked={stage.unlocked}, Cleared={stage.cleared}, " +
                          $"PromptLen={stage.prompt_length}, Time={stage.clear_time_ms}, " +
                          $"ClearedAt={stage.cleared_at}");
                GameData.stageClear[n] = true;
                n++;
            }
        }
    }

    private IEnumerator CoShowBusyNotice(string s)
    {
        executeWarn.GetComponent<TextMeshProUGUI>().text = s;
        executeWarn.SetActive(true);
        yield return new WaitForSeconds(busyNoticeDuration);
        executeWarn.SetActive(false);
    }
}
