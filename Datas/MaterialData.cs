using System;
using System.Collections.Generic;
using UnityEngine;

namespace wackydatabase.Datas
{
    [Serializable]
    public class MaterialInstance
    {
        public string Name;
        public string Original;
        public bool Overwrite = false;
        public MaterialData Changes;
    }

    [Serializable]
    public class MaterialData
    {
        public Dictionary<string, Color> Colors;
        public Dictionary<string, float> Floats;
        public Dictionary<string, string> Textures;
    }
}
