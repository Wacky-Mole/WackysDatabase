using UnityEngine;

namespace wackydatabase
{
    public class RendererShaderEffect : IRendererEffect
    {
        public string Value { get; set; }

        public RendererShaderEffect(string shader)
        {
            Value = shader;
        }

        public void Apply(Renderer r)
        {
            /*Shader s = Shader.Find(Value);

            if (s)
            {
                m.shader = s;
            }
            */
        }

        public void Apply(MaterialPropertyBlock m)
        {
            return;
        }
    }
}
