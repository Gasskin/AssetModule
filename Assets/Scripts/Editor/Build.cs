using System.IO;
using UnityEditor;
using UnityEngine;

public class Build 
{
    [MenuItem("Tools/打包")]
    public static void BuildAB()
    {
        var guid = AssetDatabase.FindAssets("AssetBundleBuildConfig");
        // 说明没有配置表
        if (guid == null || guid.Length <= 1)
        {
            Debug.LogError("没有创建打包配置文件");
        }
        else
        {
            for (int i = 0; i < guid.Length; i++)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GUIDToAssetPath(guid[i])) != typeof(AssetBundleBuildConfig))
                    continue;
                var asset = AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(AssetDatabase.GUIDToAssetPath(guid[i]));

                foreach (var path in asset.prefabList)
                {
                    var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                    for (int j = 0; j < files.Length; j++)
                    {
                        Debug.Log(files[j]);
                    }
                }

                foreach (var path in asset.assetList)
                {
                    Debug.Log(path);
                }
            }
        }
    }
}
