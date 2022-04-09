using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetLoader: IComparable<AssetLoader>,IReference
{

#region Property
    public Object Asset { get; private set; }
    
    public float LastUsedTime { get; private set; }

    public int RefCount { get; private set; }
    
    public uint CRC { get; private set; }
    
    public int GUID { get; private set; }
    
    public string AssetName { get; private set; }
#endregion

#region 加载资源
    public void Init(Object asset,uint crc,string name)
    {
        RefCount = 1;
        Asset = asset;
        CRC = crc;
        AssetName = name;
        GUID = asset.GetInstanceID();
        LastUsedTime = Time.realtimeSinceStartup;
    }
#endregion

#region 更新状态
    public void AddRef()
    {
        RefCount++;
    }

    public void ReduceRef()
    {
        RefCount--;
    }

    public void RefreshTime()
    {
        LastUsedTime = Time.realtimeSinceStartup;
    }
#endregion
    
#region Override
    public int CompareTo(AssetLoader other)
    {
        if (ReferenceEquals(this, other))
            return 0;
        if (ReferenceEquals(null, other)) 
            return 1;
        
        return LastUsedTime.CompareTo(other.LastUsedTime);
    }
    
    public void Clear()
    {
        LastUsedTime = 0;
        CRC = 0;
        RefCount = 0;
        GUID = 0;
        AssetName = "";
        Asset = null;
    }
#endregion
}
