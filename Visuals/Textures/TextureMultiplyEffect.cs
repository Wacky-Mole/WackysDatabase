using UnityEngine;

namespace wackydatabase
{
    public class TextureMultiplyEffect : TextureEffect<Color>, ITextureEffect
    {
        public TextureMultiplyEffect(string name, Color value) : base(name, value) { }

        public override Texture2D Resolve(Texture2D texture, Color color)
        {
            return Colour.AsMultiply(texture, color);
        }
    }
}
