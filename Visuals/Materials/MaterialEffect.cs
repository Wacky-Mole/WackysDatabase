using UnityEngine;

namespace wackydatabase
{
    public interface IMaterialEffect
    {
        /// <summary>
        /// Applies the changes directly to the material
        /// </summary>
        /// <param name="m">The material to apply the changes to</param>
        public abstract void Apply(Material m);

        /// <summary>
        /// Applies the material effect changes to the property block
        /// </summary>
        /// <param name="m">The material property block to apply the changes to</param>
        public abstract void Apply(MaterialPropertyBlock m);
    }

    public abstract class MaterialEffect<T>
    {
        protected string Name;
        protected T Value;

        public MaterialEffect(string name, T value) {
            Name = name;
            Value = value;
        }
    }

    public class MaterialColorEffect : MaterialEffect<Color>, IMaterialEffect
    {
        public MaterialColorEffect(string name, Color value) : base(name, value) { }

        public void Apply(Material m) { m.SetColor(Name, Value); }
        public void Apply(MaterialPropertyBlock m) { m.SetColor(Name, Value); }
    }

    public class MaterialFloatEffect : MaterialEffect<float>, IMaterialEffect
    {
        public MaterialFloatEffect(string name, float value) : base(name, value) { }

        public void Apply(Material m) { m.SetFloat(Name, Value); }
        public void Apply(MaterialPropertyBlock m) { m.SetFloat(Name, Value); }
    }

    public class MaterialTextureEffect : MaterialEffect<Texture>, IMaterialEffect
    {
        public MaterialTextureEffect(string name, Texture value) : base(name, value) { }

        public void Apply(Material m) { m.SetTexture(Name, Value); }
        public void Apply(MaterialPropertyBlock m) { m.SetTexture(Name, Value); }
    }
}
