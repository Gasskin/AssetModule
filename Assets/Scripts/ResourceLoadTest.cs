using UnityEngine;

public class ResourceLoadTest : MonoBehaviour
{
    void Start()
    {
        var ab = AssetBundle.LoadFromFile("AssetBundles/test/model.ab");
        var go = ab.LoadAsset<GameObject>("Attack");
        Instantiate(go);
    }
}
