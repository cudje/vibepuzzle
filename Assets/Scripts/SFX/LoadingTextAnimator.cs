using System.Collections;
using UnityEngine;
using TMPro;

public class LoadingTextAnimator : MonoBehaviour
{
    public TMP_Text targetText;   // "로딩중" 표시할 TMP_Text
    public float interval = 0.3f; // 점 바뀌는 간격(초)

    private string baseText = "로딩중";
    private int dotCount = 0;
    private Coroutine routine;

    void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        routine = StartCoroutine(AnimateDots());
    }

    void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    IEnumerator AnimateDots()
    {
        while (true)
        {
            dotCount = (dotCount % 3) + 1; // 1 → 2 → 3 → 1 반복
            targetText.text = baseText + new string('.', dotCount);
            yield return new WaitForSeconds(interval);
        }
    }
}
