using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UTools.Utility;

namespace UTools
{
    /// <summary>
    /// 一个基于目录的资源导入自动设置的组件
    /// 现在支持自动设置贴图格式，自动设置AssetBundle Name
    /// </summary>
    public partial class AssetImportProcessor : AssetPostprocessor
    {
        private static ImportToolSetting its => ImportToolSetting.inst;

        /// <summary>
        /// 对AssetImportSetting的一个缓存，导入相同目录贴图的时候省去递归查找Setting的过程
        /// todo 考虑维护一个AssetImportSetting的索引列表
        /// </summary>
        private static Dictionary<string, AssetImportSetting> settingsCache = new Dictionary<string, AssetImportSetting>();

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            if (its.reimportMovedAsset)
            {
                foreach (var v in movedAssets)
                {
                    AssetDatabase.ImportAsset(v);
                }
            }
        }

        private void OnPreprocessAsset()
        {
            if (its.enableBundleNameControl)
            {
                if (its.removeUnusedBundleNameWhenImport)
                {
                    AssetDatabase.RemoveUnusedAssetBundleNames();
                }

                if (assetPath.StartsWith(its.bundleRoot))
                {
                    handleBundle();
                }
            }
        }

        private void OnPreprocessTexture()
        {
            if (its.enableTexureFormatControl)
            {
                handleTexFormat();
            }
        }

        private AssetImportSetting findImportSetting(string dirPath)
        {
            if (settingsCache.ContainsKey(dirPath))
            {
                var setting = settingsCache[dirPath];
                if (!setting)
                {
                    settingsCache.Remove(dirPath);
                }
                else
                {
                    var settingPath = AssetDatabase.GetAssetPath(setting);
                    var settingDir = Path.GetDirectoryName(settingPath);
                    if (settingDir != dirPath)
                    {
                        //说明缓存的设置已经被挪位置了
                        settingsCache.Remove(dirPath);
                        settingsCache[settingDir] = setting;
                    }
                    else
                    {
                        return setting;
                    }
                }
            }

            //t:AssetImportSetting 有时候会出现找不到的情况。。。所以直接用名字，只要保证名字不改就行
            var asset = UToolsUtil.FindScriptableObject<AssetImportSetting>("AssetImportSetting", dirPath, false);

            if (asset != null)
            {
                settingsCache[dirPath] = asset;
            }

            return asset;
        }
    }

    /// <summary>
    /// 根据AssetImportSetting自动设置贴图格式
    /// 从被导入的贴图所在的目录开始，向上递归找AssetImportSetting，找到为止
    /// </summary>
    public partial class AssetImportProcessor
    {
        private void handleTexFormat()
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            var (controller, settingDir) = findTexImportControl(assetPath);
            if (controller == null)
            {
                return;
            }

            importer.textureType = controller.textureType;

            setPackingTag(importer, controller.atlasMode, controller.packingTag, assetPath, settingDir);

            if (importer.textureType == TextureImporterType.Sprite)
            {
                if (importer.spriteImportMode != SpriteImportMode.Single && importer.spriteImportMode != SpriteImportMode.Multiple)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                }
            }

            importer.npotScale = controller.nPOTScale;
            importer.alphaIsTransparency = controller.alphaIsTransparency;
            importer.mipmapEnabled = controller.mipMapEnable;
            importer.isReadable = controller.readWriteEnable;
            importer.sRGBTexture = controller.sRGB;
            importer.wrapMode = controller.wrapMode;
            importer.filterMode = controller.filterMode;
            importer.textureShape = controller.textureShape;

            TextureImporterFormat iosFormat = 0, androidFormat = 0, pcFormat = 0;

            if (controller.alphaMode != AlphaMode.Auto)
            {
                iosFormat = controller.overrideIOSFormat;
                androidFormat = controller.overrideAndroidFormat;
                pcFormat = controller.overridePCFormat;
                importer.alphaIsTransparency = controller.alphaIsTransparency && controller.alphaMode == AlphaMode.Alpha;
            }
            else
            {
                var hasAlpha = importer.DoesSourceTextureHaveAlpha();
                importer.alphaIsTransparency = controller.alphaIsTransparency && hasAlpha;
                iosFormat = TextureToolUtil.GetFormat(RuntimePlatform.IPhonePlayer, hasAlpha);
                androidFormat = TextureToolUtil.GetFormat(RuntimePlatform.Android, hasAlpha);
                pcFormat = TextureToolUtil.GetFormat(RuntimePlatform.WindowsPlayer, hasAlpha);
            }

            var iphoneSettings = CreatePlatformSettings("iPhone", iosFormat, controller.maxSize, controller.quality);
            var androidSettings = CreatePlatformSettings("Android", androidFormat, controller.maxSize, controller.quality);
            var pcSettings = CreatePlatformSettings("Standalone", pcFormat, controller.maxSize, controller.quality);

            if (controller.maxSize == 0)
            {
                iphoneSettings.maxTextureSize = importer.GetPlatformTextureSettings("iPhone").maxTextureSize;
                androidSettings.maxTextureSize = importer.GetPlatformTextureSettings("Android").maxTextureSize;
                pcSettings.maxTextureSize = importer.GetPlatformTextureSettings("Standalone").maxTextureSize;
            }

            importer.SetPlatformTextureSettings(iphoneSettings);
            importer.SetPlatformTextureSettings(androidSettings);
            importer.SetPlatformTextureSettings(pcSettings);
        }

        /// <summary>
        /// 创建对应平台的设置信息
        /// </summary>
        private TextureImporterPlatformSettings CreatePlatformSettings(string name, TextureImporterFormat format, int maxSize, QualityMode quality)
        {
            return new TextureImporterPlatformSettings
            {
                overridden = true,
                name = name,
                format = format,
                maxTextureSize = maxSize,
                compressionQuality = (int) quality
            };
        }

        /// <summary>
        /// 递归查找符合条件的TexFormatController
        /// 先查找有条件限制的Controller
        /// </summary>
        /// <returns>TexFormatController, setting所在的目录</returns>
        private (TexFormatController, string) findTexImportControl(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);

            var startDir = dir;
            while (true)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    return (null, null);
                }

                var setting = findImportSetting(dir);
                if (setting)
                {
                    if (setting.texFormatControllers.Count > 0)
                    {
                        var tempList = setting.texFormatControllers.ToList();
                        //保持原来的顺序将无条件的Controller放到最后
                        var controllers = new List<TexFormatController>();
                        foreach (var c in tempList)
                        {
                            if (c.conditionMethod != ConditionMethod.None)
                            {
                                controllers.Add(c);
                            }
                        }

                        foreach (var c in tempList)
                        {
                            if (c.conditionMethod == ConditionMethod.None)
                            {
                                controllers.Add(c);
                            }
                        }

                        foreach (var controller in controllers)
                        {
                            if (!controller.enable)
                            {
                                continue;
                            }

                            if (checkCondition(controller, assetPath))
                            {
                                if (dir == startDir || controller.recursive)
                                {
                                    return (controller, dir);
                                }
                            }
                        }
                    }
                }

                dir = Path.GetDirectoryName(dir);
            }
        }

        /// <summary>
        /// 检查Controller的条件是否满足
        /// </summary>
        internal static bool checkCondition(TexFormatController controller, string assetPath)
        {
            if (controller.conditionMethod == ConditionMethod.None)
            {
                return true;
            }

            string matchStr = string.Empty;
            switch (controller.conditionContent)
            {
                case ConditionContent.FileName:
                    matchStr = Path.GetFileName(assetPath);
                    break;
                case ConditionContent.Path:
                    matchStr = assetPath;
                    break;
                case ConditionContent.ParentFolderName:
                    matchStr = Path.GetFileName(Path.GetDirectoryName(assetPath));
                    break;
            }

            var conditionText = controller.conditionText;
            if (conditionText.IsNOE())
            {
                return false;
            }

            if (controller.ignoreCase)
            {
                conditionText = controller.conditionText.ToLower();
                matchStr = matchStr.ToLower();
            }

            switch (controller.conditionMethod)
            {
                case ConditionMethod.Regex:
                    return Regex.IsMatch(matchStr, conditionText);
                case ConditionMethod.Contain:
                    return matchStr.Contains(conditionText);
                case ConditionMethod.Equals:
                    return matchStr.Equals(conditionText);
                case ConditionMethod.StartWith:
                    return matchStr.StartsWith(conditionText);
                case ConditionMethod.EndWith:
                    return matchStr.EndsWith(conditionText);
            }

            return false;
        }

        /// <summary>
        /// 给Sprite设置PackingTag
        /// </summary>
        private static void setPackingTag(
            TextureImporter importer,
            AtlasMode mode,
            string packingTag,
            string assetPath,
            string settingDir
        )
        {
            if (importer.textureType != TextureImporterType.Sprite || mode == AtlasMode.None)
            {
                importer.spritePackingTag = "";
                return;
            }

            switch (mode)
            {
                case AtlasMode.RelativeFolder:
                {
                    var relativePath = assetPath.Substring(settingDir.Length);
                    if (relativePath.StartsWith("/"))
                    {
                        relativePath = relativePath.Substring(1);
                    }

                    var index = relativePath.IndexOf("/", StringComparison.Ordinal);
                    if (index == -1)
                    {
                        packingTag = Path.GetFileName(settingDir);
                    }
                    else
                    {
                        packingTag = relativePath.Substring(0, index);
                    }

                    break;
                }

                case AtlasMode.ParentFolder:
                {
                    var dirPath = Path.GetDirectoryName(assetPath);
                    var dirName = Path.GetFileName(dirPath);
                    packingTag = dirName;
                    break;
                }

                case AtlasMode.CurrentFolder:
                    packingTag = Path.GetFileName(settingDir);
                    break;
            }

            importer.spritePackingTag = packingTag;
        }
    }

    /// <summary>
    /// 根据ImportToolSetting自动设置BundleName
    /// 场景会按照场景资源的文件名单独设置一个bundleName
    /// 其他按照路径设置bundleName
    /// </summary>
    public partial class AssetImportProcessor
    {
        private void handleBundle()
        {
            var importer = AssetImporter.GetAtPath(assetPath);
            var path = assetPath.Replace($"{its.bundleRoot}/", "");
            if (path.EndsWith(".unity"))
            {
                // scene files.
                path = path.Replace(".unity", "");
                importer.assetBundleName = path;
            }
            else
            {
                // normal asset.
                int bundleNameStart = path.IndexOf(its.bundlePrefix, StringComparison.Ordinal);
                if (bundleNameStart == -1)
                    return;
                var bundlePath = path.Substring(0, bundleNameStart);
                bundleNameStart += its.bundlePrefix.Length;
                path = path.Substring(bundleNameStart);
                int bundleNameLength = path.IndexOf('/');
                if (bundleNameLength == -1)
                    return;
                var bundleName = path.Substring(0, bundleNameLength);

                importer.assetBundleName = bundlePath + bundleName;
            }
        }
    }
}