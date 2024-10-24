﻿using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace wackydatabase.Datas
{
    [Serializable]
    [CanBeNull]
    public class PickableData
    {
        public string name;
        public string itemPrefab;
        public string? cloneOfWhatPickable;
        public string? material;
        public int? amount;
        //public int? minAmountScaled;
        public string? size;
        public string? overrideName;
        public float? respawnTimer;
        public float? spawnOffset;
        public float? ifHasHealth;
        public string? hiddenChildWhenPicked;
        public ExtraDrops? extraDrops;
       // public bool? enable;

    }
    [Serializable]
    [CanBeNull]
    public class TreeBaseData
    {
        public string name;
        public float treeHealth;
        public string? cloneOfWhatTree;
        public string? material;
        public string? size;
        public int? minToolTier;
    }

    [Serializable]
    [CanBeNull]
    public class ExtraDrops
    {
        public List<string> drops;
        public int? dropMin;
        public int? dropMax;
        public float? dropChance;
        public bool? dropOneOfEach;
    }
}
