using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class LetterboxUI : MonoBehaviour
{
    [Header("검은 바 색상")]
    public Color barColor = Color.black;

    [SerializeField] private Image topBar, bottomBar, leftBar, rightBar;

    Canvas canvas;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        EnsureBars();
        Apply(); // 초기 1회
    }

    void Update() => Apply(); // 해상도/회전/안전영역 변화 대응(부하 적음)

    void EnsureBars()
    {
        if (!topBar) topBar = CreateBar("TopBar");
        if (!bottomBar) bottomBar = CreateBar("BottomBar");
        if (!leftBar) leftBar = CreateBar("LeftBar");
        if (!rightBar) rightBar = CreateBar("RightBar");

        SetupEdge(topBar.rectTransform, new Vector2(0, 1), new Vector2(1, 1)); // 상단
        SetupEdge(bottomBar.rectTransform, new Vector2(0, 0), new Vector2(1, 0)); // 하단
        SetupEdge(leftBar.rectTransform, new Vector2(0, 0), new Vector2(0, 1)); // 좌측
        SetupEdge(rightBar.rectTransform, new Vector2(1, 0), new Vector2(1, 1)); // 우측

        topBar.raycastTarget = bottomBar.raycastTarget = leftBar.raycastTarget = rightBar.raycastTarget = false;
        topBar.color = bottomBar.color = leftBar.color = rightBar.color = barColor;
    }

    Image CreateBar(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        return go.GetComponent<Image>();
    }

    void SetupEdge(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void Apply()
    {
        // 1) 카메라의 viewport rect -> 실제 픽셀 값으로 변환
        float sw = Screen.width;
        float sh = Screen.height;
        Rect v = SceneHubManager.I.mainCamera.rect; // [0..1] 기준
        Rect camPixelRect = new Rect(v.x * sw, v.y * sh, v.width * sw, v.height * sh);

        // 2) Canvas 픽셀 ↔ UI단위 보정
        //    Overlay든 Camera든 pixelRect와 scaleFactor로 정확히 보정
        float scale = canvas ? canvas.scaleFactor : 1f;

        float leftW = Mathf.Round(camPixelRect.xMin) / scale;
        float rightW = Mathf.Round(sw - camPixelRect.xMax) / scale;
        float bottomH = Mathf.Round(camPixelRect.yMin) / scale;
        float topH = Mathf.Round(sh - camPixelRect.yMax) / scale;

        // 3) 각 바의 offset으로 정확히 덮기 (틈 방지)
        SetSideBarWidth(leftBar.rectTransform, leftW, isLeft: true);
        SetSideBarWidth(rightBar.rectTransform, rightW, isLeft: false);
        SetTopBottomHeight(bottomBar.rectTransform, bottomH, isTop: false);
        SetTopBottomHeight(topBar.rectTransform, topH, isTop: true);

        leftBar.enabled = leftW > 0.5f / scale;
        rightBar.enabled = rightW > 0.5f / scale;
        topBar.enabled = topH > 0.5f / scale;
        bottomBar.enabled = bottomH > 0.5f / scale;
    }

    void SetSideBarWidth(RectTransform rt, float width, bool isLeft)
    {
        // 엣지 앵커 상태에서 offset으로 두께만 지정
        Vector2 min = rt.offsetMin;
        Vector2 max = rt.offsetMax;
        if (isLeft)
        {
            min.x = 0f;
            max.x = width;
        }
        else
        {
            min.x = -width;
            max.x = 0f;
        }
        min.y = 0f; max.y = 0f;
        rt.offsetMin = min;
        rt.offsetMax = max;
    }

    void SetTopBottomHeight(RectTransform rt, float height, bool isTop)
    {
        Vector2 min = rt.offsetMin;
        Vector2 max = rt.offsetMax;
        if (isTop)
        {
            min.y = -height;
            max.y = 0f;
        }
        else
        {
            min.y = 0f;
            max.y = height;
        }
        min.x = 0f; max.x = 0f;
        rt.offsetMin = min;
        rt.offsetMax = max;
    }
}
