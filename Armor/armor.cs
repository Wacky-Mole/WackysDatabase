using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using wackydatabase.Datas;

namespace wackydatabase.Armor
{
  public class ArmorHelpers 
    {

     private static bool ShouldOverride(HitData.DamageModifier a, HitData.DamageModifier b)
    {
        return a != HitData.DamageModifier.Ignore && (b == HitData.DamageModifier.Immune || ((a != HitData.DamageModifier.VeryResistant || b != HitData.DamageModifier.Resistant) && (a != HitData.DamageModifier.VeryWeak || b != HitData.DamageModifier.Weak)));
    }

    private static void LoadAllArmorData(ZNetScene scene)
    {
        foreach (var armor in armorDatas)
        {
            GameObject go = scene.GetPrefab(armor.name);
            if (go == null)
                continue;
            ItemDrop.ItemData item = go.GetComponent<ItemDrop>().m_itemData;
            SetArmorData(ref item, armor);
            go.GetComponent<ItemDrop>().m_itemData = item;
        }
    }

    private static void CheckArmorData(ref ItemDrop.ItemData instance)
    {
        try
        {
            var name = instance.m_dropPrefab.name;
            var armor = armorDatas.First(d => d.name == name);
            SetArmorData(ref instance, armor);
            //Dbgl($"Set armor data for {instance.name}");
        }
        catch
        {

        }
    }

    private static void SetArmorData(ref ItemDrop.ItemData item, ArmorData armor)
    {
        //item.m_shared.m_armor = armor.armor;
        //item.m_shared.m_armorPerLevel = armor.armorPerLevel;
        // item.m_shared.m_movementModifier = armor.movementModifier;

        item.m_shared.m_damageModifiers.Clear();
        foreach (string modString in armor.damageModifiers)
        {
            string[] mod = modString.Split(':');
            int modType = Enum.TryParse<NewDamageTypes>(mod[0], out NewDamageTypes result) ? (int)result : (int)Enum.Parse(typeof(HitData.DamageType), mod[0]);
            item.m_shared.m_damageModifiers.Add(new HitData.DamageModPair() { m_type = (HitData.DamageType)modType, m_modifier = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), mod[1]) });
        }
    }

    private static HitData.DamageModifier GetNewDamageTypeMod(NewDamageTypes type, Character character)
    {
        Traverse t = Traverse.Create(character);
        return GetNewDamageTypeMod(type, t.Field("m_chestItem").GetValue<ItemDrop.ItemData>(), t.Field("m_legItem").GetValue<ItemDrop.ItemData>(), t.Field("m_helmetItem").GetValue<ItemDrop.ItemData>(), t.Field("m_shoulderItem").GetValue<ItemDrop.ItemData>());
    }

    private static HitData.DamageModifier GetNewDamageTypeMod(NewDamageTypes type, ItemDrop.ItemData chestItem, ItemDrop.ItemData legItem, ItemDrop.ItemData helmetItem, ItemDrop.ItemData shoulderItem)
    {
        HitData.DamageModPair modPair = new HitData.DamageModPair();

        if (chestItem != null)
            modPair = chestItem.m_shared.m_damageModifiers.FirstOrDefault(s => (int)s.m_type == (int)type);

        if (legItem != null)
        {
            var leg = legItem.m_shared.m_damageModifiers.FirstOrDefault(s => (int)s.m_type == (int)type);
            if (ShouldOverride(modPair.m_modifier, leg.m_modifier))
                modPair = leg;
        }
        if (helmetItem != null)
        {
            var helm = helmetItem.m_shared.m_damageModifiers.FirstOrDefault(s => (int)s.m_type == (int)type);
            if (ShouldOverride(modPair.m_modifier, helm.m_modifier))
                modPair = helm;
        }
        if (shoulderItem != null)
        {
            var shoulder = shoulderItem.m_shared.m_damageModifiers.FirstOrDefault(s => (int)s.m_type == (int)type);
            if (ShouldOverride(modPair.m_modifier, shoulder.m_modifier))
                modPair = shoulder;
        }
        return modPair.m_modifier;
    }
}
}
