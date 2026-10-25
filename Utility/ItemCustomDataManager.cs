using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using wackydatabase.Datas;
using YamlDotNet.Serialization;

namespace wackydatabase
{
    internal static class ItemCustomDataManager
    {
        private static Dictionary<string, ItemCustomDataRule> rules = new(StringComparer.Ordinal);
        private static readonly ConditionalWeakTable<ItemDrop.ItemData, AppliedRule> Applied = new();
        private static readonly ConditionalWeakTable<Inventory, Container> Containers = new();
        private static int generation;

        [ThreadStatic] internal static int SuppressNewItems;

        private sealed class AppliedRule
        {
            internal int generation = -1;
            internal Dictionary<string, string>? data;
        }

        internal static string RuleDirectory => WMRecipeCust.assetPathCustomData;
        internal static bool Enabled => WMRecipeCust.modEnabled.Value && !WMRecipeCust.NoMoreLoading;

        internal static void Initialize()
        {
            SetData.Reload.OnAllReloaded += ApplyLoadedItems;
        }

        internal static void Dispose()
        {
            SetData.Reload.OnAllReloaded -= ApplyLoadedItems;
            Reset();
        }

        internal static void Reset()
        {
            rules = new Dictionary<string, ItemCustomDataRule>(StringComparer.Ordinal);
            generation++;
        }

        internal static bool IsRuleFile(string path) =>
            Path.GetFileName(path).StartsWith("CustomData_", StringComparison.OrdinalIgnoreCase)
            || Path.GetFullPath(path).StartsWith(Path.GetFullPath(RuleDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        internal static bool IsRuleYaml(string yaml) =>
            System.Text.RegularExpressions.Regex.IsMatch(yaml, "(?m)^(?:applyTo|\"applyTo\"|'applyTo')\\s*:");

        internal static ISerializer CreateSerializer() => new SerializerBuilder()
            .WithNewLine("\n").WithQuotingNecessaryStrings().Build();

        internal static void SetRule(ItemCustomDataRule rule)
        {
            if (rule == null)
                throw new ArgumentException("A custom-data rule must not be null.");
            rule.Validate();
            if (rules.ContainsKey(rule.name))
                throw new ArgumentException($"More than one custom-data rule targets {rule.name}.");
            rules.Add(rule.name, rule);
        }

        internal static string? PrefabName(ItemDrop.ItemData item) => item.m_dropPrefab ? item.m_dropPrefab.name : null;

        internal static bool Apply(ItemDrop.ItemData item, bool isNew, string? prefabName = null)
        {
            if (!Enabled || !rules.TryGetValue(prefabName ?? PrefabName(item) ?? "", out var rule)
                || rule.applyTo == ItemCustomDataScope.ExportOnly
                || (rule.applyTo == ItemCustomDataScope.NewItems && (!isNew || SuppressNewItems > 0)))
                return false;

            GameObject prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(rule.name) : null;
            if (prefab && ReferenceEquals(prefab.GetComponent<ItemDrop>()?.m_itemData, item))
                return false;

            var stamp = Applied.GetValue(item, _ => new AppliedRule());
            if (rule.applyTo == ItemCustomDataScope.AllItems && stamp.generation == generation && ReferenceEquals(stamp.data, item.m_customData))
                return false;

            // Unity-created objects may still reference a prefab's dictionary.
            var copy = new Dictionary<string, string>(item.m_customData, StringComparer.Ordinal);
            bool changed = rule.Apply(copy);
            if (changed)
                item.m_customData = copy;
            stamp.generation = generation;
            stamp.data = item.m_customData;
            return changed;
        }

        private static GameObject? SourcePrefab(ItemDrop.ItemData source)
        {
            if (!Enabled || rules.Count == 0 || !ObjectDB.instance)
                return null;
            foreach (var rule in rules.Values)
            {
                GameObject prefab = ObjectDB.instance.GetItemPrefab(rule.name);
                if (prefab && ReferenceEquals(prefab.GetComponent<ItemDrop>()?.m_itemData, source))
                    return prefab;
            }
            return null;
        }

        internal static void ApplyPrefabClone(ItemDrop.ItemData source, ItemDrop.ItemData result)
        {
            if (SuppressNewItems > 0)
                return;
            GameObject prefab = SourcePrefab(source);
            if (prefab)
                Apply(result, true, prefab.name);
        }

        internal static void Register(Container container)
        {
            Inventory inventory = container.GetInventory();
            if (inventory != null)
            {
                Containers.Remove(inventory);
                Containers.Add(inventory, container);
            }
        }

        internal static bool CanEdit(Inventory inventory)
        {
            if (inventory == null)
                return false;
            if (Player.m_localPlayer && ReferenceEquals(Player.m_localPlayer.GetInventory(), inventory))
                return true;
            return Containers.TryGetValue(inventory, out Container container) && container
                && !container.m_loading && container.m_nview && container.m_nview.IsValid() && container.m_nview.IsOwner();
        }

        internal static bool ApplyInventory(Inventory inventory)
        {
            if (!Enabled || !CanEdit(inventory))
                return false;
            bool changed = false;
            foreach (var item in inventory.GetAllItems())
                changed |= Apply(item, false);
            return changed;
        }

        internal static void ApplyContainer(Container container)
        {
            if (container && !container.m_loading && ApplyInventory(container.GetInventory()))
                container.GetInventory().Changed();
        }

        internal static void ApplyDrop(ItemDrop drop, bool isNew)
        {
            if (drop && drop.m_nview && drop.m_nview.IsValid() && drop.m_nview.IsOwner()
                && Apply(drop.m_itemData, isNew))
                drop.Save();
        }

        internal static void ApplyLoadedItems()
        {
            if (!Enabled || rules.Count == 0)
                return;
            if (Player.m_localPlayer && ApplyInventory(Player.m_localPlayer.GetInventory()))
                Player.m_localPlayer.GetInventory().Changed();
            foreach (var container in UnityEngine.Object.FindObjectsOfType<Container>())
            {
                Register(container);
                if (CanEdit(container.GetInventory()))
                {
                    container.Load();
                    ApplyContainer(container);
                }
            }
            foreach (var drop in ItemDrop.s_instances.ToArray())
            {
                if (drop && drop.m_nview && drop.m_nview.IsValid() && drop.m_nview.IsOwner())
                {
                    drop.Load();
                    ApplyDrop(drop, false);
                }
            }
        }

    }
}