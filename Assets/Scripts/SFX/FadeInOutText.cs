using System.Collections;
using UnityEngine;
using TMPro;


public class FadeInOutText : MonoBehaviour
{
    private float fadeInDuration = 1f;
    private float visibleDuration = 0.7f;
    private float fadeOutDuration = 0.6f;

    private TMP_Text alarmTMPText;
    private Coroutine routine;
    private GameObject alarm;

    void Awake()
    {
        alarm = transform.Find("StageStartAlarm_TMPText").gameObject;
        alarmTMPText = alarm.GetComponent<TMP_Text>();
        alarmTMPText.text = GetText();
        alarm.SetActive(true);
    }

    void OnEnable()
    {
        routine = StartCoroutine(FadeSequence());
    }

    void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    IEnumerator FadeSequence()
    {
        // 시작할 때 투명하게 초기화
        Color c = alarmTMPText.color;
        c.a = 0f;
        alarmTMPText.color = c;

        // 1) 페이드 인
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / fadeInDuration);
            alarmTMPText.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        // 2) 유지
        yield return new WaitForSeconds(visibleDuration);

        // 3) 페이드 아웃
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (t / fadeOutDuration));
            alarmTMPText.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        // 최종적으로 완전히 투명
        c.a = 0f;
        alarmTMPText.color = c;
        alarm.SetActive(false);
    }

    private string GetText()
    {
        string numberPart = GameData.recentStage.Substring(1);
        int.TryParse(numberPart, out int result);
        switch (result)
        {
            case 1: return "스테이지 1 : 프롬프트 배우기";
            case 2: return "스테이지 2 : 부품 줍기";
            case 3: return "스테이지 3 : 부품 옮기기";
            case 4: return "스테이지 4 : 여러 로드 명령하기";
            case 5: return "스테이지 5 : 조건문 배우기";
            default: return "";
        }
    }
}
