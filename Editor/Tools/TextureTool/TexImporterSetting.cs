using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UTools
{
    public class TexImporterSetting : ScriptableObject
    {
        public enum AlphaMode
        {
            Auto,
            Alpha,
            NoAlpha
        }

        public enum PresetSettings
        {
            None,
            Texture_Scene,
            Texture_UI,
            Sprite_UI
        }

        public enum AtlasMode
        {
            None = 4,
            RelativeFolder = 0,
            ParentFolder = 1,
            Custom = 2,
            CurrentFolder = 3,
        }

        public enum QualityMode
        {
            Fast = 0,
            Normal = 50,
            Best = 100
        }

        public enum SpecialFilterContent
        {
            FileName,
            Path,
            ParentFolderName,
        }

        public enum SpecialFilterMode
        {
            Regex,
            Contain,
            Equals,
            StartWith,
            EndWith
        }

        public bool ApplyDefaultSettings = true;
        public Settings ImporterSettings;

        //如果此选项为true，那么这个Setting所在的目录的子级之内的资源发生变化的时候也会使用这个设置
        //如果子级目录里已经有另一个Setting那子集目录的设置会覆盖父级目录的设置，这些效果同样适用于SpecialItems内的设置
        public bool IncludeChild = true;

        public List<SpecialItem> SpecialItems;

        [Serializable]
        public class Settings
        {
            public AlphaMode AlphaMode = AlphaMode.Auto;

            public FilterMode FilterMode = FilterMode.Bilinear;
            public int MaxSize = 2048;

            public bool AlphaIsTransparency = true;
            public bool MipMapEnable;
            public TextureImporterNPOTScale NPOTScale = TextureImporterNPOTScale.None;
            public TextureImporterFormat OverrideAndroidFormat = TextureImporterFormat.ETC2_RGBA8;
            public TextureImporterFormat OverrideIOSFormat = TextureImporterFormat.ASTC_6x6;
            public TextureImporterFormat OverridePCFormat = TextureImporterFormat.DXT5;

            //如果此值为*，那么将使用该文件夹目录名作为PackingTag
            public AtlasMode AtlasMode = AtlasMode.RelativeFolder;
            public string PackingTag = "";
            public PresetSettings PresetSetting;
            public QualityMode Quality = QualityMode.Normal;
            public bool ReadWriteEnable;
            public bool SRGB = true;

            public TextureImporterType TextureType = TextureImporterType.Sprite;
            public TextureImporterShape TextureShape = TextureImporterShape.Texture2D;
            public TextureWrapMode WrapMode = TextureWrapMode.Clamp;
        }

        [Serializable]
        public class SpecialItem
        {
            public string FilterContent;
            public SpecialFilterContent FilterContentType;
            public SpecialFilterMode FilterMode;
            public bool IgnoreCase;
            public Settings Setting = new Settings();
        }
    }
}