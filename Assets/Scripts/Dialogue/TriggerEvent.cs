using UnityEngine;
using System.Collections;

public class TriggerEvent : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera mainCamera;        // �⺻ ī�޶�
    public Camera cutsceneCamera;    // ����� ī�޶�

    [Header("Character Settings")]
    public Transform lexy;           // ���� ĳ����
    public Transform lexyTargetPos;  // ���ð� �̵��� ��ǥ ��ġ
    public float moveSpeed = 4.0f;
    public Animator lexyAnimator;

    [Header("Dialogue Settings")]
    public GameObject dialogueUI;            // ��ȭ UI ������Ʈ
    public DialogueManager dialogueManager;  // DialogueManager ����

    public int stageNumber = 1; // �� Ʈ���Ű� ������ �������� ��ȣ�� Inspector���� ����
    private bool eventStarted = false;
    private bool lexyMoving = false;

    private bool isMovingCoroutineRunning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (eventStarted) return;

        if (other.CompareTag("Player"))
        {
            eventStarted = true;

            // ī�޶� ��ȯ
            if (mainCamera != null) mainCamera.enabled = false;
            if (cutsceneCamera != null) cutsceneCamera.enabled = true;

            // ���� �̵� ����
            lexyMoving = true;

        }
    }

    private void Update()
    {
        if (lexyMoving && lexy != null && lexyTargetPos != null && !isMovingCoroutineRunning)
        {
            // �̵��� ������ �� �� �ѹ� �ڷ�ƾ ����
            StartCoroutine(StartMoveAfterDelay());
        }
    }

    private IEnumerator StartMoveAfterDelay()
    {
        isMovingCoroutineRunning = true;

        // �ִϸ��̼� Speed �ѱ�
        lexyAnimator.SetFloat("Speed", moveSpeed);

        // 0.25�� ��ٸ�
        yield return new WaitForSeconds(0.25f);

        // �������ʹ� �� ������ �̵�
        while (lexyMoving && lexy != null && lexyTargetPos != null)
        {
            lexy.position = Vector3.MoveTowards(
                lexy.position,
                lexyTargetPos.position,
                moveSpeed * Time.deltaTime
            );

            // ���� ����
            if (Vector3.Distance(lexy.position, lexyTargetPos.position) < 0.1f)
            {
                lexyAnimator.SetFloat("Speed", 0f);
                lexy.position = lexyTargetPos.position;
                lexyMoving = false;
                isMovingCoroutineRunning = false;

                Debug.Log("���� ����, ��ȭ ����!");

                if (dialogueUI != null)
                    dialogueUI.SetActive(true);
                if (dialogueManager != null)
                    dialogueManager.StartDialogue(stageNumber);
                FindObjectOfType<DialogueManager>().StartDialogue(stageNumber);
            }

            yield return null; // ���� �����ӱ��� ���
        }
    }

    // �ƽ�/��ȭ ����
    public void EndCutscene()
    {
        if (mainCamera != null) mainCamera.enabled = true;
        if (cutsceneCamera != null) cutsceneCamera.enabled = false;

        if (dialogueUI != null)
            dialogueUI.SetActive(false);
    }
}