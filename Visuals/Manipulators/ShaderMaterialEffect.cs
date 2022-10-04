using UnityEngine;

namespace VisualsModifier
{
    public class ShaderMaterialEffect : IMaterialEffect
    {
        public string Value { get; set; }

        public ShaderMaterialEffect(string shader)
        {
            Value = shader;
        }

        public void Apply(Material m)
        {
            Shader s = Shader.Find(Value);

            if (s)
            {
                m.shader = s;
            }
        }

        public void Apply(MaterialPropertyBlock m)
        {
            return;
        }
    }
}
