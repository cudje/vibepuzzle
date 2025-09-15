using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameDataControl : MonoBehaviour
{
    public TMP_InputField IDInputField;

    public void SetGameDataID()
    {
        GameData.SetUserText(IDInputField.text);
        GameData.setClear();
    }
}
