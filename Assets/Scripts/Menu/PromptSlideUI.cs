using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PromptSlideUI : MonoBehaviour
{
    public RectTransform panelRect;      // 프롬프트 패널
    public float slideDistance = 300f;   // 슬라이드 이동 거리
    public float slideDuration = 0.25f;  // 애니메이션 시간

    public RectTransform iconToRotate;   // 회전시킬 이미지 (예: 화살표 아이콘)

    private bool isVisible = true;
    private Vector2 originalPanelPos;

    void Start()
    {
        originalPanelPos = panelRect.anchoredPosition;

        // 버튼 클릭 이벤트 등록
        GetComponent<Button>().onClick.AddListener(TogglePanel);
    }

    void TogglePanel()
    {
        StopAllCoroutines();

        if (isVisible)
        {
            StartCoroutine(Slide(panelRect, originalPanelPos, originalPanelPos + new Vector2(slideDistance, 0)));
        }
        else
        {
            StartCoroutine(Slide(panelRect, panelRect.anchoredPosition, originalPanelPos));
        }

        RotateIcon();

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

        float targetZ = isVisible ? 90f : 270f;

        iconToRotate.localEulerAngles = new Vector3(
            iconToRotate.localEulerAngles.x,
            iconToRotate.localEulerAngles.y,
            targetZ
        );
    }
}