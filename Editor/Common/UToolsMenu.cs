using UnityEditor;
using UnityEngine;

namespace UTools
{
    internal static class UToolsMenu
    {
        [MenuItem("Window/UTools/" + nameof(BookmarkTool))]
        internal static void BookmarkTool() => openWindow<BookmarkTool>();

        [MenuItem("Window/UTools/" + nameof(FindStringTool))]
        internal static void FindStringTool() => openWindow<FindStringTool>();

        [MenuItem("Window/UTools/" + nameof(ReferenceTool))]
        internal static void ReferenceTool()
        {
            var win = openWindow<ReferenceTool>();
            win.minSize = new Vector2(400, 0);
        }

        [MenuItem("Assets/Create/UTools/TexImportSettings", priority = 1)]
        internal static void createTextureImporterSetting() =>
            UToolsUtil.CreateAssetInSelectionPath<TexImporterSetting>("TexImporterSetting");

        [MenuItem("Assets/Create/UTools/AssetImportSetting", priority = 1)]
        internal static void createImportSetting() =>
            UToolsUtil.CreateAssetInSelectionPath<AssetImportSetting>("AssetImportSetting");

        [MenuItem("Assets/Create/UTools/ImportToolSetting", priority = 1)]
        internal static void createImportToolSetting() => ImportToolSetting.ensureInstExist();

        private static T openWindow<T>() where T : EditorWindow
        {
            var win = EditorWindow.GetWindow<T>();
            win.Show();
            return win;
        }
    }
}