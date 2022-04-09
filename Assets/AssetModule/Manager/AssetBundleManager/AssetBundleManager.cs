using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AssetBundleManager : Singleton<AssetBundleManager>
{
#region Field
    /// 所有的AssetBundle信息
    private Dictionary<uint, AssetBundleLoader> assetBundleLoaders;
#endregion

#region 生命周期
    private void Awake()
    {
        assetBundleLoaders = new Dictionary<uint, AssetBundleLoader>();
    }
#endregion
    


#region 加载相关
    /// <summary>
    /// 加载资源所需Bundle,包括依赖
    /// </summary>
    /// <param name="path">资源全路径，带后缀</param>
    public AssetBundleLoader LoadAssetBundles(string path)
    {
        var crc32 = CRC32.GetCRC32(path);
        if (ConfigManager.TryGetAssetConfig(crc32, out var config)) 
        {
            // 加载所有依赖的Bundle
            for (int i = 0; i < config.dependence.Count; i++)
                LoadAssetBundle(config.dependence[i]);
            
            // 加载资源对应的Bundle
            return LoadAssetBundle(config.bundleName);
        }

        return null;
    }

    /// <summary>
    /// 释放一个资源引用的所有Bundle
    /// </summary>
    /// <param name="crc">资源路径的crc</param>
    public void UnLoadAssetBundles(uint crc)
    {
        if (ConfigManager.TryGetAssetConfig(crc, out var config)) 
        {
            // 卸载资源本身的Bundle
            UnLoadAssetBundle(config.bundleName);

            // 卸载所有依赖的Bundle
            for (int i = 0; i < config.dependence.Count; i++)
                UnLoadAssetBundle(config.dependence[i]);
        }
    }
    

    /// <summary>
    /// 加载Bundle
    /// </summary>
    /// <param name="bundleName">Bundle名称</param>
    private AssetBundleLoader LoadAssetBundle(string bundleName)
    {
        var crc = CRC32.GetCRC32(bundleName);
    
        // 已经加载过这个Bundle了
        if (assetBundleLoaders.TryGetValue(crc,out var loader))
        {
        #if UNITY_EDITOR
            var go = GameObject.Find($"{bundleName}_{loader.refCount}");
            go.name = $"{bundleName}_{loader.refCount + 1}";
        #endif
            loader.AddRef();
        }
        // 第一次加载这个Bundle
        else
        {
            var path = $"{ConfigManager.assetBundleBuildConfig.targetPath}/{bundleName}";
        #if UNITY_EDITOR
            var go = new GameObject($"{bundleName}_1");
            go.transform.SetParent(transform);
            var fileInfo = new FileInfo(path);
            var size = go.AddComponent<AssetBundleSize>();
            size.size = $"{fileInfo.Length / 1024f}KB";
        #endif
            loader = ReferenceManager.Instance.Acquire<AssetBundleLoader>();
            loader.Load(path);
            assetBundleLoaders.Add(crc, loader);
        }

        return loader;
    }

    /// <summary>
    /// 卸载Bundle
    /// </summary>
    /// <param name="bundleName">Bundle名称</param>
    public void UnLoadAssetBundle(string bundleName)
    {
        var crc = CRC32.GetCRC32(bundleName);
    
        // 已经加载过这个Bundle了
        if (assetBundleLoaders.TryGetValue(crc,out var loader))
        {
        #if UNITY_EDITOR
            var go = GameObject.Find($"{bundleName}_{loader.refCount}");
            go.name = $"{bundleName}_{loader.refCount - 1}";
        #endif
            // 说明被卸载了
            loader.ReduceRef();
            if (loader.refCount <= 0) 
            {
            #if UNITY_EDITOR
                DestroyImmediate(go);
            #endif
                ReferenceManager.Instance.Release(loader);
                assetBundleLoaders.Remove(crc);
            }
        }
    }
#endregion
}   
