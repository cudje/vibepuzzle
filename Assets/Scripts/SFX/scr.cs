using System.Collections;
using UnityEngine;

public class RoboHopImpact : MonoBehaviour
{
    [Header("Hop")]
    public float hopHeight   = 0.35f;
    public float upTime      = 0.25f;   // 떠오르는 시간
    public float downTime    = 0.18f;   // 내려오는 시간(조금 빠르게)
    public float groundPause = 0.08f;   // 바닥에서 쉬는 시간

    [Header("Squash & Stretch")]
    public Transform body;            // 스케일 줄 대상
    public float squashAmount = 0.12f; // 바닥에서 납작 정도
    public Transform shadow;

    Vector3 basePos;
    Vector3 bodyBaseScale;

    void Start()
    {
        basePos = transform.position;
        if (!body) body = transform;
        bodyBaseScale = body.localScale;
        StartCoroutine(HopLoop());
    }

    IEnumerator HopLoop()
    {
        while (true)
        {
            // 바닥에서 살짝 스쿼시
            SetSquash(1f + squashAmount, 1f - squashAmount);

            // 상승
            yield return MoveY(basePos.y, basePos.y + hopHeight, upTime, EaseOutQuad);

            // 최고점에서 살짝 스트레치
            SetSquash(1f - squashAmount * 0.5f, 1f + squashAmount * 0.5f);

            // 하강(빠르게)
            yield return MoveY(basePos.y + hopHeight, basePos.y, downTime, EaseInQuad);

            // 바닥에서 잠깐 멈춤 + 강한 스쿼시 후 복귀
            SetSquash(1f + squashAmount * 1.2f, 1f - squashAmount * 1.2f);
            yield return new WaitForSeconds(groundPause);
            SetSquash(1f, 1f);
        }
    }

    IEnumerator MoveY(float from, float to, float dur, System.Func<float,float> ease)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float y = Mathf.Lerp(from, to, ease(k));
            transform.position = new Vector3(basePos.x, y, basePos.z);
            UpdateShadow((y - basePos.y) / hopHeight);
            yield return null;
        }
        transform.position = new Vector3(basePos.x, to, basePos.z);
        UpdateShadow((to - basePos.y) / hopHeight);
    }

    // Easing
    float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
    float EaseInQuad (float x) => x * x;

    void SetSquash(float sx, float sy)
    {
        body.localScale = new Vector3(bodyBaseScale.x * sx, bodyBaseScale.y * sy, bodyBaseScale.z);
    }

    void UpdateShadow(float norm) // 0=바닥, 1=최고점
    {
        if (!shadow) return;
        float s = Mathf.Lerp(1f, 0.7f, norm);
        shadow.localScale = new Vector3(s, s * 0.6f, 1f);
        var sr = shadow.GetComponent<SpriteRenderer>();
        if (sr) sr.color = new Color(0,0,0, Mathf.Lerp(0.35f, 0.12f, norm));
    }
}
