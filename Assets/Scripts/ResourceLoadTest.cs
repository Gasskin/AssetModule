using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceLoadTest : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var go = AssetManager.Instance.LoadAsset<GameObject>("Assets/GameData/Prefab/Attack.prefab");
            Instantiate(go);
        }
        
        if (Input.GetMouseButtonDown(1))
        {
        }
    }
}
