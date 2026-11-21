using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;
using wackydatabase.Datas;

namespace wackydatabase
{
    public static class VisualController
    {
        private static readonly ArmorTint TintCache = new ArmorTint();
        private static readonly Dictionary<GameObject, List<Action>> TintedItems = new();

        internal static void UndoTint(List<Action> actions)
        {
            for (int i = actions.Count - 1; i >= 0; i--) actions[i]();
            actions.Clear();
        }

        internal static void RestoreArmorTints(bool release = false, HashSet<int> changedArmor = null)
        {
            foreach (var entry in TintedItems)
            {
                UndoTint(entry.Value);
                if (changedArmor != null && entry.Key) changedArmor.Add(entry.Key.name.GetStableHashCode());
            }
            TintedItems.Clear();
            if (release) TintCache.Dispose();
        }

        internal static void RefreshEquippedArmor(HashSet<int> changedArmor)
        {
            if (changedArmor.Count == 0 || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return;
            }

            foreach (VisEquipment equipment in UnityEngine.Object.FindObjectsOfType<VisEquipment>())
            {
                if (!equipment || !equipment.isActiveAndEnabled || !equipment.m_bodyModel) continue;

                int chest = equipment.m_currentChestItemHash;
                int legs = equipment.m_currentLegItemHash;
                bool refreshChest = chest != 0 && changedArmor.Contains(chest);
                bool refreshLegs = legs != 0 && changedArmor.Contains(legs);
                if (!refreshChest && !refreshLegs) continue;

                // Invalidate only the visual cache, not the equipped item or its network state.
                if (refreshChest)
                {
                    equipment.m_currentChestItemHash = 0;
                    equipment.SetChestEquipped(chest);
                }
                if (refreshLegs)
                {
                    equipment.m_currentLegItemHash = 0;
                    equipment.SetLegEquipped(legs);
                }
                equipment.UpdateLodgroup();
            }
        }

        static VisualController()
        {
            MaterialDataManager.Instance.OnMaterialAdd += DataManager_OnMaterialAdd;
            MaterialDataManager.Instance.OnMaterialChange += DataManager_OnMaterialChange;
            MaterialDataManager.Instance.OnMaterialOverwrite += DataManager_OnMaterialOverwrite;

            //TextureDataManager.OnTextureAdd += TextureDataManager_OnTextureAdd;
            //TextureDataManager.OnTextureChange += TextureDataManager_OnTextureChange;
        }

        /*private static void TextureDataManager_OnTextureChange(object sender, TextureEventArgs e)
        {
        }

        private static void TextureDataManager_OnTextureAdd(object sender, TextureEventArgs e)
        { 
        }*/

        private static void DataManager_OnMaterialChange(object sender, MaterialEventArgs e)
        {
            MaterialManipulator mm = new MaterialManipulator(e.MaterialInstance.changes);

            mm.Invoke(e.Material, null);
        }

        private static void DataManager_OnMaterialAdd(object sender, MaterialEventArgs e)
        {
            Debug.Log($"[{WMRecipeCust.ModName}]: Setting material properties for: {e.Material.name}");

            MaterialManipulator mm = new MaterialManipulator(e.MaterialInstance.changes);

            mm.Invoke(e.Material, null);
        }

        private static void DataManager_OnMaterialOverwrite(object sender, MaterialEventArgs e)
        {
            MaterialManipulator mm = new MaterialManipulator(e.MaterialInstance.changes);

            mm.Invoke(e.Material, null);
        }

        /// <summary>
        /// Updates the material references on the prefab
        /// </summary>
        public static void UpdatePrefab(string name, CustomVisual visual)
        {
            UpdatePrefab(ObjectDB.instance.GetItemPrefab(name), name, visual, sourcePrefabName: null, applyToMatchingDropSlots: false);
        }

        internal static void UpdateItemPrefab(WItemData data, GameObject prefab = null)
        {
            CustomVisual visual = data.customVisual;
            bool useArmorMaterialForDrop = visual != null && visual.useArmorMaterialForDrop != false
                && visual.itemModel == null && !string.IsNullOrWhiteSpace(data.clonePrefabName)
                && string.IsNullOrWhiteSpace(data.material)
                && (data.materials == null || data.materials.Length == 0)
                && string.IsNullOrWhiteSpace(visual.base_mat);
            UpdatePrefab(prefab ?? ObjectDB.instance.GetItemPrefab(data.name), data.name, visual, data.clonePrefabName, useArmorMaterialForDrop);
        }

        private static void UpdatePrefab(GameObject item, string name, CustomVisual visual, string sourcePrefabName, bool applyToMatchingDropSlots)
        {
            if (item == null)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to find prefab {name}");
                return;
            }

            if (visual == null)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: No custom visuals for {name}");
                return;
            }

            Debug.Log($"{name}: Base: {visual.base_mat}, Legs: {visual.legs}, Chest: {visual.chest}");

            try
            {
                if (TintedItems.TryGetValue(item, out List<Action> previous))
                {
                    UndoTint(previous);
                    TintedItems.Remove(item);
                }

                Color32? tint = null;
                if (visual.armorTint != null)
                {
                    if (ArmorTint.TryParse(visual.armorTint, out Color32 parsed)) tint = parsed;
                    else WMRecipeCust.WLog.LogWarning($"{name}: Invalid armorTint; expected #RRGGBB.");
                }

                // Alter chest material with new texture if exists
                if (visual.chest != null && visual.chest != "")
                {
                    Material m = FindArmorMaterial(visual.chest);

                    if (m != null)
                    {
                        var legacy = ApplyArmorMaterial(item, m, ItemDrop.ItemData.ItemType.Chest, sourcePrefabName, applyToMatchingDropSlots && visual.armorTint == null);
                        tint ??= legacy;
                    } else
                    {
                        Debug.LogWarning($"[{WMRecipeCust.ModName}]: Failed to get chest material {visual.chest}");
                    }
                }

                // Alter leg material with new texture if exists
                if (visual.legs != null && visual.legs != "")
                {
                    Material m = FindArmorMaterial(visual.legs);

                    if (m != null)
                    {
                        var legacy = ApplyArmorMaterial(item, m, ItemDrop.ItemData.ItemType.Legs, sourcePrefabName, applyToMatchingDropSlots && visual.armorTint == null);
                        tint ??= legacy;
                    } else
                    {
                        Debug.LogWarning($"[{WMRecipeCust.ModName}]: Failed to get leg material {visual.legs}");
                    }
                }

                if (visual.base_mat != null && visual.base_mat != "")
                {
                    Material m = MaterialDataManager.Instance.GetMaterial(visual.base_mat);

                    foreach (Renderer renderer in PrefabAssistant.GetRenderers(item))
                    {
                        PrefabAssistant.UpdateMaterialReference(renderer, m);
                    }
                }

                if (tint.HasValue) ApplyTint(item, tint.Value);
                if (visual.itemModel != null)
                {
                    ApplyItemModelSlot(item, visual.itemModel);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to update material - {e.Message} - {e.StackTrace}");
            }
        }

        private static Material FindArmorMaterial(string name)
        {
            return MaterialDataManager.Instance.materials.TryGetValue(name, out Material managed) && managed
                ? managed
                : WMRecipeCust.originalMaterials.TryGetValue(name, out Material original) ? original : null;
        }

        internal static bool SupportsEquippedArmor(Material material, ItemDrop.ItemData.ItemType armorType)
        {
            if (!material)
            {
                return false;
            }

            string[] slots = ArmorTint.Slots(armorType);
            return slots.Length != 0 && Array.TrueForAll(slots, property => material.HasTexture(property));
        }

        private static void ApplyTint(GameObject item, Color32 color)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var shared = item.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
            if (shared == null || ArmorTint.Slots(shared.m_itemType).Length == 0)
            {
                WMRecipeCust.WLog.LogWarning($"{item.name}: Quick recolor requires chest or leg armor.");
                return;
            }

            var restore = new List<Action>();
            try
            {
                Material original = shared.m_armorMaterial;
                Material worn = SupportsEquippedArmor(original, shared.m_itemType)
                    ? TintCache.Tint(original, color, ArmorTint.Slots(shared.m_itemType)[0]) : null;
                if (worn)
                {
                    restore.Add(() => { if (shared.m_armorMaterial == worn) shared.m_armorMaterial = original; });
                    shared.m_armorMaterial = worn;
                }
                else
                {
                    WMRecipeCust.WLog.LogWarning($"{item.name}: No compatible armor texture; Quick recolor only affects the item meshes.");
                }
                TintCache.TintMeshes(item.GetComponentsInChildren<Renderer>(true), color, restore);
                TintedItems[item] = restore;
            }
            catch
            {
                UndoTint(restore);
                throw;
            }
        }

        private static Color32? ApplyArmorMaterial(GameObject item, Material material, ItemDrop.ItemData.ItemType armorType, string sourcePrefabName, bool applyToMatchingDropSlots)
        {
            ItemDrop.ItemData.SharedData shared = item.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
            if (shared?.m_itemType != armorType)
            {
                WMRecipeCust.WLog.LogWarning($"{item.name}: Ignoring the {armorType} material '{material.name}' because the item is not {armorType} armor.");
                return null;
            }

            if (SupportsEquippedArmor(material, armorType))
            {
                shared.m_armorMaterial = material;
            }
            else
            {
                if (shared.m_armorMaterial == material)
                {
                    // A previous reload may already have assigned this incompatible material.
                    GameObject source = string.IsNullOrWhiteSpace(sourcePrefabName) ? null : ObjectDB.instance.GetItemPrefab(sourcePrefabName);
                    Material inherited = source ? source.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_armorMaterial : null;
                    shared.m_armorMaterial = SupportsEquippedArmor(inherited, armorType) ? inherited : null;
                }
                if (applyToMatchingDropSlots && material.HasColor("_Color") && SupportsEquippedArmor(shared.m_armorMaterial, armorType)
                    && MaterialDataManager.Instance.TryGetOriginalName(material.name, out string sourceName)
                    && WMRecipeCust.originalMaterials.TryGetValue(sourceName, out Material original) && original && original.HasColor("_Color")
                    && material.GetColor("_Color") != original.GetColor("_Color"))
                {
                    return (Color32)material.GetColor("_Color");
                }
                WMRecipeCust.WLog.LogWarning($"{item.name}: Material '{material.name}' lacks the textures Valheim requires for equipped {armorType} armor. It was not applied to the equipped armor.");
            }

            if (!applyToMatchingDropSlots || !MaterialDataManager.Instance.TryGetOriginalName(material.name, out string originalName))
            {
                return null;
            }

            Transform drop = item.transform.Find("log") ?? PrefabAssistant.GetDropChild(item);
            if (!drop)
            {
                return null;
            }

            foreach (Renderer renderer in drop.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                {
                    continue;
                }
                Material[] slots = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] && slots[i].name == originalName)
                    {
                        slots[i] = material;
                        changed = true;
                    }
                }
                if (changed)
                {
                    renderer.sharedMaterials = slots;
                }
            }
            return null;
        }

        private static void ApplyItemModelSlot(GameObject item, ItemMaterialSlot target)
        {
            Transform node = string.IsNullOrEmpty(target.path) ? item.transform : item.transform.Find(target.path);
            Renderer[] renderers = node ? node.GetComponents<Renderer>() : System.Array.Empty<Renderer>();
            if (target.renderer < 0 || target.renderer >= renderers.Length)
            {
                WMRecipeCust.WLog.LogWarning($"{item.name}: item-model renderer {target.renderer} at '{target.path}' was not found.");
                return;
            }

            Renderer renderer = renderers[target.renderer];
            Material[] slots = renderer.sharedMaterials;
            if (target.slot < 0 || target.slot >= slots.Length)
            {
                WMRecipeCust.WLog.LogWarning($"{item.name}: item-model material slot {target.slot} at '{target.path}' was not found.");
                return;
            }
            if (string.IsNullOrWhiteSpace(target.material) || !WMRecipeCust.originalMaterials.TryGetValue(target.material, out Material material) || !material)
            {
                WMRecipeCust.WLog.LogWarning($"{item.name}: item-model material '{target.material}' was not found.");
                return;
            }

            slots[target.slot] = material;
            renderer.sharedMaterials = slots;
        }

        public static void Export(DescriptorData data)
        { 

            string contents = DataManager<DescriptorData>.Serializer.Serialize(data);
            //string storage = Path.Combine(WMRecipeCust.assetPathconfig, "Visuals");
            string storage = WMRecipeCust.assetPathconfig;
    

            if (!Directory.Exists(storage))
            {
                Directory.CreateDirectory(storage);
            }

            File.WriteAllText(Path.Combine(storage, "Describe_" + data.Name + ".yml"), contents);
        }
    }
}
