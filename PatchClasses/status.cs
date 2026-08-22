using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using wackydatabase.Armor;
using wackydatabase.Datas;
using wackydatabase.SetData;
using wackydatabase.Util;
using static wackydatabase.Armor.ArmorHelpers;

namespace wackydatabase.PatchClasses
{

    [HarmonyPatch(typeof(Humanoid), "UpdateEquipmentStatusEffects")]
    static class UpdateEquipmentStatusEffects_Patch
    {
        private sealed class AdditionalSetEffectState
        {
            internal readonly HashSet<int> ActiveEffectHashes = new();
        }

        private sealed class AdditionalSetTooltipCache
        {
            internal List<SE_SET_Equip> Effects;
            internal int EquipmentStateHash;
            internal string Tooltip;
        }

        private sealed class AdditionalSetEffectGroup
        {
            internal int RequiredCount;
            internal readonly HashSet<ItemDrop.ItemData> Items = new();
            internal readonly HashSet<string> EffectNames = new(StringComparer.Ordinal);
        }

        private static readonly ConditionalWeakTable<Humanoid, AdditionalSetEffectState> AdditionalSetEffectStates = new();
        private static readonly ConditionalWeakTable<ItemDrop.ItemData, AdditionalSetTooltipCache> AdditionalSetTooltipCaches = new();
        private static readonly FieldInfo[] EquipmentItemFields =
        {
            AccessTools.Field(typeof(Humanoid), "m_chestItem"),
            AccessTools.Field(typeof(Humanoid), "m_legItem"),
            AccessTools.Field(typeof(Humanoid), "m_helmetItem"),
            AccessTools.Field(typeof(Humanoid), "m_shoulderItem")
        };

        private sealed class SuppressedEffects
        {
            internal ItemDrop.ItemData.SharedData SharedData;
            internal StatusEffect EquipStatusEffect;
            internal StatusEffect SetStatusEffect;
        }

        private static bool IsSetComplete(string setName, Player player)
        {
            var setItemCount = 0;
            var requiredCount = 0;
            foreach (var field in EquipmentItemFields)
            {
                var item = field?.GetValue(player) as ItemDrop.ItemData;
                if (item?.m_shared == null || item.m_shared.m_setName != setName)
                    continue;

                setItemCount++;
                requiredCount = Math.Max(requiredCount, item.m_shared.m_setSize);
            }

            return requiredCount > 0 && setItemCount >= requiredCount;
        }

        private static int GetEquipmentStateHash(Player player)
        {
            if (player == null)
                return 0;

            unchecked
            {
                var hash = 17;
                foreach (var field in EquipmentItemFields)
                {
                    var item = field?.GetValue(player) as ItemDrop.ItemData;
                    hash = (hash * 31) + (item == null ? 0 : RuntimeHelpers.GetHashCode(item));
                }

                return hash;
            }
        }

        static void Prefix(Humanoid __instance, ItemDrop.ItemData ___m_chestItem, ItemDrop.ItemData ___m_legItem, ItemDrop.ItemData ___m_helmetItem, ItemDrop.ItemData ___m_shoulderItem, ref List<SuppressedEffects> __state)
        {
            if (!__instance.IsPlayer() || !WMRecipeCust.modEnabled.Value || WMRecipeCust.HideEquipEffectsUntilSetComplete.Count == 0)
                return;

            var equippedItems = new[] { ___m_chestItem, ___m_legItem, ___m_helmetItem, ___m_shoulderItem }
                .Where(item => item?.m_shared != null && !string.IsNullOrEmpty(item.m_shared.m_setName))
                .ToList();

            foreach (var set in equippedItems.GroupBy(item => item.m_shared.m_setName))
            {
                if (!WMRecipeCust.HideEquipEffectsUntilSetComplete.Contains(set.Key))
                    continue;

                int requiredCount = set.Max(item => item.m_shared.m_setSize);
                if (requiredCount <= 0 || set.Count() >= requiredCount)
                    continue;

                __state ??= new List<SuppressedEffects>();
                foreach (var item in set)
                {
                    var sharedData = item.m_shared;
                    __state.Add(new SuppressedEffects
                    {
                        SharedData = sharedData,
                        EquipStatusEffect = sharedData.m_equipStatusEffect,
                        SetStatusEffect = sharedData.m_setStatusEffect
                    });
                    sharedData.m_equipStatusEffect = null;
                    sharedData.m_setStatusEffect = null;
                }
            }
        }

        static void Postfix(Humanoid __instance, ItemDrop.ItemData ___m_chestItem, ItemDrop.ItemData ___m_legItem, ItemDrop.ItemData ___m_helmetItem, ItemDrop.ItemData ___m_shoulderItem)
        {
            if (!__instance.IsPlayer())
                return;

            var state = AdditionalSetEffectStates.GetValue(__instance, _ => new AdditionalSetEffectState());
            var desiredEffectHashes = new HashSet<int>();

            if (WMRecipeCust.modEnabled.Value && WMRecipeCust.AdditionalSetEffects.Count > 0)
            {
                var groups = new Dictionary<string, AdditionalSetEffectGroup>(StringComparer.Ordinal);
                foreach (var item in new[] { ___m_chestItem, ___m_legItem, ___m_helmetItem, ___m_shoulderItem })
                {
                    var prefabName = item?.m_dropPrefab?.name;
                    if (string.IsNullOrEmpty(prefabName) || !WMRecipeCust.AdditionalSetEffects.TryGetValue(prefabName, out var effects))
                        continue;

                    foreach (var effect in effects)
                    {
                        if (!groups.TryGetValue(effect.SetName, out var group))
                        {
                            group = new AdditionalSetEffectGroup();
                            groups.Add(effect.SetName, group);
                        }

                        group.RequiredCount = Math.Max(group.RequiredCount, effect.Size ?? 0);
                        group.Items.Add(item);
                        group.EffectNames.Add(effect.EffectName);
                    }
                }

                foreach (var group in groups.Values.Where(group => group.RequiredCount > 0 && group.Items.Count >= group.RequiredCount))
                {
                    foreach (var effectName in group.EffectNames)
                    {
                        if (ObjectDB.instance?.GetStatusEffect(effectName.GetStableHashCode()) != null)
                            desiredEffectHashes.Add(effectName.GetStableHashCode());
                    }
                }
            }

            var seMan = __instance.GetSEMan();
            foreach (var effectHash in state.ActiveEffectHashes.Except(desiredEffectHashes).ToList())
            {
                var statusEffect = seMan.GetStatusEffect(effectHash);
                if (statusEffect != null)
                    seMan.RemoveStatusEffect(statusEffect, true);
            }
            foreach (var effectHash in desiredEffectHashes.Except(state.ActiveEffectHashes))
                seMan.AddStatusEffect(effectHash);

            state.ActiveEffectHashes.Clear();
            state.ActiveEffectHashes.UnionWith(desiredEffectHashes);
        }

        static Exception Finalizer(Exception __exception, List<SuppressedEffects> __state)
        {
            if (__state != null)
            {
                foreach (var suppressed in __state)
                {
                    suppressed.SharedData.m_equipStatusEffect = suppressed.EquipStatusEffect;
                    suppressed.SharedData.m_setStatusEffect = suppressed.SetStatusEffect;
                }
            }

            return __exception;
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int) })]
        private static class ItemData_GetTooltip_Patch
        {
            private static void Prefix(ItemDrop.ItemData item, ref SuppressedEffects __state)
            {
                var sharedData = item?.m_shared;
                if (sharedData == null || !WMRecipeCust.modEnabled.Value
                    || string.IsNullOrEmpty(sharedData.m_setName)
                    || !WMRecipeCust.HideEquipEffectsUntilSetComplete.Contains(sharedData.m_setName))
                    return;

                var player = Player.m_localPlayer;
                if (player == null || IsSetComplete(sharedData.m_setName, player))
                    return;

                __state = new SuppressedEffects
                {
                    SharedData = sharedData,
                    EquipStatusEffect = sharedData.m_equipStatusEffect,
                    SetStatusEffect = sharedData.m_setStatusEffect
                };
                sharedData.m_equipStatusEffect = null;
                sharedData.m_setStatusEffect = null;
            }

            private static void Postfix(ItemDrop.ItemData item, ref string __result)
            {
                var prefabName = item?.m_dropPrefab?.name;
                if (string.IsNullOrEmpty(prefabName)
                    || !WMRecipeCust.modEnabled.Value
                    || !WMRecipeCust.AdditionalSetEffects.TryGetValue(prefabName, out var effects))
                    return;

                var player = Player.m_localPlayer;
                var equipmentStateHash = GetEquipmentStateHash(player);
                var cache = AdditionalSetTooltipCaches.GetValue(item, _ => new AdditionalSetTooltipCache());
                if (cache.Effects == effects && cache.EquipmentStateHash == equipmentStateHash)
                {
                    __result += cache.Tooltip;
                    return;
                }

                var objectDb = ObjectDB.instance;
                var tooltip = new StringBuilder();
                for (var i = 0; i < effects.Count; i++)
                {
                    var effect = effects[i];
                    if (string.IsNullOrEmpty(effect.SetName) || effect.Size <= 0 || string.IsNullOrEmpty(effect.EffectName))
                        continue;

                    var duplicate = false;
                    for (var j = 0; j < i; j++)
                    {
                        if (effects[j].SetName == effect.SetName && effects[j].EffectName == effect.EffectName)
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (duplicate)
                        continue;

                    if (effect.HideEquipEffectsUntilSetComplete == true && (player == null || !IsSetComplete(effect.SetName, player)))
                        continue;

                    var statusEffect = objectDb?.GetStatusEffect(effect.EffectName.GetStableHashCode());
                    if (statusEffect == null)
                        continue;

                    tooltip.Append(Localization.instance.Localize($"\n\n$item_seteffect (<color=orange>{effect.Size}</color> $item_parts):<color=orange>{statusEffect.m_name}</color>\n{statusEffect.GetTooltipString()}"));
                }

                cache.Effects = effects;
                cache.EquipmentStateHash = equipmentStateHash;
                cache.Tooltip = tooltip.ToString();
                __result += cache.Tooltip;
            }

            [HarmonyFinalizer]
            private static Exception Finalizer(Exception __exception, SuppressedEffects __state)
            {
                if (__state != null)
                {
                    __state.SharedData.m_equipStatusEffect = __state.EquipStatusEffect;
                    __state.SharedData.m_setStatusEffect = __state.SetStatusEffect;
                }

                return __exception;
            }
        }
    }

 
        [HarmonyPatch(typeof(Player), "UpdateEnvStatusEffects")]
        static class UpdateEnvStatusEffects_Patch
        {
        internal static bool loadTranspiler = true;
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (loadTranspiler)
                    loadTranspiler = false;
                else
                    return instructions;

                WMRecipeCust.Dbgl($"Transpiling UpdateEnvStatusEffects");

                var codes = new List<CodeInstruction>(instructions);
                var outCodes = new List<CodeInstruction>();
                bool notFound = true;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (notFound && codes[i].opcode == OpCodes.Ldloc_S && codes[i + 1].opcode == OpCodes.Ldc_I4_1 && codes[i + 2].opcode == OpCodes.Beq && codes[i + 3].opcode == OpCodes.Ldloc_S && codes[i + 3].operand == codes[i].operand && codes[i + 4].opcode == OpCodes.Ldc_I4_5)
                    {
                    WMRecipeCust.Dbgl($"Adding frost immune and ignore");

                        outCodes.Add(new CodeInstruction(codes[i]));
                        outCodes.Add(new CodeInstruction(OpCodes.Ldc_I4_3));
                        outCodes.Add(new CodeInstruction(codes[i + 2]));
                        outCodes.Add(new CodeInstruction(codes[i]));
                        outCodes.Add(new CodeInstruction(OpCodes.Ldc_I4_4));
                        outCodes.Add(new CodeInstruction(codes[i + 2]));
                        notFound = false;
                    }
                    outCodes.Add(codes[i]);
                }

                return outCodes.AsEnumerable();

            }
            static void Postfix(float dt, Player __instance, ItemDrop.ItemData ___m_chestItem, ItemDrop.ItemData ___m_legItem, ItemDrop.ItemData ___m_helmetItem, ItemDrop.ItemData ___m_shoulderItem, SEMan ___m_seman)
            {
                if (!WMRecipeCust.modEnabled.Value)
                    return;

                if (___m_seman.HaveStatusEffect("Wet".GetStableHashCode()))
                {
                    HitData.DamageModifier water = GetNewDamageTypeMod(NewDamageTypes.Water, ___m_chestItem, ___m_legItem, ___m_helmetItem, ___m_shoulderItem);
                    var wet = ___m_seman.GetStatusEffect("Wet".GetStableHashCode());
                    var t = Traverse.Create(wet);

                    if (water == HitData.DamageModifier.Ignore || water == HitData.DamageModifier.Immune)
                    {
                        ___m_seman.RemoveStatusEffect(wet, true);
                    }
                    else if (water == HitData.DamageModifier.VeryResistant && !__instance.InLiquidSwimDepth())
                    {
                        ___m_seman.RemoveStatusEffect(wet, true);
                    }
                    else if (water == HitData.DamageModifier.Resistant)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() + dt);
                        ___m_seman.RemoveStatusEffect(wet, true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                    else if (water == HitData.DamageModifier.SlightlyResistant)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() + dt);
                        ___m_seman.RemoveStatusEffect(wet, true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                    else if (water == HitData.DamageModifier.Weak)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() - dt / 3);
                        ___m_seman.RemoveStatusEffect(wet, true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                    else if (water == HitData.DamageModifier.VeryWeak)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() - dt * 2 / 3);
                        ___m_seman.RemoveStatusEffect(wet, true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                }
            }
        }


    [HarmonyPatch(typeof(SEMan), "AddStatusEffect", new Type[] { typeof(StatusEffect), typeof(bool), typeof(int), typeof(float) })]
    static class SEMan_AddStatusEffect_Patch
    {
        static bool Prefix(SEMan __instance, StatusEffect statusEffect, Character ___m_character, ref StatusEffect __result)
        {
            if (!WMRecipeCust.modEnabled.Value || !___m_character.IsPlayer())
                return true;

            if (statusEffect.m_name == "$se_wet_name")
            {
                var mod = GetNewDamageTypeMod(NewDamageTypes.Water, ___m_character);
                if (mod == HitData.DamageModifier.Ignore || mod == HitData.DamageModifier.Immune)
                {
                    __result = null;
                    return false;
                }
            }

            return true;
        }

    }



    /*
    [HarmonyPatch(typeof(ItemDrop), "SlowUpdate")]
    static class ItemDrop_SlowUpdate_Patch
    {
        static void Postfix(ref ItemDrop __instance)
        {
            if (!WMRecipeCust.modEnabled.Value)
                return;
            //CheckArmorData(ref __instance.m_itemData); // from old jsson system, not needed anymore since we are using WItemDatas, but might be useful for compatibility with old jsons
        }
    }
    */

// public static string GetDamageModifiersTooltipString(List<HitData.DamageModPair> mods)
    [HarmonyPatch(typeof(SE_Stats), "GetDamageModifiersTooltipString")]  
    static class GetDamageModifiersTooltipString_Patch
        {
            static void Postfix(ref string __result, List<HitData.DamageModPair> mods)
            {
                if (!wackydatabase.WMRecipeCust.modEnabled.Value || mods.Count == 0)
                    return;

                // Optimization: Quick scan to see if we even have valid custom mods to process
                bool hasCustomMods = false;
                foreach (var mod in mods)
                {
                    // Check if it's NOT a standard defined enum (meaning it's custom) 
                    // AND not the special "None" or "chop" type if 1024 is pertinent
                    if (!Enum.IsDefined(typeof(HitData.DamageType), mod.m_type))
                    {
                        hasCustomMods = true;
                        break;
                    }
                }

                if (!hasCustomMods) return;

                __result = Regex.Replace(__result, @"\n.*<color=orange></color>", "");
                foreach (HitData.DamageModPair damageModPair in mods)
                {
                //WMRecipeCust.WLog.LogInfo("Tooltip type " + (int)damageModPair.m_type);
                if (Enum.IsDefined(typeof(HitData.DamageType), damageModPair.m_type) && (int)damageModPair.m_type != 1024)
                        continue;

                    if (damageModPair.m_modifier != HitData.DamageModifier.Ignore && damageModPair.m_modifier != HitData.DamageModifier.Normal)
                    {
                        switch (damageModPair.m_modifier)
                        {
                            case HitData.DamageModifier.SlightlyResistant:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_resistant</color> VS ";
                                break;
                            case HitData.DamageModifier.Resistant:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_resistant</color> VS ";
                                break;
                            case HitData.DamageModifier.Weak:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_weak</color> VS ";
                                break;
                            case HitData.DamageModifier.Immune:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_immune</color> VS ";
                                break;
                            case HitData.DamageModifier.VeryResistant:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_veryresistant</color> VS ";
                                break;
                            case HitData.DamageModifier.VeryWeak:
                                __result += "\n$inventory_dmgmod: <color=orange>$inventory_veryweak</color> VS ";
                                break;
                        }
                   
                        if ((int)damageModPair.m_type == (int)ArmorHelpers.NewDamageTypes.Water)
                        {
                            __result += "<color=orange>" + wackydatabase.WMRecipeCust.WaterName.Value + "</color>";
                        }
                    }
                }
            }          // water patch  https://www.nexusmods.com/valheim/mods/1162
        }


    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    public static class Add_goodies_Wackydb
    {
        // Player instance is needed to read SEs; Harmony will pass it if we include it.
        public static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
        {
            if (!__instance) return;
            if (WMRecipeCust.SEaddBonus.Count == 0) return;
            if (__instance != Player.m_localPlayer) return;


            var seMan = __instance.GetSEMan();
            if (seMan == null) return;

            var list = seMan.GetStatusEffects();
            if (list == null || list.Count == 0) return;

            float addHp = 0f, addStamina = 0f, addEitr = 0f;

            // Sum contributions of all active status effects that are defined in WackyDB
            for (int i = 0; i < list.Count; ++i)
            {
                var se = list[i];
                if (se == null) continue;

                if (TryGetBonus(se, out var b))
                {
                    if (b.AddHP.HasValue) addHp += b.AddHP.Value;
                    if (b.AddStamina.HasValue) addStamina += b.AddStamina.Value;
                    if (b.AddEitr.HasValue) addEitr += b.AddEitr.Value;
                }
            }

            // Apply totals
            hp += addHp;
            if (hp < 1f) hp = 1f;

            stamina += addStamina;
            eitr += addEitr;
        }
   
         private static bool TryGetBonus(StatusEffect se, out WackyStatusEffectBonus bonus)
        {
            bonus = null;

            // 1) prefab/internal name
            if (!string.IsNullOrEmpty(se.name) && WMRecipeCust.SEaddBonus.TryGetValue(se.name, out bonus))
                return true;

            return false;
        }
    }

}
