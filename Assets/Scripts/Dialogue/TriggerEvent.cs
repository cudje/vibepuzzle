using UnityEngine;
using System.Collections;

public class TriggerEvent : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera mainCamera;        // 기본 카메라
    public Camera cutsceneCamera;    // 연출용 카메라

    [Header("Character Settings")]
    public Transform lexy;           // 렉시 캐릭터
    public Transform lexyTargetPos;  // 렉시가 이동할 목표 위치
    public float moveSpeed = 4.0f;
    public Animator lexyAnimator;

    [Header("Dialogue Settings")]
    public GameObject dialogueUI;            // 대화 UI 오브젝트
    public DialogueManager dialogueManager;  // DialogueManager 참조

    public int stageNumber = 1; // 이 트리거가 실행할 스테이지 번호를 Inspector에서 지정
    private bool eventStarted = false;
    private bool lexyMoving = false;

    private bool isMovingCoroutineRunning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (eventStarted) return;

        if (other.CompareTag("Player"))
        {
            eventStarted = true;

            // 카메라 전환
            if (mainCamera != null) mainCamera.enabled = false;
            if (cutsceneCamera != null) cutsceneCamera.enabled = true;

            // 렉시 이동 시작
            lexyMoving = true;

        }
    }

    private void Update()
    {
        if (lexyMoving && lexy != null && lexyTargetPos != null && !isMovingCoroutineRunning)
        {
            // 이동을 시작할 때 단 한번 코루틴 실행
            StartCoroutine(StartMoveAfterDelay());
        }
    }

    private IEnumerator StartMoveAfterDelay()
    {
        isMovingCoroutineRunning = true;

        // 애니메이션 Speed 켜기
        lexyAnimator.SetFloat("Speed", moveSpeed);

        // 0.25초 기다림
        yield return new WaitForSeconds(0.25f);

        // 이제부터는 매 프레임 이동
        while (lexyMoving && lexy != null && lexyTargetPos != null)
        {
            lexy.position = Vector3.MoveTowards(
                lexy.position,
                lexyTargetPos.position,
                moveSpeed * Time.deltaTime
            );

            // 도착 판정
            if (Vector3.Distance(lexy.position, lexyTargetPos.position) < 0.1f)
            {
                lexyAnimator.SetFloat("Speed", 0f);
                lexy.position = lexyTargetPos.position;
                lexyMoving = false;
                isMovingCoroutineRunning = false;

                Debug.Log("렉시 도착, 대화 시작!");

                if (dialogueUI != null)
                    dialogueUI.SetActive(true);
                if (dialogueManager != null)
                    dialogueManager.StartDialogue(stageNumber);
                FindObjectOfType<DialogueManager>().StartDialogue(stageNumber);
            }

            yield return null; // 다음 프레임까지 대기
        }
    }

    // 컷신/대화 종료
    public void EndCutscene()
    {
        if (mainCamera != null) mainCamera.enabled = true;
        if (cutsceneCamera != null) cutsceneCamera.enabled = false;

        if (dialogueUI != null)
            dialogueUI.SetActive(false);
    }
}