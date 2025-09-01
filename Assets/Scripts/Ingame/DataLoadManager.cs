using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataLoadManager : MonoBehaviour
{
    public TMP_InputField nameIF;

    void Start()
    {
        nameIF.text = GameData.userText;
    }

}
