using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneHubManager : MonoBehaviour
{
    public static SceneHubManager I { get; private set; }

    [Header("Player Objects")]
    public Transform charRoboT;
    public Rigidbody charRoboRigidbody;
    public Transform roboT;
    public Animator roboAnimator;
    public Transform charLexyT;
    public Transform lexyT;
    public Animator lexyAnimator;
    public Transform lexyTargetPosT;
    public Transform[] roadTs;
    public Transform[] goalTs;
    public GameObject[] pieces;
    public Transform[] pieceGoalTs;

    [Header("Other Objects")]
    public Transform clearDoorLeftT;
    public Transform clearDoorRightT;
    public Transform cellingT;

    [Header("Camera Objects")]
    public Transform rotationObjectT;
    public Transform mainCameraT;
    public Camera mainCamera;
    public Camera cutsceneCamera;
    public Camera TopviewCamera;

    [Header("UI Objects")]
    public GameObject clearPopup;
    public GameObject executeWarn;
    public GameObject dialogueUI;
    public TMP_Text clearTMPText;
    public GameObject prompt;
    public TMP_InputField promptTMPInputField;
    //public GameObject promptOpen;
    public Image loadingImage;
    public TMP_Text executeWarnTMPText;
    public TMP_Text dialogueTMPText;
    public GameObject variableJoystick;
    public CanvasGroup overlayCanvasG;
    public RectTransform overlayPanelRectT;
    public Button overlayCloseButton;
    public Button overlayClearButton;
    public GameObject interact;
    public Button pauseButton;
    public Button resetButton;
    public Button switchCameraButton;

    [Header("Auto-Wire Paths / Names")]
    private string charRoboPath = "Char_Robo";
    private string charLexyPath = "Char_Lexy";
    private string lexyTargetPosPath = "LexyTargetPos";
    private string roadsPath = "MainRoom/Tile/Roads";
    private string goalsPath = "MainRoom/Tile/Goals";
    private string piecesPath = "MainRoom/Tile/Pieces";
    private string pieceGoalsPath = "MainRoom/Tile/PieceGoals";

    private string clearDoorLeftPath = "MainRoom/ClearDoor_Left";
    private string clearDoorRightPath = "MainRoom/ClearDoor_Right";
    private string cellingPath = "Map/Walls/Cell";

    private string cutsceneCameraTPath = "CutSceneCamera";
    private string TopviewCameraTPath = "TopviewCamera";

    private string clearPopupPath = "Canvas/ConditionUI/ClearPopup";
    private string executeWarnPath = "Canvas/ConditionUI/ExecuteWarn";
    private string dialogueUIPath = "Canvas/ConditionUI/DialogueUI";
    private string promptPath = "Canvas/Prompt";
    private string variableJoystickPath = "Canvas/Variable Joystick";
    private string overlayPath = "Canvas/Overlay";
    private string interactPath = "Canvas/Interact";

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        // Player Objects 초기화
        charRoboT = GameObject.Find(charRoboPath).transform;
        charRoboRigidbody = charRoboT.GetComponent<Rigidbody>();
        roboT = charRoboT.Find("Robo");
        roboAnimator = roboT.GetComponent<Animator>();
        charLexyT = GameObject.Find(charLexyPath).transform;
        lexyT = charLexyT.Find("Lexy");
        lexyAnimator = lexyT.GetComponent<Animator>();
        lexyTargetPosT = GameObject.Find(lexyTargetPosPath).transform;
        Transform roadsParent = GameObject.Find(roadsPath)?.transform;
        roadTs = new Transform[roadsParent.childCount];
        for (int i = 0; i < roadsParent.childCount; ++i)
        {
            roadTs[i] = roadsParent.GetChild(i);
        }
        Transform goalsParent = GameObject.Find(goalsPath)?.transform;
        goalTs = new Transform[goalsParent.childCount];
        for (int i = 0; i < goalsParent.childCount; ++i)
        {
            goalTs[i] = goalsParent.GetChild(i);
        }
        Transform piecesParent = GameObject.Find(piecesPath)?.transform;
        pieces = new GameObject[piecesParent.childCount];
        for (int i = 0; i < piecesParent.childCount; ++i)
        {
            pieces[i] = piecesParent.GetChild(i).gameObject;
        }
        Transform pieceGoalsParent = GameObject.Find(pieceGoalsPath)?.transform;
        pieceGoalTs = new Transform[pieceGoalsParent.childCount];
        for (int i = 0; i < pieceGoalsParent.childCount; ++i)
        {
            pieceGoalTs[i] = pieceGoalsParent.GetChild(i);
        }

        //Other Objects 초기화
        clearDoorLeftT = GameObject.Find(clearDoorLeftPath).transform;
        clearDoorRightT = GameObject.Find(clearDoorRightPath).transform;
        cellingT = GameObject.Find(cellingPath).transform;

        //Camera Objects 초기화
        rotationObjectT = charRoboT.Find("Rotation Camera");
        mainCameraT = rotationObjectT.Find("Main Camera");
        mainCamera = mainCameraT.GetComponent<Camera>();
        cutsceneCamera = GameObject.Find(cutsceneCameraTPath).GetComponent<Camera>();
        TopviewCamera = GameObject.Find(TopviewCameraTPath).GetComponent<Camera>();

        // UI Objects 초기화
        clearPopup = GameObject.Find(clearPopupPath);
        executeWarn = GameObject.Find(executeWarnPath);
        dialogueUI = GameObject.Find(dialogueUIPath);
        clearTMPText = clearPopup.transform.Find("Clear_TMPText").GetComponent<TMP_Text>();
        prompt = GameObject.Find(promptPath);
        promptTMPInputField = prompt.transform.Find("Prompt_TMPInputField").GetComponent<TMP_InputField>();
        //promptOpen = prompt.transform.Find("PromptOpen_Button").gameObject;
        loadingImage = prompt.transform.Find("Loading_Image").GetComponent<Image>();
        executeWarnTMPText = executeWarn.GetComponent<TMP_Text>();
        dialogueTMPText = dialogueUI.transform.Find("Button/DialogueText").GetComponent <TMP_Text>();
        variableJoystick = GameObject.Find(variableJoystickPath);
        overlayCanvasG = GameObject.Find(overlayPath).GetComponent<CanvasGroup>();
        overlayPanelRectT = overlayCanvasG.transform.Find("Panel").GetComponent<RectTransform>();
        overlayCloseButton = overlayCanvasG.transform.Find("Close_Button").GetComponent<Button>();
        overlayClearButton = overlayCanvasG.transform.Find("ClearAll_Button").GetComponent<Button>();
        interact = GameObject.Find(interactPath);
        pauseButton = interact.transform.Find("Pause").GetComponent<Button>();
        resetButton = interact.transform.Find("Reset").GetComponent<Button>();
        switchCameraButton = interact.transform.Find("Switch_camera").GetComponent<Button>();
    }
}