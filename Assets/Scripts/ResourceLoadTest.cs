using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class ResourceLoadTest : MonoBehaviour
{
    void Start()
    {
        var instance = new TestXML();
        instance.list = new List<int>();
        
        for (int i = 0; i < 100000; i++)
        {
            instance.list.Add(i);
        }
        
        using (var stream = new FileStream("AssetBundles/test.bytes", FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            var bf = new BinaryFormatter();
            bf.Serialize(stream, instance);
        }
    }
}
