using System;
using System.Collections.Generic;

[Serializable]
public class AssetConfig
{
    public uint crc;
    public string bundleName;
    public string assetName;
    public List<string> dependence;
}

[Serializable]
public class AssetBundleConfig
{
    public List<AssetConfig> bundleList;
}
