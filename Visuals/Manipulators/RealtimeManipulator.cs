using System;
using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    public class RealtimeManipulator: IManipulator
    {
        private RealtimeEffectData Effect;
        private List<IRealtimeApplicator> Applicators = new List<IRealtimeApplicator>();

        public RealtimeManipulator(RealtimeEffectData effect)
        {
            Effect = effect;

            switch (Effect.Type)
            {
                case RealtimeEffectType.Time:
                    AddValue(new RealtimeApplicator(typeof(TimeEffector)));
                    break;
                case RealtimeEffectType.Proximity:
                    AddValue(new RealtimeApplicator(typeof(ProximityEffector)));
                    break;
                case RealtimeEffectType.Biome:
                    AddValue(new RealtimeApplicator(typeof(BiomeEffector)));
                    break;
            }
        }

        /// <summary>
        /// Adds a realtime monobehaviour to this manipulator
        /// </summary>
        public void AddValue<IRealtimeApplicator>(IRealtimeApplicator data)
        {
            Applicators.Add((wackydatabase.IRealtimeApplicator) data);
        }

        public void Invoke(Renderer renderer, GameObject prefab)
        {
            Applicators.ForEach(e =>
            {
                e.Apply(renderer, prefab);
            });
        }            
    }
}
