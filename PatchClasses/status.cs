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
using UnityEngine;
using UnityEngine.SceneManagement;
using wackydatabase.Armor;
using wackydatabase.Datas;
using wackydatabase.SetData;
using wackydatabase.Util;
using static wackydatabase.Armor.ArmorHelpers;

namespace wackydatabase.PatchClasses
{

 
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
                if (!wackydatabase.WMRecipeCust.modEnabled.Value)
                    return;

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
