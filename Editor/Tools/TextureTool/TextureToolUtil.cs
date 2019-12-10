using UnityEditor;
using UnityEngine;


internal static class TextureToolUtil
{
    internal static TextureImporterFormat GetFormat(RuntimePlatform platform, bool hasAlpha)
    {
        switch (platform)
        {
            case RuntimePlatform.Android:
                return hasAlpha
                    ? TextureImporterFormat.ETC2_RGBA8
                    : TextureImporterFormat.ETC2_RGB4;
            case RuntimePlatform.IPhonePlayer:
                return hasAlpha
                    ? TextureImporterFormat.ASTC_6x6
                    : TextureImporterFormat.ASTC_RGB_6x6;
            case RuntimePlatform.WindowsPlayer:
                return TextureImporterFormat.DXT5;
            default:
                return TextureImporterFormat.DXT5;
        }
    }

    internal static bool IsTexture(string ex)
    {
        switch (ex.ToLower())
        {
            case ".jpg":
                return true;
            case ".png":
                return true;
            case ".tga":
                return true;
            case ".bmp":
                return true;
            case ".psd":
                return true;
            case ".gif":
                return true;
            default:
                return false;
        }
    }
}