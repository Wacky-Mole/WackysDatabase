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
        public static HitData.DamageTypes ParseDamageTypes(wackydatabase.Datas.WDamages dmg)
        {
            HitData.DamageTypes damages = default(HitData.DamageTypes);

            damages.m_blunt = dmg.Blunt;
            damages.m_chop = dmg.Chop;
            damages.m_damage = dmg.Damage;
            damages.m_fire = dmg.Fire;
            damages.m_frost = dmg.Frost;
            damages.m_lightning = dmg.Lightning;
            damages.m_pickaxe = dmg.Pickaxe;
            damages.m_pierce = dmg.Pierce;
            damages.m_poison = dmg.Poison;
            damages.m_slash = dmg.Slash;
            damages.m_spirit = dmg.Spirit;
            
            return damages;
        }
    }
}
