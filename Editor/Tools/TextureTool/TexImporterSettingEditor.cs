using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UTools.Utility;

namespace UTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TexImporterSetting))]
    internal class TexImporterSettingEditor : Editor
    {
        private static Dictionary<int, bool> showMap = new Dictionary<int, bool>();
        private TexImporterSetting impSettings;

        private bool showDefaultSettings = true;
        private bool showSpecials = true;

        private void OnDisable()
        {
            if (EditorUtility.IsDirty(impSettings))
            {
                AssetDatabase.SaveAssets();
            }
        }

        private void OnEnable() => impSettings = (TexImporterSetting) target;

        public override void OnInspectorGUI()
        {
            if (targets.Length == 1)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal("box");
                impSettings.IncludeChild = UTGUI.Toggle(
                    "Recursive",
                    impSettings.IncludeChild
                );
                EditorGUILayout.EndHorizontal();

                DrawDefaultSetting();
                GUILayout.Space(1f);
                DrawSpecialItems();


                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(impSettings);
                }

                GUILayout.BeginHorizontal();

                if (EditorUtility.IsDirty(impSettings))
                {
                    if (GUILayout.Button("Save", GUILayout.Height(30f)))
                    {
                        AssetDatabase.SaveAssets();
                    }
                }

                DrawRefreshBtn();

                GUILayout.EndHorizontal();
            }
            else
            {
                DrawRefreshBtn();
            }

            if (GUILayout.Button("FixAsset", GUILayout.Height(30f)))
            {
                foreach (var v in targets)
                {
                    EditorUtility.SetDirty(v);
                }

                AssetDatabase.SaveAssets();
            }
        }

        private void DrawRefreshBtn()
        {
            if (GUILayout.Button("Reimport Related Textures", GUILayout.Height(30f)))
            {
                var allPaths = new List<string>();
                for (var j = 0; j < targets.Length; j++)
                {
                    var tmis = (TexImporterSetting) targets[j];
                    var dir = UToolsUtil.GetAssetDir(tmis);
                    var paths = UToolsUtil.FindAssetsPath("t:Texture2D", dir, tmis.IncludeChild);
                    if (!tmis.ApplyDefaultSettings)
                    {
                        paths.RemoveAll(
                            v =>
                            {
                                var setting = TexImportProcessor.GetSettings(tmis, v);
                                if (setting == null)
                                {
                                    return true;
                                }

                                return false;
                            }
                        );
                    }

                    allPaths.AddRange(paths);
                }

                AssetDatabase.StartAssetEditing();
                foreach (var path in allPaths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.DontDownloadFromCacheServer);
                }

                AssetDatabase.StopAssetEditing();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawDefaultSetting()
        {
            showDefaultSettings = EditorGUILayout.BeginFoldoutHeaderGroup(
                showDefaultSettings,
                "Default Settings"
            );
            if (showDefaultSettings)
            {
                EditorGUILayout.BeginHorizontal("box");
                impSettings.ApplyDefaultSettings = EditorGUILayout.Toggle(
                    "Enable",
                    impSettings.ApplyDefaultSettings
                );
                EditorGUILayout.EndHorizontal();

                if (impSettings.ApplyDefaultSettings)
                {
                    DrawSetting(impSettings.ImporterSettings, false);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal("box");
                    EditorGUILayout.LabelField("Default Setting Disabled");
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawSpecialItems()
        {
            showSpecials =
                EditorGUILayout.BeginFoldoutHeaderGroup(showSpecials, "Special Items");

            EditorGUI.indentLevel++;

            if (showSpecials)
            {
                EditorGUILayout.BeginVertical("box");

                if (impSettings.SpecialItems == null)
                {
                    impSettings.SpecialItems = new List<TexImporterSetting.SpecialItem>();
                }

                for (var i = 0; i < impSettings.SpecialItems.Count; i++)
                {
                    var specialItem = impSettings.SpecialItems[i];
                    DrawSpecialItem(specialItem, i);
                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Add"))
                {
                    var newSpecialItem = new TexImporterSetting.SpecialItem();
                    impSettings.SpecialItems.Add(newSpecialItem);
                }

                if (GUILayout.Button("Clear"))
                {
                    impSettings.SpecialItems.Clear();
                }

                EditorGUILayout.EndHorizontal();


                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawSpecialItem(TexImporterSetting.SpecialItem item, int index)
        {
            var hashCode = item.GetHashCode();

            EditorGUILayout.BeginHorizontal("box");
            showMap[hashCode] = EditorGUILayout.Foldout(
                showMap.GetValueOrDefault(hashCode),
                $"SpecialItem{index} [Content:{item.FilterContent}]",
                true
            );
            if (GUILayout.Button("X", GUILayout.Width(20f)))
            {
                impSettings.SpecialItems.RemoveAt(index);
            }

            EditorGUILayout.EndHorizontal();

            if (showMap[hashCode])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");

                item.FilterContent = UTGUI.Text("Content", item.FilterContent);
                item.IgnoreCase = UTGUI.Toggle("IgnoreCase", item.IgnoreCase);

                item.FilterContentType = UTGUI.Enum("ContentType", item.FilterContentType);
                item.FilterMode = UTGUI.Enum("MatchMode", item.FilterMode);

                DrawSetting(item.Setting, true);

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSetting(
            TexImporterSetting.Settings setting,
            bool foldout,
            string header = "Settings"
        )
        {
            if (foldout)
            {
                showMap[setting.GetHashCode()] = EditorGUILayout.Foldout(
                    showMap.GetValueOrDefault(setting.GetHashCode()),
                    header,
                    true
                );
            }

            if (!foldout || showMap[setting.GetHashCode()])
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginVertical("box");
                setting.PresetSetting = UTGUI.Enum("Preset: ", setting.PresetSetting);

                var haspreset =
                    setting.PresetSetting != TexImporterSetting.PresetSettings.None;
                EditorGUI.BeginDisabledGroup(haspreset);

                setting.TextureType = UTGUI.Enum("TexType: ", setting.TextureType);
                if (setting.TextureType == TextureImporterType.Default)
                {
                    setting.TextureShape = UTGUI.Enum("TexShape: ", setting.TextureShape);
                }

//            setting.AlphaIsTransparency = MaidEditorGUI.Toggle(
//                "Alpha Is Transparency?",
//                setting.AlphaIsTransparency
//            );
                setting.MipMapEnable = UTGUI.Toggle(
                    "MipMap?",
                    setting.MipMapEnable
                );
                setting.WrapMode = UTGUI.Enum("Wrap", setting.WrapMode);

                if (setting.AlphaMode != TexImporterSetting.AlphaMode.Auto)
                {
                    setting.OverrideAndroidFormat = UTGUI.Enum(
                        "Android",
                        setting.OverrideAndroidFormat
                    );

                    setting.OverrideIOSFormat = UTGUI.Enum(
                        "IOS",
                        setting.OverrideIOSFormat
                    );

                    setting.OverridePCFormat = UTGUI.Enum(
                        "PC",
                        setting.OverridePCFormat
                    );
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginDisabledGroup(setting.TextureType != TextureImporterType.Sprite);

                setting.AtlasMode = UTGUI.Enum("AtlasMode: ", setting.AtlasMode);
                if (setting.AtlasMode == TexImporterSetting.AtlasMode.Custom)
                {
                    setting.PackingTag = UTGUI.Text("Atlas: ", setting.PackingTag);
                }

                EditorGUI.EndDisabledGroup();

                setting.ReadWriteEnable = UTGUI.Toggle(
                    "Readable?",
                    setting.ReadWriteEnable
                );

//                setting.SRGB = MaidEditorGUI.Toggle(
//                    "sRGB?",
//                    setting.SRGB
//                );

                setting.FilterMode = UTGUI.Enum("Filter", setting.FilterMode);
                setting.MaxSize = UTGUI.Int("MaxSize", setting.MaxSize);
                setting.AlphaMode = UTGUI.Enum("AlphaMode", setting.AlphaMode);
                setting.NPOTScale = UTGUI.Enum("NPOT", setting.NPOTScale);
//                setting.Quality = MaidEditorGUI.Enum("Quality", setting.Quality);

                EditorGUILayout.EndVertical();

                EditorGUI.indentLevel--;

                CheckPreset(setting);
            }
        }

        private void CheckPreset(TexImporterSetting.Settings setting)
        {
            switch (setting.PresetSetting)
            {
                case TexImporterSetting.PresetSettings.Texture_Scene:
                    setting.TextureType = TextureImporterType.Default;
                    setting.WrapMode = TextureWrapMode.Repeat;
                    setting.MipMapEnable = true;
                    setting.TextureShape = TextureImporterShape.Texture2D;
                    setting.AlphaIsTransparency = false;
                    break;
                case TexImporterSetting.PresetSettings.Texture_UI:
                    setting.TextureType = TextureImporterType.Default;
                    setting.WrapMode = TextureWrapMode.Clamp;
                    setting.MipMapEnable = false;
                    setting.TextureShape = TextureImporterShape.Texture2D;
                    setting.AlphaIsTransparency = true;
                    break;
                case TexImporterSetting.PresetSettings.Sprite_UI:
                    setting.TextureType = TextureImporterType.Sprite;
                    setting.WrapMode = TextureWrapMode.Clamp;
                    setting.MipMapEnable = false;
                    setting.TextureShape = TextureImporterShape.Texture2D;
                    setting.AlphaIsTransparency = true;
                    break;
            }


            if (setting.AlphaMode != TexImporterSetting.AlphaMode.Auto)
            {
                var hasAlpha = setting.AlphaMode == TexImporterSetting.AlphaMode.Alpha;
                setting.OverrideAndroidFormat = TextureToolUtil.GetFormat(RuntimePlatform.Android, hasAlpha);
                setting.OverrideIOSFormat = TextureToolUtil.GetFormat(RuntimePlatform.IPhonePlayer, hasAlpha);
                setting.OverridePCFormat = TextureToolUtil.GetFormat(RuntimePlatform.WindowsPlayer, hasAlpha);
            }
        }
    }
}