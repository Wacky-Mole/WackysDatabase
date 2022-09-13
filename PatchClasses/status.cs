using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Security.Cryptography;


using wackydatabase.Datas;
using wackydatabase.Util;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using wackydatabase.Armor;

namespace wackydatabase.PatchClasses
{

    [HarmonyPatch(typeof(Player), "UpdateEnvStatusEffects")]
        static class UpdateEnvStatusEffects_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
            wackydatabase.WMRecipeCust.Dbgl($"Transpiling UpdateEnvStatusEffects");

                var codes = new List<CodeInstruction>(instructions);
                var outCodes = new List<CodeInstruction>();
                bool notFound = true;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (notFound && codes[i].opcode == OpCodes.Ldloc_S && codes[i + 1].opcode == OpCodes.Ldc_I4_1 && codes[i + 2].opcode == OpCodes.Beq && codes[i + 3].opcode == OpCodes.Ldloc_S && codes[i + 3].operand == codes[i].operand && codes[i + 4].opcode == OpCodes.Ldc_I4_5)
                    {
                    wackydatabase.WMRecipeCust.Dbgl($"Adding frost immune and ignore");

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
                if (!wackydatabase.WMRecipeCust.modEnabled.Value)
                    return;

                if (___m_seman.HaveStatusEffect("Wet"))
                {
                    HitData.DamageModifier water = ArmorHelpers.GetNewDamageTypeMod(ArmorHelpers.NewDamageTypes.Water, ___m_chestItem, ___m_legItem, ___m_helmetItem, ___m_shoulderItem);
                    var wet = ___m_seman.GetStatusEffect("Wet");
                    var t = Traverse.Create(wet);

                    if (water == HitData.DamageModifier.Ignore || water == HitData.DamageModifier.Immune)
                    {
                        ___m_seman.RemoveStatusEffect("Wet", true);
                    }
                    else if (water == HitData.DamageModifier.VeryResistant && !__instance.InLiquidSwimDepth())
                    {
                        ___m_seman.RemoveStatusEffect("Wet", true);
                    }
                    else if (water == HitData.DamageModifier.Resistant)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() + dt);
                        ___m_seman.RemoveStatusEffect("Wet", true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                    else if (water == HitData.DamageModifier.Weak)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() - dt / 3);
                        ___m_seman.RemoveStatusEffect("Wet", true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                    else if (water == HitData.DamageModifier.VeryWeak)
                    {
                        t.Field("m_time").SetValue(t.Field("m_time").GetValue<float>() - dt * 2 / 3);
                        ___m_seman.RemoveStatusEffect("Wet", true);
                        ___m_seman.AddStatusEffect(wet);
                    }
                }
            }
        }
        //Water Patches
        [HarmonyPatch(typeof(SEMan), "AddStatusEffect", new Type[] { typeof(StatusEffect), typeof(bool) })]
        static class SEMan_AddStatusEffect_Patch
        {
            static bool Prefix(SEMan __instance, StatusEffect statusEffect, Character ___m_character, ref StatusEffect __result)
            {
                if (!wackydatabase.WMRecipeCust.modEnabled.Value || !___m_character.IsPlayer())
                    return true;

                if (statusEffect.m_name == "$se_wet_name")
                {
                    var mod = ArmorHelpers.GetNewDamageTypeMod(ArmorHelpers.NewDamageTypes.Water, ___m_character);
                    if (mod == HitData.DamageModifier.Ignore || mod == HitData.DamageModifier.Immune)
                    {
                        __result = null;
                        return false;
                    }
                }

                return true;
            }

        }


        [HarmonyPatch(typeof(ItemDrop), "SlowUpdate")] //checks every once in a while
        static class ItemDrop_SlowUpdate_Patch
        {
            static void Postfix(ref ItemDrop __instance)
            {
                if (!wackydatabase.WMRecipeCust.modEnabled.Value)
                    return;
            ArmorHelpers.CheckArmorData(ref __instance.m_itemData);
            }
        }
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
                    if (Enum.IsDefined(typeof(HitData.DamageType), damageModPair.m_type))
                        continue;

                    if (damageModPair.m_modifier != HitData.DamageModifier.Ignore && damageModPair.m_modifier != HitData.DamageModifier.Normal)
                    {
                        switch (damageModPair.m_modifier)
                        {
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

    }
