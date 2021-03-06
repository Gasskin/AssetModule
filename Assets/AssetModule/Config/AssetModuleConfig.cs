using System.Collections.Generic;
using UnityEngine;

public enum ResourceMode
{
    Editor,
    Application,
}

public class AssetModuleConfig : ScriptableObject
{
    public const string buildConfigName = "AssetModuleConfig";
    
    // 是否生成XML文件（默认配置文件是bytes，xml用于debug）
    public bool buildXML = true;

    [SerializeField]
    public ResourceMode resourceMode;

    // 资源垃圾池中每个资源可以存活的最大时间，秒
    public float aliveTime;

    // 异步加载时，每一帧最大的加载时间，毫秒
    public int asyncTimeLimit;
    
    // 配置文件的保存路径，以及配置文件的名称
    public string configPath = "Assets/Resources";
    public string configName = "AssetBundleConfig";
    
    // 打包路径
    public string targetPath;
    
    // 该文件夹路径下的所有Prefab都会被单独打成一个AB包，包名即Prefab名
    public List<string> prefabList;
    
    // 该文件夹会被打成一个AB包，文件夹名即包名
    public List<string> assetList;
}
