using UnityEditor;
using UnityEngine;

public class ImportToolSetting : ScriptableObject
{
    //是否重新导入移动的资源
    public bool reimportMovedAsset = true;

    //Bundles//////////////

    public bool enableBundleNameControl = true;
    public bool enableTexureFormatControl = true;

    public bool removeUnusedBundleNameWhenImport = true;

    public string bundleRoot = "Assets/AssetBundles";
    public string bundlePrefix = "ab_";

    public static string assetPath = "Assets/Plugins/Editor/UTools/ImportToolSetting.asset";

    private static ImportToolSetting _inst;

    public static ImportToolSetting inst
    {
        get
        {
            if (!_inst)
            {
                _inst = AssetDatabase.LoadAssetAtPath<ImportToolSetting>(assetPath);
                if (!_inst)
                {
                    create();
                }
            }

            return _inst;
        }
    }

    public static void create()
    {
        var temp = inst;
    }

    ///////////////////////
}