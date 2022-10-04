using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    class MaterialManipulator : IManipulator
    {
        List<IMaterialEffect> properties = new List<IMaterialEffect>();

        public MaterialManipulator(MaterialData data)
        {
            if (data.Colors != null)
            {
                foreach (KeyValuePair<string, Color> entry in data.Colors)
                {
                    AddValue(new MaterialColorEffect(entry.Key, entry.Value));
                }
            }

            if (data.Floats != null)
            {
                foreach (KeyValuePair<string, float> entry in data.Floats)
                {
                    AddValue(new MaterialFloatEffect(entry.Key, entry.Value));
                }
            }
        }

        public void Invoke(Renderer smr, GameObject _prefab)
        {
            properties.ForEach(p => {
                // Skip the root material, it already exists in materials at index 0
                // p.Apply(smr.sharedMaterial);

                foreach (Material m in smr.materials)
                {
                    p.Apply(m);
                }
            });
        }

        public void AddValue<IMaterialEffect>(IMaterialEffect p)
        {
            if (p != null)
            {
                properties.Add((wackydatabase.IMaterialEffect)p);
            }
        }
    }
}
