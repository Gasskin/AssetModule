using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class ResourceLoadTest : MonoBehaviour
{
    void Start()
    {
        using (var stream = new FileStream("AssetBundles/test.xml", FileMode.OpenOrCreate, FileAccess.ReadWrite)) 
        {
            var xmlSerializer = new XmlSerializer(typeof(TestXML));
            var instance = xmlSerializer.Deserialize(stream) as TestXML;

            Debug.Log(instance.id);
            Debug.Log(instance.test.name);
        }
    }
}

