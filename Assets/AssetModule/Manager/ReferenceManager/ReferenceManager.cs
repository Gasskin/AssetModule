
using System;
using System.Collections.Generic;
using UnityEngine;

public class ReferenceManager : Singleton<ReferenceManager>
{
#region Field
    private Dictionary<string, ReferenceCollection> referenceCollections;
#endregion

#region 生命周期
    private void Awake()
    {
        referenceCollections = new Dictionary<string, ReferenceCollection>();

    }

    protected override void OnDestroyHandler()
    {
        ClearAll();
    }
#endregion

#region 接口
    public T Acquire<T>() where T : class, IReference, new()
    {
        return GetReferenceCollection(typeof(T).FullName).Acquire<T>();
    }

    public void Release<T>(T reference) where T : class, IReference
    {
        GetReferenceCollection(typeof(T).FullName).Release(reference);
    }

    public  void RemoveAll<T>() where T : class, IReference
    {
        GetReferenceCollection(typeof(T).FullName).RemoveAll();
    }
    
    public  void ClearAll()
    {
        lock (referenceCollections)
        {
            foreach (KeyValuePair<string, ReferenceCollection> referenceCollection in referenceCollections)
                referenceCollection.Value.RemoveAll();
            referenceCollections.Clear();
        }
    }
#endregion

#region 工具方法
    /// 获取引用集合，实际上获取引用的方法
    private  ReferenceCollection GetReferenceCollection(string fullName)
    {
        ReferenceCollection referenceCollection = null;
        lock (referenceCollections)
        {
            if (!referenceCollections.TryGetValue(fullName, out referenceCollection))
            {
            #if UNITY_EDITOR
                var go = new GameObject(fullName);
                go.AddComponent<ReferenceDebug>();
                go.transform.SetParent(transform, false);
            #endif
                referenceCollection = new ReferenceCollection();
                referenceCollections.Add(fullName, referenceCollection);
            }
        }
        return referenceCollection;
    }
#endregion
}
