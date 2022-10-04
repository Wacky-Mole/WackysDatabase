using UnityEngine;

namespace wackydatabase
{
    public class RendererMaterialEffect : IRendererEffect
    {
        public string Value { get; set; }

        public RendererMaterialEffect(string material)
        {
            Value = material;
        }

        public void Apply(Renderer r)
        {
            //r.sharedMaterials[0] = Value;
        }
    }
}
