using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace wackydatabase.PatchClasses
{
    [HarmonyPatch]
    internal static class ItemCustomDataRestoreGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(Inventory)).Where(method => method.Name == nameof(Inventory.Load)
                || (method.Name == nameof(Inventory.AddItem) && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(Dictionary<string, string>))))
                .Concat<MethodBase>(AccessTools.GetDeclaredMethods(typeof(ItemDrop.ItemData)).Where(method => method.Name == nameof(ItemDrop.ItemData.Load)))
                .Concat(new[] { AccessTools.Method(typeof(ItemDrop), nameof(ItemDrop.DropItem)) });
        }

        private static void Prefix(out int __state)
        {
            __state = ItemCustomDataManager.SuppressNewItems;
            ItemCustomDataManager.SuppressNewItems++;
        }

        private static Exception? Finalizer(Exception? __exception, int __state)
        {
            ItemCustomDataManager.SuppressNewItems = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.Clone))]
    internal static class ItemCustomDataClone
    {
        private static void Postfix(ItemDrop.ItemData __instance, ItemDrop.ItemData __result) =>
            ItemCustomDataManager.ApplyPrefabClone(__instance, __result);
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
    internal static class ItemCustomDataAwake
    {
        private static void Postfix(ItemDrop __instance)
        {
            if (__instance.m_nview && __instance.m_nview.IsValid())
            {
                // A persisted item has a serialized item-data payload; a fresh spawn does not.
                bool isNew = __instance.m_nview.GetZDO().GetByteArray(ZDOVars.s_itemData) == null
                    && ItemCustomDataManager.SuppressNewItems == 0;
                ItemCustomDataManager.ApplyDrop(__instance, isNew);
            }
            else if (ItemCustomDataManager.SuppressNewItems == 0)
            {
                // Inventory creation also instantiates ItemDrop with network initialization disabled.
                ItemCustomDataManager.Apply(__instance.m_itemData, true);
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Load))]
    internal static class ItemCustomDataDropLoad
    {
        private static void Postfix(ItemDrop __instance) => ItemCustomDataManager.ApplyDrop(__instance, false);
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem))]
    internal static class ItemCustomDataDropTransfer
    {
        private static void Postfix(ItemDrop __result) => ItemCustomDataManager.ApplyDrop(__result, false);
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    internal static class ItemCustomDataContainerAwake
    {
        private static void Postfix(Container __instance)
        {
            ItemCustomDataManager.Register(__instance);
            ItemCustomDataManager.ApplyContainer(__instance);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Load))]
    internal static class ItemCustomDataContainerLoad
    {
        private static void Postfix(Container __instance) => ItemCustomDataManager.ApplyContainer(__instance);
    }

    [HarmonyPatch]
    internal static class ItemCustomDataPlayerLoad
    {
        private static IEnumerable<MethodBase> TargetMethods() => new[]
        {
            AccessTools.Method(typeof(Player), nameof(Player.Load)),
            AccessTools.Method(typeof(Player), nameof(Player.SetLocalPlayer))
        };

        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer && ItemCustomDataManager.ApplyInventory(__instance.GetInventory()))
                __instance.GetInventory().Changed();
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
    internal static class ItemCustomDataInventoryChanged
    {
        private static void Prefix(Inventory __instance) => ItemCustomDataManager.ApplyInventory(__instance);
    }


    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
    internal static class ItemCustomDataWorldEnd
    {
        private static void Prefix() => ItemCustomDataManager.Reset();
    }
}