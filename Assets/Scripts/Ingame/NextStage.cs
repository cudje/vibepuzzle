using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextStage : MonoBehaviour
{
    private SceneLoadManager sceneLoad;
    public int load;

    private void Start()
    {
        sceneLoad = GameObject.Find("Manager/SceneLoadManager").GetComponent<SceneLoadManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            sceneLoad.load(load);
        }
    }
}
