using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UTools.Utility;

/*
 * Documents:
 * https://docs.unity3d.com/2019.1/Documentation/ScriptReference/TextureImporter.html
 * If your app utilizes an unsupported texture compression,
 * the textures will be uncompressed to RGBA 32 and stored in memory along with the compressed ones.
 * So in this case you lose time decompressing textures and lose memory storing them twice.
 * It may also have a very negative impact on rendering performance.
 */

#if !DISABLE_TEXTOOL

internal class TexImportProcessor : AssetPostprocessor
{
    private static Dictionary<string, TexImporterSetting> settingsCache =
        new Dictionary<string, TexImporterSetting>();

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths
    )
    {
        foreach (var v in movedAssets)
        {
            var ex = Path.GetExtension(v);
            if (TextureToolUtil.IsTexture(ex))
                //https://docs.unity3d.com/2019.1/Documentation/ScriptReference/AssetPostprocessor.OnPostprocessTexture.html
                //如果在Postprocess事件里修改ImportSetting的话需要等待下次imported才会生效
                //所以在这儿import一下
                //todo 重命名的时候也会走这里，目前没什么好办法解决
            {
                AssetDatabase.ImportAsset(v);
            }
        }
    }

    private void OnPreprocessTexture()
    {
        ImportTexture(assetPath);
    }

    private static void ImportTexture(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        var (importImporterSetting, settingDir) = GetImporterSetting(assetPath);
        if (!importImporterSetting || !importer)
        {
            return;
        }

        var setting = GetSettings(importImporterSetting, assetPath);
        if (setting == null)
        {
            return;
        }

        importer.textureType = setting.TextureType;
        TrySetPackingTag(
            importer,
            setting.AtlasMode,
            setting.PackingTag,
            assetPath,
            settingDir
        );
        if (importer.textureType == TextureImporterType.Sprite)
        {
            if (importer.spriteImportMode != SpriteImportMode.Single &&
                importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }
        }

        importer.npotScale = setting.NPOTScale;
        importer.alphaIsTransparency = setting.AlphaIsTransparency;
        importer.mipmapEnabled = setting.MipMapEnable;
        importer.isReadable = setting.ReadWriteEnable;
        importer.sRGBTexture = setting.SRGB;
        importer.wrapMode = setting.WrapMode;
        importer.filterMode = setting.FilterMode;
        importer.textureShape = setting.TextureShape;

        TextureImporterFormat iosFormat = 0, androidFormat = 0, pcFormat = 0;

        if (setting.AlphaMode != TexImporterSetting.AlphaMode.Auto)
        {
            iosFormat = setting.OverrideIOSFormat;
            androidFormat = setting.OverrideAndroidFormat;
            pcFormat = setting.OverridePCFormat;
            importer.alphaIsTransparency =
                setting.AlphaIsTransparency &&
                setting.AlphaMode == TexImporterSetting.AlphaMode.Alpha;
            if ((int) iosFormat == 51)
            {
                iosFormat = TextureImporterFormat.ASTC_8x8;
            }
            else if ((int) iosFormat == 56)
            {
                iosFormat = TextureImporterFormat.ASTC_6x6;
            }
            else if ((int) iosFormat == 50)
            {
                iosFormat = TextureImporterFormat.ASTC_6x6;
            }
            else if ((int) iosFormat == 54)
            {
                iosFormat = TextureImporterFormat.ASTC_4x4;
            }
            else if ((int) iosFormat == 57)
            {
                iosFormat = TextureImporterFormat.ASTC_8x8;
            }
        }
        else
        {
            var hasAlpha = importer.DoesSourceTextureHaveAlpha();
            importer.alphaIsTransparency = setting.AlphaIsTransparency && hasAlpha;
            iosFormat = TextureToolUtil.GetFormat(RuntimePlatform.IPhonePlayer, hasAlpha);
            androidFormat = TextureToolUtil.GetFormat(RuntimePlatform.Android, hasAlpha);
            pcFormat = TextureToolUtil.GetFormat(RuntimePlatform.WindowsPlayer, hasAlpha);
        }

        var iphoneSettings =
            GetPlatformSettings(
                "iPhone",
                iosFormat,
                setting.MaxSize,
                setting.Quality
            );

        var androidSettings =
            GetPlatformSettings(
                "Android",
                androidFormat,
                setting.MaxSize,
                setting.Quality
            );

        var pcSettings =
            GetPlatformSettings(
                "Standalone",
                pcFormat,
                setting.MaxSize,
                setting.Quality
            );

        if (setting.MaxSize == 0)
        {
            iphoneSettings.maxTextureSize =
                importer.GetPlatformTextureSettings("iPhone").maxTextureSize;
            androidSettings.maxTextureSize =
                importer.GetPlatformTextureSettings("Android").maxTextureSize;
            pcSettings.maxTextureSize =
                importer.GetPlatformTextureSettings("Standalone").maxTextureSize;
        }

        importer.SetPlatformTextureSettings(iphoneSettings);
        importer.SetPlatformTextureSettings(androidSettings);
        importer.SetPlatformTextureSettings(pcSettings);
    }

    internal static TexImporterSetting.Settings GetSettings(
        TexImporterSetting importImporterSetting,
        string assetPath
    )
    {
        var fileName = Path.GetFileName(assetPath);
        for (var i = 0; i < importImporterSetting.SpecialItems.Count; i++)
        {
            var specialItem = importImporterSetting.SpecialItems[i];
            var matchStr = string.Empty;
            switch (specialItem.FilterContentType)
            {
                case TexImporterSetting.SpecialFilterContent.FileName:
                    matchStr = fileName;
                    break;
                case TexImporterSetting.SpecialFilterContent.Path:
                    matchStr = assetPath;
                    break;
                case TexImporterSetting.SpecialFilterContent.ParentFolderName:
                    matchStr = Path.GetFileName(Path.GetDirectoryName(assetPath));
                    break;
            }

            if (CheckSpecialItemMatch(specialItem, matchStr))
            {
                return specialItem.Setting;
            }
        }

        if (importImporterSetting.ApplyDefaultSettings)
        {
            return importImporterSetting.ImporterSettings;
        }

        return null;
    }

    internal static bool CheckSpecialItemMatch(
        TexImporterSetting.SpecialItem specialItem,
        string importerName
    )
    {
        var filterContent = specialItem.FilterContent;
        if (filterContent.IsNOE())
        {
            return false;
        }

        if (specialItem.IgnoreCase)
        {
            filterContent = specialItem.FilterContent.ToLower();
            importerName = importerName.ToLower();
        }

        switch (specialItem.FilterMode)
        {
            case TexImporterSetting.SpecialFilterMode.Regex:
                return Regex.IsMatch(
                    importerName,
                    filterContent
                );
            case TexImporterSetting.SpecialFilterMode.Contain:
                return importerName.Contains(filterContent);
            case TexImporterSetting.SpecialFilterMode.Equals:
                return importerName.Equals(filterContent);
            case TexImporterSetting.SpecialFilterMode.StartWith:
                return importerName.StartsWith(filterContent);
            case TexImporterSetting.SpecialFilterMode.EndWith:
                return importerName.EndsWith(filterContent);
        }

        return false;
    }

    internal static TexImporterSetting FirstSetting(string dirPath)
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

        var asset = UToolsUtil.FindScriptableObject<TexImporterSetting>(dirPath, false);

        if (asset != null)
        {
            settingsCache[dirPath] = asset;
        }

        return asset;
    }

    private static (TexImporterSetting setting, string path) GetImporterSetting(
        string assetPath
    )
    {
        var dir = Path.GetDirectoryName(assetPath);

        var startDir = dir;
        while (true)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                return (null, null);
            }

            var setting = FirstSetting(dir);
            if (setting)
            {
                if (dir == startDir || setting.IncludeChild)
                {
                    return (setting, dir);
                }
            }

            //如果对一个目录调用这个函数，获得的结果相当于是上一级目录
            dir = Path.GetDirectoryName(dir);
        }
    }

    private static TextureImporterPlatformSettings GetPlatformSettings(
        string name,
        TextureImporterFormat format,
        int maxSize,
        TexImporterSetting.QualityMode quality
    )
    {
        var setting = new TextureImporterPlatformSettings();
        setting.overridden = true;
        setting.name = name;
        setting.format = format;
        setting.maxTextureSize = maxSize;
        setting.compressionQuality = (int) quality;
        return setting;
    }

    private static void TrySetPackingTag(
        TextureImporter importer,
        TexImporterSetting.AtlasMode mode,
        string packingTag,
        string assetPath,
        string settingDir
    )
    {
        if (importer.textureType != TextureImporterType.Sprite ||
            mode == TexImporterSetting.AtlasMode.None)
        {
            importer.spritePackingTag = "";
            return;
        }

        switch (mode)
        {
            case TexImporterSetting.AtlasMode.RelativeFolder:
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

            case TexImporterSetting.AtlasMode.ParentFolder:
            {
                var dirPath = Path.GetDirectoryName(assetPath);
                var dirName = Path.GetFileName(dirPath);
                packingTag = dirName;
                break;
            }

            case TexImporterSetting.AtlasMode.CurrentFolder:
                packingTag = Path.GetFileName(settingDir);
                break;
        }

        importer.spritePackingTag = packingTag;
    }
}
#endif