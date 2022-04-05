using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class AssetBundleManager : Singleton<AssetBundleManager>
{
#region Field
    /// 打包配置文件，存放了Bundle配置文件的名称，以及打包后的Bundle路径
    private AssetBundleBuildConfig assetBundleBuildConfig;
    /// Bundle配置文件，存放了Bundle的所有信息
    private AssetBundleConfig assetBundleConfig;

    /// 所有的AssetBundle信息
    private Dictionary<uint, AssetBundleLoader> assetBundleLoaders;
#endregion

#region 生命周期
    private void Awake()
    {
        LoadAssetBundleBuildConfig();
        
        assetBundleLoaders = new Dictionary<uint, AssetBundleLoader>();
    }

    protected override void OnDestroyHandler()
    {
        Resources.UnloadAsset(assetBundleBuildConfig);
    }
#endregion
    
#region 初始化相关
    /// <summary>
    /// 加载配置文件
    /// </summary>
    private void LoadAssetBundleBuildConfig()
    {
        assetBundleBuildConfig = Resources.Load<AssetBundleBuildConfig>(AssetBundleBuildConfig.buildConfigName);
        if (assetBundleBuildConfig == null)
        {
            Debug.LogError($"Resources目录下不存在：{AssetBundleBuildConfig.buildConfigName}");
            return;
        }

        var configBytes = Resources.Load<TextAsset>($"{assetBundleBuildConfig.configName}");
        if (configBytes == null)
        {
            Debug.LogError($"加载AssetBundle配置文件失败：{assetBundleBuildConfig.configName}");
            return;
        }

        using var stream = new MemoryStream(configBytes.bytes);
        var bf = new BinaryFormatter();
        assetBundleConfig = bf.Deserialize(stream) as AssetBundleConfig;

        if (configBytes == null)
            Debug.LogError($"反序列化失败");
    }
#endregion

#region 加载相关
    /// <summary>
    /// 加载资源所需Bundle,包括依赖
    /// </summary>
    /// <param name="path">资源全路径，带后缀</param>
    public void LoadAssetBundles(string path)
    {
        var crc32 = CRC32.GetCRC32(path);
        if (TryGetAssetConfig(crc32, out var config)) 
        {
            // 加载资源对应的Bundle
            LoadAssetBundle(config.bundleName);

            // 加载所有依赖的Bundle
            for (int i = 0; i < config.dependence.Count; i++)
                LoadAssetBundle(config.dependence[i]);
        }
    }
    
    /// <summary>
    /// 获取资源信息
    /// </summary>
    /// <param name="crc32">资源名称对应的CRC</param>
    /// <param name="config">资源配置表</param>
    /// <returns>是否成功</returns>
    private bool TryGetAssetConfig(uint crc32, out AssetConfig config)
    {
        for (int i = 0; i < assetBundleConfig.bundleList.Count; i++)
        {
            config = assetBundleConfig.bundleList[i];
            if (config.crc == crc32)
                return true;
        }
        
        config = null;
        return false;
    }

    /// <summary>
    /// 加载Bundle
    /// </summary>
    /// <param name="bundleName">Bundle名称</param>
    private void LoadAssetBundle(string bundleName)
    {
        var crc = CRC32.GetCRC32(bundleName);
    
        // 已经加载过这个Bundle了
        if (assetBundleLoaders.TryGetValue(crc,out var loader))
        {
            loader.AddRef();
        }
        // 第一次加载这个Bundle
        else
        {
            loader = ReferenceManager.Instance.Acquire<AssetBundleLoader>();
            loader.Load(Path.Combine(assetBundleBuildConfig.targetPath, bundleName));
            assetBundleLoaders.Add(crc, loader);
        }
    }

    /// <summary>
    /// 卸载Bundle
    /// </summary>
    /// <param name="bundleName">Bundle名称</param>
    private void UnLoadAssetBundle(string bundleName)
    {
        var crc = CRC32.GetCRC32(bundleName);
    
        // 已经加载过这个Bundle了
        if (assetBundleLoaders.TryGetValue(crc,out var loader))
        {
            // 说明被卸载了
            if (loader.ReduceRef())
            {
                ReferenceManager.Instance.Release(loader);
                assetBundleLoaders.Remove(crc);
            }
        }
    }
#endregion
}   