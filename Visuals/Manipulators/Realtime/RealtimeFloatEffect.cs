using System;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    [Serializable]
    public class RealtimeFloatEffect : IRealtimeEffect
    {
        public float Value { get; set; }
        public string Name { get; set; }

        private bool Cached;

        private float Cache = 0.0f;

        public RealtimeFloatEffect(string name, float value)
        {
            Name = name;
            Value = value;
            Cached = true;
        }

        public void Apply(MaterialPropertyBlock mpb, Material m, RealtimeEffectData context, float ratio)
        {
            if (!Cached)
            {
                Cache = m.GetFloat(Name);
                Debug.Log(string.Format("[RealtimeFloatEffect] - Cache ({0})", Cache.ToString()));
                Cached = true;
            }

            mpb.SetFloat(Name, Mathf.Lerp(Cache, Value, ratio));
        }
    }
}
