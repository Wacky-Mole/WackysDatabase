using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    public interface IRealtimeEffect
    {
        public void Apply(MaterialPropertyBlock mpb, Material m, RealtimeEffectData data, float ratio);
    }
}