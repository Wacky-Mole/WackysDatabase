using UnityEngine;

namespace VisualsModifier
{
    public class MaterialFloatEffect : MaterialEffect<float>, IMaterialEffect
    {
        public MaterialFloatEffect(string name, float value) : base(name, value) { }

        public override void Apply(Material m)
        {
            Debug.Log(string.Format("[MaterialFloatEffect]: {0} - {1}", Name, Value));
            m.SetFloat(Name, Value);
        }

        public override void Apply(MaterialPropertyBlock m)
        {
            Debug.Log(string.Format("[MaterialFloatEffect] (MPB): {0} - {1}", Name, Value));
            m.SetFloat(Name, Value);
        }
    }
}
