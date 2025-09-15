using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingOverlayController : MonoBehaviour
{
    public static LoadingOverlayController Instance;   // 중복 생성 방지(선택)
    [Header("Refs")]
    public CanvasGroup canvasGroup;
    public Slider progressBar; // optional
    [Header("UX")]
    public float fadeDuration = 0.25f;

    // 이중 안전장치 플래그
    private bool destroyOnActiveSceneChanged = false;
    private bool destroyRequested = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 이미 있으면 새로 생긴 건 제거
            return;
        }
        Instance = this;

        if (!canvasGroup) canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        DontDestroyOnLoad(gameObject);

        // 최상단 보장
        var cv = GetComponent<Canvas>();
        if (cv)
        {
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 30000;
        }

        // 혹시 놓쳐도 내려가게: ActiveScene 바뀌면 제거
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (Instance == this) Instance = null;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (destroyOnActiveSceneChanged && !destroyRequested)
        {
            destroyRequested = true;
            StartCoroutine(DestroyNextFrame());
        }
    }

    public void SetProgress(float v)
    {
        if (progressBar) progressBar.value = Mathf.Clamp01(v);
    }

    public IEnumerator FadeIn()
    {
        if (!canvasGroup) yield break;
        float t = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    // 씬 전환 직전: 검은 화면 유지
    public void HoldBlack(bool on = true)
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = on ? 1f : canvasGroup.alpha;
        canvasGroup.blocksRaycasts = on;
    }

    // 매니저가 호출: “반드시 내려가라” 신호 + 완료 콜백/씬변경 모두 대응
    public void RequestDestroy(bool instant = true, float microFade = 0.12f)
    {
        if (destroyRequested) return;
        destroyRequested = true;
        destroyOnActiveSceneChanged = true; // 씬 바뀌면 무조건 내려가도록

        if (instant) StartCoroutine(DestroyNextFrame());
        else StartCoroutine(FadeOutThenDestroy(microFade));

        // 워치독: 혹시 위가 실패해도 N초 뒤 강제 제거
        StartCoroutine(WatchdogForceKill(3f));
    }

    private IEnumerator DestroyNextFrame()
    {
        // 새 씬 첫 프레임 렌더 뒤 정리(깜빡임/초기화 스파이크 회피)
        yield return null;
        Destroy(gameObject);
    }

    private IEnumerator FadeOutThenDestroy(float duration)
    {
        if (!canvasGroup) { yield return null; Destroy(gameObject); yield break; }
        float t = 0f, start = canvasGroup.alpha;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }

    private IEnumerator WatchdogForceKill(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds; // timeScale=0에도 동작
        while (Time.realtimeSinceStartup < end) yield return null;
        if (this) Destroy(gameObject);
    }
}
