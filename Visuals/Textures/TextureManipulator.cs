using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    public class TextureManipulator
    {
        private List<ITextureEffect> effects = new();
        private Texture2D _texture;
        public TextureManipulator(TextureData data, Texture2D texture = null)
        {
            if (texture)
            {
                _texture = texture;
            }

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

        public void AddValue(ITextureEffect value)
        {
            if (value != null)
            {
                effects.Add(value);
            }
        }

        public void Invoke(Renderer smr)
        {
            effects.ForEach(e =>
            { 
                foreach (Material m in smr.sharedMaterials)
                {
                    e.Apply(m, _texture);
                }
            });
        }

        public void Invoke(Material m)
        {
            effects.ForEach(e => { e.Apply(m, _texture); });
        }
    }
}
