using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    public class TextureManipulator: IManipulator
    {
        List<IMaterialEffect> effects = new List<IMaterialEffect>();

        public TextureManipulator(TextureData data)
        {
            switch (data.Effect)
            {
                case TextureEffect.Multiply:
                    AddValue(new TextureMultiplyEffect(data.Name, data.Colors[0]));
                    break;
                case TextureEffect.Screen:
                    AddValue(new TextureScreenEffect(data.Name, data.Colors[0]));
                    break;
                case TextureEffect.Edge:
                    AddValue(new TextureToonEffect(data.Name, data.Colors.ToArray()));
                    break;
            }
        }

        public void AddValue<IMaterialEffect>(IMaterialEffect value)
        {
            if (value != null)
            {
                effects.Add((wackydatabase.IMaterialEffect) value);
            }
        }

        public void Invoke(Renderer smr, GameObject _prefab)
        {
            effects.ForEach(e => { 
                e.Apply(smr.material); 

                foreach (Material m in smr.materials)
                {
                    e.Apply(m);
                }
            });
        }
    }
}
