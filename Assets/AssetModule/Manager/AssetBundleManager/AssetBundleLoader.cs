using UnityEngine;

public class AssetBundleLoader: IReference
{
    private AssetBundle assetBundle;
    private int refCount;
    private string path;
    
    public void Load(string path)
    {
        if (assetBundle != null) 
        {
            Debug.LogError("没有释放资源");
            return;
        }
        
        assetBundle = AssetBundle.LoadFromFile(path);
        if (assetBundle == null)
            Debug.LogError($"加载AssetBundle失败：{path}");
        
        this.path = path;
        refCount = 1;
    }

    public void AddRef()
    {
    #if UNITY_EDITOR
        var go = GameObject.Find($"{path}_{refCount}");
        if (go != null)
        {
            go.name = $"{path}_{refCount + 1}";
        }
    #endif
        refCount++;
    }

    public bool ReduceRef()
    {
    #if UNITY_EDITOR
        var go = GameObject.Find($"{path}_{refCount}");
        if (go != null)
        {
            go.name = $"{path}_{refCount - 1}";
            if (refCount - 1 <= 0)
                Object.DestroyImmediate(go);
        }
    #endif
        
        refCount--;
        if (refCount <= 0)
        {
            assetBundle.Unload(true);
            return true;
        }

        return false;
    }
    
    public void Clear()
    {
        if (assetBundle != null) 
        {
            assetBundle.Unload(true);
        }

        refCount = 0;
        path = "";
    }
}