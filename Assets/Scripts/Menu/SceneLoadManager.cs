using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public string[] SceneNames;

    [Header("Loading Overlay Prefab (Canvas + CanvasGroup + Slider + LoadingOverlayController 포함)")]
    public GameObject loadingOverlayPrefab; // 인스펙터에 할당. 비어있으면 Resources에서 시도.

    [Header("UX")]
    public float minShowTime = 0.8f;
    public float fadeDuration = 0.25f;

    public void SetStageData(string stage)
    {
        GameData.SetRecentStage(stage);
    }

    public void load(int sceneNumber)
    {
        if (sceneNumber < 0 || sceneNumber >= SceneNames.Length)
        {
            Debug.LogError($"[SceneLoadManager] 잘못된 인덱스: {sceneNumber}");
            return;
        }
        StartCoroutine(LoadRoutine(SceneNames[sceneNumber]));
    }

    public void quitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator LoadRoutine(string targetScene)
    {
        // 프리팹 인스턴스 생성 & overlay 참조 획득
        var overlayGO = Instantiate(loadingOverlayPrefab);
        var overlay = overlayGO.GetComponent<LoadingOverlayController>();

        // 1) 페이드 인(검은 화면)
        yield return overlay.FadeIn();

        float start = Time.unscaledTime;

        // 2) 비동기 로드 (활성화 보류)
        var op = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        // 3) 진행률
        while (op.progress < 0.9f)
        {
            overlay.SetProgress(op.progress / 0.9f);
            yield return null;
        }
        overlay.SetProgress(1f);

        // 4) 최소 표시 시간
        while (Time.unscaledTime - start < minShowTime) yield return null;

        // 5) “씬 활성화 완료되면 내려가라” 완료 콜백 등록 (이중 안전장치)
        op.completed += _ =>
        {
            // 메인 스레드 컨텍스트이므로 바로 호출 OK
            if (overlay) overlay.RequestDestroy(instant: true);
        };

        // 6) 검은 화면 유지한 채 활성화
        overlay.HoldBlack(true);
        op.allowSceneActivation = true;

        // (선택) 완료까지 대기
        while (!op.isDone) yield return null;
        // 여기서 끝. overlay는 콜백/activeSceneChanged/워치독 중 하나로 반드시 제거됨.
    }

}
