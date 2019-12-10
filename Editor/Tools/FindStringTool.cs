using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UTools.Utility;

internal class FindStringTool : EditorWindow
{
    private string findValue;
    [SerializeField] private List<GameObject> resultList = new List<GameObject>();
    private int curIndex;

    private void OnGUI()
    {
        findValue = EditorGUILayout.TextField("Value", findValue);
        if (GUILayout.Button("Find"))
        {
            resultList.Clear();

            var comps = Resources.FindObjectsOfTypeAll<Component>();
            foreach (var v in comps)
            {
                if (!v.gameObject.scene.IsValid())
                {
                    continue;
                }

                var texts = v.FindMemberValues<string>();
                foreach (var text in texts)
                {
                    if (text.ToLower() == findValue.ToLower() ||
                        text.ToLower().Contains(findValue.ToLower())
                    )
                    {
                        resultList.Add(v.gameObject);
                        break;
                    }
                }
            }

            if (resultList.Count == 1)
            {
                curIndex = 0;
                FocusObj();
            }
        }

        EditorGUILayout.LabelField($"Index:{curIndex}  ResultCount:{resultList.Count}");

        EditorGUILayout.BeginHorizontal();
        if (resultList.Count > 0)
        {
            if (GUILayout.Button("First"))
            {
                curIndex = 0;
                FocusObj();
            }

            if (GUILayout.Button("Last"))
            {
                curIndex = resultList.Count - 1;
                FocusObj();
            }
        }

        if (curIndex > 0)
        {
            if (GUILayout.Button("Prev"))
            {
                curIndex--;
                FocusObj();
            }
        }

        if (curIndex < resultList.Count - 1)
        {
            if (GUILayout.Button("Next"))
            {
                curIndex++;
                FocusObj();
            }
        }

        if (GUILayout.Button("Focus"))
        {
            FocusObj();
            Selection.activeGameObject = resultList[curIndex];
        }

        EditorGUILayout.EndHorizontal();
    }

    private void FocusObj()
    {
        var obj = resultList.GetValueOrDefault(curIndex);
        EditorGUIUtility.PingObject(obj);
    }
}