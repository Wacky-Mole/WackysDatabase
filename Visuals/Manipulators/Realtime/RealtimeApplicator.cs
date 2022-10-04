using System;
using UnityEngine;

namespace wackydatabase
{
    internal interface IRealtimeApplicator
    {
        void Apply(Renderer renderer, GameObject prefab);
    }

    public class RealtimeApplicator: IRealtimeApplicator
    {
        private Type _type;

        public RealtimeApplicator(Type type)
        {
            _type = type;
        }

        public void Apply(Renderer renderer, GameObject prefab)
        {
            object te = renderer.gameObject.GetComponent(_type);

            if (te == null)
            {
                te = renderer.gameObject.AddComponent(_type);
            }

            IRealtimeEffector effector = (IRealtimeEffector)te;

            effector.SetVisuals(VisualController.GetVisualIndex(prefab.name));
        }
    }
}