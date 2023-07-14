using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wackydatabase.Datas
{

    [Serializable]
    [CanBeNull]
    public class CreatureData
    {
        public string name;
        public string? mob_display_name;
        public string? custom_material;
        public string? clone_creature;
        public string? creature_replacer;
        //public Humanoid.Faction? faction;
    }
}
