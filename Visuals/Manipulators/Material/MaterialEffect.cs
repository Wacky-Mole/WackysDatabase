using UnityEngine;

namespace wackydatabase
{
    public abstract class MaterialEffect<T>
    {
        protected string Name;
        protected T Value;

        public MaterialEffect(string name, T value) {
            Name = name;
            Value = value;
        }

        public abstract void Apply(Material material);
        public abstract void Apply(MaterialPropertyBlock block);
    }
}
