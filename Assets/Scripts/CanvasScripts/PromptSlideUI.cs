using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PromptSlideUI : MonoBehaviour
{
    public RectTransform panelRect;      // ������Ʈ �г�
    public RectTransform buttonRect;     // ��ư ��ü
    public float slideDistance = 300f;   // �����̵� �̵� �Ÿ�
    public float slideDuration = 0.25f;  // �ִϸ��̼� �ð�

    public RectTransform iconToRotate;   // ȸ����ų �̹��� (��: ȭ��ǥ ������)

    private bool isVisible = true;
    private Vector2 originalPanelPos;
    private Vector2 originalButtonPos;
    private Quaternion originalIconRotation;

    void Start()
    {
        originalPanelPos = panelRect.anchoredPosition;
        originalButtonPos = buttonRect.anchoredPosition;

        if (iconToRotate != null)
            originalIconRotation = iconToRotate.rotation;

        // ��ư Ŭ�� �̺�Ʈ ���
        GetComponent<Button>().onClick.AddListener(TogglePanel);
    }

    void TogglePanel()
    {
        StopAllCoroutines();

        if (isVisible)
        {
            StartCoroutine(Slide(panelRect, originalPanelPos, originalPanelPos + new Vector2(slideDistance, 0)));
            StartCoroutine(Slide(buttonRect, originalButtonPos, originalButtonPos + new Vector2(slideDistance, 0)));
        }
        else
        {
            StartCoroutine(Slide(panelRect, panelRect.anchoredPosition, originalPanelPos));
            StartCoroutine(Slide(buttonRect, buttonRect.anchoredPosition, originalButtonPos));
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