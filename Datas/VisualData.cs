using System;
using System.Collections.Generic;
using UnityEngine;

namespace wackydatabase.Datas
{
    [Serializable]
    public class VisualData
    {
        public string PrefabName;
        public string Material = null;
        public string Chest;
        public string Legs;
        public string[] Materials = null;
    }
}