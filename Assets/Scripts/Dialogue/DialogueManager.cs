using UnityEngine;
using TMPro;
using System.Collections;
using JetBrains.Annotations;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue Data")]
    [TextArea] public string[] dialogues1; // 1�������� ���
    [TextArea] public string[] dialogues2; // 2�������� ���
    [TextArea] public string[] dialogues3; // 3�������� ���
    [TextArea] public string[] dialogues4; // 4�������� ���
    [TextArea] public string[] dialogues5; // 5�������� ���


    private string[] currentDialogues; // ���� ���� ���� ��� �迭

    private int dialogueIndex = 0;
    private bool dialogueActive = false;

    [Header("UI References")]
    public GameObject dialogueUI;
    public TextMeshProUGUI dialogueText;

    public Camera cutsceneCamera;
    public Camera mainCamera;

    [Header("Extra UI to Disable During Dialogue")]
    public GameObject variableJoystick;     // Variable Joystick ������Ʈ
    public GameObject promptOpenButton;     // PromptOpen_Button ������Ʈ

    [Header("Blocker During Dialogue")]
    public GameObject dialogueBlocker;

    [Header("SFX")]
    public Animator lexyAnimator;
    public AudioSource audioSource;    // ���� ��¿� ����� �ҽ�
    public AudioClip[] clips;

    private Coroutine talkRoutine = null;

    void Start()
    {
        dialogueUI.SetActive(false);
        // ��ȭ ������ ���� ����
        if (dialogueBlocker != null) dialogueBlocker.SetActive(true);
    }

    /// <summary>
    /// �ܺο��� �������� ��ȣ�� ���޹޾� ��ȭ�� ����
    /// </summary>
    public void StartDialogue(int stageNumber)
    {
        Debug.Log("StartDialogue ����� : Stage " + stageNumber);

        dialogueIndex = 0;
        dialogueActive = true;

        dialogueUI.SetActive(true);
        cutsceneCamera.enabled = true;
        mainCamera.enabled = false;

        //  �������� ��ȣ�� �´� ��ȭ ����
        switch (stageNumber)
        {
            case 1: currentDialogues = dialogues1; break;
            case 2: currentDialogues = dialogues2; break;
            case 3: currentDialogues = dialogues3; break;
            case 4: currentDialogues = dialogues4; break;
            case 5: currentDialogues = dialogues5; break;
            default: currentDialogues = dialogues1; break;
        }

        dialogueText.text = currentDialogues[dialogueIndex];

        dialogueIndex = 0;
        dialogueActive = true;

        dialogueUI.SetActive(true);
        cutsceneCamera.enabled = true;
        mainCamera.enabled = false;
        if (variableJoystick != null) variableJoystick.SetActive(false);
        if (promptOpenButton != null) promptOpenButton.SetActive(false);

        dialogueText.text = currentDialogues[dialogueIndex];
        PlayDialogue();
    }

    public void PlayDialogue()
    {
        int idx = Random.Range(0, clips.Length);
        audioSource.clip = clips[idx];
        audioSource.Play();

        if (talkRoutine != null)
        {
            StopCoroutine(talkRoutine);
        }

        // �� �ڷ�ƾ ����
        talkRoutine = StartCoroutine(TalkRoutine(audioSource.clip.length));
    }

    private IEnumerator TalkRoutine(float duration)
    {
        lexyAnimator.SetBool("isTalking", true);

        yield return new WaitForSeconds(duration);

        lexyAnimator.SetBool("isTalking", false);
        talkRoutine = null;
    }

    public void NextDialogue()
    {
        if (!dialogueActive || currentDialogues == null)
        {
            Debug.Log($"[NextDialogue] blocked. active={dialogueActive}, hasDialogues={(currentDialogues != null)}");
            return;
        }

        //Debug.Log($"[NextDialogue] before: index={dialogueIndex}, len={currentDialogues.Length}");

        // ������ ���̸� �ٷ� ����
        if (dialogueIndex >= currentDialogues.Length - 1)
        {
            EndDialogue();
            return;
        }

        dialogueIndex++;
        dialogueText.text = currentDialogues[dialogueIndex];

        PlayDialogue();

        //Debug.Log($"[NextDialogue] after: index={dialogueIndex}, len={currentDialogues.Length}");
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        dialogueUI.SetActive(false);

        cutsceneCamera.enabled = false;
        mainCamera.enabled = true;
        audioSource.Stop();
        if (talkRoutine != null)
        {
            StopCoroutine(talkRoutine);
            lexyAnimator.SetBool("isTalking", false);
            talkRoutine = null;
        }

        if (variableJoystick != null) variableJoystick.SetActive(true);
        if (promptOpenButton != null) promptOpenButton.SetActive(true);

        Debug.Log("��ȭ ����");
        // ��ȭ ������ �ٽ� ���� ���
        if (dialogueBlocker != null) dialogueBlocker.SetActive(false);
    }


    void Awake()
    {
        dialogues1 = new string[]
        {
        "??? : ��.. ���� ������.",
        "??? : ������! ���� ������ �κ����� ������ �߱���.",
        "���� : ���� ����.",
        "���� : �� ������ ������Ʈ ���̵�����, ���� ù ������ ������ ����̾�.",
        "���� : �ʴ� ���� �ܼ��� �����̴� �κ��� �ƴ϶�, ����� ������ �κ�, �� �����ڰ� �Ȱž�.",
        "���� : �� ���������� ��� ���� �κ����� �ڿ��� ���, �� ��������Ʈ���� ��������.",
        "���� :��������Ʈ���� �װ� ���ῡ�� ������ �츮�� ����̾�.",
        "���� : �츮�� �ڵ� ��� ���� ����� ������ Ư���� ������� �۵���.",
        "���� : ���� ���, �̷��� ���ϸ� ��. ������������ 3ĭ ��������",
        "���� : �׷��� ���� �κ��� �� ���� �����ϰ� ��Ȯ�� ���������� �� ĭ �̵��ϰ� ��.",
        "���� : �߿��� ��, �� ���� ��Ȯ�ϰ� ��ü���̾�� �Ѵٴ� �ž�.",
        "���� : ���� ��ȣ�ϰų� �̻��ϸ�, ���� �κ��� �򰥸� �� �־�.",
        "���� : ���ݺ��� �ʴ� ������ �κ����μ�, ��Ȳ�� �ľ��ϰ� ������Ʈ�� ��Ȯ�� ���ø� ���� �ӹ��� �ذ��ؾ� ��.",
        "���� : ������ ���� ��! ó���� ������ ������ �����̴ϱ�.",
        "���� : ���� ������ �ϳ��� �������� �˷��ٰ�.",
        "���� : ȭ�� ���� ��ܿ� �ִ°� Ȩ��ư�̾�. ������ �������� ���� �޴��� ���ư� �� �־�.",
        "���� : �̰����� �ɾ������ �˰�����, ���̽�ƽ�� ���� ���� �����̸鼭 ���� �ѷ��� �� �־�.",
        "���� : ���� ������Ʈ�� ���� �κ����� ����� ������ ���ʾ�.",
        "���� : ȭ�� �������� ȭ��ǥ�� ������ �Է� ĭ�� ������ ���� �Է��غ�. '���������� 3ĭ ������.'"
        };

        dialogues2 = new string[]
        {
        "���� : ���! �ٽ� ������ �ݰ���!",
        "���� : ���� �������� ��ɾ �� ����� ���ʾ�.",
        "���� : �̹��� ���� �κ����� ������ �ݵ��� ����غ��ž�.",
        "���� : �κ��� �ܼ��� �����̴� �ͻӸ� �ƴ϶�, ��ȣ�ۿ뵵 �� �� �־�.",
        "���� : ���� ���, �̷��� �� �� �� �־�. '���� �ڸ��� �ִ� ������ �ֿ�'",
        "���� : �׷��� �κ��� �ٴڿ� �����ִ� ������ ���� �� �����ϰ� ��.",
        "���� : ��ó�� ���ݴ١�,�����١�,�����' ���� ǥ���� �˾Ƶ��� �� �־�. ������, ���� ��ȣ�ϸ� ���۵��� �� ������, �����ϰ� ��Ȯ�ϰ� ���ϴ� �� ����.",
        "���� : �� �׷�, �����غ���. ������ �����ִ� ĭ���� �̵��� �Ŀ�, �ݴ°ž�."
        };

        dialogues3 = new string[]
        {
        "���� : ���� �?",
        "���� : ���� �� �ͼ�������?",
        "���� : �̹��� ������ �ű�� �̼��̾�.",
        "���� : �κ��� �ݴ� �ͱ��� �����?",
        "���� : ������ �ݰ� �� ������ ��� �������� ����ؾ� ��.",
        "���� : �߿��� ��, �������� �ϰ� -> ���� ���� -> � �ൿ�� �ϴ����� ������� ��Ȯ�� ���ϴ� �ž�.",
        "���� : ��, ���� �غ���?",
        "���� : �κ��� �� ��ǰ�� ��, �ٱ��� ��ġ�� ������ ����غ��� �ž�."
        };
        dialogues4 = new string[]
        {
        "���� : ������� �� ������� �־�!",
        "���� : �̹��� �����ڷκ��ٿ� ��� ����� ����� ���ʾ�.",
        "���� : ���� ���̴� �� �κ���, ���� �� �����.",
        "���� : �������� �ϳ��� ����� ������ �����δٸ�?",
        "���� : ù ��° �κ�, �Ʒ��� �� ĭ ��. �� ��° �κ�, �ʵ� �Ʒ��� �� ĭ ��. �� ��° �κ� �ʵ���",
        "���� : �̷��� �ϸ� �ð��� ���� �ɸ��� �Ǽ��� ����������?",
        "���� : �׷��� ������ �κ��� �޶�.",
        "���� : �ϳ��� ������� ��� �κ����� ���ÿ� ������ �� �־�.",
        "���� : ��� ���� �κ��� �� ����� �ڱ� ��ġ�� ���� ������ �ؼ��ϰ� ���� ������ �̵���.",
        "���� : �������ʹ� �ϳ��� ������� �� ��ü�� ������ �� �־�� ��.",
        "���� : ������ �� �����ڴ���?",
        "���� : ��� �κ��� �� ��ǰ�� �鵵�� ����غ��� �ž�."
        };
        dialogues5 = new string[]
        {
        "���� : ����, ������ �κ�.",
        "���� : �̹��� ���� ��ٷο�.",
        "���� : �� �������� ���������� �־�.",
        "���� : �״�� �����ߴٰ� �߶��ؼ� �κ��� �ջ�� ���� �־�.",
        "���� : ������ �� � �κ��� ���� ������ �������� ������.",
        "���� : �׷��ϱ�, ���� �ʿ��� ���� ���ǿ� ���� �ٸ��� �����̴� ����̾�.",
        "���� : ������ �κ��̶��, �� ���� ��Ȳ �Ǵ� �ɷµ� ��� �ȿ� ���� �� �־�� �ϰ���?",
        "���� : �κ����� �ֺ��� ���ǰ�, ��Ż���� �����ϰ� �̵��� �� �ֵ��� ��������.",
        };
    }
}

