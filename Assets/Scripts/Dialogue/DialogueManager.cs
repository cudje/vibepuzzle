using UnityEngine;
using TMPro;
using System.Collections;
using JetBrains.Annotations;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue Data")]
    [TextArea] public string[] dialogues1; // 1스테이지 대사
    [TextArea] public string[] dialogues2; // 2스테이지 대사
    [TextArea] public string[] dialogues3; // 3스테이지 대사
    [TextArea] public string[] dialogues4; // 4스테이지 대사
    [TextArea] public string[] dialogues5; // 5스테이지 대사


    private string[] currentDialogues; // 현재 진행 중인 대사 배열

    private int dialogueIndex = 0;
    private bool dialogueActive = false;

    [Header("UI References")]
    public GameObject dialogueUI;
    public TextMeshProUGUI dialogueText;

    public Camera cutsceneCamera;
    public Camera mainCamera;

    [Header("Extra UI to Disable During Dialogue")]
    public GameObject variableJoystick;     // Variable Joystick 오브젝트
    public GameObject promptOpenButton;     // PromptOpen_Button 오브젝트

    [Header("Blocker During Dialogue")]
    public GameObject dialogueBlocker;

    [Header("SFX")]
    public Animator lexyAnimator;
    public AudioSource audioSource;    // 사운드 출력용 오디오 소스
    public AudioClip[] clips;

    private Coroutine talkRoutine = null;

    void Start()
    {
        dialogueUI.SetActive(false);
        // 대화 전에는 통행 금지
        if (dialogueBlocker != null) dialogueBlocker.SetActive(true);
    }

    /// <summary>
    /// 외부에서 스테이지 번호를 전달받아 대화를 시작
    /// </summary>
    public void StartDialogue(int stageNumber)
    {
        Debug.Log("StartDialogue 실행됨 : Stage " + stageNumber);

        dialogueIndex = 0;
        dialogueActive = true;

        dialogueUI.SetActive(true);
        cutsceneCamera.enabled = true;
        mainCamera.enabled = false;

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

        // 새 코루틴 시작
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

        // 마지막 줄이면 바로 종료
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
        "렉시 : 이 구역에서는 모든 동료 로봇들이 자연어 명령, 즉 ‘프롬프트’로 움직이지.",
        "렉시 :‘프롬프트’는 네가 동료에게 내리는 우리말 명령이야.",
        "렉시 : 우리는 코드 대신 말로 명령을 내리는 특별한 방식으로 작동해.",
        "렉시 : 예를 들어, 이렇게 말하면 돼. ”오른쪽으로 3칸 움직여”",
        "렉시 : 그러면 동료 로봇은 네 말을 이해하고 정확히 오른쪽으로 세 칸 이동하게 돼.",
        "렉시 : 중요한 건, 네 말이 명확하고 구체적이어야 한다는 거야.",
        "렉시 : 말이 모호하거나 이상하면, 동료 로봇은 헷갈릴 수 있어.",
        "렉시 : 지금부터 너는 관리자 로봇으로서, 상황을 파악하고 프롬프트로 정확한 지시를 내려 임무를 해결해야 해.",
        "렉시 : 하지만 걱정 마! 처음은 누구나 서툴기 마련이니까.",
        "렉시 : 내가 옆에서 하나씩 차근차근 알려줄게.",
        "렉시 : 화면 좌측 상단에 있는건 홈버튼이야. 누르면 스테이지 선택 메뉴로 돌아갈 수 있어.",
        "렉시 : 이곳까지 걸어왔으니 알겠지만, 조이스틱을 통해 직접 움직이면서 맵을 둘러볼 수 있어.",
        "렉시 : 이제 프롬프트로 동료 로봇에게 명령을 내려볼 차례야.",
        "렉시 : 화면 오른쪽의 화살표를 누르고 입력 칸에 다음과 같이 입력해봐. '오른쪽으로 3칸 움직여.'"
        };

        dialogues2 = new string[]
        {
        "렉시 : 어서와! 다시 만나서 반가워!",
        "렉시 : 이제 본격적인 명령어를 더 배워볼 차례야.",
        "렉시 : 이번엔 동료 로봇에게 물건을 줍도록 명령해볼거야.",
        "렉시 : 로봇은 단순히 움직이는 것뿐만 아니라, 상호작용도 할 수 있어.",
        "렉시 : 예를 들어, 이렇게 말 할 수 있어. '지금 자리에 있는 물건을 주워'",
        "렉시 : 그러면 로봇은 바닥에 놓여있는 물건을 집어 들어서 소지하게 돼.",
        "렉시 : 이처럼 ‘줍다’,‘집다’,‘들다' 같은 표현도 알아들을 수 있어. 하지만, 말이 모호하면 오작동할 수 있으니, 간결하고 명확하게 말하는 게 좋아.",
        "렉시 : 자 그럼, 연습해보자. 물건이 놓여있는 칸까지 이동한 후에, 줍는거야."
        };

        dialogues3 = new string[]
        {
        "렉시 : 일은 어때?",
        "렉시 : 이제 꽤 익숙해졌지?",
        "렉시 : 이번엔 물건을 옮기는 미션이야.",
        "렉시 : 로봇이 줍는 것까진 배웠지?",
        "렉시 : 이제는 줍고 난 다음에 어디에 둘지까지 명령해야 해.",
        "렉시 : 중요한 건, “무엇을 하고 -> 어디로 가서 -> 어떤 행동을 하는지” 순서대로 정확히 말하는 거야.",
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

