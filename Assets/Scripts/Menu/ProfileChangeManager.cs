using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileChangeManager : MonoBehaviour
{
    public ProfileTelecom profileTManager;
    public StageBounce bounceManager;

    [Header("Profile UI")]
    public Image profile;          // 바꿀 프로필 Image
    public Sprite[] sprites;       // 선택 가능한 스프라이트들

    [Header("Selection UI")]
    public RectTransform checkmark;     // 선택 표시(체크마크) UI
    public RectTransform[] positions;   // 각 스프라이트에 대응되는 체크 위치들(썸네일/슬롯)

    [Header("Nickname UI")]
    public TextMeshProUGUI nicknameText; // 닉네임 표시할 TMP 텍스트

    [Header("Checkmark Offset")]
    public Vector2 offset = new Vector2(60f, 70f); // 오른쪽 위로 이동할 오프셋 값

    private void Start()
    {
        Debug.Log($"현재 프로필 INDEX={GameData.profileImage}");
        // GameData.profileImage가 범위 내에 있으면 해당 인덱스로 설정
        if (GameData.profileImage >= 0 && GameData.profileImage < sprites.Length)
        {
            InitProfile();
        }

        if (nicknameText != null)
        {
            nicknameText.text = GameData.userText;
        }
    }

    /// <summary>
    /// 정수 n을 받아 프로필 이미지를 sprites[n]로 변경하고
    /// 체크마크를 positions[n]으로 이동
    /// </summary>
    /// 
    private void InitProfile()
    {
        profile.sprite = sprites[GameData.profileImage];
        MoveCheckmark(GameData.profileImage);
    }

    public void SetProfile(int n)
    {
        if(GameData.profileImage == n) { return; }

        if (profile == null || sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("[ProfileChangeManager] profile 또는 sprites가 설정되지 않았습니다.");
            return;
        }

        // n 범위 보정
        n = Mathf.Clamp(n, 0, sprites.Length - 1);

        // 프로필 이미지 교체
        profile.sprite = sprites[n];

        // 체크마크 이동
        MoveCheckmark(n);

        GameData.profileImage = n;
        bounceManager.UpdateBounceImageToProfileImage();
        StartCoroutine(profileTManager.UpdateProfile(n));
    }

    /// <summary>
    /// 체크마크를 positions[n]으로 이동
    /// (체크마크를 해당 슬롯의 자식으로 붙이고 로컬 위치 0 + offset)
    /// </summary>
    private void MoveCheckmark(int n)
    {
        if (checkmark == null || positions == null || positions.Length == 0)
        {
            Debug.LogWarning("[ProfileChangeManager] checkmark 또는 positions가 설정되지 않았습니다.");
            return;
        }

        if (n < 0 || n >= positions.Length)
        {
            Debug.LogWarning($"[ProfileChangeManager] positions에 인덱스 {n}가 없습니다.");
            return;
        }

        // 체크마크를 해당 슬롯의 자식으로 붙이고 offset 적용
        checkmark.SetParent(positions[n], worldPositionStays: false);
        checkmark.anchoredPosition = offset;   // 오른쪽 위로 20px 이동
        checkmark.localScale = Vector3.one;
        checkmark.SetAsLastSibling();
    }

    // 버튼에서 직접 연결하기 편하도록 제공 (OnClick에 index 넣어 연결)
    public void OnClick_Select(int index) => SetProfile(index);
}
