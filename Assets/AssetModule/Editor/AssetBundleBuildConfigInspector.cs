using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(AssetBundleBuildConfig))]
public class AssetBundleBuildConfigInspector : Editor
{
#region Field
    private AssetBundleBuildConfig script;
    
    private SerializedProperty targetPath;
    private SerializedProperty prefabs;
    private SerializedProperty assets;

    private ReorderableList prefabList;
    private ReorderableList assetList;
#endregion

#region 生命周期
    private void OnEnable()
    {
        script = target as AssetBundleBuildConfig;

        targetPath = serializedObject.FindProperty(nameof(AssetBundleBuildConfig.targetPath));
        prefabs = serializedObject.FindProperty(nameof(AssetBundleBuildConfig.prefabList));
        assets = serializedObject.FindProperty(nameof(AssetBundleBuildConfig.assetList));

        prefabList = new ReorderableList(serializedObject, prefabs);
        assetList = new ReorderableList(serializedObject, assets);

        RegisterList(prefabList,prefabs,"以下文件夹内的每一个Prefab都会被打成一个AB包");
        RegisterList(assetList,assets,"以下文件夹会被打成一个AB包");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        TargetPath();
        
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

    private void TargetPath()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = false;
        EditorGUIUtility.labelWidth = 55f;
        EditorGUILayout.TextField("保存路径：",targetPath.stringValue);
        GUI.enabled = true;
        if (GUILayout.Button("保存路径", GUILayout.Width(100))) 
        {
            var tempPath = EditorUtility.OpenFolderPanel("保存路径", targetPath.stringValue, "");
            // 工程根目录
            var projectRoot = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/"));
            if (!tempPath.Contains(projectRoot))
            {
                Debug.LogError("保存路径必须位于项目路径以下");
                return;
            }

            targetPath.stringValue = tempPath.Substring(projectRoot.Length + 1);
        }
        
        EditorGUILayout.EndHorizontal();
    }
#endregion

#region 打包
    // 所有的文件夹AB包，key是包名，value是路径
    private Dictionary<string, string> folderBundles = new Dictionary<string, string>();
    // 所有单独打包的AB包
    private Dictionary<string, List<string>> prefabBundles = new Dictionary<string, List<string>>();
    // 过滤
    private List<string> filter = new List<string>();

    private void Build()
    {
        if (GUILayout.Button("打包", GUILayout.Height(40)))
        {
            Clear();
            folderBundles.Clear();
            prefabBundles.Clear();
            filter.Clear();
            
            // 所有资源
            for (int i = 0; i < script.assetList.Count; i++)
            {
                var path = script.assetList[i];
                var bundleName = path.Substring(path.LastIndexOf("/")+1);
                if (!folderBundles.ContainsKey(bundleName))
                {
                    folderBundles.Add(bundleName, path);
                    filter.Add(path);
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

            BuildBundle();
        }
    }

    private void BuildBundle()
    {
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
            if (path == filter[i] || path.Contains(filter[i]))
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


      