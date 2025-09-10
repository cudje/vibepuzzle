using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue Data")]
    [TextArea] public string[] dialogues1; // 1스테이지 대사
    [TextArea] public string[] dialogues2; // 2스테이지 대사
    [TextArea] public string[] dialogues3; // 3스테이지 대사
    [TextArea] public string[] dialogues4; // 4스테이지 대사
    [TextArea] public string[] dialogues5; // 5스테이지 대사

    private string[] currentDialogues;     // 현재 진행 중인 대사 배열
    private int dialogueIndex = 0;
    private bool dialogueActive = false;

    [Header("Blocker During Dialogue")]
    public GameObject dialogueBlocker;

    [Header("SFX / Animation")]
    public AudioSource audioSource;    // 사운드 출력용 오디오 소스
    public AudioClip[] clips;

    // ===== Typewriter Settings =====
    [Header("Typewriter")]
    [Tooltip("초당 출력할 문자 수")]
    [Range(5f, 120f)] public float charsPerSecond = 35f;
    [Tooltip("구두점(.,!?,…/:;)에서 잠깐 멈춤")]
    public bool usePunctuationPause = true;
    [Tooltip("콤마/세미콜론/콜론 일시정지(초)")]
    [Range(0f, 0.25f)] public float smallPause = 0.06f;   // , : ;
    [Tooltip("마침표/느낌표/물음표/말줄임표 일시정지(초)")]
    [Range(0f, 0.35f)] public float bigPause = 0.16f;   // . ! ? …
    [Tooltip("타이핑 중 Next 입력 시 내용을 즉시 완성하는 대신 곧바로 다음 줄로 넘길지 여부")]
    public bool skipToNextOnTyping = false;

    private Coroutine typeRoutine;
    private bool isTyping;

    void Start()
    {
        // 대화 전에는 통행 금지
        if (dialogueBlocker != null) dialogueBlocker.SetActive(true);
    }

    public void SetMainCamera(bool mainon = true)
    {
        if (mainon)
        {
            SceneHubManager.I.cutsceneCamera.enabled = false;
            SceneHubManager.I.mainCamera.enabled = true;
        }
        else
        {
            SceneHubManager.I.cutsceneCamera.enabled = true;
            SceneHubManager.I.mainCamera.enabled = false;
        }
    }

    /// <summary>
    /// 외부에서 스테이지 번호를 전달받아 대화를 시작
    /// </summary>
    public void StartDialogue(int stageNumber)
    {
        Debug.Log("StartDialogue 실행됨 : Stage " + stageNumber);

        dialogueIndex = 0;
        dialogueActive = true;

        SceneHubManager.I.dialogueUI.SetActive(true);
        SceneHubManager.I.variableJoystick.SetActive(false);
        SceneHubManager.I.promptOpen.SetActive(false);

        //  스테이지 번호에 맞는 대화 선택
        switch (stageNumber)
        {
            case 1: currentDialogues = dialogues1; break;
            case 2: currentDialogues = dialogues2; break;
            case 3: currentDialogues = dialogues3; break;
            case 4: currentDialogues = dialogues4; break;
            case 5: currentDialogues = dialogues5; break;
            default: currentDialogues = dialogues1; break;
        }

        if (currentDialogues == null || currentDialogues.Length == 0)
        {
            Debug.LogWarning("[Dialogue] 해당 스테이지 대사가 비어있습니다.");
            EndDialogue();
            return;
        }

        // 첫 줄부터 타자기 시작
        SceneHubManager.I.dialogueTMPText.text = "";
        StartTypewriter(currentDialogues[dialogueIndex]);
    }

    // ====== Typewriter ======
    private void StartTypewriter(string line)
    {
        // 진행 중 코루틴 정리
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeRoutine(line));
    }

    private IEnumerator TypeRoutine(string line)
    {
        isTyping = true;
        SceneHubManager.I.dialogueTMPText.text = "";
        if (SceneHubManager.I.lexyAnimator != null) SceneHubManager.I.lexyAnimator.SetBool("isTalking", true);

        // 말소리: 랜덤 클립 1회 재생(필요 시 loop SFX로 교체 가능)
        if (clips != null && clips.Length > 0 && audioSource != null)
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
            if (line[i] == '<') // 리치텍스트 태그 시작
            {
                int close = line.IndexOf('>', i);
                if (close < 0) close = i; // 방어 코드
                SceneHubManager.I.dialogueTMPText.text += line.Substring(i, close - i + 1);
                i = close + 1;
                continue;
            }

            SceneHubManager.I.dialogueTMPText.text += line[i];
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
        if (SceneHubManager.I.lexyAnimator != null) SceneHubManager.I.lexyAnimator.SetBool("isTalking", false);
        if (audioSource != null) audioSource.Stop();
        typeRoutine = null;
    }

    /// <summary>
    /// 다음 대사. 타이핑 중이면 즉시 완성(혹은 옵션에 따라 다음 줄로).
    /// </summary>
    public void NextDialogue()
    {
        if (!dialogueActive || currentDialogues == null)
        {
            Debug.Log($"[NextDialogue] blocked. active={dialogueActive}, hasDialogues={(currentDialogues != null)}");
            return;
        }

        // 타이핑 중 처리
        if (isTyping)
        {
            if (skipToNextOnTyping)
            {
                // 바로 다음 줄로 스킵
                if (typeRoutine != null) StopCoroutine(typeRoutine);
                isTyping = false;
                if (SceneHubManager.I.lexyAnimator != null) SceneHubManager.I.lexyAnimator.SetBool("isTalking", false);
                if (audioSource != null) audioSource.Stop();
                typeRoutine = null;
                GoNextOrEnd();
            }
            else
            {
                // 현재 줄 즉시 완성
                if (typeRoutine != null) StopCoroutine(typeRoutine);
                SceneHubManager.I.dialogueTMPText.text = currentDialogues[dialogueIndex];
                isTyping = false;
                if (SceneHubManager.I.lexyAnimator != null) SceneHubManager.I.lexyAnimator.SetBool("isTalking", false);
                if (audioSource != null) audioSource.Stop();
                typeRoutine = null;
            }
            return;
        }

        // 타이핑이 끝난 상태면 다음 줄/종료
        GoNextOrEnd();
    }

    private void GoNextOrEnd()
    {
        // 마지막 줄이면 바로 종료
        if (dialogueIndex >= currentDialogues.Length - 1)
        {
            EndDialogue();
            return;
        }

        dialogueIndex++;
        StartTypewriter(currentDialogues[dialogueIndex]);
    }

    private void EndDialogue()
    {
        dialogueActive = false;

        SceneHubManager.I.dialogueUI.SetActive(false);

        SetMainCamera(true);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;
        isTyping = false;

        if (audioSource != null) audioSource.Stop();
        if (SceneHubManager.I.lexyAnimator != null) SceneHubManager.I.lexyAnimator.SetBool("isTalking", false);

        SceneHubManager.I.variableJoystick.SetActive(true);
        SceneHubManager.I.promptOpen.SetActive(true);

        Debug.Log("대화 종료");
        // 대화 끝나면 다시 통행 허용
        if (dialogueBlocker != null) dialogueBlocker.SetActive(false);
    }

    void Awake()
    {
        dialogues1 = new string[]
        {
            "??? : 오.. 눈을 떴구나.",
            "??? : 축하해! 드디어 관리자 로봇으로 승진을 했구나.",
            "렉시 : 나는 렉시.",
            "렉시 : 이 공간의 프롬프트 가이드이자, 너의 첫 업무를 도와줄 도우미야.",
            "렉시 : 너는 이제 단순히 움직이는 로봇이 아니라, 명령을 내리는 로봇, 즉 관리자가 된거야.",
            "렉시 : 이 구역에서는 모든 동료 로봇들이 자연어 명령, 즉 '프롬프트'로 움직이지.",
            "렉시 : '프롬프트'는 네가 동료에게 내리는 우리말 명령이야.",
            "렉시 : 우리는 코드 대신 말로 명령을 내리는 특별한 방식으로 작동해.",
            "렉시 : 예를 들어, 이렇게 말하면 돼. \"오른쪽으로 3칸 움직여\"",
            "렉시 : 그러면 동료 로봇은 네 말을 이해하고 정확히 오른쪽으로 세 칸 이동하게 돼.",
            "렉시 : 중요한 건, 네 말이 명확하고 구체적이어야 한다는 거야.",
            "렉시 : 말이 모호하거나 이상하면, 동료 로봇은 헷갈릴 수 있어.",
            "렉시 : 지금부터 너는 관리자 로봇으로서, 상황을 파악하고 프롬프트로 정확한 지시를 내려 임무를 해결해야 해.",
            "렉시 : 하지만 걱정 마! 처음은 누구나 서툴기 마련이니까.",
            "렉시 : 내가 옆에서 하나씩 차근차근 알려줄게.",
            "렉시 : 화면 우측 상단에 있는건 홈버튼이야. 누르면 스테이지 선택 메뉴로 돌아갈 수 있어.",
            "렉시 : 이곳까지 걸어왔으니 알겠지만, 조이스틱을 통해 직접 움직이면서 맵을 둘러볼 수 있어.",
            "렉시 : 이제 프롬프트로 동료 로봇에게 명령을 내려볼 차례야.",
            "렉시 : 화면 오른쪽의 화살표를 누르고 입력 칸에 다음과 같이 입력해봐. \"오른쪽으로 3칸 움직여.\""
        };

        dialogues2 = new string[]
        {
            "렉시 : 어서와! 다시 만나서 반가워!",
            "렉시 : 이제 본격적인 명령어를 더 배워볼 차례야.",
            "렉시 : 이번엔 동료 로봇에게 물건을 줍도록 명령해볼거야.",
            "렉시 : 로봇은 단순히 움직이는 것뿐만 아니라, 상호작용도 할 수 있어.",
            "렉시 : 예를 들어, 이렇게 말 할 수 있어. \"지금 자리에 있는 물건을 주워\"",
            "렉시 : 그러면 로봇은 바닥에 놓여있는 물건을 집어 들어서 소지하게 돼.",
            "렉시 : 이처럼 '줍다', '집다', '들다' 같은 표현도 알아들을 수 있어. 하지만, 말이 모호하면 오작동할 수 있으니, 간결하고 명확하게 말하는 게 좋아.",
            "렉시 : 자 그럼, 연습해보자. 물건이 놓여있는 칸까지 이동한 후에, 줍는거야."
        };

        dialogues3 = new string[]
        {
            "렉시 : 일은 어때?",
            "렉시 : 이제 꽤 익숙해졌지?",
            "렉시 : 이번엔 물건을 옮기는 미션이야.",
            "렉시 : 로봇이 줍는 것까진 배웠지?",
            "렉시 : 이제는 줍고 난 다음에 어디에 둘지까지 명령해야 해.",
            "렉시 : 중요한 건, \"무엇을 하고 -> 어디로 가서 -> 어떤 행동을 하는지\" 순서대로 정확히 말하는 거야.",
            "렉시 : 자, 직접 해볼까?",
            "렉시 : 로봇이 저 부품을 들어서, 바구니 위치에 놓도록 명령해보는 거야."
        };

        dialogues4 = new string[]
        {
            "렉시 : 여기까지 잘 따라오고 있어!",
            "렉시 : 이번엔 관리자로봇다운 명령 방식을 배워볼 차례야.",
            "렉시 : 지금 보이는 이 로봇들, 전부 네 동료야.",
            "렉시 : 하지만… 하나씩 명령을 내려서 움직인다면?",
            "렉시 : 첫 번째 로봇, 아래로 두 칸 가. 두 번째 로봇, 너도 아래로 두 칸 가. 세 번째 로봇 너도…",
            "렉시 : 이렇게 하면 시간도 오래 걸리고 실수도 많아지겠지?",
            "렉시 : 그래서 관리자 로봇은 달라.",
            "렉시 : 하나의 명령으로 모든 로봇에게 동시에 지시할 수 있어.",
            "렉시 : 모든 동료 로봇은 그 명령을 자기 위치에 맞춰 스스로 해석하고 같은 시점에 이동해.",
            "렉시 : 이제부터는 하나의 명령으로 팀 전체를 움직일 수 있어야 해.",
            "렉시 : 이제야 좀 관리자답지?",
            "렉시 : 모든 로봇이 저 부품을 들도록 명령해보는 거야."
        };

        dialogues5 = new string[]
        {
            "렉시 : 좋아, 관리자 로봇.",
            "렉시 : 이번엔 조금 까다로워.",
            "렉시 : 이 구역엔… 낭떠러지가 있어.",
            "렉시 : 그대로 전진했다간 추락해서 로봇이 손상될 수도 있어.",
            "렉시 : 하지만 또 어떤 로봇은 앞이 평지라서 움직여도 괜찮지.",
            "렉시 : 그러니까, 지금 필요한 것은 조건에 따라 다르게 움직이는 명령이야.",
            "렉시 : 관리자 로봇이라면, 이 정도 상황 판단 능력도 명령 안에 담을 수 있어야 하겠지?",
            "렉시 : 로봇들이 주변을 살피고, 포탈까지 안전하게 이동할 수 있도록 지시해줘.",
        };
    }
}
