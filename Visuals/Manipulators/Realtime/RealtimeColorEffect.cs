using System;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    [Serializable]
    public class RealtimeColorEffect : IRealtimeEffect
    {
        public Color Value { get; set; }
        public string Name { get; set; }

        private bool Cached;

        private Color Cache;

        public RealtimeColorEffect(string name, Color value)
        {
            Name = name;
            Value = value;
            Cached = false;
            Cache = new Color(1, 1, 1, 1);
        }

        public void Apply(MaterialPropertyBlock mpb, Material m, RealtimeEffectData data, float ratio)
        {
            if (!Cached)
            {
                Cache = m.GetColor(Name);
                Debug.Log(string.Format("[RealtimeColorEffect] - Cache ({0},{1},{2},{3})", Cache.r, Cache.g, Cache.b, Cache.a));
                Cached = true;
            }

            mpb.SetColor(Name, Color.Lerp(Cache, Value, ratio));
        }
    }
}