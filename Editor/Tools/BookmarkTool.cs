using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UTools.Utility;
using Object = UnityEngine.Object;

internal class BookmarkTool : EditorWindow
{
    private List<Bookmark> bookmarks = new List<Bookmark>();
    private List<History> histories = new List<History>();

    private Vector2 scrollPosHistory;
    private Vector2 scrollPosBookmark;

    private Tab curTab;

    private void OnEnable()
    {
        readBookmarks();
        readHistories();
        PrefabStage.prefabStageOpened += prefabStageOnPrefabStageOpened;
        Selection.selectionChanged += eventSelectionChange;
    }

    private void OnDisable()
    {
        saveBookmarks();
        saveHistories();
        PrefabStage.prefabStageOpened -= prefabStageOnPrefabStageOpened;
        Selection.selectionChanged -= eventSelectionChange;
    }

    private void OnDestroy()
    {
        saveBookmarks();
        saveHistories();
    }


    private void OnGUI()
    {
        curTab = UTGUI.EnumToolbar(curTab);
        switch (curTab)
        {
            case Tab.All:
                drawHistory(position.height / 2);
                drawBookmark();
                break;
            case Tab.Bookmark:
                drawBookmark();
                break;
            case Tab.History:
                drawHistory();
                break;
        }

        void drawHistory(float height = -1)
        {
            scrollPosHistory = height >= 0
                ? EditorGUILayout.BeginScrollView(scrollPosHistory, false, false, GUILayout.Height(height))
                : EditorGUILayout.BeginScrollView(scrollPosHistory, false, false);

            var removeCount = histories.RemoveAll(v => !v.asset);
            if (removeCount > 0)
            {
                saveHistories();
            }

            for (var i = histories.Count - 1; i >= 0; i--)
            {
                var v = histories[i];
                EditorGUILayout.ObjectField(v.asset, v.AssetType, false);
                GUILayout.Space(1.5f);
            }

            EditorGUILayout.EndScrollView();
        }

        void drawBookmark(float height = -1)
        {
            EditorGUILayout.BeginVertical("box");
            scrollPosBookmark = height >= 0
                ? EditorGUILayout.BeginScrollView(scrollPosBookmark, false, false, GUILayout.Height(height))
                : EditorGUILayout.BeginScrollView(scrollPosBookmark, false, false);

            for (var i = 0; i < bookmarks.Count; i++)
            {
                var v = bookmarks[i];
                if (v.path.IsNOE())
                {
                    bookmarks.RemoveAt(i);
                    saveBookmarks();
                    i--;
                    continue;
                }

                drawPath(v, i);
            }

            EditorGUILayout.BeginVertical("box", GUILayout.Height(30f));
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Drag folder here to add bookmark");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10f);
            EditorGUILayout.EndVertical();
            var dragObjs = UTGUI.HandleDrag();
            if (dragObjs != null && dragObjs.Length > 0)
            {
                foreach (var obj in dragObjs)
                {
                    if (obj != null && obj is DefaultAsset)
                    {
                        var path = AssetDatabase.GetAssetPath(obj);
                        if (!bookmarks.Exists(v => v.path == path))
                        {
                            bookmarks.Add(new Bookmark(path));
                            saveBookmarks();
                            Repaint();
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void drawPath(Bookmark bookmark, int index)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(4f);
            bookmark.path = EditorGUILayout.TextField(bookmark.path);
            GUILayout.EndVertical();

            if (GUILayout.Button(bookmark.name))
            {
                UToolsUtil.HighlightProjectPath(bookmark.path);
            }

            GUILayout.EndHorizontal();
            var dragObjs = UTGUI.HandleDrag(DragAndDropVisualMode.Copy);
            if (dragObjs != null && dragObjs.Length > 0)
            {
                foreach (var obj in dragObjs)
                {
                    if (obj.IsAsset())
                    {
                        var assetPath = AssetDatabase.GetAssetPath(obj);
                        var assetName = Path.GetFileName(assetPath);
                        var newPath = Path.Combine(bookmark.path, assetName);
                        if (newPath == assetPath)
                        {
                            return;
                        }

                        var error = AssetDatabase.MoveAsset(assetPath, newPath);
                        if (error.IsNNOE())
                        {
                            Debug.LogError(error);
                        }
                    }
                }
            }
        }
    }

    private void prefabStageOnPrefabStageOpened(PrefabStage obj)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(obj.prefabAssetPath);
        var last = histories.LastOrDefault();
        if (last != null && last.asset == prefab)
        {
            return;
        }

        addHistory(prefab);
    }

    private void eventSelectionChange() => addHistory(Selection.activeObject);

    private void addHistory(Object asset)
    {
        if (!asset)
        {
            return;
        }

        if (!AssetDatabase.Contains(asset))
        {
            return;
        }

        var last = histories.LastOrDefault();
        if (last != null && last.asset == asset)
        {
            return;
        }

        if (histories.Count > 20)
        {
            histories.RemoveAt(0);
        }

        histories.Add(new History(asset));
        Repaint();

        saveHistories();
    }

    private void readBookmarks()
    {
        var bookmarkString = EditorPrefs.GetString("bookMarkMaid_bookmarks");
        if (bookmarkString.IsNOE())
        {
            return;
        }

        bookmarks.Clear();
        bookmarks.AddRange(bookmarkString.Split('|').Select(v => new Bookmark(v)));
    }

    private void saveBookmarks() => EditorPrefs.SetString("bookMarkMaid_bookmarks", string.Join("|", bookmarks.Select(v => v.path)));

    private void readHistories()
    {
        var historiesString = EditorPrefs.GetString("bookMarkMaid_histories");
        if (historiesString.IsNOE())
        {
            return;
        }

        histories.Clear();
        histories.AddRange(historiesString.Split('|').Select(v => new History(v)));
    }

    private void saveHistories() => EditorPrefs.SetString("bookMarkMaid_histories", string.Join("|", histories.Select(v => v.guid)));

    private enum Tab
    {
        All,
        Bookmark,
        History,
    }

    [Serializable]
    internal class Bookmark
    {
        public string path;
        public string name;

        internal Bookmark(string path)
        {
            this.path = path;
            name = Path.GetFileName(path);
        }
    }

    [Serializable]
    internal class History
    {
        public string guid;
        public string fileName;
        public Object asset;

        [NonSerialized] private Type assetType;

        public Type AssetType
        {
            get
            {
                if (assetType == null)
                {
                    if (!asset)
                    {
                        return null;
                    }

                    assetType = asset.GetType();
                }

                return assetType;
            }
        }

        internal History(Object asset)
        {
            this.asset = asset;
            guid = UToolsUtil.GetAssetGUID(asset);
            fileName = asset.name;
        }

        internal History(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tempAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (tempAsset != null)
            {
                this.guid = guid;
                asset = tempAsset;
                fileName = asset.name;
            }
        }
    }
}