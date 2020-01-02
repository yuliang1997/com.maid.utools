using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UTools.Utility;

namespace UTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AssetImportSetting))]
    public class AssetImportSettingEditor : Editor
    {
        private AssetImportSetting setting;
        GenericMenu menu = new GenericMenu();

        private void OnEnable()
        {
            setting = (AssetImportSetting) target;

            var names = Enum.GetNames(typeof(ControllerType));
            for (int i = 0; i < names.Length; i++)
            {
                int tempi = i;
                menu.AddItem(new GUIContent(names[tempi]), false, () => { addControl((ControllerType) tempi); });
            }

            if (showTexControlMap == null)
            {
                showTexControlMap = new bool[setting.texFormatControllers.Count];
                for (int i = 0; i < showTexControlMap.Length; i++)
                {
                    showTexControlMap[i] = true;
                }
            }
        }

        private bool showTexCtrls = true;

        public override void OnInspectorGUI()
        {
            if (targets.Length == 1)
            {
                showTexCtrls =
                    EditorGUILayout.BeginFoldoutHeaderGroup(showTexCtrls, "Texture Format Controls");

                EditorGUI.indentLevel++;

                if (showTexCtrls)
                {
                    EditorGUILayout.BeginVertical();
                    // change check


                    for (int i = 0; i < setting.texFormatControllers.Count; i++)
                    {
                        drawTexFormatControl(setting.texFormatControllers[i], i);
                    }

                    drawRefreshTexturesBtn();

                    if (willRemoveTexControl >= 0)
                    {
                        setting.texFormatControllers.RemoveAt(willRemoveTexControl);
                        willRemoveTexControl = -1;
                        EditorUtility.SetDirty(setting);
                    }

                    EditorGUILayout.EndVertical();
                }

                GUILayout.BeginHorizontal();
                {
                    float height = 30f;

                    if (GUILayout.Button("Add Control", GUILayout.Height(height)))
                    {
                        menu.ShowAsContext();
                    }

                    using (new EditorGUI.DisabledScope(!EditorUtility.IsDirty(setting)))
                    {
                        if (GUILayout.Button("Apply", GUILayout.Height(height)))
                        {
                            AssetDatabase.SaveAssets();
                        }
                    }

                    GUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                drawRefreshTexturesBtn();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void drawRefreshTexturesBtn()
        {
            if (GUILayout.Button("Reimport Related Textures", GUILayout.Height(30f)))
            {
                var allPaths = new List<string>();
                for (var j = 0; j < targets.Length; j++)
                {
                    var s = (AssetImportSetting) targets[j];
                    var dir = UToolsUtil.GetAssetDir(s);
                    var recursive = s.texFormatControllers.Any(v => v.recursive);
                    var paths = UToolsUtil.FindAssetsPath("t:Texture2D", dir, recursive);
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


        private bool[] showTexControlMap;
        private int willRemoveTexControl = -1;

        private void drawTexFormatControl(TexFormatController control, int index)
        {
            GUILayout.BeginVertical();
            {
                drawHeader();
                if (showTexControlMap[index])
                {
                    GUILayout.BeginVertical("box");

                    EditorGUI.BeginChangeCheck();
                    {
                        control.enable = EditorGUILayout.Toggle("Enable", control.enable);

                        UTGUI.splitLine();

                        EditorGUI.BeginDisabledGroup(!control.enable);
                        {
                            drawCondition();

                            UTGUI.splitLine();

                            control.recursive = EditorGUILayout.Toggle("Recursive", control.recursive);

                            drawPresetDisableGroup();
                            drawOtherTextureSetting();

                            checkPreset(control);

                            EditorGUI.EndDisabledGroup();
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(setting);
                        }
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.EndVertical();
            }


            void drawHeader()
            {
                GUILayout.BeginHorizontal("box");

                string header = $"Preset:{control.preset}";
                if (control.conditionMethod != ConditionMethod.None)
                {
                    header = header + $", Condition:{control.conditionMethod}({control.conditionText})";
                }

                showTexControlMap[index] = EditorGUILayout.Foldout(showTexControlMap[index], header, true);

                float width = 30f;

                using (new EditorGUI.DisabledScope(index <= 0))
                {
                    if (GUILayout.Button("↑", GUILayout.Width(width)))
                    {
                        var temp = setting.texFormatControllers[index - 1];
                        setting.texFormatControllers[index - 1] = control;
                        setting.texFormatControllers[index] = temp;
                        EditorUtility.SetDirty(setting);
                    }
                }

                using (new EditorGUI.DisabledScope(index >= setting.texFormatControllers.Count - 1))
                {
                    if (GUILayout.Button("↓", GUILayout.Width(width)))
                    {
                        var temp = setting.texFormatControllers[index + 1];
                        setting.texFormatControllers[index + 1] = control;
                        setting.texFormatControllers[index] = temp;
                        EditorUtility.SetDirty(setting);
                    }
                }

                if (GUILayout.Button("X", GUILayout.Width(width)))
                {
                    willRemoveTexControl = index;
                }

                GUILayout.EndHorizontal();
            }

            void drawCondition()
            {
                control.conditionMethod = UTGUI.Enum("Condition", control.conditionMethod);
                if (control.conditionMethod != ConditionMethod.None)
                {
                    control.conditionContent = UTGUI.Enum("ContentType", control.conditionContent);
                    control.conditionText = UTGUI.Text("ContentText", control.conditionText);
                    control.ignoreCase = UTGUI.Toggle("IgnoreCase", control.ignoreCase);
                }
            }

            void drawPresetDisableGroup()
            {
                control.preset = UTGUI.Enum("Preset: ", control.preset);
                // preset disable group
                var haspreset = control.preset != PresetSettings.None;
                EditorGUI.BeginDisabledGroup(haspreset);
                {
                    control.textureType = UTGUI.Enum("TexType: ", control.textureType);
                    if (control.textureType == TextureImporterType.Default)
                    {
                        control.textureShape = UTGUI.Enum("TexShape: ", control.textureShape);
                    }

                    control.mipMapEnable = EditorGUILayout.Toggle("MipMap?", control.mipMapEnable);

                    control.wrapMode = UTGUI.Enum("Wrap", control.wrapMode);

                    if (control.alphaMode != AlphaMode.Auto)
                    {
                        control.overrideAndroidFormat = UTGUI.Enum("Android", control.overrideAndroidFormat);
                        control.overrideIOSFormat = UTGUI.Enum("IOS", control.overrideIOSFormat);
                        control.overridePCFormat = UTGUI.Enum("PC", control.overridePCFormat);
                    }

                    EditorGUI.EndDisabledGroup();
                }
            }

            void drawOtherTextureSetting()
            {
                // sprite
                EditorGUI.BeginDisabledGroup(control.textureType != TextureImporterType.Sprite);
                {
                    control.atlasMode = UTGUI.Enum("AtlasMode: ", control.atlasMode);
                    if (control.atlasMode == AtlasMode.Custom)
                    {
                        control.packingTag = UTGUI.Text("Atlas: ", control.packingTag);
                    }

                    EditorGUI.EndDisabledGroup();
                }

                control.readWriteEnable = EditorGUILayout.Toggle("Readable?", control.readWriteEnable);
                control.filterMode = UTGUI.Enum("Filter", control.filterMode);
                control.maxSize = UTGUI.Int("MaxSize", control.maxSize);
                control.alphaMode = UTGUI.Enum("AlphaMode", control.alphaMode);
                control.nPOTScale = UTGUI.Enum("NPOT", control.nPOTScale);
            }
        }

        private void checkPreset(TexFormatController control)
        {
            switch (control.preset)
            {
                case PresetSettings.Texture_Scene:
                    control.textureType = TextureImporterType.Default;
                    control.wrapMode = TextureWrapMode.Repeat;
                    control.mipMapEnable = true;
                    control.textureShape = TextureImporterShape.Texture2D;
                    control.alphaIsTransparency = false;
                    break;
                case PresetSettings.Texture_UI:
                    control.textureType = TextureImporterType.Default;
                    control.wrapMode = TextureWrapMode.Clamp;
                    control.mipMapEnable = false;
                    control.textureShape = TextureImporterShape.Texture2D;
                    control.alphaIsTransparency = true;
                    break;
                case PresetSettings.Sprite_UI:
                    control.textureType = TextureImporterType.Sprite;
                    control.wrapMode = TextureWrapMode.Clamp;
                    control.mipMapEnable = false;
                    control.textureShape = TextureImporterShape.Texture2D;
                    control.alphaIsTransparency = true;
                    break;
            }

            if (control.alphaMode != AlphaMode.Auto)
            {
                var hasAlpha = control.alphaMode == AlphaMode.Alpha;
                control.overrideAndroidFormat = TextureToolUtil.GetFormat(RuntimePlatform.Android, hasAlpha);
                control.overrideIOSFormat = TextureToolUtil.GetFormat(RuntimePlatform.IPhonePlayer, hasAlpha);
                control.overridePCFormat = TextureToolUtil.GetFormat(RuntimePlatform.WindowsPlayer, hasAlpha);
            }
        }

        private void addControl(ControllerType type)
        {
            switch (type)
            {
                case ControllerType.TexFormat:
                    setting.texFormatControllers.Add(new TexFormatController());
                    ArrayUtility.Add(ref showTexControlMap, true);
                    EditorUtility.SetDirty(setting);
                    break;
            }
        }
    }
}