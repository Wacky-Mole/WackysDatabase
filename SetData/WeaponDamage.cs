using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wackydatabase.Util;

namespace wackydatabase.SetData
{
    internal class WeaponDamage
    {
        public static HitData.DamageTypes ParseDamageTypes(string[] values)
        {
            HitData.DamageTypes damages = default(HitData.DamageTypes);

            damages.m_blunt = Functions.stringtoFloat(values[0]);
            damages.m_chop = Functions.stringtoFloat(values[1]);
            damages.m_damage = Functions.stringtoFloat(values[2]);
            damages.m_fire = Functions.stringtoFloat(values[3]);
            damages.m_frost = Functions.stringtoFloat(values[4]);
            damages.m_lightning = Functions.stringtoFloat(values[5]);
            damages.m_pickaxe = Functions.stringtoFloat(values[6]);
            damages.m_pierce = Functions.stringtoFloat(values[7]);
            damages.m_poison = Functions.stringtoFloat(values[8]);
            damages.m_slash = Functions.stringtoFloat(values[9]);
            damages.m_spirit = Functions.stringtoFloat(values[10]);
            
            return damages;
        }
    }
}
