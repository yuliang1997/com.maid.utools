using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UTools
{
    internal static class ReferenceToolUtil
    {
        internal static string GetContent(string path) => File.ReadAllText(path);

        internal static List<string> GetGuidsByPath(string path) => GetRefsFromContent(GetContent(path));

        internal const string refRegexString = @"fileID: -?\d*, guid: [a-zA-Z0-9]*";
        internal static Regex refRegex = new Regex(refRegexString);

        internal static List<string> GetRefsFromContent(string str)
        {
            var refInfos = new List<string>();
            var matchs = refRegex.Matches(str);
            foreach (Match v in matchs)
            {
                refInfos.Add(v.Value);
            }

            return refInfos;
        }

        internal static string[] guidMapHandleExs =
        {
            ".asset",
            ".unity",
            ".prefab",
            ".mat",
            ".anim",
        };

        internal static string[] rfMapHandleExs =
        {
            ".unity",
            ".prefab",
        };

        internal static readonly Type type_object = typeof(UnityEngine.Object);
        internal static readonly Type type_gameobject = typeof(GameObject);
        internal static readonly Type type_scene = typeof(SceneAsset);
        internal static readonly Type type_material = typeof(Material);
        internal static readonly Type type_script = typeof(MonoScript);
        internal static readonly Type type_anim = typeof(Animation);
        internal static readonly Type type_scriptable = typeof(ScriptableObject);

        internal static Type GetTypeByExtension(string ex)
        {
            switch (ex.ToLower())
            {
                case ".prefab":
                    return type_gameobject;
                case ".unity":
                    return type_scene;
                case ".mat":
                    return type_material;
                case ".cs":
                    return type_script;
                case ".anim":
                    return type_anim;
                case ".asset":
                    return type_scriptable;
            }

            return type_object;
        }
    }
}