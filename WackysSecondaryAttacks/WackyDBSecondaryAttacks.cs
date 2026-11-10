using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using API;
using BepInEx;
using HarmonyLib;
using ServerSync;

[assembly: AssemblyVersion(WackysSecondaryAttacks.WackyDBSecondaryAttacks.Version)]
[assembly: AssemblyFileVersion(WackysSecondaryAttacks.WackyDBSecondaryAttacks.Version)]

namespace WackysSecondaryAttacks
{
    [BepInPlugin(SecondaryAttackDamage.PluginGuid, "WackyDBSecondaryAttacks", Version)]
    [BepInDependency("WackyMole.WackysDatabase", "2.6.14")]
    public sealed class WackyDBSecondaryAttacks : BaseUnityPlugin
    {
        public const string Version = "1.0.1";
        private readonly Harmony harmony = new Harmony(SecondaryAttackDamage.PluginGuid);
        private ConfigSync configSync;

        private void Awake()
        {
            try
            {
                harmony.PatchAll(typeof(WackyDBSecondaryAttacks).Assembly);
                configSync = new ConfigSync(SecondaryAttackDamage.PluginGuid)
                {
                    DisplayName = "WackyMole.WackyDBSecondaryAttacks",
                    CurrentVersion = Version,
                    MinimumRequiredVersion = Version,
                    ModRequired = true
                };
                SecondaryAttackDamage.ProviderAvailable = true;
            }
            catch
            {
                harmony.UnpatchSelf();
                throw;
            }
        }

        private void OnDestroy()
        {
            SecondaryAttackDamage.ProviderAvailable = false;
            //harmony.UnpatchSelf();
        }
    }

    internal static class DamageContext
    {
        private static readonly ConditionalWeakTable<Attack, Attack> Origins = new ConditionalWeakTable<Attack, Attack>();
        [ThreadStatic] private static SecondaryAttackDamage.Definition current;
        [ThreadStatic] private static ItemDrop.ItemData currentWeapon;

        internal static void TrackClone(Attack source, Attack clone)
        {
            Origins.Add(clone, source);
        }

        internal static bool TryGet(Attack attack, ItemDrop.ItemData weapon, out SecondaryAttackDamage.Definition definition)
        {
            while (attack != null)
            {
                if (ReferenceEquals(attack, weapon.m_shared.m_secondaryAttack))
                    return SecondaryAttackDamage.TryGet(weapon, out definition);
                if (SecondaryAttackDamage.TryGet(attack, out definition))
                    return true;
                if (!Origins.TryGetValue(attack, out attack))
                    break;
            }
            definition = null;
            return false;
        }

        internal static HitData.DamageTypes GetWeaponDamage(ItemDrop.ItemData weapon, Attack attack)
        {
            return GetDamage(weapon, attack, weapon.m_quality, weapon.m_worldLevel, true);
        }

        internal static HitData.DamageTypes GetDamage(ItemDrop.ItemData weapon, Attack attack, int quality, float worldLevel, bool useItemQuality = false)
        {
            TryGet(attack, weapon, out var definition);
            var previous = current;
            var previousWeapon = currentWeapon;
            current = definition;
            currentWeapon = weapon;
            try
            {
                return useItemQuality ? weapon.GetDamage() : weapon.GetDamage(quality, worldLevel);
            }
            finally
            {
                current = previous;
                currentWeapon = previousWeapon;
            }
        }

        internal static HitData.DamageTypes GetBaseDamage(ItemDrop.ItemData.SharedData shared, ItemDrop.ItemData item)
        {
            return ReferenceEquals(item, currentWeapon) && current?.BaseDamage != null
                ? current.BaseDamage.Value : shared.m_damages;
        }

        internal static HitData.DamageTypes GetPerLevelDamage(ItemDrop.ItemData.SharedData shared, ItemDrop.ItemData item)
        {
            return ReferenceEquals(item, currentWeapon) && current?.PerLevelDamage != null
                ? current.PerLevelDamage.Value : shared.m_damagesPerLevel;
        }
    }

    internal static class PatchInstructions
    {
        internal static void ReplaceWithInstanceCall(List<CodeInstruction> codes, int index, MethodInfo replacement)
        {
            var original = codes[index];
            var loadInstance = new CodeInstruction(OpCodes.Ldarg_0);
            loadInstance.labels.AddRange(original.labels);
            original.labels.Clear();
            loadInstance.blocks.AddRange(original.blocks.Where(block => block.blockType != ExceptionBlockType.EndExceptionBlock));
            original.blocks.RemoveAll(block => block.blockType != ExceptionBlockType.EndExceptionBlock);
            original.opcode = OpCodes.Call;
            original.operand = replacement;
            codes.Insert(index, loadInstance);
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.Clone))]
    internal static class ClonePatch
    {
        private static void Postfix(Attack __instance, Attack __result)
        {
            DamageContext.TrackClone(__instance, __result);
        }
    }

    [HarmonyPatch]
    internal static class AttackDamagePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Attack), "DoMeleeAttack");
            yield return AccessTools.DeclaredMethod(typeof(Attack), "FireProjectileBurst");
            yield return AccessTools.GetDeclaredMethods(typeof(Attack)).Single(method =>
                !method.IsStatic && method.Name.StartsWith("<DoAreaAttack>g__checkHits|", StringComparison.Ordinal));
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var weaponField = AccessTools.Field(typeof(Attack), "m_weapon");
            var getDamage = AccessTools.DeclaredMethod(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), Type.EmptyTypes);
            var replacement = AccessTools.DeclaredMethod(typeof(DamageContext), nameof(DamageContext.GetWeaponDamage));
            int matches = 0;
            for (int i = 2; i < codes.Count; i++)
            {
                if (!codes[i].Calls(getDamage) || !codes[i - 1].LoadsField(weaponField) || codes[i - 2].opcode != OpCodes.Ldarg_0)
                    continue;

                PatchInstructions.ReplaceWithInstanceCall(codes, i++, replacement);
                matches++;
            }
            if (matches != 1)
                throw new InvalidOperationException($"Secondary damage patch expected one weapon GetDamage call, found {matches}. Check Valheim/mod versions.");
            return codes;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), new[] { typeof(int), typeof(float) })]
    internal static class DamageFieldsPatch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var baseField = AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), "m_damages");
            var perLevelField = AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), "m_damagesPerLevel");
            int baseMatches = 0;
            int perLevelMatches = 0;
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(baseField))
                {
                    PatchInstructions.ReplaceWithInstanceCall(codes, i++, AccessTools.DeclaredMethod(typeof(DamageContext), nameof(DamageContext.GetBaseDamage)));
                    baseMatches++;
                }
                else if (codes[i].LoadsField(perLevelField))
                {
                    PatchInstructions.ReplaceWithInstanceCall(codes, i++, AccessTools.DeclaredMethod(typeof(DamageContext), nameof(DamageContext.GetPerLevelDamage)));
                    perLevelMatches++;
                }
            }
            if (baseMatches != 1 || perLevelMatches != 1)
                throw new InvalidOperationException($"Secondary damage patch expected one base/per-level field read, found {baseMatches}/{perLevelMatches}. Check Valheim/mod versions.");
            return codes;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), new[]
    {
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool)
    })]
    internal static class TooltipPatch
    {
        private static void Postfix(ItemDrop.ItemData item, int qualityLevel, float worldLevel, ref string __result)
        {
            var attack = item.m_shared.m_secondaryAttack;
            if (!DamageContext.TryGet(attack, item, out _))
                return;

            var damage = DamageContext.GetDamage(item, attack, qualityLevel, worldLevel);
            var damageTooltip = GetDamageTooltip(damage, item.m_shared.m_skillType);
            if (attack.m_damageMultiplier > 1f)
            {
                damage.Modify(attack.m_damageMultiplier);
                damageTooltip = AppendMultipliedValues(damageTooltip, GetDamageTooltip(damage, item.m_shared.m_skillType));
            }

            __result += "\n\n<color=orange>Secondary base damage</color>" + damageTooltip +
                $"\nSecondary damage multiplier: <color=#66B2FF>{attack.m_damageMultiplier:0.###}x</color>";
        }

        internal static string GetDamageTooltip(HitData.DamageTypes damage, Skills.SkillType skill)
        {
            var tooltip = damage.GetTooltipString(skill);
            // Native tooltips omit Chop and Pickaxe; reuse their generic damage row for identical skill ranges.
            if (damage.m_chop != 0f)
                tooltip += new HitData.DamageTypes { m_damage = damage.m_chop }.GetTooltipString(skill)
                    .Replace("$inventory_damage", "Chop");
            if (damage.m_pickaxe != 0f)
                tooltip += new HitData.DamageTypes { m_damage = damage.m_pickaxe }.GetTooltipString(skill)
                    .Replace("$inventory_damage", "Pickaxe");
            return tooltip;
        }

        internal static string AppendMultipliedValues(string baseTooltip, string multipliedTooltip)
        {
            var baseLines = baseTooltip.Split('\n');
            var multipliedLines = multipliedTooltip.Split('\n');
            if (baseLines.Length != multipliedLines.Length)
                throw new InvalidOperationException("Secondary damage tooltip rows do not match the multiplied damage rows.");

            for (int i = 0; i < baseLines.Length; i++)
            {
                int valueStart = multipliedLines[i].IndexOf("<color=orange>", StringComparison.Ordinal);
                if (valueStart < 0)
                    continue;

                var values = multipliedLines[i].Substring(valueStart).TrimEnd()
                    .Replace("<color=orange>", "<color=#66B2FF>");
                baseLines[i] = baseLines[i].TrimEnd() + " " + values;
            }
            return string.Join("\n", baseLines);
        }
    }
}
