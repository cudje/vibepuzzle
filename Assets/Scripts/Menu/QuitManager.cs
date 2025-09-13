using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitManager : MonoBehaviour
{
    private float lastBackPressedTime = 0f;
    private float backDelay = 2f; // 2초 안에 두 번 눌러야 종료

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.time - lastBackPressedTime < backDelay)
            {
                // 앱 종료
                Application.Quit();

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
            else
            {
                // 첫 번째 뒤로가기 입력 → 시간 기록
                lastBackPressedTime = Time.time;
            }
        }
    }
}