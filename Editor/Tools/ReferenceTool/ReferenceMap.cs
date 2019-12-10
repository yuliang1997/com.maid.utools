using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UTools.Utility;

[Serializable]
internal class MapPair
{
    internal string key;
    internal List<string> values;

    internal MapPair(string key, List<string> values)
    {
        this.key = key;
        this.values = values;
    }
}

[Serializable]
internal struct ReferenceMapData
{
    internal List<MapPair> pairs;
}

internal class ReferenceMap
{
    internal Dictionary<string, List<string>> mapDic = new Dictionary<string, List<string>>();
    internal string dataPath;

    private static readonly object lockObj = new object();

    internal ReferenceMap(string path)
    {
        loadFromPath(path);
    }

    internal void loadFromPath(string path)
    {
        dataPath = path;
        if (File.Exists(dataPath))
        {
            ReferenceMapData data = JsonUtility.FromJson<ReferenceMapData>(File.ReadAllText(dataPath));
            mapDic = data.pairs.ToDictionary(v => v.key, v => v.values);
        }
    }

    internal void clear()
    {
        mapDic.Clear();
    }

    internal void remove(string key)
    {
        lock (lockObj)
        {
            mapDic.Remove(key);
        }
    }

    internal void update(string key, List<string> values)
    {
        lock (lockObj)
        {
            mapDic[key] = values;
        }
    }

    internal void update(Dictionary<string, List<string>> mapDic)
    {
        this.mapDic = mapDic;
    }

    internal void writeToDisk()
    {
        ReferenceMapData data = new ReferenceMapData();
        data.pairs = mapDic.SelectL(v => new MapPair(v.Key, v.Value));

        foreach (var v in data.pairs)
        {
            v.values = v.values.Distinct().ToList();
        }

        File.WriteAllText(dataPath, JsonUtility.ToJson(data));
    }
}