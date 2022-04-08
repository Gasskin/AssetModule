using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetLoader: IComparable<AssetLoader>,IReference
{

#region Property
    public Object Asset { get; private set; }
    
    public float LastUsedTime { get; private set; }

    public int RefCount { get; private set; }
    
    public uint Crc { get; private set; }
#endregion


#region 加载资源
    public void Init(Object asset,uint crc)
    {
        RefCount = 1;
        Asset = asset;
        Crc = crc;
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
        
        var refComp = RefCount.CompareTo(other.RefCount);
        if (refComp != 0) 
            return refComp;
        return LastUsedTime.CompareTo(other.LastUsedTime);
    }
    
    public void Clear()
    {
        LastUsedTime = 0;
        Crc = 0;
        RefCount = 0;
        Asset = null;
    }
#endregion
}
