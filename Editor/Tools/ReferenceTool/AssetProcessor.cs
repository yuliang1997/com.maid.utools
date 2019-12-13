using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace UTools
{
//由于GUIDMap会非常大，所以保存会很慢，这里只记录资源修改的历史
    internal class AssetProcessor : AssetPostprocessor
    {
        private static void AppendChangeLog(string prefix, IEnumerable<string> logs)
        {
            var path = ReferenceToolSetting.assetChangeLogPath;
            UToolsUtil.EnsureFileExist(path);
            var fs = new FileStream(path, FileMode.Append);
            var writer = new StreamWriter(fs);
            foreach (var assetPath in logs)
            {
                var assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
                writer.WriteLine($"{prefix},{assetGUID},{assetPath}");
            }

            writer.Close();
            fs.Close();
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            var updateList = importedAssets.ToList();
            updateList.RemoveAll(RemovePredicate);

            var removeList = deletedAssets.ToList();
            removeList.RemoveAll(RemovePredicate);

            AppendChangeLog("update", updateList);
            AppendChangeLog("remove", removeList);
        }

        private static bool RemovePredicate(string v)
        {
            if (v.Contains("DELETED_GUID_Trash"))
            {
                return true;
            }

            if (!v.StartsWith("Assets"))
            {
                return true;
            }

            var ex = Path.GetExtension(v).ToLower();

            return !ReferenceToolUtil.guidMapHandleExs.Contains(ex);
        }
    }
}