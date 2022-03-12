using System.Collections.Generic;

namespace recipecustomization
{
    internal class RecipeData
    {
        public string name;
        public bool clone;
        public string clonePrefabName;
        public string cloneEffects;
        public string cloneColor;
        public string craftingStation;
        public string piecehammer;
        public int minStationLevel;
        public int amount;
        public bool disabled;
        public List<string> reqs = new List<string>();
    }

    internal class PieceData
    {
        public string name;
        public bool clone;
        public string clonePrefabName;
        public string cloneEffects;
        public string cloneColor;
        public string craftingStation;
        public string piecehammer;
        public int minStationLevel;
        public int amount;
        public bool disabled;
        public List<string> reqs = new List<string>();
    }




}