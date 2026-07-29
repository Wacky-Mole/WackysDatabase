using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace wackydatabase.VisualEditor
{
    internal sealed class WackyDbShaderPropertyInfo
    {
        internal string Name = string.Empty;
        internal ShaderPropertyType Type;
        internal Color ColorValue;
    }

    internal sealed class WackyDbMaterialInfo
    {
        internal int Slot;
        internal Material Material;
        internal string Name = string.Empty;
        internal string ShaderName = string.Empty;
        internal List<WackyDbShaderPropertyInfo> ColorProperties = new List<WackyDbShaderPropertyInfo>();
    }

    internal sealed class WackyDbRendererInfo
    {
        internal Renderer Renderer;
        internal string Path = string.Empty;
        internal List<WackyDbMaterialInfo> Materials = new List<WackyDbMaterialInfo>();
    }

    internal sealed class WackyDbMaterialSlotInspector
    {
        internal List<WackyDbRendererInfo> GetRendererInfos(GameObject prefab)
        {
            List<WackyDbRendererInfo> result = new List<WackyDbRendererInfo>();
            if (!prefab)
            {
                return result;
            }

            List<Renderer> renderers = PrefabAssistant.GetRenderers(prefab);
            HashSet<int> rendererIds = new HashSet<int>(renderers.Where(renderer => renderer)
                .Select(renderer => renderer.GetInstanceID()));

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer && !(renderer is ParticleSystemRenderer) && rendererIds.Add(renderer.GetInstanceID()))
                {
                    renderers.Add(renderer);
                }
            }

            foreach (Renderer renderer in renderers.Where(renderer => renderer))
            {
                WackyDbRendererInfo rendererInfo = new WackyDbRendererInfo
                {
                    Renderer = renderer,
                    Path = GetTransformPath(prefab.transform, renderer.transform)
                };

                Material[] materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    Material material = materials[slot];
                    WackyDbMaterialInfo materialInfo = new WackyDbMaterialInfo
                    {
                        Slot = slot,
                        Material = material,
                        Name = material ? material.name : "<missing material>",
                        ShaderName = material && material.shader ? material.shader.name : "<no shader>"
                    };

                    materialInfo.ColorProperties.AddRange(GetShaderProperties(material));
                    rendererInfo.Materials.Add(materialInfo);
                }

                result.Add(rendererInfo);
            }

            return result.OrderBy(info => info.Path, StringComparer.OrdinalIgnoreCase).ToList();
        }

        internal List<WackyDbShaderPropertyInfo> GetShaderProperties(Material material)
        {
            List<WackyDbShaderPropertyInfo> properties = new List<WackyDbShaderPropertyInfo>();
            if (!material || !material.shader)
            {
                return properties;
            }

            Shader shader = material.shader;
            int propertyCount = shader.GetPropertyCount();
            for (int index = 0; index < propertyCount; index++)
            {
                if (shader.GetPropertyType(index) != ShaderPropertyType.Color)
                {
                    continue;
                }

                string propertyName = shader.GetPropertyName(index);
                properties.Add(new WackyDbShaderPropertyInfo
                {
                    Name = propertyName,
                    Type = ShaderPropertyType.Color,
                    ColorValue = material.GetColor(propertyName)
                });
            }

            return properties;
        }

        private static string GetTransformPath(Transform root, Transform current)
        {
            if (current == root)
            {
                return root.name;
            }

            List<string> parts = new List<string>();
            while (current && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Add(root.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
