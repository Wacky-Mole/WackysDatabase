using System;
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace wackydatabase.Datas
{
    [Serializable]
    [CanBeNull]
    public class RecipeData
    {
        public string? name; //must have
        public string? clonePrefabName;
        //public string cloneColor;
        public string? craftingStation;
        public int? minStationLevel;
        public int? maxStationLevelCap;
        public string? repairStation;
        public int? amount;
        public bool? disabled;
        public List<string>? reqs = new List<string>(); // must have

    }


}