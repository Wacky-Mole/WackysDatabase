using UnityEngine;

namespace wackydatabase
{
    public interface ITextureEffect
    {
        /// <summary>
        /// Applies the changes directly to the material
        /// </summary>
        /// <param name="m">The material to apply the changes to</param>
        void Apply(Material m, Texture2D t = null);

        /// <summary>
        /// Applies the material effect changes to the property block
        /// </summary>
        /// <param name="m">The material property block to apply the changes to</param>
        void Apply(MaterialPropertyBlock m, Texture2D t = null);
    }

    public abstract class TextureEffect<T>
    {
        public T Value { get; set; }
        public string Name { get; set; }

        protected Texture2D Cache { get; set; }

        public TextureEffect(string name, T value)
        {
            Name = name;
            Value = value;
        }

        public abstract Texture2D Resolve(Texture2D texture, T color);

        public void Apply(Material m, Texture2D tex = null)
        {
            if (tex == null)
            {
                tex = GetTexture(m);
            }

            if (tex != null)
            {
                if (m.HasProperty(this.Name))
                {
                    m.SetTexture(this.Name, Resolve(tex, this.Value));
                }
            }
        }

        public void Apply(MaterialPropertyBlock m, Texture2D tex = null)
        {
            if (tex == null)
            {
                tex = GetTexture(m);
            }

            if (tex != null)
            {
                m.SetTexture(this.Name, Resolve(tex, this.Value));
            }
        }

        public Texture2D GetTexture(Material m)
        {
            if (!Cache)
            {
                Cache = (Texture2D)m.GetTexture(Name);
            }

            return Cache;
        }

        public Texture2D GetTexture(MaterialPropertyBlock m)
        {
            if (!Cache)
            {
                Cache = (Texture2D)m.GetTexture(Name);
            }

            return Cache;
        }
    }
}
