using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class ResourceLoadTest : MonoBehaviour
{
    void Start()
    {
        using (var stream = new FileStream($"{Application.dataPath}/Resources/AssetBundleConfig.xml", FileMode.OpenOrCreate, FileAccess.ReadWrite)) 
        {
            var xmlSerializer = new XmlSerializer(typeof(AssetBundleConfig));
            var instance = xmlSerializer.Deserialize(stream) as AssetBundleConfig;

            var path = "Assets/GameData/Prefab/Cube.prefab";
            var crc = CRC32.GetCRC32(path);
            foreach (var assetConfig in instance.bundleList)
            {
                if (crc == assetConfig.crc) 
                {
                    for (int i = 0; i < assetConfig.dependence.Count; i++)
                        AssetBundle.LoadFromFile($"AssetBundles/{assetConfig.dependence[i]}");
                    var ab = AssetBundle.LoadFromFile($"AssetBundles/{assetConfig.bundleName}");
                    var go = ab.LoadAsset<GameObject>(assetConfig.assetName);
                    Instantiate(go);
                    break;
                }
            }
        }
    }
}

