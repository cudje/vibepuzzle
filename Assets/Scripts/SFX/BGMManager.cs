using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Defaults")]
    [SerializeField] private AudioClip defaultBgm;
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.25f;
    [SerializeField, Tooltip("씬 전환 시 BGM 크로스페이드 시간(초). 0이면 즉시 전환")]
    private float crossFadeSeconds = 0.5f;
    [SerializeField, Tooltip("같은 곡으로 돌아올 때 마지막 재생 위치부터 이어서 재생")]
    private bool resumeLastPosition = true;

    [System.Serializable]
    public struct SceneBgm
    {
        public string sceneName;   // 빌드 세팅의 씬 이름과 정확히 일치
        public AudioClip bgm;
    }
    [SerializeField] private List<SceneBgm> sceneBgms = new();

    // 내부 상태
    private AudioSource mainSource;
    private AudioSource tempSource; // 크로스페이드용 임시 소스
    private readonly Dictionary<AudioClip, float> lastPositions = new(); // 클립별 마지막 재생 위치(초)

    private Coroutine crossfadeRoutine;   // 진행 중인 크로스페이드 코루틴
    private int fadeNonce = 0;            // 전환 세대 번호(취소 토큰)

    private void Awake()
    {
        // 싱글톤 보장
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 메인 오디오 소스
        mainSource = gameObject.AddComponent<AudioSource>();
        mainSource.loop = true;
        mainSource.spatialBlend = 0f;
        mainSource.playOnAwake = false;
        mainSource.volume = defaultVolume;

        // 씬 전환 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // 첫 씬에서도 자동으로 맞는 BGM 적용
        RefreshForCurrentScene();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        // 파괴 시 안전 정리
        fadeNonce++;
        if (crossfadeRoutine != null) { StopCoroutine(crossfadeRoutine); crossfadeRoutine = null; }
        if (tempSource != null) { SafeStop(tempSource); Destroy(tempSource); tempSource = null; }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyBgmForScene(scene.name);
    }

    private void RefreshForCurrentScene()
    {
        ApplyBgmForScene(SceneManager.GetActiveScene().name);
    }

    private void ApplyBgmForScene(string sceneName)
    {
        // 씬 이름에 매핑된 BGM 찾기 (없으면 defaultBgm)
        AudioClip target = defaultBgm;
        for (int i = 0; i < sceneBgms.Count; i++)
        {
            var sb = sceneBgms[i];
            if (!string.IsNullOrEmpty(sb.sceneName) && sb.sceneName == sceneName && sb.bgm != null)
            {
                target = sb.bgm;
                break;
            }
        }

        // 자동 전환
        PlayInternal(target, resume: resumeLastPosition, fadeSeconds: crossFadeSeconds);
    }

    /// <summary>
    /// 외부에서 특정 곡을 강제로 재생하고 싶을 때 사용하는 API(선택).
    /// </summary>
    public void ForcePlay(AudioClip clip, float fadeSeconds = 0.5f, bool resume = true)
    {
        PlayInternal(clip, resume, fadeSeconds);
    }

    /// <summary>
    /// 현재 위치를 저장하고 정지(필요 시 호출).
    /// </summary>
    public void StopAndRemember()
    {
        if (mainSource && mainSource.clip != null)
            lastPositions[mainSource.clip] = mainSource.time;
        if (mainSource) mainSource.Stop();
    }

    private void PlayInternal(AudioClip clip, bool resume, float fadeSeconds)
    {
        if (clip == null) return;

        // 같은 클립이면 그대로(이어재생)
        if (mainSource.clip == clip)
        {
            if (mainSource && !mainSource.isPlaying) mainSource.Play();
            return;
        }

        // 현재 곡 위치 저장
        if (mainSource.clip != null)
            lastPositions[mainSource.clip] = mainSource.time;

        // ★ 새 전환 시작 전: 이전 코루틴/임시소스 안전 정리
        fadeNonce++; // 토큰 업데이트(이전 코루틴 무효화)
        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = null;
        }
        if (tempSource != null)
        {
            SafeStop(tempSource);
            Destroy(tempSource);
            tempSource = null;
        }

        if (fadeSeconds > 0f)
        {
            // 임시 소스로 새 곡 준비
            tempSource = gameObject.AddComponent<AudioSource>();
            CopyAudioSourceSettings(mainSource, tempSource);
            tempSource.clip = clip;
            tempSource.volume = 0f;

            if (resume && lastPositions.TryGetValue(clip, out float lastPos))
                tempSource.time = Mathf.Clamp(lastPos, 0f, Mathf.Max(0f, clip.length - 0.05f));

            if (!tempSource.isPlaying) tempSource.Play();
            if (!mainSource.isPlaying) mainSource.Play(); // 혹시 멈춰있었다면

            int myNonce = fadeNonce; // 토큰 캡처
            crossfadeRoutine = StartCoroutine(CoCrossFade(mainSource, tempSource, fadeSeconds, defaultVolume, myNonce));
        }
        else
        {
            // 즉시 전환
            mainSource.clip = clip;
            if (resume && lastPositions.TryGetValue(clip, out float lastPos))
                mainSource.time = Mathf.Clamp(lastPos, 0f, Mathf.Max(0f, clip.length - 0.05f));
            else
                mainSource.time = 0f;

            if (!mainSource.isPlaying) mainSource.Play();
        }
    }

    private IEnumerator CoCrossFade(AudioSource from, AudioSource to, float duration, float targetVolume, int myNonce)
    {
        float t = 0f;
        float fromStartVol = (from ? from.volume : 0f);

        while (t < duration)
        {
            // 새 전환 시작/파괴/소스 소실 시 안전 종료
            if (myNonce != fadeNonce || this == null || !from || !to)
                yield break;

            t += Time.unscaledDeltaTime; // 타임스케일 영향 X
            float k = Mathf.Clamp01(t / duration);

            if (from) from.volume = Mathf.Lerp(fromStartVol, 0f, k);
            if (to) to.volume = Mathf.Lerp(0f, targetVolume, k);

            yield return null;
        }

        // 최종 이관(여기도 방어)
        if (myNonce != fadeNonce || this == null || !from || !to)
            yield break;

        var playingClip = to.clip;
        float playingTime = to.time;

        try { if (from) { from.volume = 0f; from.Stop(); } } catch { /* ignore */ }

        if (to) { Destroy(to); } // 임시 소스 파괴
        tempSource = null;

        if (this != null && mainSource)
        {
            mainSource.clip = playingClip;
            mainSource.time = playingTime;
            mainSource.volume = targetVolume;
            if (!mainSource.isPlaying) mainSource.Play();
        }

        crossfadeRoutine = null;
    }

    private void CopyAudioSourceSettings(AudioSource src, AudioSource dst)
    {
        if (!src || !dst) return;
        dst.loop = src.loop;
        dst.spatialBlend = src.spatialBlend;
        dst.playOnAwake = false;
        dst.pitch = src.pitch;
        dst.priority = src.priority;
        dst.outputAudioMixerGroup = src.outputAudioMixerGroup;
    }

    private void SafeStop(AudioSource src)
    {
        try { if (src) src.Stop(); } catch { /* ignore */ }
    }
}
