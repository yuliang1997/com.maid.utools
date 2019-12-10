using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class UToolsSetting
{
    internal static string packagePath => Path.Combine(UToolsUtil.DataPath, "../Packages/com.maid.utools");
    internal static string dataPath => Path.Combine(UToolsUtil.DataPath, "../Packages/Datas/UTools");
}