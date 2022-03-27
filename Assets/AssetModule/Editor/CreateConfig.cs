using UnityEditor;
using UnityEngine;

public class CreateConfig 
{
    [MenuItem("Assets/Create/AssetModule/Create Config")]
    private static void Create()
    {
        var guid = AssetDatabase.FindAssets("AssetBundleBuildConfig");
        // 说明没有配置表
        if (guid == null || guid.Length <= 1)
        {
            var savePath = Selection.activeObject == null
                ? "Assets/AssetBundleBuildConfig.asset"
                : $"{AssetDatabase.GetAssetPath(Selection.activeObject)}/AssetBundleBuildConfig.asset";

            var asset = ScriptableObject.CreateInstance<AssetBundleBuildConfig>();

            AssetDatabase.CreateAsset(asset, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
        }
        else
        {
            for (int i = 0; i < guid.Length; i++)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GUIDToAssetPath(guid[i])) != typeof(AssetBundleBuildConfig))
                    continue;
                Debug.LogError("配置表已经存在：", AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(AssetDatabase.GUIDToAssetPath(guid[i])));
            }
        }
    }
}
