using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(AssetModuleConfig))]
public class AssetModuleConfigInspector : Editor
{
#region Field
    private AssetModuleConfig script;

    private SerializedProperty resourceMode;
    private SerializedProperty aliveTime;
    private SerializedProperty asyncTimeLimit;

    private SerializedProperty buildXML;
    private SerializedProperty configName;
    
    private SerializedProperty targetPath;
    private SerializedProperty prefabs;
    private SerializedProperty assets;

    private ReorderableList prefabList;
    private ReorderableList assetList;
    
    // 所有的文件夹AB包，key是包名，value是路径
    private Dictionary<string, string> folderBundles = new Dictionary<string, string>();
    // 所有单独打包的AB包
    private Dictionary<string, List<string>> prefabBundles = new Dictionary<string, List<string>>();
    // 过滤
    private List<string> filter = new List<string>();
    // XML的过滤
    private List<string> xmlFilter = new List<string>();
#endregion

#region 生命周期
    private void OnEnable()
    {
        script = target as AssetModuleConfig;
        
        resourceMode = serializedObject.FindProperty(nameof(AssetModuleConfig.resourceMode));
        aliveTime = serializedObject.FindProperty(nameof(AssetModuleConfig.aliveTime));
        asyncTimeLimit = serializedObject.FindProperty(nameof(AssetModuleConfig.asyncTimeLimit));
        buildXML = serializedObject.FindProperty(nameof(AssetModuleConfig.buildXML));
        configName = serializedObject.FindProperty(nameof(AssetModuleConfig.configName));
        targetPath = serializedObject.FindProperty(nameof(AssetModuleConfig.targetPath));
        prefabs = serializedObject.FindProperty(nameof(AssetModuleConfig.prefabList));
        assets = serializedObject.FindProperty(nameof(AssetModuleConfig.assetList));

        prefabList = new ReorderableList(serializedObject, prefabs);
        assetList = new ReorderableList(serializedObject, assets);

        RegisterList(prefabList,prefabs,"以下文件夹内的每一个Prefab都会被打成一个AB包");
        RegisterList(assetList,assets,"以下文件夹会被打成一个AB包");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Draw();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        prefabList.DoLayoutList();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        assetList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();

        Build();
    }

#endregion

#region 面板
    private void RegisterList(ReorderableList list, SerializedProperty property,string title)
    {
        list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect,title);
        };

        list.drawElementCallback = (rect, index, active, focused) =>
        {
            var item = property.GetArrayElementAtIndex(index);

            var folderRect = new Rect(rect)
            {
                y = rect.y + 2.5f,
                width = (rect.width - 5f) / 4f,
                height = rect.height - 5,
            };
            var folder = EditorGUI.ObjectField(folderRect, AssetDatabase.LoadAssetAtPath<DefaultAsset>(item.stringValue), typeof(DefaultAsset), true);
            item.stringValue = AssetDatabase.GetAssetPath(folder);

            var pathRect = new Rect(rect)
            {
                y = rect.y + 2.5f,
                x = rect.x + folderRect.width + 5,
                width = (rect.width - 5) / 4f * 3f,
                height = rect.height-5
            };
            GUI.enabled = false;
            EditorGUI.TextField(pathRect,item.stringValue);
            GUI.enabled = true;
        };
    }

    private void Draw()
    {
        // AssetBundle的保存路径
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = false;
        EditorGUIUtility.labelWidth = 55f;
        EditorGUILayout.TextField("保存路径",targetPath.stringValue);
        GUI.enabled = true;
        if (GUILayout.Button("...", GUILayout.Width(100))) 
        {
            var tempPath = EditorUtility.OpenFolderPanel("保存路径", targetPath.stringValue, "");
            // 只能选择StreamingAsset下的目录
            var streamingAssetsPath = Application.streamingAssetsPath;
            if (!tempPath.Contains(streamingAssetsPath))
            {
                Debug.LogError("保存路径必须位于项目路径以下");
                targetPath.stringValue = "";
                return;
            }

            var index = tempPath.IndexOf("Assets");
            targetPath.stringValue = tempPath.Substring(index);
        }
        EditorGUILayout.EndHorizontal();

        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        // 真机模式
        resourceMode.enumValueIndex = EditorGUILayout.EnumPopup("资源模式", (ResourceMode)resourceMode.enumValueIndex).GetHashCode();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        // 存活时间
        EditorGUIUtility.labelWidth = 100f;
        aliveTime.floatValue = EditorGUILayout.Slider("资源存活时间(秒)", aliveTime.floatValue, 0, 600);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        // 异步加载限时
        EditorGUIUtility.labelWidth = 105f;
        asyncTimeLimit.intValue = (int)EditorGUILayout.Slider("异步加载限时(毫秒)", asyncTimeLimit.intValue, 0, 10000);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        // 是否生成XML，以及配置文件的名称
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 80f;
        buildXML.boolValue = EditorGUILayout.Toggle("生成XML文件", buildXML.boolValue, GUILayout.Width(120f));
        configName.stringValue = EditorGUILayout.TextField("配置文件名称", configName.stringValue);
        EditorGUILayout.LabelField(".bytes",GUILayout.Width(40f));
        EditorGUILayout.EndHorizontal();
    }
#endregion

#region 打包
    

    private void Build()
    {
        if (GUILayout.Button("打包", GUILayout.Height(40)))
        {
            Clear();
            folderBundles.Clear();
            prefabBundles.Clear();
            filter.Clear();
            xmlFilter.Clear();
            
            // 所有资源
            for (int i = 0; i < script.assetList.Count; i++)
            {
                var path = script.assetList[i];
                var bundleName = path.Substring(path.LastIndexOf("/")+1);
                if (!folderBundles.ContainsKey(bundleName))
                {
                    folderBundles.Add(bundleName, path);
                    filter.Add(path);
                    xmlFilter.Add(path);
                }
                else
                {
                    throw new Exception($"重复的文件夹：{bundleName}");
                }
            }
            // 所有单独打包的Prefab
            var allFolderAssetGUID = AssetDatabase.FindAssets("t:prefab", script.prefabList.ToArray());
            for (int i = 0; i < allFolderAssetGUID.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(allFolderAssetGUID[i]);
                var bundleName = path.Substring(path.LastIndexOf("/") + 1);
                bundleName = bundleName.Substring(0, bundleName.Length - 7);
                xmlFilter.Add(path);
                
                var depend = AssetDatabase.GetDependencies(path);
                var tempDepend = new List<string>();
                for (int j = 0; j < depend.Length; j++)
                {
                    if (!ContainAsset(depend[j]) && !depend[j].EndsWith(".cs"))
                    {
                        filter.Add(depend[j]);
                        tempDepend.Add(depend[j]);
                    }
                }
                if (!prefabBundles.ContainsKey(bundleName)) 
                    prefabBundles.Add(bundleName,tempDepend);
                else
                    throw new Exception($"重复的Prefab：{bundleName}");
            }
            
            // 设置Bundle
            foreach (var bundle in folderBundles)
                SetBundle(bundle.Key,bundle.Value);
            foreach (var bundle in prefabBundles)
                SetBundle(bundle.Key,bundle.Value);

            BuildConfig();
            BuildBundle();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void BuildConfig()
    {
        // 所有的BundleName
        var bundleNames = AssetDatabase.GetAllAssetBundleNames();
        // 资源路径：资源所属Bundle
        var assetInBundle = new Dictionary<string, string>();
        for (int i = 0; i < bundleNames.Length; i++)
        {
            // 某一个Bundle下所有的资源路径
            var assetPath = AssetDatabase.GetAssetPathsFromAssetBundle(bundleNames[i]);
            for (int j = 0; j < assetPath.Length; j++)
            {
                if (assetPath[j].EndsWith(".cs") || !IsValidPath(assetPath[j])) 
                    continue;
                assetInBundle.Add(assetPath[j],bundleNames[i]);
            }
        }
        
        AssetBundleConfig config = new AssetBundleConfig();
        config.bundleList = new List<AssetConfig>();
        foreach (var assetInfo in assetInBundle)
        {
            var asset = new AssetConfig();
            asset.crc = CRC32.GetCRC32(assetInfo.Key);
            asset.bundleName = assetInfo.Value;
            asset.assetName = assetInfo.Key.Remove(0, assetInfo.Key.LastIndexOf("/", StringComparison.Ordinal) + 1);
            asset.dependence = new List<string>();

            var tempDep = AssetDatabase.GetDependencies(assetInfo.Key);
            for (int i = 0; i < tempDep.Length; i++)
            {
                if (tempDep[i] == assetInfo.Key || tempDep[i].EndsWith(".cs")) 
                    continue;
                if (assetInBundle.TryGetValue(tempDep[i],out var depBundle))
                {
                    if (depBundle == assetInfo.Value)
                        continue;
                    if (!asset.dependence.Contains(depBundle))
                        asset.dependence.Add(depBundle);
                }
            }
            config.bundleList.Add(asset);
        }

        if (Directory.Exists(script.configPath))
            Directory.CreateDirectory(script.configPath);

        // XML
        var xmlPath = $"{script.configPath}/{script.configName}XML.xml";
        if (File.Exists(xmlPath)) 
            File.Delete(xmlPath);
        if (buildXML.boolValue) 
        {
            using (var stream = new FileStream(xmlPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    var xs = new XmlSerializer(typeof(AssetBundleConfig));
                    xs.Serialize(writer, config);
                }
            }
        }
        // bytes
        var bytesPath = $"{script.configPath}/{script.configName}.bytes";
        if (File.Exists(bytesPath)) 
            File.Delete(bytesPath);
        using (var stream = new FileStream(bytesPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            var bf = new BinaryFormatter();
            bf.Serialize(stream, config);
        }
    }

    private void BuildBundle()
    {
        if (!Directory.Exists(script.targetPath))
            Directory.CreateDirectory(script.targetPath);
        
        BuildPipeline.BuildAssetBundles(script.targetPath, BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void SetBundle(string name, string path)
    {
        var asset = AssetImporter.GetAtPath(path);
        if (asset == null)
            throw new Exception($"不存在{path}");
        asset.assetBundleName = name;
    }

    private void SetBundle(string name, List<string> paths)
    {
        for (int i = 0; i < paths.Count; i++)
            SetBundle(name,paths[i]);
    }
    
    private bool ContainAsset(string path)
    {
        for (int i = 0; i < filter.Count; i++)
        {
            if (path == filter[i] || (path.Contains(filter[i]) && path.Replace(filter[i], "")[0] == '/')) 
                return true;
        }
        return false;
    }

    private bool IsValidPath(string path)
    {
        for (int i = 0; i < xmlFilter.Count; i++)
        {
            if (path.Contains(xmlFilter[i]))
                return true;
        }

        return false;
    }

    private void Clear()
    {
        var bundles = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < bundles.Length; i++)
            AssetDatabase.RemoveAssetBundleName(bundles[i], true);
    }
#endregion
}

