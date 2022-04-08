using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceLoadTest : MonoBehaviour
{
    void Start()
    {
        var go = AssetManager.Instance.LoadAsset<GameObject>("Assets/GameData/Prefab/Cube.prefab");
        Instantiate(go);
    }

}
