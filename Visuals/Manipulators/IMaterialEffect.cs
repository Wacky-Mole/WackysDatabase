using UnityEngine;

namespace wackydatabase
{
    public interface IMaterialEffect
    {
        /// <summary>
        /// Applies the changes directly to the material
        /// </summary>
        /// <param name="m">The material to apply the changes to</param>
        void Apply(Material m);

        /// <summary>
        /// Applies the material effect changes to the property block
        /// </summary>
        /// <param name="m">The material property block to apply the changes to</param>
        void Apply(MaterialPropertyBlock m);
    }
}
