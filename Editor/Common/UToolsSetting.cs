using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class UToolsSetting
{
    internal static string dataPath => Path.Combine(UToolsUtil.DataPath, "../Packages/UTools/Data");
    internal static string depPath => Path.Combine(UToolsUtil.DataPath, "../Packages/UTools/Deps");

    static UToolsSetting()
    {
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        if (!Directory.Exists(depPath))
        {
            Directory.CreateDirectory(depPath);
        }
    }
}