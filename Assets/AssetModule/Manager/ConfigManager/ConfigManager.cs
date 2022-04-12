using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class ConfigManager
{
    /// 打包配置文件，存放了Bundle配置文件的名称，以及打包后的Bundle路径
    public static AssetModuleConfig assetModuleConfig { get;private set;}

    /// Bundle配置文件，存放了Bundle的所有信息
    public static AssetBundleConfig assetBundleConfig { get; private set; }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        assetModuleConfig = Resources.Load<AssetModuleConfig>(AssetModuleConfig.buildConfigName);
        if (assetModuleConfig == null)
        {
            Debug.LogError($"Resources目录下不存在：{AssetModuleConfig.buildConfigName}");
            return;
        }

        var configBytes = Resources.Load<TextAsset>($"{assetModuleConfig.configName}");
        if (configBytes == null)
        {
            Debug.LogError($"加载AssetBundle配置文件失败：{assetModuleConfig.configName}");
            return;
        }

        using var stream = new MemoryStream(configBytes.bytes);
        var bf = new BinaryFormatter();
        assetBundleConfig = bf.Deserialize(stream) as AssetBundleConfig;

        if (configBytes == null)
            Debug.LogError($"反序列化失败");
    }
    
    
    /// <summary>
    /// 获取资源信息
    /// </summary>
    /// <param name="crc32">资源名称对应的CRC</param>
    /// <param name="config">资源配置表</param>
    /// <returns>是否成功</returns>
    public static bool TryGetAssetConfig(uint crc32, out AssetConfig config)
    {
        for (int i = 0; i < ConfigManager.assetBundleConfig.bundleList.Count; i++)
        {
            config = ConfigManager.assetBundleConfig.bundleList[i];
            if (config.crc == crc32)
                return true;
        }
        
        config = null;
        return false;
    }
}


