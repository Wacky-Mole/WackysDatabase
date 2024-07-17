using JetBrains.Annotations;
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
       // public bool? enable;

    }
    [Serializable]
    [CanBeNull]
    public class TreeBaseData
    {
        public string name;
        public float treeTealth;
        public string? cloneOfWhatTree;
        public string? material;
        public string? size;
    }
}
