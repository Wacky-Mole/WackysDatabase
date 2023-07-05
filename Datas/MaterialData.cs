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
        public MaterialData Changes = new MaterialData();
    }

    [Serializable]
    public class MaterialData
    {
        public Dictionary<string, Color> Colors = new Dictionary<string, Color>();
        public Dictionary<string, float> Floats = new Dictionary<string, float>();
        public Dictionary<string, string> Textures = new Dictionary<string, string>();
    }
}
