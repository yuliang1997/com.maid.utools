namespace UTools
{
    internal class ReferenceToolSetting
    {
        internal static string assetChangeLogPath => UToolsSetting.dataPath + "/assetChangeLogPath.txt";
        internal static string guidMapPath => UToolsSetting.dataPath + "/guidMap.json";

        internal static void Initialize()
        {
        }
    }
}