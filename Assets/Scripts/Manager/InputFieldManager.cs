using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

/// <summary>
/// InputFieldManager (단일 스크립트)
/// - 작은 TMP_InputField를 탭하면 전체 확장 (부모 이동 + 앵커 풀스크린)
/// - 오버레이 페이드 인/아웃으로 표시, 레이캐스트 차단
/// - 닫기 버튼: 원래 부모/Rect로 복원
/// - 전체 지우기 버튼: 입력 텍스트 모두 삭제
///
/// 기존 개별 스크립트들을 하나로 결합:
/// 1) BackToTheFuture: RectTransform 상태 백업/복원 + 전체 확장
/// 2) InputExpandToTarget: 대상 부모로 이동 후 전체 채우기
/// 3) FullscreenOverlay: CanvasGroup 페이드 표시/숨김
/// 4) ClearInputButton: TMP_InputField 텍스트 지우기
///
/// *추가/수정 기능 없이 위 4가지 동작만 그대로 수행합니다.
///
/// 구성 예 (사용자 제공 구조 기준):
///   Prompt
///     ├─ Text (TMP)
///     ├─ InputField (TMP)   ← smallInput 에 할당 & 이 스크립트 부착
///     └─ Send_Button
///   overlay                  ← overlayRoot (CanvasGroup 포함) 에 할당
///     ├─ close_button        ← closeButton 에 할당 (OnClick = Collapse)
///     ├─ all_remove_button   ← clearAllButton 에 할당 (OnClick = ClearAll)
///     └─ Panel               ← targetParent 에 할당 (확장시 이 밑으로 이동)
/// </summary>
public class InputFieldManager : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] float overlayFade = 0.25f;         // FullscreenOverlay와 동일 동작

    [Header("옵션")]
    [SerializeField] bool ignoreLayoutOnExpand = true;  // InputExpandToTarget 과 동일 동작

    // ----- 내부 상태 (BackToTheFuture와 동일 개념의 백업) -----
    RectTransform rt;               // smallInput 캐시
    Transform originalParent;
    int originalSiblingIndex;
    Vector2 origAnchorMin, origAnchorMax, origAnchoredPos, origSizeDelta, origPivot;
    bool isExpanded = false;

    void Start()
    {
        rt = SceneHubManager.I.promptTMPInputField.transform.GetComponent<RectTransform>();
        ShowOverlay(false, true); // 시작은 숨김
        BackupRect();

        // 버튼 연결 (단순 연결만, 기능 추가 없음)
        SceneHubManager.I.overlayCloseButton.onClick.AddListener(Collapse);
        SceneHubManager.I.overlayClearButton.onClick.AddListener(ClearAll);
    }

    // 초기 RectTransform 상태 백업 (BackToTheFuture.BackupRect)
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

    // 원래 상태로 되돌리기 (BackToTheFuture.RestoreRect)
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

    // 작은 입력필드를 탭했을 때 (IPointerDownHandler & Expand)
    public void OnPointerDown(PointerEventData _)
    {
        if (isExpanded) return;
        Expand();
    }

    // 전체 확장: 대상 부모로 이동 후 전체 채우기 (InputExpandToTarget + BackToTheFuture.OnPointerDown)
    public void Expand()
    {
        if (isExpanded || rt == null) return;

        // (선택) 레이아웃 간섭 끄기
        if (ignoreLayoutOnExpand)
        {
            var le = rt.GetComponent<LayoutElement>();
            if (!le) le = rt.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true; // 원본 스크립트와 동일: 복원 로직 추가하지 않음
        }

        // 목표 부모로 이동 (InputExpandToTarget)
        rt.SetParent(SceneHubManager.I.overlayPanelRectT, false);

        // 새 부모 기준으로 꽉 채우기 (BackToTheFuture의 전체 확장 값 사용)
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

    // 닫기: 원래 상태 복구 + 오버레이 숨김
    public void Collapse()
    {
        if (!isExpanded) return;
        RestoreRect();
        ShowOverlay(false);
    }

    // 전체 지우기 (ClearInputButton.ClearText)
    public void ClearAll()
    {
        SceneHubManager.I.promptTMPInputField.text = string.Empty;
    }

    // ----- FullscreenOverlay 동작 그대로 -----
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

    // 버튼에 쉽게 연결할 수 있는 래퍼 (원본 FullscreenOverlay와 동일 API)
    public void ShowOn()  => ShowOverlay(true);
    public void ShowOff() => ShowOverlay(false);
}
