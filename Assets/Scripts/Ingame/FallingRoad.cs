using UnityEngine;

public class FallingRoad : MonoBehaviour
{
    public InteractManager interact;
    public Clear3DConditionManager condition;

    // Trigger 规侥老 版快 (Collider俊 isTrigger 眉农)
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name.Contains("Road"))
        {
            interact.DoReset();
            condition.CheckClear();
        }
    }
}