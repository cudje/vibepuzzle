using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// ===============================
// ====== Data classes ===========
// ===============================

// �������� �����ִ� ���� ��ȸ ����(JSON ����)
[Serializable]
public class ProgressResponse
{
    public string user_id;
    public System.Collections.Generic.List<StageProgress> stages;
}

// ���������� ���� ����
[Serializable]
public class StageProgress
{
    public string code;
    public bool unlocked;
    public bool cleared;
    public int prompt_length;
    public int clear_time_ms;
    public string cleared_at;   // ISO8601 ���ڿ�
}

// ���� ���� ��û(JSON �ٵ�)
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

    // ���� �����(PlayerPrefs)�� user_id ����
    void SaveUserId(string uid) => PlayerPrefs.SetString(PREF_USER_ID, uid);

    // ���ÿ��� user_id �ҷ�����
    string LoadUserIdLocal() => PlayerPrefs.GetString(PREF_USER_ID, string.Empty);

    // HTTPS ������ ���� �ڵ鷯 (���߿�)
    class DevCertBypass : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certData) => true;
    }

    bool IsHttps(string url) => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    // ==================================
    // �ܺο��� ȣ���� ���� �޼ҵ�
    // ==================================
    public void LoadOrCreateAndFetch()
    {
        string uid = GameData.userText;   // �ܺ� �Է� �� ���

        if (string.IsNullOrEmpty(uid))
        {
            Debug.Log("ID�� �Է��ϼ���.");
            return;
        }

        StartCoroutine(FetchProgressOrCreate(uid));
    }

    // ==================================
    // ���� ��ȸ �õ� �� �����ϸ� �ű� ���� �� ����ȸ
    // ==================================
    IEnumerator FetchProgressOrCreate(string uid)
    {
        StartCoroutine(CoShowBusyNotice("�α��� �� �Դϴ�..."));
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

                // JSON �Ľ�
                ProgressResponse resp = JsonUtility.FromJson<ProgressResponse>(body);
                if (resp == null || resp.stages == null || resp.stages.Count == 0)
                {
                    Debug.Log("[LoadID] ���� ������ ���� �� �ű� ID ����");
                    yield return CreateNewId(uid);

                    // �ű� ���� �� �ٽ� ���� ��ȸ
                    yield return FetchProgressOnly(uid);
                    sceneLoadManager.load(0);
                    yield break;
                }

                // ��ȸ ���� �� ���
                PrintStages(resp);
                sceneLoadManager.load(0);
            }
            else
            {
                // ������ �����ִ� ���
                StartCoroutine(CoShowBusyNotice("������ ����Ǿ� ���� �ʽ��ϴ�..."));
                Debug.LogWarning($"[LoadID] ���� ��ȸ ���� (HTTP {code}) {req.error} :: {body}");
                GameData.stageClear[0] = true;
                sceneLoadManager.load(0);
            }
        }
    }

    // ==================================
    // �ű� ID ���� ��û
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
                Debug.Log($"[LoadID] ID ���� �Ϸ� (HTTP {code}) :: {body}");
                SaveUserId(uid);
            }
            else
            {
                Debug.LogError($"[LoadID] ID ���� ���� (HTTP {code}) {req.error} :: {body}");
            }
        }
    }

    // ==================================
    // �ܼ� ��ȸ + ��� (���� ���� ����ȸ��)
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
                Debug.LogWarning("[LoadID] ����ȸ������ ������ ����");
        }
    }

    // ==================================
    // Stage ����Ʈ ��� (Unlocked=true��)
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
