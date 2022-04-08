using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetManager : Singleton<AssetManager>
{
#region Field
    private Dictionary<uint, AssetLoader> assets;
    private List<AssetLoader> assetGarbage;
#endregion

#region 生命周期
    private void Awake()
    {
        assets = new Dictionary<uint, AssetLoader>();
        assetGarbage = new List<AssetLoader>();
    }
#endregion

#region 资源的同步加载
    public T LoadAsset<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path))
            return null;
    #if UNITY_EDITOR
        if (ConfigManager.assetBundleBuildConfig.resourceMode == ResourceMode.Editor)
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        else
    #endif
        {
            if (TryGetAssetFromGarbage(path, out var loader))
                return loader.Asset as T;
            if (TryGetAsset(path, out loader)) 
                return loader.Asset as T;
            return LoadNewAsset<T>(path);
        }
    }
#endregion

#region 工具方法
    /// <summary>
    /// 尝试从缓存中获取资源
    /// </summary>
    /// <param name="path">资源全路径</param>
    /// <param name="assetLoader">返回的资源结构</param>
    /// <returns>是否成功</returns>
    private bool TryGetAsset(string path, out AssetLoader assetLoader)
    {
        var crc = CRC32.GetCRC32(path);
        
        if (assets.TryGetValue(crc,out assetLoader))
        {
            assetLoader.AddRef();
            assetLoader.RefreshTime();
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 尝试中垃圾池中获取资源
    /// </summary>
    /// <param name="path">资源全路径</param>
    /// <param name="assetLoader">返回的资源结构</param>
    /// <returns>是否成功</returns>
    private bool TryGetAssetFromGarbage(string path, out AssetLoader assetLoader)
    {
        var crc = CRC32.GetCRC32(path);
        
        for (int i = 0; i < assetGarbage.Count; i++)
        {
            if (assetGarbage[i].Crc == crc)
            {
                assetLoader = assetGarbage[i];
                assetLoader.AddRef();
                assetLoader.RefreshTime();
                assets.Add(crc,assetLoader);
                assetGarbage.RemoveAt(i);
                return true;
            }
        }
        
        assetLoader = null;
        return false;
    }

    /// <summary>
    /// 加载一个新的资源，以及他的所有Bundle
    /// </summary>
    /// <param name="path">资源全路径</param>
    /// <returns></returns>
    private T LoadNewAsset<T>(string path) where T : Object
    {
        var crc = CRC32.GetCRC32(path);

        var bundle = AssetBundleManager.Instance.LoadAssetBundles(path);
        if (bundle != null)
        {
            if (ConfigManager.TryGetAssetConfig(crc, out var config))
            {
            #if UNITY_EDITOR
                var go = new GameObject($"{config.assetName}_1");
                go.transform.SetParent(transform);
            #endif
                var loader = ReferenceManager.Instance.Acquire<AssetLoader>();
                var asset = bundle.assetBundle.LoadAsset<T>(config.assetName);
                loader.Init(asset, crc);
                assets.Add(crc, loader);
                return asset;
            }
        }

        return null;
    }
#endregion
}
