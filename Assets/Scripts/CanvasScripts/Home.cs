using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Home : MonoBehaviour
{
    public GameObject confirmPanel;

    public void OnHomeClicked()
    {
        confirmPanel.SetActive(true);
    }

    public void OnNoClicked()
    {
        confirmPanel.SetActive(false);
    }
}
