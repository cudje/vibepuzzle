using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controll3DManager : MonoBehaviour
{
    public Behavior3DManager behavior;
    public Clear3DConditionManager condition;
    public PromptInterpreter promptInterpreter;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            behavior.Up();
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            behavior.Down();
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            behavior.Left();
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            behavior.Right();
        else if (Input.GetKeyDown(KeyCode.Q))
            behavior.Pick();
        else if (Input.GetKeyDown(KeyCode.E))
            behavior.Drop();
        else if (Input.GetKeyDown(KeyCode.F))
            condition.CheckClear();
        else if (Input.GetKeyDown(KeyCode.T))
            promptInterpreter.fusion_start();
        else if (Input.GetKeyDown(KeyCode.F1))
            behavior.GetSearch(1);
        else if (Input.GetKeyDown(KeyCode.F2))
            behavior.GetSearch(2);
        else if (Input.GetKeyDown(KeyCode.F3))
            behavior.GetSearch(3);
        else if (Input.GetKeyDown(KeyCode.F4))
            behavior.GetSearch(4);
    }
}
