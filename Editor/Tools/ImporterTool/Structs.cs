using UnityEditor;
using UnityEngine;

namespace UTools
{
    public enum ControllerType
    {
        TexFormat,
    }

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

    public enum ConditionContent
    {
        FileName,
        Path,
        ParentFolderName,
    }

    public enum ConditionMethod
    {
        None,
        Regex,
        Contain,
        Equals,
        StartWith,
        EndWith
    }

    [System.Serializable]
    public class TexFormatController
    {
        public virtual ControllerType type => ControllerType.TexFormat;

        public bool enable = true;
        public bool recursive = true;

        public ConditionMethod conditionMethod = ConditionMethod.None;
        public ConditionContent conditionContent = ConditionContent.FileName;
        public string conditionText;
        public bool ignoreCase = true;

        public AlphaMode alphaMode = AlphaMode.Auto;

        public FilterMode filterMode = FilterMode.Bilinear;
        public int maxSize = 2048;

        public bool alphaIsTransparency = true;
        public bool mipMapEnable;
        public TextureImporterNPOTScale nPOTScale = TextureImporterNPOTScale.None;
        public TextureImporterFormat overrideAndroidFormat = TextureImporterFormat.ETC2_RGBA8;
        public TextureImporterFormat overrideIOSFormat = TextureImporterFormat.ASTC_6x6;
        public TextureImporterFormat overridePCFormat = TextureImporterFormat.DXT5;

        public AtlasMode atlasMode = AtlasMode.RelativeFolder;
        public string packingTag = "";
        public PresetSettings preset;
        public QualityMode quality = QualityMode.Normal;
        public bool readWriteEnable;
        public bool sRGB = true;

        public TextureImporterType textureType = TextureImporterType.Sprite;
        public TextureImporterShape textureShape = TextureImporterShape.Texture2D;
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    }
}