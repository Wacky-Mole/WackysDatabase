using UnityEngine;

namespace wackydatabase
{
    public class MaterialColorEffect: MaterialEffect<Color>, IMaterialEffect
    {
        public MaterialColorEffect(string name, Color value) : base(name, value) { }

        public override void Apply(Material m)
        {
            Debug.Log(string.Format("[MaterialColorEffect]: {0} - {1}", Name, Value));
            m.SetColor(Name, Value);
        }

        public override void Apply(MaterialPropertyBlock m)
        {
            Debug.Log(string.Format("[MaterialColorEffect] (MPB): {0} - {1}", Name, Value));
            m.SetColor(Name, Value);
        }
    }
}
