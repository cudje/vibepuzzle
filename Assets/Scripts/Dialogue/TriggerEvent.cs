using UnityEngine;
using System.Collections;

public class TriggerEvent : MonoBehaviour
{
    [Header("Character Settings")]
    public float moveSpeed = 4.0f;

    [Header("Dialogue Settings")]
    public DialogueManager dialogueManager;  // DialogueManager 참조

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
            dialogueManager.SetMainCamera(false);

            // 렉시 이동 시작
            lexyMoving = true;

        }
    }

    private void Update()
    {
        if (lexyMoving && SceneHubManager.I.charLexyT != null && SceneHubManager.I.lexyTargetPosT != null && !isMovingCoroutineRunning)
        {
            // 이동을 시작할 때 단 한번 코루틴 실행
            StartCoroutine(StartMoveAfterDelay());
        }
    }

    private IEnumerator StartMoveAfterDelay()
    {
        isMovingCoroutineRunning = true;

        // 애니메이션 Speed 켜기
        SceneHubManager.I.lexyAnimator.SetFloat("Speed", moveSpeed);

        // 0.25초 기다림
        yield return new WaitForSeconds(0.25f);

        // 이제부터는 매 프레임 이동
        while (lexyMoving && SceneHubManager.I.charLexyT != null && SceneHubManager.I.lexyTargetPosT != null)
        {
            SceneHubManager.I.charLexyT.position = Vector3.MoveTowards(
                SceneHubManager.I.charLexyT.position,
                SceneHubManager.I.lexyTargetPosT.position,
                moveSpeed * Time.deltaTime
            );

            // 도착 판정
            if (Vector3.Distance(SceneHubManager.I.charLexyT.position, SceneHubManager.I.lexyTargetPosT.position) < 0.1f)
            {
                SceneHubManager.I.lexyAnimator.SetFloat("Speed", 0f);
                SceneHubManager.I.charLexyT.position = SceneHubManager.I.lexyTargetPosT.position;
                lexyMoving = false;
                isMovingCoroutineRunning = false;

                Debug.Log("렉시 도착, 대화 시작!");

                dialogueManager.StartDialogue(TransStageToNumber());
            }

            yield return null; // 다음 프레임까지 대기
        }
    }

    private int TransStageToNumber()
    {
        if (string.IsNullOrEmpty(GameData.recentStage))
            return 0;

        // 예: "A3" → "3"
        string numberPart = GameData.recentStage.Substring(1);

        if (int.TryParse(numberPart, out int result))
            return result;

        return 0;
    }

}