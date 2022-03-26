using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class ResourceLoadTest : MonoBehaviour
{
    void Start()
    {
        var mainFestAB = AssetBundle.LoadFromFile("AssetBundles/AssetBundles");
        var mainFest = mainFestAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

        var deps = mainFest.GetAllDependencies("cube");
        foreach (var dep in deps)
            AssetBundle.LoadFromFile($"AssetBundles/{dep}");

        var cube = AssetBundle.LoadFromFile("AssetBundles/cube");
        Instantiate(cube.LoadAsset<GameObject>("Cube"));
    }
}