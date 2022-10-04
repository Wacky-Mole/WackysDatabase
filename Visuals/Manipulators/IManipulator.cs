using UnityEngine;

namespace wackydatabase
{
    public interface IManipulator
    {
        void Invoke(Renderer m, GameObject prefab);

        void AddValue<T>(T value);
    }
}
