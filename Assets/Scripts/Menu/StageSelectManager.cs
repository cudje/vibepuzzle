using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageSelectManager : MonoBehaviour
{
    public GameObject[] parent;

    void Start()
    {
        for (int i = 0; i < GameData.stageClear.Length && i < parent.Length; i++)
        {
            foreach (Transform child in parent[i].transform)
            {
                if (child.name.Contains("On"))
                {
                    child.gameObject.SetActive(GameData.stageClear[i]);
                }
                else if (child.name.Contains("Off"))
                {
                    child.gameObject.SetActive(!GameData.stageClear[i]);
                }
            }
        }
    }

}