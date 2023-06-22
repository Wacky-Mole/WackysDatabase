using UnityEngine;

namespace wackydatabase
{
    public class TextureToonEffect : TextureEffect<Color[]>, ITextureEffect
    {
        public TextureToonEffect(string name, Color[] value) : base(name, value) { }

        public override Texture2D Resolve(Texture2D texture, Color[] color)
        {
            return Colour.AsCell(texture, this.Value[0], this.Value[1], this.Value[2]);
        }
    }
}
