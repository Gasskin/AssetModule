using System.IO;
using UnityEditor;

public class Build 
{
    [MenuItem("Tools/打包")]
    public static void BuildAB()
    {
        if (!Directory.Exists("AssetBundles"))
            Directory.CreateDirectory("AssetBundles");
        BuildPipeline.BuildAssetBundles("AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
    }
}
