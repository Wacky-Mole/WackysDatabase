using System;
using System.Collections.Generic;
using UnityEngine;

namespace wackydatabase.Datas
{
    [Serializable]
    public class MaterialInstance
    {
        public string name;
        public string original;
        public bool overwrite = false;
        public MaterialData changes = new MaterialData();
    }

    [Serializable]
    public class MaterialData
    {
        public Dictionary<string, Color> colors = new Dictionary<string, Color>();
        public Dictionary<string, float> floats = new Dictionary<string, float>();
        public Dictionary<string, string> textures = new Dictionary<string, string>();
    }
}
