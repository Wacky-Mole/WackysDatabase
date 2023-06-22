using BepInEx;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    public static class VisualController
    {
        static VisualController()
        {
            MaterialDataManager.Instance.OnMaterialAdd += DataManager_OnMaterialAdd;
            MaterialDataManager.Instance.OnMaterialChange += DataManager_OnMaterialChange;
            MaterialDataManager.Instance.OnMaterialOverwrite += DataManager_OnMaterialOverwrite;

            //TextureDataManager.OnTextureAdd += TextureDataManager_OnTextureAdd;
            //TextureDataManager.OnTextureChange += TextureDataManager_OnTextureChange;

            VisualDataManager.Instance.OnVisualChanged += VisualDataManager_OnVisualChange;
        }

        /*private static void TextureDataManager_OnTextureChange(object sender, TextureEventArgs e)
        {
        }

        private static void TextureDataManager_OnTextureAdd(object sender, TextureEventArgs e)
        { 
        }*/

        private static void VisualDataManager_OnVisualChange(object sender, DataEventArgs<VisualData> e)
        {
            UpdatePrefab(e.Data);
        }

        private static void DataManager_OnMaterialChange(object sender, MaterialEventArgs e)
        {
            MaterialManipulator mm = new MaterialManipulator(e.MaterialInstance.Changes);

            mm.Invoke(e.Material, null);
        }

        private static void DataManager_OnMaterialAdd(object sender, MaterialEventArgs e)
        {
            MaterialManipulator mm = new MaterialManipulator(e.MaterialInstance.Changes);

            mm.Invoke(e.Material, null);
        }

        private static void DataManager_OnMaterialOverwrite(object sender, MaterialEventArgs e)
        {
            MaterialManipulator mm = new MaterialManipulator(e.MaterialInstance.Changes);

            mm.Invoke(e.Material, null);
        }

        /// <summary>
        /// Updates the material references on the prefab
        /// </summary>
        /// <param name="data">The visual data the specifies the prefab and changes</param>
        public static void UpdatePrefab(VisualData data)
        {
            GameObject item = ObjectDB.instance.GetItemPrefab(data.PrefabName);

            if (item == null)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to find prefab {data.PrefabName}");
                return;
            }

            try
            {
                Transform skin_meshes = item.transform.Find("attach_skin"); // Find Skinned Meshes
                Transform static_meshes = item.transform.Find("attach");    // Find Static Meshes
                Transform drop_meshes = PrefabAssistant.GetDropChild(item); // Find Drop Visual

                List<Renderer> renderers = new List<Renderer>();

                // Get renderers for each visual component
                Renderer[] skinRenderers = skin_meshes != null ? skin_meshes.GetComponentsInChildren<SkinnedMeshRenderer>(true) : null;
                Renderer[] dropRenderers = drop_meshes != null ? drop_meshes.GetComponentsInChildren<MeshRenderer>(true) : null;
                Renderer[] meshRenderers = static_meshes != null ? static_meshes.GetComponentsInChildren<MeshRenderer>(true) : null;

                if (skinRenderers != null) { renderers.AddRange(skinRenderers); }
                if (dropRenderers != null) { renderers.AddRange(dropRenderers); }
                if (meshRenderers != null) { renderers.AddRange(meshRenderers); }

                // Alter chest material with new texture if exists
                if (data.Chest != null)
                {
                    Material m = MaterialDataManager.Instance.GetMaterial(data.Chest);

                    PrefabAssistant.UpdateItemMaterialReference(item, m);
                }

                // Alter leg material with new texture if exists
                if (data.Legs != null)
                {
                    Material m = MaterialDataManager.Instance.GetMaterial(data.Legs);

                    PrefabAssistant.UpdateItemMaterialReference(item, m);
                }

                if (data.Material != null)
                {
                    Material m = MaterialDataManager.Instance.GetMaterial(data.Material);

                    for (int i = 0; i < renderers.Count; i++)
                    {
                        PrefabAssistant.UpdateMaterialReference(renderers[i], m);
                    }
                } else if (data.Materials != null)
                {
                    Material[] instances = MaterialDataManager.Instance.GetMaterials(data.Materials);

                    for (int i = 0; i < renderers.Count; i++)
                    {
                        PrefabAssistant.UpdateMaterialReferences(renderers[i], instances);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to update material - {e.Message}");
            }
        }

        /// <summary>
        /// Updates the material references on the prefab
        /// </summary>
        /// <param name="prefabName">The name of the prefab</param>
        public static void UpdatePrefab(string prefabName) { 
            UpdatePrefab(VisualDataManager.Instance.GetVisualByName(prefabName));
        }

        public static void Export(DescriptorData data)
        {
            string contents = DataManager<DescriptorData>.Serializer.Serialize(data);
            string storage = Path.Combine(Paths.ConfigPath, "Visuals");

            if (!Directory.Exists(storage))
            {
                Directory.CreateDirectory(storage);
            }

            File.WriteAllText(Path.Combine(storage, "Describe_" + data.Name + ".yml"), contents);
        }

        public static void Apply()
        {
            VisualDataManager.Instance._visuals.ForEach(action => { UpdatePrefab(action.PrefabName); });
        }
    }
}
