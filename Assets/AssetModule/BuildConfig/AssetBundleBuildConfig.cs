using System.Collections.Generic;
using UnityEngine;

public class AssetBundleBuildConfig : ScriptableObject
{
    // 打包路径
    public string targetPath;
    
    // 该文件夹路径下的所有Prefab都会被单独打成一个AB包，包名即Prefab名
    public List<string> prefabList;
    
    // 该文件夹会被打成一个AB包，文件夹名即包名
    public List<string> assetList;
}
