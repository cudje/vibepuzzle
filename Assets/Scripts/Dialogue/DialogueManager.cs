using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue Data")]
    [TextArea] public string[] dialogues1;
    [TextArea] public string[] dialogues2;
    [TextArea] public string[] dialogues3;
    [TextArea] public string[] dialogues4;
    [TextArea] public string[] dialogues5;

    private string[] currentDialogues;

    private int dialogueIndex = 0;
    private bool dialogueActive = false;

    [Header("UI References")]
    public GameObject dialogueUI;
    public TextMeshProUGUI dialogueText;

    public Camera cutsceneCamera;
    public Camera mainCamera;

    [Header("Extra UI to Disable During Dialogue")]
    public GameObject variableJoystick;
    public GameObject promptOpenButton;

    [Header("Blocker During Dialogue")]
    public GameObject dialogueBlocker;

    [Header("SFX / Anim")]
    public Animator lexyAnimator;
    public AudioSource audioSource;
    public AudioClip[] clips;

    // ===== Typewriter Settings =====
    [Header("Typewriter")]
    [Range(5f, 120f)] public float charsPerSecond = 35f;
    public bool usePunctuationPause = true;
    [Range(0f, 0.25f)] public float smallPause = 0.06f;   // , : ;
    [Range(0f, 0.35f)] public float bigPause   = 0.16f;   // . ! ? …

    private Coroutine talkRoutine;
    private Coroutine typeRoutine;
    private bool isTyping;

    void Awake()
    {
        // (샘플 대사들 초기화는 네 코드 그대로 둠)
        // ... 생략 ...
    }

    void Start()
    {
        dialogueUI.SetActive(false);
        if (dialogueBlocker != null) dialogueBlocker.SetActive(true);
    }

    public void StartDialogue(int stageNumber)
    {
        dialogueIndex = 0;
        dialogueActive = true;

        dialogueUI.SetActive(true);
        cutsceneCamera.enabled = true;
        mainCamera.enabled = false;
        if (variableJoystick != null) variableJoystick.SetActive(false);
        if (promptOpenButton != null) promptOpenButton.SetActive(false);

        // 스테이지에 맞춰 배열 선택
        switch (stageNumber)
        {
            case 1: currentDialogues = dialogues1; break;
            case 2: currentDialogues = dialogues2; break;
            case 3: currentDialogues = dialogues3; break;
            case 4: currentDialogues = dialogues4; break;
            case 5: currentDialogues = dialogues5; break;
            default: currentDialogues = dialogues1; break;
        }

        // 첫 줄 출력 (타자기 시작)
        dialogueText.text = "";
        StartTypewriter(currentDialogues[dialogueIndex]);
    }

    // ====== Typewriter ======
    void StartTypewriter(string line)
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeRoutine(line));
    }

    IEnumerator TypeRoutine(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        lexyAnimator.SetBool("isTalking", true);

        // 말소리: 기존처럼 랜덤 클립 재생 (원하면 루프 SFX로 교체)
        if (clips != null && clips.Length > 0)
        {
            int idx = Random.Range(0, clips.Length);
            audioSource.clip = clips[idx];
            audioSource.Play();
        }

        float delayPerChar = 1f / Mathf.Max(1f, charsPerSecond);

        // 리치텍스트 태그는 즉시 처리
        int i = 0;
        while (i < line.Length)
        {
            if (line[i] == '<') // tag 시작
            {
                int close = line.IndexOf('>', i);
                if (close == -1) close = i; // 비정상 방어
                dialogueText.text += line.Substring(i, close - i + 1);
                i = close + 1;
                continue;
            }

            dialogueText.text += line[i];
            i++;

            // 구두점 일시정지
            if (usePunctuationPause && i <= line.Length)
            {
                char c = line[i - 1];
                if (c == '.' || c == '!' || c == '?' || c == '…')
                    yield return new WaitForSeconds(bigPause);
                else if (c == ',' || c == ';' || c == ':')
                    yield return new WaitForSeconds(smallPause);
                else
                    yield return new WaitForSeconds(delayPerChar);
            }
            else
            {
                yield return new WaitForSeconds(delayPerChar);
            }
        }

        // 타자기 완료
        isTyping = false;
        lexyAnimator.SetBool("isTalking", false);
        audioSource.Stop();
        typeRoutine = null;
    }

    /// <summary>
    /// 다음 대사. 타이핑 중이면 즉시 완성만 함.
    /// </summary>
    public void NextDialogue()
    {
        if (!dialogueActive || currentDialogues == null) return;

        // 타이핑 중이면 줄 완성만
        if (isTyping)
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            dialogueText.text = currentDialogues[dialogueIndex];
            isTyping = false;
            lexyAnimator.SetBool("isTalking", false);
            audioSource.Stop();
            typeRoutine = null;
            return;
        }

        // 마지막 줄이면 종료
        if (dialogueIndex >= currentDialogues.Length - 1)
        {
            EndDialogue();
            return;
        }

        // 다음 줄 시작
        dialogueIndex++;
        StartTypewriter(currentDialogues[dialogueIndex]);
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        dialogueUI.SetActive(false);

        cutsceneCamera.enabled = false;
        mainCamera.enabled = true;

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;
        isTyping = false;

        audioSource.Stop();
        if (talkRoutine != null)
        {
            StopCoroutine(talkRoutine);
            talkRoutine = null;
        }
        lexyAnimator.SetBool("isTalking", false);

        if (variableJoystick != null) variableJoystick.SetActive(true);
        if (promptOpenButton != null) promptOpenButton.SetActive(true);

        if (dialogueBlocker != null) dialogueBlocker.SetActive(false);
    }
}
