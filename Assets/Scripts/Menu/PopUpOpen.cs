using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopUpOpen : MonoBehaviour
{
    public GameObject confirmPanel;

    public void OnHomeClicked()
    {
        confirmPanel.SetActive(!confirmPanel.activeSelf);
    }

    public void OnNoClicked()
    {
        confirmPanel.SetActive(false);
    }
}
