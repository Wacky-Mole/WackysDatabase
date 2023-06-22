using UnityEngine;

namespace wackydatabase
{
    public class TextureScreenEffect : TextureEffect<Color>, ITextureEffect
    {
        public TextureScreenEffect(string name, Color value) : base(name, value) { }

        public override Texture2D Resolve(Texture2D texture, Color color)
        {
            return Colour.AsScreen(texture, color);
        }
    }
}