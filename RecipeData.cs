using System.Collections.Generic;

namespace wackydatabase
{
    internal class RecipeData
    {
        public string name;
        public bool clone;
        public string clonePrefabName;
        //public string cloneColor;
        public string craftingStation;
        public int minStationLevel;
        public int amount;
        public bool disabled;
        public List<string> reqs = new List<string>();
    }

    internal class PieceData
    {
        public string name;
        public string m_name;
        public string m_description;
        public bool clone;
        public string clonePrefabName;
        //public string cloneEffects;
        public string cloneMaterial;
        public string craftingStation;
        public string piecehammer;
        public string piecehammerCategory;
        public int minStationLevel;
        public int amount;
        public bool disabled;
        public bool adminonly;
        public List<string> reqs = new List<string>();
    }




}