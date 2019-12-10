using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UTools.Utility;
using Object = UnityEngine.Object;


internal class HighlightUtil
{
    private enum ReferType
    {
        RefByScriptField,
        RefByPrefabInst,
        RefByScript,
        RefByObjectName,
    }

    private class Result
    {
        internal ReferType type;
        internal object refObj;

        internal string referSceneName;
        internal Transform referRoot;
        internal GameObject referObj;
        internal Component referScript;
        internal MemberInfo referMember;
    }

    private static List<Result> results = new List<Result>();

    internal static void HighlightRefer(
        GameObject referRoot,
        object refObj,
        bool logInfo,
        FindThingType mode
    )
    {
        results.Clear();
        UToolsUtil.RecursiveObjs(
            v => { FindRefInGameObject(v, refObj, referRoot.transform, null, mode); },
            referRoot
        );
        HighlightFirstResult();

        if (logInfo)
        {
            LogResult();
        }
    }

    internal static void HighlightRefer(
        Scene referScene,
        object refObj,
        bool logInfo,
        FindThingType mode
    )
    {
        results.Clear();
        var rootGOS = referScene.GetRootGameObjects();
        UToolsUtil.RecursiveObjs(
            v => { FindRefInGameObject(v, refObj, null, referScene.name, mode); },
            rootGOS
        );
        HighlightFirstResult();

        if (logInfo)
        {
            LogResult();
        }
    }

    private static void HighlightFirstResult()
    {
        if (results.Count > 0)
        {
            var firstResult = results[0];

            Selection.SetActiveObjectWithContext(firstResult.referObj, firstResult.referObj);
            EditorGUIUtility.PingObject(firstResult.referObj);
//                Selection.activeGameObject = firstResult.referObj;
//                EditorGUIUtility.PingObject(firstResult.referObj);
        }
    }

    private static void LogResult()
    {
        foreach (var result in results)
        {
            var path = UToolsUtil.GetTransformTreePath(
                result.referObj.transform,
                result.referRoot
            );
            if (result.referSceneName.IsNNOE())
            {
                path = result.referSceneName + "/" + path;
            }

            switch (result.type)
            {
                case ReferType.RefByScriptField:
                {
                    var message = string.Format(
                        "Refer Info: Path:{0}, Script:{1}, Member:{2}",
                        path.ToColor(Color.yellow),
                        result.referScript.GetType().ToString().ToColor(Color.yellow),
                        result.referMember.Name.ToColor(Color.yellow)
                    );
                    Debug.LogWarning(message, result.referObj);
                    break;
                }

                default:
                {
                    var message =
                        $"Refer Info : Path:{path.ToColor(Color.yellow)}, {result.type}";
                    Debug.LogWarning(
                        message,
                        result.referObj
                    );
                    break;
                }
            }
        }
    }

    internal static void FindRefInGameObject(
        GameObject referObj,
        object refObj,
        Transform referRoot,
        string referSceneName,
        FindThingType mode
    )
    {
        var comps = referObj.GetComponents<Component>();
        for (var i = 0; i < comps.Length; i++)
        {
            var m = comps[i];
            if (m == null)
            {
                Debug.LogWarning($"{referObj} has a missing Component", referObj);
                continue;
            }

            FindRefInComponent(m, refObj, referRoot, referSceneName);
        }

        if (mode == FindThingType.CustomStr)
        {
            var referObjName = referObj.name;
            var refObjStr = refObj.ToString();
            if (referObjName == refObjStr || referObjName.Contains(refObjStr))
            {
                var result = new Result();
                result.type = ReferType.RefByObjectName;
                result.refObj = refObj;
                result.referSceneName = referSceneName;
                result.referRoot = referRoot;
                result.referObj = referObj;
                results.Add(result);
            }
        }

        if (refObj is GameObject)
        {
            var isPrefabInstance = UToolsUtil.IsPrefabInstance(referObj);
            if (isPrefabInstance)
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(referObj);
                if (root != referObj)
                {
                    return;
                }

                var prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(referObj);
                if (prefabParent == (GameObject) refObj)
                {
                    var result = new Result();
                    result.type = ReferType.RefByPrefabInst;
                    result.refObj = refObj;
                    result.referSceneName = referSceneName;
                    result.referRoot = referRoot;
                    result.referObj = referObj;
                    results.Add(result);
                }
            }
        }
    }

    internal static void FindRefInComponent(
        Component m,
        object refObj,
        Transform referRoot,
        string referSceneName
    )
    {
        if (refObj is MonoScript)
        {
            var script = (MonoScript) refObj;
            if (script.GetClass() == m.GetType())
            {
                var result = new Result();
                result.type = ReferType.RefByScript;
                result.refObj = refObj;
                result.referSceneName = referSceneName;
                result.referRoot = referRoot;
                result.referObj = m.gameObject;
                result.referScript = m;
                results.Add(result);
            }
        }

        var t = m.GetType();
        var bindingFlags = BindingFlags.Public |
                           BindingFlags.NonPublic |
                           BindingFlags.Instance |
                           BindingFlags.Default;
        var fields = t.GetFields(bindingFlags);

        foreach (var f in fields)
        {
            FindRefInField(m, f, refObj, referRoot, referSceneName);
        }
    }

    internal static void FindRefInField(
        Component m,
        MemberInfo memberInfo,
        object refObj,
        Transform referRoot,
        string referSceneName
    )
    {
        var attrs = memberInfo.GetCustomAttributes(typeof(ObsoleteAttribute))
            .Cast<ObsoleteAttribute>();
        var isError = attrs.Any(v => v.IsError);
        if (isError)
        {
            return;
        }

        Type valueType = null;
        object obj = null;
        if (memberInfo is FieldInfo f)
        {
            valueType = f.FieldType;
        }

        if (IsDriverFrom(refObj.GetType(), valueType))
        {
            if (memberInfo is FieldInfo f1)
            {
                obj = f1.GetValue(m);
            }

            if (CheckEquals(obj, refObj))
            {
                var result = new Result
                {
                    type = ReferType.RefByScriptField,
                    refObj = refObj,
                    referSceneName = referSceneName,
                    referRoot = referRoot,
                    referObj = m.gameObject,
                    referScript = m,
                    referMember = memberInfo
                };
                results.Add(result);
            }
        }
    }

    private static bool CheckEquals(object fieldObj, object refObj)
    {
        if (fieldObj == null)
        {
            return false;
        }

        if (fieldObj is Sprite fieldSprite && refObj is Texture2D tex)
        {
            return fieldSprite && fieldSprite.texture == tex;
        }

        return fieldObj.Equals(refObj);
    }

    private static Type textureType = typeof(Texture);
    private static Type spriteType = typeof(Sprite);

    internal static bool IsDriverFrom(Type subType, Type targetType)
    {
        if (subType == targetType)
        {
            return true;
        }

        if (subType.IsSubclassOf(targetType))
        {
            return true;
        }

        if ((subType == textureType || subType.IsSubclassOf(textureType)) &&
            targetType == spriteType)
        {
            return true;
        }

        return false;
    }
}