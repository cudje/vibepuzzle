using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarningManager : MonoBehaviour
{
    private Coroutine busyNoticeCoroutine;

    public void ShowBusyNotice(string s, bool isWhite = true, int fontsize = 48)
    {
        // 기존 코루틴이 실행 중이면 멈추기
        if (busyNoticeCoroutine != null)
        {
            StopCoroutine(busyNoticeCoroutine);
            busyNoticeCoroutine = null;
        }

        // 새 코루틴 시작
        busyNoticeCoroutine = StartCoroutine(CoShowBusyNotice(s, isWhite, fontsize));
    }

    private IEnumerator CoShowBusyNotice(string s, bool isWhite, int fontsize)
    {
        if (isWhite)
        {
            SceneHubManager.I.executeWarnTMPText.color = Color.white;
        }
        else
        {
            SceneHubManager.I.executeWarnTMPText.color = Color.black;
        }
        SceneHubManager.I.executeWarnTMPText.text = s;
        SceneHubManager.I.executeWarnTMPText.fontSize = fontsize;
        SceneHubManager.I.executeWarn.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        SceneHubManager.I.executeWarn.SetActive(false);
        busyNoticeCoroutine = null; // 끝났으니 null로 초기화
    }

    public void ReadyPopUP()
    {
        ShowBusyNotice("준비 중입니다...", false);
    }
}
