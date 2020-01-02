using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UTools.Utility;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class UToolsUtil
{
    internal static string DataPath;

    static UToolsUtil()
    {
        platform = Application.platform;
        DataPath = Application.dataPath;
    }

    internal static string GetTransformTreePath(Transform t, Transform root = null)
    {
        var sb = new StringBuilder();
        while (true)
        {
            sb.Insert(0, t.name);

            if (root)
            {
                if (t == root)
                {
                    break;
                }
            }
            else
            {
                if (t.parent == null)
                {
                    break;
                }
            }

            sb.Insert(0, "/");

            t = t.parent;
        }

        return sb.ToString();
    }

    internal static string AssetAbsolutePath(string pathName)
    {
        var index = pathName.IndexOf("Assets/", StringComparison.Ordinal);
        if (index >= 0)
        {
            pathName = pathName.Substring(6);
        }

        return DataPath + pathName;
    }

    internal static string AssetRelativePath(string pathName)
    {
        const string forwardSlash = "/";
        const string backSlash = "\\";
        pathName = pathName.Replace(backSlash, forwardSlash);
        return pathName.Replace(DataPath, "Assets");
    }

    internal static RuntimePlatform platform;

    internal static bool IsMac => platform == RuntimePlatform.OSXEditor;

    private static List<Type> types;

    internal static Type GetBuiltinClassType(string name)
    {
        if (types == null)
        {
            types = new List<Type>();
            types.AddRange(Assembly.GetAssembly(typeof(UnityEngine.Object)).GetTypes());
            types.AddRange(Assembly.GetAssembly(typeof(UnityEngine.UI.Image)).GetTypes());
            types.AddRange(Assembly.GetAssembly(typeof(Editor)).GetTypes());
        }

        return types.Find(v => v.FullName == name || v.Name == name);
    }

    internal static string GetAssetDir(UnityEngine.Object asset)
    {
        var tmsPath = AssetDatabase.GetAssetPath(asset);
        var dir = Path.GetDirectoryName(tmsPath);
        return dir;
    }

    internal static T FindAsset<T>(
        string filter,
        string relativePath,
        bool recursive
    )
        where T : UnityEngine.Object
    {
        var paths = FindAssetPath(filter, relativePath, recursive, false);

        foreach (var path in paths)
        {
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (obj is T o)
            {
                return o;
            }
        }

        return default;
    }

    internal static List<string> FindAllAssetPath(
        string filter,
        string relativePath,
        bool recursive
    ) =>
        FindAssetPath(filter, relativePath, recursive, false);

    private static List<string> FindAssetPath(
        string filter,
        string relativePath,
        bool recursive,
        bool firstReturn
    )
    {
        var results = new List<string>();

        var guids = AssetDatabase.FindAssets(filter, new[] {relativePath});

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (recursive)
            {
                results.Add(path);
                if (firstReturn)
                {
                    return results;
                }
            }
            else
            {
                var itemDir = Path.GetDirectoryName(path);
                if (itemDir == relativePath)
                {
                    results.Add(path);
                    if (firstReturn)
                    {
                        return results;
                    }
                }
            }
        }

        return results;
    }

    internal static void RecursiveObjs(Action<GameObject> action, params GameObject[] objs)
    {
        for (var i = 0; i < objs.Length; i++)
        {
            if (objs[i])
            {
                RecursiveObj(action, objs[i]);
            }
        }
    }

    internal static void RecursiveObj(Action<GameObject> action, GameObject obj)
    {
        action?.Invoke(obj);
        if (obj)
        {
            for (var i = 0; i < obj.transform.childCount; i++)
            {
                RecursiveObj(action, obj.transform.GetChild(i).gameObject);
            }
        }
    }

    internal static void HighlightProjectPath(string path)
    {
        var obj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
        if (obj == null)
        {
            Debug.LogError("path not exist");
            return;
        }

        var insId = obj.GetInstanceID();
        var result = ReflectionUtil.CallStatic("UnityEditor.ProjectBrowser", "GetAllProjectBrowsers") as IList;
        foreach (var pb in result)
        {
            var viewMode = pb.GetMemberValue("m_ViewMode").ToString();
            if (viewMode == "OneColumn")
            {
                var tree = pb.GetMemberValue("m_AssetTree");
                tree.Call("SetSelection", new int[] {insId}, true);
                tree.Call("Frame", insId, false, true);
                EditorApplication.RepaintProjectWindow();
            }
            else if (viewMode == "TwoColumns")
            {
                var tree = pb.GetMemberValue("m_FolderTree");
                tree.Call("Frame", insId, false, true);
                pb.Call("ShowFolderContents", insId, false);
            }
        }
    }

    internal static bool IsPrefabInstance(Object asset)
    {
        var prefabType = PrefabUtility.GetPrefabAssetType(asset);
        var status = PrefabUtility.GetPrefabInstanceStatus(asset);

        if (prefabType != PrefabAssetType.NotAPrefab &&
            status != PrefabInstanceStatus.NotAPrefab)
        {
            return true;
        }

        return false;
    }

    internal static void EnsureFileExist(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(path))
        {
            var fs = File.Create(path);
            fs.Close();
        }
    }

    internal static T CreateAssetInSelectionPath<T>(string name) where T : ScriptableObject
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path.IsNOE())
        {
            path = "Assets";
        }
        else
        {
            if (Path.GetExtension(path) != "")
            {
                path = Path.GetDirectoryName(path);
            }
        }

        path = $"{path}/{name}.asset";

        return CreateAsset<T>(path);
    }

    internal static T CreateAsset<T>(string path) where T : ScriptableObject
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        return asset;
    }

    internal static (string guid, long fileID) GetAssetGUIDAndFileID(Object asset)
    {
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
            asset,
            out var dllGUID,
            out long fileId
        ))
        {
            return (dllGUID, fileId);
        }

        return (null, 0);
    }

    internal static Object GetAssetByGUID(string guid, Type type) =>
        AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type);

    internal static T GetAssetByGUID<T>(string guid) where T : Object => (T) GetAssetByGUID(guid, typeof(T));

    internal static (string, long, Object) GetGuidAndFileIdByType(Type type)
    {
        var dlllocation = type.Assembly.Location;
        var assets = AssetDatabase.LoadAllAssetsAtPath(dlllocation);
        foreach (var v in assets)
        {
            if (v.name == type.Name)
            {
                var (guid, fileID) = GetAssetGUIDAndFileID(v);
                return (guid, fileID, v);
            }
        }

        return (null, 0, null);
    }

    internal static string GetAssetGUID(Object obj) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));

    internal static List<string> FindAssetsPath(string filter, string path, bool recursive = true)
    {
        var results = new List<string>();
        var guids = AssetDatabase.FindAssets(filter, new[] {path});
        foreach (var guid in guids)
        {
            var vPath = AssetDatabase.GUIDToAssetPath(guid);
            if (recursive)
            {
                results.Add(vPath);
            }
            else
            {
                var itemDir = Path.GetDirectoryName(vPath);
                if (itemDir == path)
                {
                    results.Add(vPath);
                }
            }
        }

        return results;
    }

    internal static T FindScriptableObject<T>(string name, string relativePath, bool recursive) where T : ScriptableObject
    {
        var paths = FindAssetsPath(name, relativePath, recursive);

        foreach (var path in paths)
        {
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (obj is T o)
            {
                return o;
            }
        }

        return default;
    }
}