using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceLoadTest : MonoBehaviour
{
    void Start()
    {
        AssetBundleManager.Instance.LoadAssetBundles("Assets/GameData/Prefab/Cube.prefab");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
        }
    }
}
