using System.Collections.Generic;
using UnityEngine;

public class ReferenceCollection
{
#region Field
    private Queue<IReference> references;

#if UNITY_EDITOR
    private Transform transform;
#endif
#endregion


#region 生命周期
    public ReferenceCollection()
    {
        references = new Queue<IReference>();
    #if UNITY_EDITOR
        transform = GameObject.Find("ReferenceManager").transform;
    #endif
    }
#endregion

#region 接口
    public T Acquire<T>() where T : class, IReference, new()
    {
    #if UNITY_EDITOR
        var debug = transform.Find(typeof(T).FullName).GetComponent<ReferenceDebug>();
    #endif
        lock (references)
        {
            if (references.Count > 0)
            {
            #if UNITY_EDITOR
                debug.waitForUse = references.Count - 1;
            #endif
                return references.Dequeue() as T;
            }
        }
    #if UNITY_EDITOR
        debug.totalAcquire++;
    #endif
        return new T();
    }

    public void Release<T>(T reference) where T : class,IReference
    {
    #if UNITY_EDITOR
        transform.Find(typeof(T).FullName).GetComponent<ReferenceDebug>().waitForUse++;
    #endif
        reference.Clear();
        lock (references)
        {
            references.Enqueue(reference);
        }
    }
    
    public void RemoveAll()
    {
        lock (references)
        {
            references.Clear();
        }
    }
#endregion
}