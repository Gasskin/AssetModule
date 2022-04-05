using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateConfig 
{
    [MenuItem("Assets/Create/AssetModule/Create Config")]
    private static void Create()
    {
        var config = Resources.Load<AssetBundleBuildConfig>(AssetBundleBuildConfig.buildConfigName);
        // 说明没有配置表
        if (config == null)
        {
            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");
            var asset = ScriptableObject.CreateInstance<AssetBundleBuildConfig>();

            AssetDatabase.CreateAsset(asset, $"Assets/Resources/{AssetBundleBuildConfig.buildConfigName}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
        }
        else
        {
            EditorGUIUtility.PingObject(config);
            Debug.Log("资源管理 - 配置文件已经存在");
        }
    }
}
