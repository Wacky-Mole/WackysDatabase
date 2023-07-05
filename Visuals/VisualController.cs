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
        }

        /*private static void TextureDataManager_OnTextureChange(object sender, TextureEventArgs e)
        {
        }

        private static void TextureDataManager_OnTextureAdd(object sender, TextureEventArgs e)
        { 
        }*/

        private static void DataManager_OnMaterialChange(object sender, MaterialEventArgs e)
        {
            MaterialManipulator mm = new MaterialManipulator(e.MaterialInstance.Changes);

            mm.Invoke(e.Material, null);
        }

        private static void DataManager_OnMaterialAdd(object sender, MaterialEventArgs e)
        {
            Debug.Log($"[{WMRecipeCust.ModName}]: Setting material properties for: {e.Material.name}");

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
        public static void UpdatePrefab(string name, CustomVisual visual)
        {
            GameObject item = ObjectDB.instance.GetItemPrefab(name);

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

            Debug.Log($"Base: {visual.base_mat}, Legs: {visual.legs}, Chest: {visual.chest}");

            try
            {
                List<Renderer> renderers = PrefabAssistant.GetRenderers(item);

                // Alter chest material with new texture if exists
                if (visual.chest != null && visual.chest != "")
                {
                    Material m = MaterialDataManager.Instance.GetMaterial(visual.chest);

                    if (m != null)
                    {
                        PrefabAssistant.UpdateItemMaterialReference(item, m);   
                    } else
                    {
                        Debug.LogWarning($"[{WMRecipeCust.ModName}]: Failed to get leg material {visual.chest}");
                    }
                }

                // Alter leg material with new texture if exists
                if (visual.legs != null && visual.legs != "")
                {
                    Material m = MaterialDataManager.Instance.GetMaterial(visual.legs);

                    if (m != null)
                    {
                        PrefabAssistant.UpdateItemMaterialReference(item, m);
                    } else
                    {
                        Debug.LogWarning($"[{WMRecipeCust.ModName}]: Failed to get leg material {visual.legs}");
                    }
                }

                if (visual.base_mat != null && visual.base_mat != "")
                {
                    Material m = MaterialDataManager.Instance.GetMaterial(visual.base_mat);

                    for (int i = 0; i < renderers.Count; i++)
                    {
                        PrefabAssistant.UpdateMaterialReference(renderers[i], m);
                    }
                } 
                
                /*
                else if (data.Materials != null)
                {
                    Material[] instances = MaterialDataManager.Instance.GetMaterials(data.Materials);

                    for (int i = 0; i < renderers.Count; i++)
                    {
                        PrefabAssistant.UpdateMaterialReferences(renderers[i], instances);
                    }
                }
                */
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to update material - {e.Message} - {e.StackTrace}");
            }
        }

        public static void Export(DescriptorData data)
        { 

            string contents = DataManager<DescriptorData>.Serializer.Serialize(data);
            string storage = Path.Combine(WMRecipeCust.assetPathconfig, "Visuals");
    

            if (!Directory.Exists(storage))
            {
                Directory.CreateDirectory(storage);
            }

            File.WriteAllText(Path.Combine(storage, "Describe_" + data.Name + ".yml"), contents);
        }
    }
}
