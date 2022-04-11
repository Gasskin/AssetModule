using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetManager : Singleton<AssetManager>
{
#region Field
    // 所有正在使用的资源
    private Dictionary<uint, AssetLoader> assets;
    // 不在使用的资源
    private List<AssetLoader> assetGarbage;
    // 异步加载队列
    private Dictionary<int, Queue<AsyncLoadTask<Object>>> asyncLoadQueue;
#endregion

#region Event
    public Action<string, Object> assetAsyncLoaded;
#endregion

#region 生命周期
    public AssetManager(Dictionary<uint, AssetLoader> assets, List<AssetLoader> assetGarbage)
    {
        this.assets = assets;
        this.assetGarbage = assetGarbage;
    }

    private void Awake()
    {
        assets = new Dictionary<uint, AssetLoader>();
        assetGarbage = new List<AssetLoader>();
        asyncLoadQueue = new Dictionary<int, Queue<AsyncLoadTask<Object>>>();

        StartCoroutine(AsyncLoadCoroutine());
    }

    private void Update()
    {
        var realTime = Time.realtimeSinceStartup;
        for (int i = assetGarbage.Count - 1; i >= 0; i--) 
        {
            var asset = assetGarbage[i];
            if (realTime - asset.LastUsedTime >= ConfigManager.assetBundleBuildConfig.aliveTime) 
            {
            #if UNITY_EDITOR
                var trans = transform.Find($"{asset.AssetName}_0");
                DestroyImmediate(trans.gameObject);
            #endif
                
                assetGarbage.RemoveAt(i);

            #if UNITY_EDITOR
                if (ConfigManager.assetBundleBuildConfig.resourceMode == ResourceMode.Editor) 
                {
                    Resources.UnloadAsset(asset.Asset);
                    ReferenceManager.Instance.Release(asset);
                }
                else
            #endif
                {
                    AssetBundleManager.Instance.UnLoadAssetBundles(asset.CRC);
                    ReferenceManager.Instance.Release(asset);
                }
            }
        }
    }
#endregion

#region 资源的同步加载/卸载
    /// <summary>
    /// 加载一个资源
    /// </summary>
    /// <param name="path"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T LoadAsset<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path))
            return null;
    #if UNITY_EDITOR
        if (ConfigManager.assetBundleBuildConfig.resourceMode == ResourceMode.Editor)
        {
            return LoadAssetEditor<T>(path);
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

    /// <summary>
    /// 释放一个资源
    /// </summary>
    /// <param name="asset"></param>
    public void UnLoadAsset(Object asset)
    {
        var guid = asset.GetInstanceID();
        foreach (var loader in assets.Values)
        {
            if (loader.GUID == guid)
            {
            #if UNITY_EDITOR
                var go = GameObject.Find($"{loader.AssetName}_{loader.RefCount}");
                go.name = $"{loader.AssetName}_{loader.RefCount - 1}";
                if (loader.RefCount <= 1) 
                {
                    if (ConfigManager.assetBundleBuildConfig.aliveTime <= 0)
                    {
                        DestroyImmediate(go);
                    }
                    else
                    {
                        go.SetActive(false);
                        go.transform.SetAsLastSibling();
                    }
                }
            #endif
                loader.ReduceRef();
                if (loader.RefCount <= 0)
                {
                    // 直接释放，否则垃圾垃圾池，等待释放
                    if (ConfigManager.assetBundleBuildConfig.aliveTime <= 0)
                    {
                        assets.Remove(loader.CRC);
                    #if UNITY_EDITOR
                        if (ConfigManager.assetBundleBuildConfig.resourceMode == ResourceMode.Editor)
                        {
                            Resources.UnloadAsset(loader.Asset);
                            ReferenceManager.Instance.Release(loader);
                        }
                        else
                    #endif
                        {
                            AssetBundleManager.Instance.UnLoadAssetBundles(loader.CRC);
                            ReferenceManager.Instance.Release(loader);  
                        }
                    }
                    else
                    {
                        loader.RefreshTime();
                        assetGarbage.Add(loader);
                        assets.Remove(loader.CRC);
                    }
                }
                return;
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下加载一个资源
    /// </summary>
    /// <param name="path">资源全路径</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private T LoadAssetEditor<T>(string path) where T : Object
    {
        var crc = CRC32.GetCRC32(path);
        
        if (assets.TryGetValue(crc,out var assetLoader))
        {
        #if UNITY_EDITOR
            var trans = transform.Find($"{assetLoader.AssetName}_{assetLoader.RefCount}");
            trans.gameObject.name = $"{assetLoader.AssetName}_{assetLoader.RefCount + 1}";
        #endif
            assetLoader.AddRef();
            assetLoader.RefreshTime();
            return assetLoader.Asset as T;
        }
        
        ConfigManager.TryGetAssetConfig(crc, out var config);

    #if UNITY_EDITOR
        var go = new GameObject($"{config.assetName}_1"); 
        go.transform.SetParent(transform);
    #endif
        
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        assetLoader = ReferenceManager.Instance.Acquire<AssetLoader>();
        assetLoader.Init(asset, crc, config.assetName);
        
        assets.Add(crc,assetLoader);

        return assetLoader.Asset as T;
    }
#endif
#endregion

#region 资源的异步加载
    /// <summary>
    /// 异步加载资源的协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator AsyncLoadCoroutine()
    {
        yield break;
    }

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="priority">优先级（越高越优先）</param>
    /// <param name="callBack">回调</param>
    private void LoadAssetAsync<T>(string path, int priority, Action<Object> callBack)where T : Object
    {
        var crc = CRC32.GetCRC32(path);
        LoadAssetAsync<T>(crc, priority, callBack);
    }
    
    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <param name="crc">资源路径CRC</param>
    /// <param name="priority">优先级（越高越优先）</param>
    /// <param name="callBack">回调</param>
    private void LoadAssetAsync<T>(uint crc, int priority, Action<Object> callBack) where T : Object
    {
        if (TryGetAssetFromGarbage(crc, out var loader)) 
        {
            callBack?.Invoke(loader.Asset as T);
            return;
        }

        if (TryGetAsset(crc,out loader))
        {
            callBack?.Invoke(loader.Asset as T);
            return;
        }
        
        AddAsyncTask(crc,priority,callBack);
    }

    private void AddAsyncTask(uint crc,int priority,Action<Object> callBack)
    {
        var task = ReferenceManager.Instance.Acquire<AsyncLoadTask<Object>>();
        task.crc = crc;
        task.callback = callBack;
        
        if (asyncLoadQueue.TryGetValue(priority, out var queue))
        {
            queue.Enqueue(task);
        }
        else
        {
            queue = new Queue<AsyncLoadTask<Object>>();
            queue.Enqueue(task);
            asyncLoadQueue.Add(priority, queue);
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
        return TryGetAsset(crc, out assetLoader);
    }
    
    /// <summary>
    /// 尝试从缓存中获取资源
    /// </summary>
    /// <param name="crc">资源全路径</param>
    /// <param name="assetLoader">返回的资源结构</param>
    /// <returns>是否成功</returns>
    private bool TryGetAsset(uint crc, out AssetLoader assetLoader)
    {
        if (assets.TryGetValue(crc,out assetLoader))
        {
        #if UNITY_EDITOR
            var trans = transform.Find($"{assetLoader.AssetName}_{assetLoader.RefCount}");
            trans.gameObject.name = $"{assetLoader.AssetName}_{assetLoader.RefCount + 1}";
        #endif
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
        return TryGetAssetFromGarbage(crc, out assetLoader);
    }
    
    /// <summary>
    /// 尝试中垃圾池中获取资源
    /// </summary>
    /// <param name="crc">资源全路径CRC</param>
    /// <param name="assetLoader">返回的资源结构</param>
    /// <returns>是否成功</returns>
    private bool TryGetAssetFromGarbage(uint crc, out AssetLoader assetLoader)
    {
        for (int i = 0; i < assetGarbage.Count; i++)
        {
            if (assetGarbage[i].CRC == crc)
            {
            #if UNITY_EDITOR
                var trans = transform.Find($"{assetGarbage[i].AssetName}_0");
                trans.gameObject.name =$"{assetGarbage[i].AssetName}_1";
                trans.gameObject.SetActive(true);
                trans.SetAsFirstSibling();
            #endif
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
        return LoadNewAsset<T>(crc);
    }
    
    /// <summary>
    /// 加载一个新的资源，以及他的所有Bundle
    /// </summary>
    /// <param name="crc">资源全路径CRC</param>
    /// <returns></returns>
    private T LoadNewAsset<T>(uint crc) where T : Object
    {
        var bundle = AssetBundleManager.Instance.LoadAssetBundles(crc);
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
                loader.Init(asset, crc, config.assetName);
                assets.Add(crc, loader);
                return asset;
            }
        }

        return null;
    }
#endregion
}
