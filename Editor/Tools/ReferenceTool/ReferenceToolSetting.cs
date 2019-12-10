internal class ReferenceToolSetting
{
    internal static string ripgrepPath;
    internal static string assetChangeLogPath => UToolsSetting.dataPath + "/assetChangeLogPath.txt";
    internal static string guidMapPath => UToolsSetting.dataPath + "/guidMap.json";

    internal static void Initialize()
    {
#if UNITY_EDITOR_OSX
        ripgrepPath = "/usr/local/bin/rg";
#elif UNITY_EDITOR
        ripgrepPath = UToolsSetting.packagePath + "/Editor/Tools/ReferenceTool/Deps/rg.exe";
#endif
    }
}