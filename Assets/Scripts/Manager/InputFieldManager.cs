using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class InputFieldManager : MonoBehaviour, 
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] float overlayFade = 0.25f;

    [Header("옵션")]
    [SerializeField] bool ignoreLayoutOnExpand = true;

    [Header("탭 판정 임계값")]
    [SerializeField] float tapTime = 0.25f;    // 누르고 뗄 때까지 걸린 시간(초)이 이 값 이하이면 탭 후보
    [SerializeField] float tapMovePx = 10f;    // 누른 위치 대비 이동 픽셀이 이 값 이하이면 탭 후보

    // 내부 상태
    RectTransform rt;
    Transform originalParent;
    int originalSiblingIndex;
    Vector2 origAnchorMin, origAnchorMax, origAnchoredPos, origSizeDelta, origPivot;
    bool isExpanded = false;

    // 작은 상태 입력 제스처 판정용
    bool pointerDown;
    bool draggingWhileSmall;
    Vector2 downPos;
    float downTime;

    void Start()
    {
        rt = SceneHubManager.I.promptTMPInputField.transform.GetComponent<RectTransform>();
        ShowOverlay(false, true);
        BackupRect();

        SceneHubManager.I.overlayCloseButton.onClick.AddListener(Collapse);
        SceneHubManager.I.overlayClearButton.onClick.AddListener(ClearAll);
    }

    void BackupRect()
    {
        if (!rt) return;
        origAnchorMin   = rt.anchorMin;
        origAnchorMax   = rt.anchorMax;
        origAnchoredPos = rt.anchoredPosition;
        origSizeDelta   = rt.sizeDelta;
        origPivot       = rt.pivot;
        originalParent       = rt.parent;
        originalSiblingIndex = rt.GetSiblingIndex();
    }

    public void RestoreRect()
    {
        if (!rt) return;
        rt.anchorMin        = origAnchorMin;
        rt.anchorMax        = origAnchorMax;
        rt.anchoredPosition = origAnchoredPos;
        rt.sizeDelta        = origSizeDelta;
        rt.pivot            = origPivot;
        rt.SetParent(originalParent, false);
        rt.SetSiblingIndex(originalSiblingIndex);
        isExpanded = false;
    }

    // ▼ 기존: OnPointerDown에서 곧바로 Expand() → (수정) 기록만 하고 대기
    public void OnPointerDown(PointerEventData e)
    {
        if (isExpanded) return;
        pointerDown = true;
        draggingWhileSmall = false;
        downPos = e.position;
        downTime = Time.unscaledTime;
    }

    // 드래그가 시작되면 "작은 상태 드래그"로 간주
    public void OnBeginDrag(PointerEventData e)
    {
        if (isExpanded) return;
        if (!pointerDown) return; // 안전장치
        draggingWhileSmall = true;
        // 필요한 경우: 작은 상태에서 드래그 시 동작(스크롤 위임 등)을 여기서 처리
        // Debug.Log("Small BeginDrag");
    }

    public void OnDrag(PointerEventData e)
    {
        if (isExpanded) return;
        if (!draggingWhileSmall) return;
        // 작은 상태 드래그 중… 필요 시 처리
        // Debug.Log("Small Drag " + e.delta);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (isExpanded) return;
        if (!draggingWhileSmall) return;
        draggingWhileSmall = false;
        pointerDown = false;
        // Debug.Log("Small EndDrag");
    }

    // 손을 뗄 때, 드래그가 없고 짧고 거의 안 움직였으면 탭 → Expand()
    public void OnPointerUp(PointerEventData e)
    {
        if (isExpanded) return;

        float dt = Time.unscaledTime - downTime;
        float moved = Vector2.Distance(downPos, e.position);

        bool isTapLike = dt <= tapTime && moved <= tapMovePx;

        pointerDown = false;

        if (!draggingWhileSmall && isTapLike)
        {
            Expand();
        }
        else
        {
            // 작은 상태에서 드래그로 간주된 경우: 아무 것도 하지 않고 유지
            // Debug.Log("Small gesture was drag or long/large move; stay collapsed");
        }
    }

    public void Expand()
    {
        if (isExpanded || rt == null) return;

        if (ignoreLayoutOnExpand)
        {
            var le = rt.GetComponent<LayoutElement>();
            if (!le) le = rt.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true; // 원 코드 의도 유지(복원은 하지 않음)
        }

        rt.SetParent(SceneHubManager.I.overlayPanelRectT, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;

        isExpanded = true;
        ShowOverlay(true);
    }

    public void Collapse()
    {
        if (!isExpanded) return;
        RestoreRect();
        ShowOverlay(false);
    }

    public void ClearAll()
    {
        SceneHubManager.I.promptTMPInputField.text = string.Empty;
    }

    void ShowOverlay(bool on, bool instant = false)
    {
        if (!SceneHubManager.I.overlayCanvasG) return;
        StopAllCoroutines();
        if (instant)
        {
            SceneHubManager.I.overlayCanvasG.alpha = on ? 1f : 0f;
            SceneHubManager.I.overlayCanvasG.blocksRaycasts = on;
            SceneHubManager.I.overlayCanvasG.interactable   = on;
            return;
        }
        StartCoroutine(FadeOverlay(on));
    }

    IEnumerator FadeOverlay(bool on)
    {
        float s = SceneHubManager.I.overlayCanvasG.alpha;
        float e = on ? 1f : 0f;
        float t = 0f;
        while (t < overlayFade)
        {
            t += Time.unscaledDeltaTime;
            SceneHubManager.I.overlayCanvasG.alpha = Mathf.Lerp(s, e, t / overlayFade);
            yield return null;
        }
        SceneHubManager.I.overlayCanvasG.alpha = e;
        SceneHubManager.I.overlayCanvasG.blocksRaycasts = on;
        SceneHubManager.I.overlayCanvasG.interactable   = on;
    }

    public void ShowOn()  => ShowOverlay(true);
    public void ShowOff() => ShowOverlay(false);
}
