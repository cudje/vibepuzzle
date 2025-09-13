using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

// 서버에 보낼 바디
[System.Serializable]
public class UpdateProfileImageReq { public int profile_image; }

public class ProfileTelecom : MonoBehaviour
{
    public string restBaseUrl;

    private void Start()
    {
        if (restBaseUrl == "")
        {
            restBaseUrl = "https://" + GameData.serverurl + ":8001";
        }
    }

    /// <summary>
    /// 프로필 이미지를 0~2로 변경 (PATCH /users/{user_id}/profile_image)
    /// </summary>
    public IEnumerator UpdateProfile(int newProfile)
    {
        if (string.IsNullOrEmpty(GameData.userText))
        {
            Debug.LogError("[Profile] GameData.userId가 비어있습니다. 먼저 사용자 ID를 설정하세요.");
            yield break;
        }
        Debug.Log(GameData.userText);
        int clamped = Mathf.Clamp(newProfile, 0, 2);
        string url = $"{restBaseUrl}/users/{UnityWebRequest.EscapeURL(GameData.userText)}/profile_image";

        string json = JsonUtility.ToJson(new UpdateProfileImageReq { profile_image = clamped });
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, "PATCH"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;

            if (IsHttps(url)) req.certificateHandler = new DevCertBypass(); // 로컬 HTTPS 개발용

            yield return req.SendWebRequest();

            long code = (long)req.responseCode;
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (req.result == UnityWebRequest.Result.Success)
            {
                GameData.profileImage = clamped; // 런타임 상태 갱신
                Debug.Log($"[Profile] 프로필 변경 성공 → {clamped} (HTTP {code}) :: {body}");
            }
            else
            {
                Debug.LogError($"[Profile] 프로필 변경 실패 (HTTP {code}) {req.error} :: {body}");
            }
        }
    }

    // ───────── helpers ─────────
    private bool IsHttps(string url) => url.StartsWith("https://");

    // 개발용(자체서명 인증서) 우회 핸들러 – 이미 프로젝트에 있다면 이 중복 정의는 제거
    private class DevCertBypass : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}