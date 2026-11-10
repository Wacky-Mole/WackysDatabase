using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using wackydatabase;
using wackydatabase.Datas;
using wackydatabase.SetData;

namespace API;

[PublicAPI]
public class WackyDatabase_API
{

    private static readonly bool _IsInstalled;
    private static MethodInfo eAddBlacklistClone;
    private static MethodInfo eGetClonedMap;

    public static bool IsInstalled() => _IsInstalled;
    public static void AddBlacklistClone(string value)
    {
        eAddBlacklistClone?.Invoke(null, new object[] { value });
    }
    public static string GetClonedMap(string maybeclone)
    {
        string result ="";
        if (eGetClonedMap != null) result =  (string)eGetClonedMap.Invoke(null, new object[] { maybeclone });
        return result;
    }

    static WackyDatabase_API()
    {
        if (Type.GetType("API.WackyAPI, WackysDatabase") == null)
            {
            _IsInstalled = false;
            return;
            }
        Type wackydatabaseAPI = Type.GetType("API.WackyAPI, WackysDatabase");
        _IsInstalled = true;
        eAddBlacklistClone = wackydatabaseAPI.GetMethod("AddBlacklistClone", BindingFlags.Public | BindingFlags.Static);
        eGetClonedMap = wackydatabaseAPI.GetMethod("GetClonedMap", BindingFlags.Public | BindingFlags.Static);
    }
}

// don't use
public static class WackyAPI
{
   public static void AddBlacklistClone(string value)
    {
        //WMRecipeCust.WLog.LogInfo("Added to blacklist "+ value);

        WMRecipeCust.AddBlacklistClone(value);
    }

    public static string GetClonedMap(string maybeclone)
    {
        if (WMRecipeCust.ClonedPrefabsMap.ContainsKey(maybeclone))
        {
            return WMRecipeCust.ClonedPrefabsMap[maybeclone];
        }
        else
        {
            return "";
        }
    }

}

public static class SecondaryAttackDamage
{
    public const string PluginGuid = "WackyMole.WackysSecondaryAttacks";
    public static bool ProviderAvailable { get; set; }

    private static ConditionalWeakTable<Attack, Definition> Definitions = new();
    private static readonly Dictionary<string, Definition> PrefabDefinitions = new(StringComparer.Ordinal);

    internal static void Reset()
    {
        Definitions = new ConditionalWeakTable<Attack, Definition>();
        PrefabDefinitions.Clear();
    }

    public sealed class Definition
    {
        public WDamages? Damage { get; }
        public WDamages? DamagePerLevel { get; }
        public HitData.DamageTypes? BaseDamage { get; }
        public HitData.DamageTypes? PerLevelDamage { get; }

        internal Definition(WDamages? damage, WDamages? damagePerLevel)
        {
            Damage = damage;
            DamagePerLevel = damagePerLevel;
            BaseDamage = damage == null ? null : WeaponDamage.ParseDamageTypes(damage);
            PerLevelDamage = damagePerLevel == null ? null : WeaponDamage.ParseDamageTypes(damagePerLevel);
        }
    }

    public static bool TryGet(Attack? attack, out Definition? definition)
    {
        definition = null;
        return attack != null && Definitions.TryGetValue(attack, out definition);
    }

    public static bool TryGet(ItemDrop.ItemData item, out Definition? definition)
    {
        if (item.m_dropPrefab != null && PrefabDefinitions.TryGetValue(item.m_dropPrefab.name, out definition))
            return true;
        return TryGet(item.m_shared.m_secondaryAttack, out definition);
    }

    internal static void Set(Attack? attack, AttackArm? data, string itemName)
    {
        PrefabDefinitions.Remove(itemName);
        if (attack == null)
            return;

        Definitions.Remove(attack);
        var damage = data?.Damage;
        var damagePerLevel = data?.Damage_Per_Level;
        if (damage == null && damagePerLevel == null)
            return;

        var definition = new Definition(damage, damagePerLevel);
        Definitions.Add(attack, definition);
        PrefabDefinitions.Add(itemName, definition);
        if (!ProviderAvailable)
            WMRecipeCust.WLog.LogWarning($"Item '{itemName}': secondary damage requires the WackysSecondaryAttacks companion plugin.");
    }
}
