using System.IO;
using UnityEditor;

public class Build 
{
    [MenuItem("Tools/打包")]
    public static void BuildAB()
    {
        if (Directory.Exists("AssetBundles"))
        {
            var info = new DirectoryInfo("AssetBundles");
            var all = info.GetDirectories();
            foreach (var child in all)
                child.Delete(true);
            info.Delete(true);
        }
        else
        {
            Directory.CreateDirectory("AssetBundles");
        }        
        
        BuildPipeline.BuildAssetBundles("AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
    }
}
