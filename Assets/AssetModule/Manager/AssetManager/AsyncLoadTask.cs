using System;
using Object = UnityEngine.Object;

public class AsyncLoadTask: IReference
{
    public string path;
    public uint crc;
    public Action<Object> callback;
    
    public void Clear()
    {
        path = "";
        crc = 0;
        callback = null;
    }
}
