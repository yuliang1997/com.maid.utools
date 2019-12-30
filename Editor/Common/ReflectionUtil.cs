using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace UTools.Utility
{
    internal static class ReflectionUtil
    {
        internal static BindingFlags bindingFlag = BindingFlags.NonPublic |
                                                   BindingFlags.Instance |
                                                   BindingFlags.Static |
                                                   BindingFlags.Public;

        internal static object GetMemberValue(string typeName, string fieldName) => GetMemberValue(GetTypeByName(typeName), fieldName);

        internal static object GetMemberValue(this Type type, string fieldName) => GetMemberValue(type, null, fieldName);

        internal static object GetMemberValue(this object obj, string fieldName) => GetMemberValue(obj.GetType(), obj, fieldName);

        internal static List<T> FindMemberValues<T>(this object obj)
        {
            var result = new List<T>();
            var objType = obj.GetType();
            var fields = objType.GetFields(bindingFlag);
            foreach (var v in fields)
            {
                if (v.FieldType == typeof(T))
                {
                    result.Add((T) v.GetValue(obj));
                }
            }

            var properties = objType.GetProperties(bindingFlag);
            foreach (var v in properties)
            {
                if (v.PropertyType == typeof(T))
                {
                    result.Add((T) v.GetValue(obj));
                }
            }

            return result;
        }

        internal static void SetMemberValue(string typeName, string fieldName, object value) =>
            SetMemberValue(GetTypeByName(typeName), fieldName, value);

        internal static void SetMemberValue(this Type type, string fieldName, object value) => SetMemberValue(type, null, fieldName, value);

        internal static void SetMemberValue(this object obj, string fieldName, object value) =>
            SetMemberValue(obj.GetType(), obj, fieldName, value);

        internal static object CallStatic(string typeName, string methodName, params object[] args)
        {
            var type = GetTypeByName(typeName);
            return CallStatic(type, methodName, args);
        }

        internal static object CallStatic(this Type type, string methodName, params object[] args) =>
            CallMethod(type, null, methodName, args);

        internal static object Call(this object obj, string methodName, params object[] args) =>
            CallMethod(obj.GetType(), obj, methodName, args);

        internal static Type GetTypeByName(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var v in assemblies)
            {
                var type = v.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        internal static EditorWindow GetEditorWindow(string typeName)
        {
            var type = GetTypeByName(typeName);
            var wnd = EditorWindow.GetWindow(type, false, null, false);
            return wnd;
        }
#endif

        private static object GetMemberValue(Type type, object obj, string memberName)
        {
            if (type == null)
            {
                return null;
            }

            var field = type.GetField(memberName, bindingFlag);
            if (field != null)
            {
                return field.GetValue(obj);
            }

            var property = type.GetProperty(memberName, bindingFlag);
            if (property != null)
            {
                return property.GetValue(obj);
            }

            return GetMemberValue(type.BaseType, obj, memberName);
        }

        private static void SetMemberValue(Type type, object obj, string memberName, object value)
        {
            if (type == null)
            {
                return;
            }

            var field = type.GetField(memberName, bindingFlag);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                var property = type.GetProperty(memberName, bindingFlag);
                if (property != null)
                {
                    property.SetValue(obj, value);
                }
            }
        }

        private static object CallMethod(Type type, object obj, string methodName, params object[] args)
        {
            if (type == null)
            {
                return null;
            }

            var methods = type.GetMethods(bindingFlag);

            if (methods.Length == 1)
            {
                var method = methods[0];
                return method.Invoke(obj, args);
            }
            else
            {
                foreach (var m in methods)
                {
                    if (m.Name == methodName && m.GetParameters().Length == args.Length)
                    {
                        return m.Invoke(obj, args);
                    }
                }
            }

            return null;
        }
    }
}