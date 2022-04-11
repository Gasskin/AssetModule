using System;
using Object = UnityEngine.Object;

public class AsyncLoadTask<T>: IReference
{
    public uint crc;
    public Action<Object> callback;
    
    public void Clear()
    {
        crc = 0;
        callback = null;
    }
}
