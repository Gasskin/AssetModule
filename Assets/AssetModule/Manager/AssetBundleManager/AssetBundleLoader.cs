using System.IO;
using UnityEngine;

public class AssetBundleLoader: IReference
{
#region Property
    public AssetBundle assetBundle { get; private set; }
    public int refCount{ get; private set; }
    public string path{ get; private set; }
#endregion

#region 加载资源
    public void Load(string path, int count = 1)
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
        refCount = count;
    }
#endregion

#region 引用计数
    public void AddRef(int count = 1)
    {
        refCount += count;
    }

    public void ReduceRef(int count = 1)
    {
        refCount -= count;
        if (refCount <= 0) 
            assetBundle.Unload(true);
    }
#endregion

#region 归还以及卸载
    public void Clear()
    {
        if (assetBundle != null) 
        {
            assetBundle.Unload(true);
        }

        refCount = 0;
        path = "";
    }
#endregion
}