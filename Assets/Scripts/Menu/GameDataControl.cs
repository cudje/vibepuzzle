using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameDataControl : MonoBehaviour
{
    public TMP_InputField IDInputField;
    public int stageCount = 5;

    private void Start()
    {
        GameData.stageClear = new bool[stageCount];
    }

    public void SetGameDataID()
    {
        GameData.SetUserText(IDInputField.text);
        GameData.setClear();
    }
}
