using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace wackydatabase
{
    internal sealed class ArmorTint : IDisposable
    {
        internal static readonly string[] ChestSlots = { "_ChestTex", "_ChestBumpMap", "_ChestMetal" };
        internal static readonly string[] LegSlots = { "_LegsTex", "_LegsBumpMap", "_LegsMetal" };
        private readonly Dictionary<(Texture, Color32), Texture2D> _textures = new();
        private readonly Dictionary<(Material, Texture, string), Material> _materials = new();

        internal static string[] Slots(ItemDrop.ItemData.ItemType type) => type switch
        {
            ItemDrop.ItemData.ItemType.Chest => ChestSlots,
            ItemDrop.ItemData.ItemType.Legs => LegSlots,
            _ => Array.Empty<string>()
        };

        internal static bool TryParse(string value, out Color32 color)
        {
            color = default;
            if (value == null || value.Length != 7 || value[0] != '#'
                || !uint.TryParse(value.Substring(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint rgb))
            {
                return false;
            }
            color = new Color32((byte)(rgb >> 16), (byte)((rgb >> 8) & 0xff), (byte)(rgb & 0xff), 255);
            return true;
        }

        internal static Color32 TintPixel(Color32 pixel, Color32 color)
        {
            int light = (54 * pixel.r + 183 * pixel.g + 19 * pixel.b + 128) >> 8;
            return new Color32((byte)((light * color.r + 127) / 255), (byte)((light * color.g + 127) / 255),
                (byte)((light * color.b + 127) / 255), pixel.a);
        }

        private Texture2D GetTexture(Texture source, Color32 color)
        {
            if (_textures.TryGetValue((source, color), out Texture2D cached)) return cached;

            RenderTexture previous = RenderTexture.active;
            RenderTexture buffer = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D texture = null;
            try
            {
                Graphics.Blit(source, buffer);
                RenderTexture.active = buffer;
                texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, source.mipmapCount > 1)
                {
                    name = source.name + " (WackyDB Tint)",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapModeU = source.wrapModeU,
                    wrapModeV = source.wrapModeV,
                    filterMode = source.filterMode,
                    anisoLevel = source.anisoLevel
                };
                texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                Color32[] pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++) pixels[i] = TintPixel(pixels[i], color);
                texture.SetPixels32(pixels);
                texture.Apply(true, true);
                _textures.Add((source, color), texture);
                return texture;
            }
            catch
            {
                if (texture) Object.Destroy(texture);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(buffer);
            }
        }

        private static string GetAlbedoProperty(Material material)
        {
            Shader shader = material.shader;
            if (!shader) return null;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                if ((shader.GetPropertyFlags(i) & UnityEngine.Rendering.ShaderPropertyFlags.MainTexture) == 0) continue;
                string name = shader.GetPropertyName(i);
                if (material.HasTexture(name)) return name;
            }
            if (material.HasTexture("_MainTex")) return "_MainTex";
            return material.HasTexture("_BaseMap") ? "_BaseMap" : null;
        }

        internal Material Tint(Material source, Color32 color, string property = null)
        {
            if (!source) return null;
            property ??= GetAlbedoProperty(source);
            if (property == null) return null;
            Texture texture = source.GetTexture(property);
            if (!texture || texture.dimension != UnityEngine.Rendering.TextureDimension.Tex2D) return null;
            Texture tint = GetTexture(texture, color);
            if (!_materials.TryGetValue((source, tint, property), out Material copy))
            {
                copy = new Material(source) { name = source.name, hideFlags = HideFlags.HideAndDontSave };
                _materials.Add((source, tint, property), copy);
            }
            else
            {
                // Material YAML can change on reload even when the source texture stays the same.
                copy.CopyPropertiesFromMaterial(source);
            }
            copy.SetTexture(property, tint);
            return copy;
        }

        internal void TintMeshes(IEnumerable<Renderer> renderers, Color32 color, List<Action> restore)
        {
            foreach (Renderer renderer in renderers)
            {
                if (!renderer || (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))) continue;
                Material[] original = renderer.sharedMaterials;
                Material[] applied = null;
                for (int i = 0; i < original.Length; i++)
                {
                    Material tinted = Tint(original[i], color);
                    if (!tinted) continue;
                    applied ??= (Material[])original.Clone();
                    applied[i] = tinted;
                }
                if (applied == null) continue;
                restore.Add(() => RestoreMesh(renderer, original, applied));
                renderer.sharedMaterials = applied;
            }
        }

        private static void RestoreMesh(Renderer renderer, Material[] original, Material[] applied)
        {
            if (!renderer) return;
            Material[] current = renderer.sharedMaterials;
            for (int i = 0; i < Math.Min(current.Length, applied.Length); i++)
            {
                if (current[i] == applied[i]) current[i] = original[i];
            }
            renderer.sharedMaterials = current;
        }

        public void Dispose()
        {
            foreach (Material material in _materials.Values) Object.Destroy(material);
            foreach (Texture2D texture in _textures.Values) Object.Destroy(texture);
            _materials.Clear();
            _textures.Clear();
        }
    }
}