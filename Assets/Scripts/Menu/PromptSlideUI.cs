using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PromptSlideUI : MonoBehaviour
{
    [Header("슬라이드 패널 설정")]
    public RectTransform panelRect;      // 움직일 패널

    [Header("아이콘 회전")]
    public RectTransform iconToRotate;   // 회전시킬 아이콘 (예: 화살표)

    private float slideDistance = -600f;   // 이동 거리(+X로 숨김)
    private float slideDuration = 0.1f;  // 애니메이션 시간
    private bool isVisible = true;
    private Vector2 originalPanelPos;
    private CanvasGroup inputCanvasGroup;
    private CanvasGroup sendCanvasGroup;

    void Start()
    {
        if (!panelRect)
        {
            Debug.LogError("[PromptSlideUI] panelRect 미지정");
            enabled = false;
            return;
        }

        originalPanelPos = panelRect.anchoredPosition;

        // 버튼 클릭 이벤트 연결(같은 오브젝트에 Button 컴포넌트가 있어야 함)
        var btn = GetComponent<Button>();
        if (btn) btn.onClick.AddListener(TogglePanel);
        else Debug.LogWarning("[PromptSlideUI] 같은 오브젝트에 Button 이 없습니다.");

        inputCanvasGroup = SceneHubManager.I.promptTMPInputField.GetComponent<CanvasGroup>();
        sendCanvasGroup = SceneHubManager.I.promptSendButton.GetComponent<CanvasGroup>();

        // 초기 상태: 꺼진 상태(레이캐스트 막음)
        inputCanvasGroup.blocksRaycasts = false;
        sendCanvasGroup.blocksRaycasts = false;
    }

    void TogglePanel()
    {
        StopAllCoroutines();

        if (isVisible)
        {
            // 보이는 상태 → 오른쪽으로 숨김
            StartCoroutine(Slide(panelRect, originalPanelPos, originalPanelPos + new Vector2(slideDistance, 0)));
        }
        else
        {
            // 숨김 상태 → 원위치로 보이기
            StartCoroutine(Slide(panelRect, panelRect.anchoredPosition, originalPanelPos));
        }

        // 아이콘 시각 갱신(현재 상태 기준)
        RotateIcon();

        // 버튼을 누를 때마다 blocksRaycasts 토글
        ToggleBlocksRaycasts();

        // 최종적으로 상태 반전
        isVisible = !isVisible;
    }

    IEnumerator Slide(RectTransform target, Vector2 from, Vector2 to)
    {
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            target.anchoredPosition = Vector2.Lerp(from, to, t / slideDuration);
            yield return null;
        }
        target.anchoredPosition = to;
    }

    void RotateIcon()
    {
        if (iconToRotate == null) return;

        // 현재 isVisible 기준으로 회전각 적용
        float targetZ = isVisible ? 90f : 270f;

        iconToRotate.localEulerAngles = new Vector3(
            iconToRotate.localEulerAngles.x,
            iconToRotate.localEulerAngles.y,
            targetZ
        );
    }

    void ToggleBlocksRaycasts()
    {
        inputCanvasGroup.blocksRaycasts = !inputCanvasGroup.blocksRaycasts;
        sendCanvasGroup.blocksRaycasts = !sendCanvasGroup.blocksRaycasts;
    }
}
