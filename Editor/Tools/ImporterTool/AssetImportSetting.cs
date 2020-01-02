using System.Collections.Generic;
using UnityEngine;

namespace UTools
{
    public class AssetImportSetting : ScriptableObject
    {
        public List<TexFormatController> texFormatControllers = new List<TexFormatController>();
    }
}