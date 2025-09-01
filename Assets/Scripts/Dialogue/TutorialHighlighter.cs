using UnityEngine;
using UnityEngine.UI;

public class TutorialHighlighter : MonoBehaviour
{
    [Header("References")]
    public Canvas overlayCanvas;          // TutorialOverlay�� ���� ĵ����
    public RectTransform overlayRoot;     // TutorialOverlay�� RectTransform
    public RectTransform top, bottom, left, right; // �� �� �г�

    [Header("Settings")]
    public float padding = 16f;           // ���� �����ڸ� ����
    public bool followTarget = true;      // ����� �����̸� ����

    RectTransform currentTarget;
    Camera cam;                           // UI ī�޶�(Overlay�� null)
    bool visible = false;

    void Awake()
    {
        if (overlayCanvas && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = overlayCanvas.worldCamera; // Screen Space - Camera�� ��
        Hide();
    }

    void LateUpdate()
    {
        if (visible && followTarget && currentTarget)
            RepositionTo(currentTarget);
    }

    public void Highlight(RectTransform target, float? customPadding = null)
    {
        currentTarget = target;
        if (customPadding.HasValue) padding = customPadding.Value;
        visible = true;
        gameObject.SetActive(true);
        RepositionTo(target);
    }

    public void Hide()
    {
        visible = false;
        gameObject.SetActive(false);
        currentTarget = null;
    }

    void RepositionTo(RectTransform target)
    {
        if (!target || !overlayRoot) return;

        // ����� ���� �ڳ� �� �������� ���� ��ǥ
        Vector3[] w = new Vector3[4];
        target.GetWorldCorners(w);

        Vector2 bl = WorldToOverlayLocal(w[0]);
        Vector2 tr = WorldToOverlayLocal(w[2]);

        var r = overlayRoot.rect;
        float xMin = bl.x - padding;
        float yMin = bl.y - padding;
        float xMax = tr.x + padding;
        float yMax = tr.y + padding;

        // �� �� �г� ��ġ (�θ� ���� inset/size)
        SetLeft(Mathf.Max(0, xMin - r.xMin));
        SetRight(Mathf.Max(0, r.xMax - xMax));
        SetBottom(Mathf.Max(0, yMin - r.yMin), xMin - r.xMin, r.xMax - xMax);
        SetTop(Mathf.Max(0, r.yMax - yMax), xMin - r.xMin, r.xMax - xMax);

    }

    Vector2 WorldToOverlayLocal(Vector3 world)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screen, cam, out var local);
        return local;
    }

    void SetLeft(float width)
    {
        if (!left) return;
        left.anchorMin = new Vector2(0, 0);
        left.anchorMax = new Vector2(0, 1);
        left.pivot = new Vector2(1, 0.5f);
        left.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, width);
    }

    void SetRight(float width)
    {
        if (!right) return;
        right.anchorMin = new Vector2(1, 0);
        right.anchorMax = new Vector2(1, 1);
        right.pivot = new Vector2(0, 0.5f);
        right.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, width);
    }

    void SetBottom(float height, float leftCut, float rightCut)
    {
        if (!bottom) return;
        bottom.anchorMin = new Vector2(0, 0);
        bottom.anchorMax = new Vector2(1, 0);
        bottom.pivot = new Vector2(0.5f, 1);
        bottom.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, height);

        // ��/�� �߶󳻱�(���� ����ŭ ����� �������� ���� ����)
        bottom.offsetMin = new Vector2(leftCut, bottom.offsetMin.y);
        bottom.offsetMax = new Vector2(-rightCut, bottom.offsetMax.y);
    }

    void SetTop(float height, float leftCut, float rightCut)
    {
        if (!top) return;
        top.anchorMin = new Vector2(0, 1);
        top.anchorMax = new Vector2(1, 1);
        top.pivot = new Vector2(0.5f, 0);
        top.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, height);

        top.offsetMin = new Vector2(leftCut, top.offsetMin.y);
        top.offsetMax = new Vector2(-rightCut, top.offsetMax.y);
    }
}

public class TutorialSteps : MonoBehaviour
{
    public TutorialHighlighter highlighter;

    [Header("Targets")]
    public RectTransform homeButton;
    public RectTransform joystick;
    public RectTransform mapButton;

    public DialogueManager dialogueManager;

    public void Step_Home()
    {
        dialogueManager.StartDialogue( /* �ܰ� ��ȣ */ 1);
        highlighter.Highlight(homeButton, 12f);
    }

    public void Step_Joystick()
    {
        dialogueManager.NextDialogue();
        highlighter.Highlight(joystick, 16f);
    }

    public void Step_Map()
    {
        dialogueManager.NextDialogue();
        highlighter.Highlight(mapButton, 12f);
    }

    public void Step_End()
    {
        dialogueManager.NextDialogue();
        highlighter.Hide();
    }
}