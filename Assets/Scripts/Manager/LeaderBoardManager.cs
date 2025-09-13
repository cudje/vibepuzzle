using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class LeaderBoardManager : MonoBehaviour
{
    public Toggle[] initToggles;
    public GameObject entryPrefab;
    public GameObject[] Tabs;
    public Sprite[] profileSprites;
    public Sprite[] rankingSprites;

    private Image[] targets;
    private Color onColor = new Color(1f, 1f, 1f, 222f / 255f); // On (투명도 210)
    private Color offColor = new Color(1f, 1f, 1f, 150f / 255f); // Off (투명도 150)
    private Transform clearTimeContent;
    private Transform promptLenContent;
    private GameObject clearTimeMe;
    private GameObject promptLenMe;
    private TMP_Text recentStage;

    // Start is called before the first frame update
    void Start()
    {
        clearTimeContent = Tabs[0].transform.Find("ScrollView/Viewport/Content");
        clearTimeMe = Tabs[0].transform.Find("MeBackground").gameObject;
        promptLenContent = Tabs[1].transform.Find("ScrollView/Viewport/Content");
        promptLenMe = Tabs[1].transform.Find("MeBackground").gameObject;
        recentStage = SceneHubManager.I.clearPopup.transform.Find("Top_Image/Top_TMPText").GetComponent<TMP_Text>();
        recentStage.text = "스테이지 " + GameData.recentStage.Substring(1);

        targets = new Image[initToggles.Length];
        for(int i = 0; i < initToggles.Length; i++)
        {
            int index = i;
            targets[i] = initToggles[i].transform.Find("Background_Image").GetComponent<Image>();
            initToggles[index].onValueChanged.AddListener(isOn =>
            {
                UpdateColors();               // 색 갱신
                Tabs[index].SetActive(isOn);  // 탭 표시 갱신
            });

            Tabs[index].SetActive(initToggles[index].isOn);
        }
    }

    void UpdateColors()
    {
        for (int i = 0; i < initToggles.Length; i++)
            targets[i].color = initToggles[i].isOn ? onColor : offColor;
    }

    public void AddRanker()
    {
        //자기자신
        LeaderboardEntry leaderE = new LeaderboardEntry(GameData.userText, GameData.promptLen, GameData.moveCount, GameData.profileImage);
        SetPrefab(clearTimeMe, leaderE, GameData.rank_clear_time - 1);
        SetPrefab(promptLenMe, leaderE, GameData.rank_tokens - 1);


        //남들
        for (int i = 0; i < GameData.prompt_top10.Count; i++)
        {
            GameObject entry = Instantiate(entryPrefab, promptLenContent);
            SetPrefab(entry, GameData.prompt_top10[i], i);
        }
        for (int i = 0; i < GameData.time_top10.Count; i++)
        {
            GameObject entry = Instantiate(entryPrefab, clearTimeContent);
            SetPrefab(entry, GameData.time_top10[i], i);
        }
    }

    void SetPrefab(GameObject entry, LeaderboardEntry e, int ranking)
    {
        Image rankingImage = entry.transform.Find("RankingBackground/Ranking_Image").GetComponent<Image>();
        updateRankingImage(rankingImage, ranking);

        TMP_Text rankingText = entry.transform.Find("RankingBackground/Ranking_Image/Ranking_TMPText").GetComponent<TMP_Text>();
        rankingText.text = (ranking + 1).ToString();

        Image profileImage = entry.transform.Find("ProfileBackground/Profile_Image").GetComponent<Image>();
        profileImage.sprite = profileSprites[e.profile_image];

        TMP_Text nameText = entry.transform.Find("Name_TMPText").GetComponent<TMP_Text>();
        nameText.text = e.user_id;

        TMP_Text clearTimeText = entry.transform.Find("ClearTime_TMPText").GetComponent<TMP_Text>();
        clearTimeText.text = e.clear_time_ms.ToString() + "초";

        TMP_Text promptLenText = entry.transform.Find("PromptLen_TMPText").GetComponent<TMP_Text>();
        promptLenText.text = e.prompt_length.ToString() + "Byte";
    }

    void updateRankingImage(Image rankingImage, int ranking)
    {
        if (ranking < 3)
        {
            rankingImage.sprite = rankingSprites[ranking];
        }
        else
        {
            Color c = rankingImage.color;
            c.a = 0f;
        }
    }
}
