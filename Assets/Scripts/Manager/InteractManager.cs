using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class InteractManager : MonoBehaviour
{
    [Header("Buttons")]
    public Behavior3DManager behavior;                 // 인스펙터에 연결
    public PromptInterpreter promptInterpreter;

    [Header("UI Sprites")]
    public Sprite playSprite;
    public Sprite pauseSprite;

    [Header("UI Components")]
    public Image pauseButtonImage;


    private bool paused;

    [Header("Ceiling (Component Control)")]
    public bool controlRenderers = true;  // 렌더러 토글 (보이기/숨기기)
    public bool controlColliders = false; // 콜라이더 토글 (충돌/레이캐스트 막기까지)

    private bool isTop = false;

    // 캐시: 자식들까지 포함한 컴포넌트 목록과 초기 상태
    private Renderer[] renderers;
    private bool[] rendererInitEnabled;
    private Collider[] colliders;
    private bool[] colliderInitEnabled;

    void Start()
    {
        SceneHubManager.I.switchCameraButton.onClick.AddListener(ToggleCamera);
        SceneHubManager.I.pauseButton.onClick.AddListener(TogglePausePlay);
        SceneHubManager.I.resetButton.onClick.AddListener(DoReset);
        SceneHubManager.I.pauseButton.interactable = false;
        SceneHubManager.I.resetButton.interactable = false;
        CacheCeilingComponents();
        if (promptInterpreter != null)
        {
            promptInterpreter.OnScriptStarted += HandleScriptStarted;
        }
        if (promptInterpreter && promptInterpreter.condition != null)
        {
            promptInterpreter.condition.OnCheckClear += HandleCheckClear;
        }

        ShowMain(false); // 시작은 메인뷰
    }

    private void HandleCheckClear()
    {
        // 스테이지 성공이든 실패든 무조건 반응
        if (pauseButtonImage != null && playSprite != null)
            pauseButtonImage.sprite = playSprite;
    }

    private void HandleScriptStarted()
    {
        //  스크립트 실행 시작 시 버튼 활성화
        SceneHubManager.I.pauseButton.interactable = true;
        SceneHubManager.I.resetButton.interactable = true;
    }

    void TogglePausePlay()
    {
        var pi = promptInterpreter;
        if (pi == null) return;

        // 1) Idle(아무 것도 안 돌고 있거나 Reset 직후) → 처음부터 시작
        if (pi.IsIdle)
        {
            // Reset 직후 “이전에 사용한 스크립트”는 split_monjang에 이미 남아있음
            pi.StartScriptFromBeginning();
            pi.PauseScript(false);
            paused = false;
            Debug.Log("[UI] Play (from start)");
        }

        // 2) 실행 중 → Pause
        else if (pi.IsRunning)
        {
            pi.PauseScript(true);
            paused = true;
            Debug.Log("[UI] Paused");
        }

        // 3) Pause 상태 → Resume
        else if (pi.IsPaused)
        {
            pi.PauseScript(false);
            paused = false;
            Debug.Log("[UI] Resumed");
        }
        UpdateButtonImage();
    }

    private void UpdateButtonImage()
    {
        if (pauseButtonImage == null) return;

        pauseButtonImage.sprite = paused ? playSprite : pauseSprite;
    }

    public void DoReset()
    {
        // 즉시 중단
        promptInterpreter?.StopScript();
        behavior?.HardStopAll();
        behavior?.ResetAllToStartPose();
        behavior?.PauseLogical(false);

        paused = false; // 상태값 초기화
        pauseButtonImage.sprite = playSprite;

        Debug.Log("[UI] Reset done");
    }

    // ---- 카메라 토글 ----
    public void ToggleCamera()
    {
        if (isTop) ShowMain(true);
        else ShowTop(true);
    }

    public void ShowTop(bool _)
    {
        isTop = true;

        SceneHubManager.I.mainCamera.enabled = false;
        SceneHubManager.I.TopviewCamera.enabled = true;
        ToggleAudioListener(SceneHubManager.I.mainCamera, false);
        ToggleAudioListener(SceneHubManager.I.TopviewCamera, true);

        // 천장 컴포넌트 끄기
        SetCeilingVisible(false);   // Renderer
        SetCeilingCollidable(false);// Collider(옵션)

        SceneHubManager.I.variableJoystick.SetActive(false);
        SceneHubManager.I.variableJoystick.GetComponent<VariableJoystick>().OnPointerUp(new PointerEventData(EventSystem.current));
        StartCoroutine(WaitForTimes());
        SceneHubManager.I.JoystickManager.SetActive(false);
    }

    IEnumerator WaitForTimes()
    {
        yield return new WaitForSeconds(0.25f);
    }

    public void ShowMain(bool _)
    {
        isTop = false;

        SceneHubManager.I.TopviewCamera.enabled = false;
        SceneHubManager.I.mainCamera.enabled = true;
        ToggleAudioListener(SceneHubManager.I.TopviewCamera, false);
        ToggleAudioListener(SceneHubManager.I.mainCamera, true);

        // 초기 상태로 복구
        RestoreCeilingStates();

        // 메인뷰 복귀 → 이동 재개
        SceneHubManager.I.variableJoystick.SetActive(true);
        SceneHubManager.I.JoystickManager.SetActive(true);
    }

    public void ResetLevel()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    // ---- 내부 로직 ----
    private void CacheCeilingComponents()
    {
        if (!SceneHubManager.I.cellingT) return;

        // 자식까지 전부 포함해서 가져오기 (비활성 자식도 포함하려면 true)
        renderers = controlRenderers ? SceneHubManager.I.cellingT.GetComponentsInChildren<Renderer>(true) : null;
        if (renderers != null)
        {
            rendererInitEnabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                rendererInitEnabled[i] = renderers[i] ? renderers[i].enabled : false;
        }

        colliders = controlColliders ? SceneHubManager.I.cellingT.GetComponentsInChildren<Collider>(true) : null;
        if (colliders != null)
        {
            colliderInitEnabled = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
                colliderInitEnabled[i] = colliders[i] ? colliders[i].enabled : false;
        }
    }

    private void SetCeilingVisible(bool visible)
    {
        if (!controlRenderers || renderers == null) return;
        foreach (var r in renderers) if (r) r.enabled = visible;
    }

    private void SetCeilingCollidable(bool enable)
    {
        if (!controlColliders || colliders == null) return;
        foreach (var c in colliders) if (c) c.enabled = enable;
    }

    private void RestoreCeilingStates()
    {
        if (controlRenderers && renderers != null && rendererInitEnabled != null)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i]) renderers[i].enabled = rendererInitEnabled[i];
        }

        if (controlColliders && colliders != null && colliderInitEnabled != null)
        {
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i]) colliders[i].enabled = colliderInitEnabled[i];
        }
    }

    private void ToggleAudioListener(Camera cam, bool enable)
    {
        if (!cam) return;
        var al = cam.GetComponent<AudioListener>();
        if (al) al.enabled = enable; // 다중 AudioListener 경고 방지
    }
}