using System;
using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    public class MaterialDataManager : DataManager<MaterialInstance>
    {
        public static MaterialDataManager Instance = new MaterialDataManager();

        public Dictionary<string, Material> materials;

        public event EventHandler<MaterialEventArgs> OnMaterialAdd;
        public event EventHandler<MaterialEventArgs> OnMaterialChange;
        public event EventHandler<MaterialEventArgs> OnMaterialOverwrite;

        public MaterialDataManager() : base(WMRecipeCust.assetPathMaterials, "Material") {
            materials = new Dictionary<string, Material>();
        }

        public override void Cache(MaterialInstance mi)
        {
            try
            {
                if (mi.Overwrite)
                {
                    if (WMRecipeCust.originalMaterials.ContainsKey(mi.Original))
                    {
                        // Due to the threading that occurs, this will not actually fire the event on the correct thread
                        //OnMaterialOverwrite?.Invoke(this, new MaterialEventArgs(WMRecipeCust.originalMaterials[mi.Original], mi));

                        // Do the material changes here so that it occurs on the thread that reads the files
                        MaterialManipulator mm = new MaterialManipulator(mi.Changes);

                        mm.Invoke(WMRecipeCust.originalMaterials[mi.Original], null);

                        return;
                    }

                    Debug.LogError($"[{WMRecipeCust.ModName}]: Unable to pull material from cache: {mi.Original}");
                }

                if (!WMRecipeCust.originalMaterials.ContainsKey(mi.Original))
                {
                    Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to get material from cache: {mi.Original}");
                }
                else if (!materials.ContainsKey(mi.Name))
                {
                    Debug.Log($"[{WMRecipeCust.ModName}]: Adding Material: {mi.Name}");

                    Material m = Material.Instantiate(WMRecipeCust.originalMaterials[mi.Original]);
                    m.name = mi.Name;

                    // Add the material into the material cache
                    if (!WMRecipeCust.originalMaterials.ContainsKey(mi.Name))
                    {
                        WMRecipeCust.originalMaterials.Add(mi.Name, m);
                    } else
                    {
                        WMRecipeCust.originalMaterials[mi.Name] = m;
                    }

                    materials.Add(mi.Name, m);

                    // Due to the threading that occurs, this will not actually fire the event on the correct thread
                    //OnMaterialAdd?.Invoke(this, new MaterialEventArgs(m, mi));

                    // Do the material changes here so that it occurs on the thread that reads the files
                    MaterialManipulator mm = new MaterialManipulator(mi.Changes);

                    mm.Invoke(m, null);
                }
                else if (materials.ContainsKey(mi.Name))
                {
                    OnMaterialChange?.Invoke(this, new MaterialEventArgs(materials[mi.Name], mi));
                }
            } catch (Exception e)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to cache material: {mi.Name} - {e.Message} - {e.StackTrace}");
            }
        }

        public Material[] GetMaterials(string[] mats)
        {
            Material[] instances = new Material[mats.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                if (!materials.ContainsKey(mats[i]))
                {
                    Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to get new material: {mats[i]}");

                    continue;
                }

                Material m = materials[mats[i]];

                instances[i] = m;
            }

            return instances;
        }

        public Material GetMaterial(string material)
        {
            if (!materials.ContainsKey(material))
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to get material: {material}");

                return null;
            }

            return materials[material];
        }
    }

    public class MaterialEventArgs : EventArgs
    {
        public Material Material { get; private set; }
        public MaterialInstance MaterialInstance { get; private set; }

        public MaterialEventArgs(Material m, MaterialInstance mi)
        {
            Material = m;
            MaterialInstance = mi;
        }
    }
}
