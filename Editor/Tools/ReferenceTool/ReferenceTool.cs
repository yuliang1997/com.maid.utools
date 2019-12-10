using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UTools.Utility;
using Object = UnityEngine.Object;

internal class ReferenceTool : EditorWindow
{
    [SerializeField] private ReferenceToolMap assetRefFinder;
    [SerializeField] private ReferenceToolMap builtinCompRefFinder;
    [SerializeField] private ReferenceToolString stringFinder;

    private Object curFindAsset;
    private string customStr;
    private bool enableReplace;

    private FindAssetsMode findAssetMode;
    private Type findCompType;
    private FindThingType findThingType;

    private ReferenceMap guidMap;
    private GUIDMapGen guidMapGen;
    private Object replaceTargetAsset;
    private string replaceTargetString;
    private string rgoption = "";

    private Vector2 scrollPos;

    private WhenShouldFind whenShouldFind;

    private void OnEnable()
    {
        ReferenceToolSetting.Initialize();
        if (assetRefFinder == null)
        {
            assetRefFinder = new ReferenceToolMap();
        }

        if (builtinCompRefFinder == null)
        {
            builtinCompRefFinder = new ReferenceToolMap();
        }

        if (stringFinder == null)
        {
            stringFinder = new ReferenceToolString();
        }

        if (guidMapGen == null)
        {
            guidMapGen = new GUIDMapGen();
            guidMapGen.eventStateChanged = Repaint;
        }
    }

    private Vector2 scrollerPos;

    private void OnGUI()
    {
        if (DrawWorking(guidMapGen))
        {
            return;
        }

        findThingType = UTGUI.EnumToolbar(findThingType);

        EditorGUILayout.BeginVertical("box");

        scrollerPos = EditorGUILayout.BeginScrollView(scrollerPos);

        switch (findThingType)
        {
            case FindThingType.Assets:
                DrawFindAssetView();
                break;
            case FindThingType.CustomStr:
                DrawFindStrView();
                break;
            case FindThingType.BuiltinComponent:
                DrawFindBuiltinComponentView();
                break;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        DrawOptionsView();
        GUILayout.EndVertical();
    }

    private void OnSelectionChange()
    {
        Repaint();
        if (findThingType == FindThingType.Assets &&
            findAssetMode == FindAssetsMode.Selection)
        {
            switch (whenShouldFind)
            {
                case WhenShouldFind.ClickFind:
                    break;
                case WhenShouldFind.SelectionChange:
                    DoFindAssets();
                    break;
            }
        }
    }

    private void DrawFindAssetView()
    {
        if (DrawWorking(assetRefFinder))
        {
            return;
        }

        EditorGUILayout.BeginHorizontal();

        findAssetMode = UTGUI.EnumToolbar(findAssetMode);

        if (findAssetMode == FindAssetsMode.Field)
        {
            whenShouldFind = WhenShouldFind.ClickFind;
        }

        EditorGUI.BeginDisabledGroup(findAssetMode == FindAssetsMode.Field);
        whenShouldFind = UTGUI.EnumToolbar(whenShouldFind);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

//            this.findAssetFrom = MaidEditorGUI.EnumToolbar<FindAssetFrom>(0x1004);

        var objType = ReferenceToolUtil.type_object;

        if (curFindAsset && !(curFindAsset is DefaultAsset))
        {
            objType = curFindAsset.GetType();
        }

        if (findAssetMode == FindAssetsMode.Field)
        {
            curFindAsset = EditorGUILayout.ObjectField(
                "FindTarget:",
                curFindAsset,
                objType,
                false
            );
        }
        else
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("FindTarget:", curFindAsset, objType, false);
            EditorGUI.EndDisabledGroup();
        }

        if (enableReplace)
        {
            EditorGUILayout.BeginHorizontal();

            replaceTargetAsset = EditorGUILayout.ObjectField(
                "ReplaceTarget:",
                replaceTargetAsset,
                objType,
                false
            );

            EditorGUILayout.EndHorizontal();
        }

        if (curFindAsset is GameObject)
        {
            enableReplace = false;
        }
        else
        {
            enableReplace = EditorGUILayout.ToggleLeft(
                "EnableReplace",
                enableReplace,
                GUILayout.Width(100f)
            );
        }

        if (GUILayout.Button("Find"))
        {
            DoFindAssets();
        }

        DrawResultView(assetRefFinder);
    }

    private void DoFindAssets()
    {
        var working = assetRefFinder.working;
        if (working)
        {
            return;
        }

        EnsureMapExist();
        var findTarget = findAssetMode == FindAssetsMode.Selection
            ? Selection.activeObject
            : curFindAsset;
        if (findTarget.IsAsset())
        {
            curFindAsset = findTarget;
            assetRefFinder.FindOnlyRefAsset(guidMap, findTarget);
        }
    }

    private void DrawFindBuiltinComponentView()
    {
        if (DrawWorking(builtinCompRefFinder))
        {
            return;
        }

        EditorGUI.BeginChangeCheck();

        customStr = EditorGUILayout.TextField("FindTarget:", customStr);

        if (EditorGUI.EndChangeCheck() || findCompType == null)
        {
            findCompType = UToolsUtil.GetBuiltinClassType(customStr);
        }

        EditorGUI.BeginDisabledGroup(true);
        if (findCompType != null && findCompType.IsSubclassOf(typeof(Component)))
        {
            EditorGUILayout.LabelField(findCompType.ToString());
        }
        else
        {
            EditorGUILayout.LabelField("InvalidType");
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(!enableReplace);

        replaceTargetString = EditorGUILayout.TextField(
            "ReplaceTarget:",
            replaceTargetString
        );

        EditorGUI.EndDisabledGroup();

        enableReplace = EditorGUILayout.ToggleLeft(
            "EnableReplace",
            enableReplace,
            GUILayout.Width(100f)
        );


        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Find"))
        {
            if (findCompType != null)
            {
                EnsureMapExist();
                builtinCompRefFinder.FindDllComponent(guidMap, findCompType);
            }
        }

        DrawResultView(builtinCompRefFinder);
    }

    private void DrawFindStrView()
    {
        if (DrawWorking(stringFinder))
        {
            return;
        }

        customStr = EditorGUILayout.TextField("FindTarget:", customStr);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(!enableReplace);
        replaceTargetString = EditorGUILayout.TextField(
            "ReplaceTarget:",
            replaceTargetString
        );
        EditorGUI.EndDisabledGroup();

        enableReplace = EditorGUILayout.ToggleLeft(
            "EnableReplace",
            enableReplace,
            GUILayout.Width(100f)
        );

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Find"))
        {
            stringFinder.FindString(customStr, rgoption, null);
        }

        DrawResultView(stringFinder);
    }

    private void DrawOptionsView()
    {
        EditorGUILayout.BeginVertical("box");
//        GUILayout.BeginHorizontal();
//
//        EditorGUILayout.LabelField("RgOptions:", GUILayout.Width(65f));
//        rgoption = EditorGUILayout.TextField(rgoption);

//        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

//            findStringTool =
//                (FindStringTool) EditorGUILayout.EnumPopup(
//                    findStringTool,
//                    GUILayout.Height(16),
//                    GUILayout.Width(60)
//                );
        if (GUILayout.Button("Sync Change", GUILayout.Height(16)))
        {
            EnsureMapExist();
            guidMapGen.SyncChangeLogToMap(guidMap);
        }

        void GenMap(MapGen mapGen, ReferenceMap map)
        {
            var all = EditorUtility.DisplayDialog(
                "Tips",
                "Which files do you want to index?",
                "All",
                "Custom"
            );
            if (all)
            {
                mapGen.GenerateMap(
                    map,
                    UToolsUtil.DataPath,
                    rgoption
                );
            }
            else
            {
                var folder = EditorUtility.OpenFolderPanel("Choose", "Assets", "Assets");
                GUIUtility.ExitGUI();
                if (folder.IsNNOE())
                {
                    if (!folder.StartsWith(UToolsUtil.DataPath))
                    {
                        EditorUtility.DisplayDialog(
                            "Error",
                            "this folder not in this project",
                            "OK"
                        );
                    }
                    else
                    {
                        mapGen.GenerateMap(
                            map,
                            folder,
                            rgoption
                        );
                    }
                }
            }
        }

        if (GUILayout.Button("Generate GUIDMap", GUILayout.Height(16)))
        {
            EnsureMapExist();
            GenMap(guidMapGen, guidMap);
            return;
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(1f);
        EditorGUILayout.EndVertical();
    }

    private void DrawResultView(ReferenceToolBase worker)
    {
        GUILayout.Space(3f);
        EditorGUILayout.LabelField($"Find Result:{worker.findResult.Count}");
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!(enableReplace && worker.findResult.Count > 0));
        EditorGUI.BeginDisabledGroup(worker.findResult.Count == 0);

        if (enableReplace && replaceTargetAsset)
        {
            if (GUILayout.Button("ReplaceAll", GUILayout.Width(90f), GUILayout.Height(16)))
            {
                if (EnsureReplaceToStr(worker))
                {
                    worker.ReplaceStringAll();
                }
            }

            EditorGUI.BeginDisabledGroup(worker.replaceResult.Count == 0);
            if (GUILayout.Button("RevertAll", GUILayout.Width(90f), GUILayout.Height(16)))
            {
                worker.RevertReplaceInfoAll();
            }
        }

        EditorGUI.EndDisabledGroup();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3f);
        foreach (var v in worker.findResult)
        {
            if (v == null)
            {
                continue;
            }

            DrawUFileInfo(v, worker);
            GUILayout.Space(2f);
        }
    }

    private void DrawUFileInfo(UFileInfo v, ReferenceToolBase maid)
    {
        GUILayout.BeginHorizontal();
        var content = EditorGUIUtility.ObjectContent(null, v.assetType);
        content.text = v.fileName;
        if (GUILayout.Button(
            content,
            EditorStyles.objectField,
            GUILayout.Height(16f),
            GUILayout.Width(250f)
        ))
        {
            EditorGUIUtility.PingObject(v.asset);
        }

//        EditorGUI.BeginDisabledGroup(true);
//        EditorGUILayout.ObjectField(v.Obj, v.ObjType, false);
//        EditorGUI.EndDisabledGroup();
        switch (v.extension)
        {
            case ".prefab":
            case ".unity":
                if (GUILayout.Button("Open", GUILayout.Height(16)))
                {
                    EditRefer(v);
                }

                break;
        }

        EditorGUI.BeginDisabledGroup(!enableReplace);
        DrawUFileInfoReplacePne(v, maid);
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
    }

    private void DrawUFileInfoReplacePne(UFileInfo v, ReferenceToolBase worker)
    {
        if (v.assetFrom == AssetFrom.Resources)
        {
            //todo 暂时禁用
            return;
        }

        if (v.isWaiting)
        {
            GUILayout.Label("Waiting...", GUILayout.Height(16));
        }

        else
        {
            var replaceInfo = worker.FindReplaceInfo(v.relativePath);
            if (replaceInfo != null)
            {
                if (GUILayout.Button("Revert", GUILayout.Width(60f), GUILayout.Height(16)))
                {
                    worker.RevertReplace(replaceInfo);
                }
            }
            else
            {
                switch (findThingType)
                {
                    case FindThingType.Assets:
                        if (!(curFindAsset is GameObject))
                        {
                            EditorGUI.BeginDisabledGroup(!replaceTargetAsset);
                            {
                                if (GUILayout.Button(
                                    "Replace",
                                    GUILayout.Width(60f),
                                    GUILayout.Height(16)
                                ))
                                {
                                    if (EnsureReplaceToStr(worker))
                                    {
                                        worker.ReplaceOne(v);
                                    }
                                }

                                EditorGUI.EndDisabledGroup();
                            }
                        }


                        break;
                    case FindThingType.CustomStr:
                        EditorGUI.BeginDisabledGroup(!replaceTargetString.IsNNOE());
                        if (GUILayout.Button(
                            "Replace",
                            GUILayout.Width(60f),
                            GUILayout.Height(16)
                        ))
                        {
                            if (EnsureReplaceToStr(worker))
                            {
                                worker.ReplaceOne(v);
                            }
                        }

                        EditorGUI.EndDisabledGroup();
                        break;
                }
            }
        }
    }

    private bool DrawWorking(params ReferenceToolWork[] maid)
    {
        var working = maid.Any(v => v.working);

        if (working)
        {
            var progressRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, 1f, "Waiting...");
        }

        return working;
    }

    private void EditRefer(UFileInfo info)
    {
        void ClearConsoleLog()
        {
            var consoleWindowInst =
                ReflectionUtil.GetEditorWindow("UnityEditor.ConsoleWindow");
            ReflectionUtil.CallStatic("UnityEditor.LogEntries", "Clear");
            consoleWindowInst.Call("DoLogChanged");
        }

        ClearConsoleLog();

        switch (info.extension)
        {
            case ".prefab":
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage == null || stage.prefabContentsRoot.name != info.fileName)
                {
                    AssetDatabase.OpenAsset(info.asset);
                    stage = PrefabStageUtility.GetCurrentPrefabStage();
                }

                var root = stage.prefabContentsRoot;

                if (root)
                {
                    var refObj = GetRefObj(info);
                    HighlightUtil.HighlightRefer(
                        root,
                        refObj,
                        true,
                        findThingType
                    );
                }

                break;
            }

            case ".unity":
            {
                var path = UToolsUtil.AssetRelativePath(info.relativePath);
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid())
                {
                    var index = EditorUtility.DisplayDialogComplex(
                        "Warning",
                        $"How to open this scene {info.fileName} ?",
                        "Cancel",
                        "SingleOpen",
                        "Additive"
                    );
                    switch (index)
                    {
                        case 0:
                            break;
                        case 1:
                            scene = EditorSceneManager.OpenScene(
                                path,
                                OpenSceneMode.Single
                            );
                            break;
                        case 2:
                            scene = EditorSceneManager.OpenScene(
                                path,
                                OpenSceneMode.Additive
                            );
                            break;
                    }

                    if (!scene.isLoaded)
                    {
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    }
                }

                if (scene.IsValid())
                {
                    var refObj = GetRefObj(info);
                    HighlightUtil.HighlightRefer(
                        scene,
                        refObj,
                        true,
                        findThingType
                    );
                }

                break;
            }
        }
    }

    private object GetRefObj(UFileInfo info)
    {
        switch (info.assetFrom)
        {
            case AssetFrom.StringFinder:
                return stringFinder.curFindString;
            case AssetFrom.OtherAsset:
                return curFindAsset;
            case AssetFrom.Resources:
                return assetRefFinder.curFindString;
        }

        return null;
    }

    private void EnsureMapExist()
    {
        guidMap = guidMap ?? new ReferenceMap(ReferenceToolSetting.guidMapPath);
    }

    private bool EnsureReplaceToStr(ReferenceToolBase maid)
    {
        var result = false;
        switch (findThingType)
        {
            case FindThingType.Assets:
                if (replaceTargetAsset)
                {
                    var (guid, fileID) =
                        UToolsUtil.GetAssetGUIDAndFileID(replaceTargetAsset);
                    if (guid.IsNNOE())
                    {
                        replaceTargetString = $"guid: {guid}";
                        result = true;
                    }
                }

                break;
            case FindThingType.BuiltinComponent:
                result = false;
                break;
            case FindThingType.CustomStr:
                result = replaceTargetString.IsNNOE();
                break;
        }

        maid.EnsureReplaceToStr(replaceTargetString);
        return result;
    }
}