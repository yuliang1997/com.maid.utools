using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UTools.Utility
{
    internal static class UTGUI
    {
        internal static T Enum<T>(string label, T value, bool disable = false) where T : Enum
        {
            if (disable)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            Enum result = EditorGUILayout.EnumPopup(label, value);

            if (disable)
            {
                EditorGUI.EndDisabledGroup();
            }

            return (T) result;
        }

        internal static bool Toggle(string label, bool value, bool disable = false)
        {
            if (disable)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            bool result = EditorGUILayout.Toggle(label, value);

            if (disable)
            {
                EditorGUI.EndDisabledGroup();
            }

            return result;
        }

        internal static string Text(string label, string value, bool disable = false)
        {
            if (disable)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            string result = EditorGUILayout.TextField(label, value);
            if (disable)
            {
                EditorGUI.EndDisabledGroup();
            }

            return result;
        }

        internal static int Int(string label, int value, bool disable = false)
        {
            if (disable)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            int result = EditorGUILayout.IntField(label, value);
            if (disable)
            {
                EditorGUI.EndDisabledGroup();
            }

            return result;
        }

        internal static T EnumToolbar<T>(T selected) where T : Enum
        {
            var enumType = typeof(T);
            var names = System.Enum.GetNames(enumType);
            var values = System.Enum.GetValues(enumType);
            var index = Array.IndexOf(values, selected);
            var selectedIndex = GUILayout.Toolbar(index, names);
            var value = (T) System.Enum.Parse(enumType, names[selectedIndex]);
            return value;
        }

        internal static Object[] HandleDrag(DragAndDropVisualMode visualMode = DragAndDropVisualMode.Link)
        {
            var dragArea = GUILayoutUtility.GetLastRect();
            var ec = Event.current;
            switch (ec.type)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                    {
                        HandleUtility.Repaint();
                    }

                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (dragArea.Contains(ec.mousePosition))
                    {
                        DragAndDrop.visualMode = visualMode;

                        if (ec.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            DragAndDrop.activeControlID = 0;
                            return DragAndDrop.objectReferences;
                        }

                        Event.current.Use();
                    }

                    break;
            }

            return null;
        }
    }
}