using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    class RendererManipulator : IManipulator
    {
        List<IRendererEffect> effects = new List<IRendererEffect>();

        public RendererManipulator(ShaderData data)
        {
            if (data.Override != null && data.Override != "")
            {
                AddValue(new RendererShaderEffect(data.Override));
            }

            if (data.Material != null && data.Material != "")
            {
                AddValue(new RendererMaterialEffect(data.Material));
            }
        }

        public void Invoke(Renderer smr, GameObject _prefab)
        {
            effects.ForEach(e => {
                e.Apply(smr);
            });
        }

        public void AddValue<IRendererEffect>(IRendererEffect e)
        {
            if (e != null)
            {
                effects.Add((wackydatabase.IRendererEffect)e);
            }
        }
    }
}
