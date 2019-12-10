using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UTools.Utility
{
    internal static class EditorExtensions
    {
        internal static bool IsAsset(this Object obj) => obj && AssetDatabase.Contains(obj) && !(obj is DefaultAsset);

        internal static bool IsNOE(this string str) => string.IsNullOrEmpty(str);

        internal static bool IsNNOE(this string str) => !IsNOE(str);

        internal static string ToColor(this string str, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";

        internal static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dic,
            TKey key,
            bool remove = false
        )
        {
            var value = default(TValue);
            if (dic.ContainsKey(key))
            {
                value = dic[key];
                if (remove)
                {
                    dic.Remove(key);
                }
            }

            return value;
        }

        internal static T GetValueOrDefault<T>(this IList<T> l, int index)
        {
            if (index < 0)
            {
                return default;
            }

            if (l.Count > index)
            {
                return l[index];
            }

            return default;
        }

        internal static List<TField> SelectL<TItem, TField>(this IEnumerable<TItem> l, Func<TItem, TField> func, bool ignoneDefault = false)
        {
            var fields = new List<TField>();
            if (l is IList<TItem> list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var v = func(list[i]);
                    if (ignoneDefault && Equals(default(TField), v))
                    {
                        continue;
                    }

                    fields.Add(v);
                }
            }
            else
            {
                foreach (var f in l)
                {
                    var v = func(f);
                    if (ignoneDefault && Equals(default(TField), v))
                    {
                        continue;
                    }

                    fields.Add(v);
                }
            }

            return fields;
        }
    }
}