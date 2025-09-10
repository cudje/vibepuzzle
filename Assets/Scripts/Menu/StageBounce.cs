using UnityEngine;
using UnityEngine.UI;

public class StageBounce : MonoBehaviour
{
    public GameObject[] stageObjects; // 에디터에서 연결할 스테이지 오브젝트들
    public Sprite[] profileSprite;

    void Start()
    {
        UpdateStageObjects();
    }

    // 마지막 true인 인덱스를 찾아 해당 오브젝트만 활성화
    public void UpdateStageObjects()
    {
        // 모든 오브젝트 비활성화
        foreach (var obj in stageObjects)
            obj.SetActive(false);

        // 마지막 true인 인덱스 찾기
        int lastTrueIndex = -1;
        for (int i = 0; i < GameData.stageClear.Length; i++)
        {
            if (GameData.stageClear[i])
                lastTrueIndex = i;
        }

        // true가 하나도 없으면 아무 것도 활성화하지 않음
        if (lastTrueIndex == -1)
            return;

        // 마지막 true인 인덱스만 활성화
        if (lastTrueIndex >= 0 && lastTrueIndex < stageObjects.Length)
        {
            stageObjects[lastTrueIndex].GetComponent<Image>().sprite = profileSprite[GameData.profileImage];
            stageObjects[lastTrueIndex].SetActive(true);
        }
    }

    public void UpdateBounceImageToProfileImage()
    {
        for(int i = 0; i < stageObjects.Length; ++i)
        {
            if (stageObjects[i].activeSelf)
            {
                stageObjects[i].GetComponent<Image>().sprite = profileSprite[GameData.profileImage];
            }
        }
    }
}
