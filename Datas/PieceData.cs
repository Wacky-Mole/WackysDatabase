using System;
using UnityEngine;
using System.Collections.Generic;

namespace wackydatabase.Datas
{
    [Serializable]
    public class PieceData
    {
        public string name; //must have
        public string m_name;
        public string m_description;
        public bool clone;
        public string clonePrefabName;
        public string customIcon;
        //public string cloneEffects;
        public string cloneMaterial;
        public string craftingStation;
        public string piecehammer; // must have
        public string piecehammerCategory;
        public int minStationLevel;
        public int amount;
        public bool disabled;
        public bool adminonly;
        public List<string> reqs = new List<string>();
    }




}