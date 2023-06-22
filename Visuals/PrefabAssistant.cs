using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using wackydatabase.Datas;

namespace wackydatabase
{
    public static class PrefabAssistant
    {
        /// <summary>
        /// Gets the component which will appear on the ground
        /// </summary>
        /// <param name="item">The game object for the item</param>
        /// <returns>The transform for the component</returns>
        public static Transform GetDropChild(GameObject item)
        {
            for (int i = 0; i < item.transform.childCount; i++)
            {
                Transform child = item.transform.GetChild(i);

                if (child.name.Contains("attach")) { continue; }

                return child;
            }

            return null;
        }

        /// <summary>
        /// Adds a flare to the component
        /// </summary>
        /// <param name="t">The transform that hosts the components</param>
        /// <returns>The new flare transform</returns>
        public static Transform AddFlare(Transform t)
        {
            // Retrieve known flare and use that to add the effect to our weapon.
            Transform flare = ObjectDB.instance.GetItemPrefab("BattleaxeCrystal").transform.Find("attach").Find("flare");
            Transform newFlare = GameObject.Instantiate(flare, t, false);

            newFlare.name = "flare";
            newFlare.localPosition = Vector3.zero;

            return newFlare;
        }

        /// <summary>
        /// Creates a description of all materials, properties and renderers for a prefab
        /// </summary>
        /// <param name="prefabName">The name of the prefab</param>
        public static DescriptorData Describe(string prefabName)
        {
            DescriptorData data = new DescriptorData() { Name = prefabName };
            GameObject item = ObjectDB.instance.GetItemPrefab(prefabName);

            if (!item)
            {
                return data;
            }

            // Fetch the skin
            Transform skin = item.transform.Find("attach_skin") ?? item.transform.Find("attach");

            if (!skin)
            {
                skin = GetDropChild(item);
            }

            if (skin)
            {
                Renderer[] skin_renderers = skin.GetComponentsInChildren<Renderer>(true);

                for (int i = 0; i < skin_renderers.Length; i++)
                {
                    RendererDescriptor rd = new RendererDescriptor() { Name = skin.name };

                    // Reference sharedMaterials as accessing 'materials' causes new instances
                    for (int j = 0; j < skin_renderers[i].sharedMaterials.Length; j++)
                    {
                        Material m = skin_renderers[i].sharedMaterials[j];
                        MaterialDescriptor md = new MaterialDescriptor() { Name = skin_renderers[i].sharedMaterials[j].name, Shader = m.shader.name };

                        int propertyCount = m.shader.GetPropertyCount();

                        for (int k = 0; k < propertyCount; k++)
                        {
                            ShaderPropertyType type = m.shader.GetPropertyType(k);
                            string name = m.shader.GetPropertyName(k);

                            MaterialPropertyDescriptor mpd = new MaterialPropertyDescriptor()
                            {
                                Name = name,
                                Type = type.ToString(),
                                Range = type == ShaderPropertyType.Range ? string.Format("{0} to {1}", m.shader.GetPropertyRangeLimits(k).x, m.shader.GetPropertyRangeLimits(k).y) : null,
                                Value = type == ShaderPropertyType.Color ? m.GetColor(name).ToString() :
                                        type == ShaderPropertyType.Range ? m.GetFloat(name).ToString() :
                                        type == ShaderPropertyType.Float ? m.GetFloat(name).ToString() :
                                        type == ShaderPropertyType.Vector ? m.GetVector(name).ToString() : null
                            };

                            md.MaterialProperties.Add(mpd);
                        }

                        rd.Materials.Add(md);
                    }

                    data.Renderers.Add(rd);
                }
            }
            else
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Unable to find base Renderer for {prefabName}");
            }

            return data;
        }

        public static void UpdateItemMaterialReference(GameObject prefab, Material m)
        {
            ItemDrop id = prefab.GetComponent<ItemDrop>();

            id.m_itemData.m_shared.m_armorMaterial = m;
        }

        public static void UpdateChestTexture(GameObject prefab, Texture2D texture)
        {
            ItemDrop id = prefab.GetComponent<ItemDrop>();

            id.m_itemData.m_shared.m_armorMaterial.SetTexture("_ChestTex", texture);
        }

        public static void UpdateLegTexture(GameObject prefab, Texture2D texture)
        {
            ItemDrop id = prefab.GetComponent<ItemDrop>();

            id.m_itemData.m_shared.m_armorMaterial.SetTexture("_LegTex", texture);
        }

        public static void UpdateIcon(ItemDrop item, Vector3 r)
        {
            const int layer = 30;

            Camera camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.backgroundColor = Color.clear;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 0.5f;
            camera.farClipPlane = 10000000;
            camera.cullingMask = 1 << layer;

            Light topLight = new GameObject("Light", typeof(Light)).GetComponent<Light>();
            topLight.transform.rotation = Quaternion.Euler(60, -5f, 0);
            topLight.type = LightType.Directional;
            topLight.cullingMask = 1 << layer;
            topLight.intensity = 0.7f;

            try
            {
                Rect rect = new(0, 0, 64, 64);
                Quaternion rotation = Quaternion.Euler(r.x, r.y, r.z);

                Transform target = item.transform.Find("attach"); // ?? item.transform.Find("attach_skin");

                if (!target)
                {
                    target = PrefabAssistant.GetDropChild(item.gameObject);
                }

                GameObject visual = Object.Instantiate(target.gameObject, Vector3.zero, rotation);
                foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = layer;
                }

                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
                Vector3 min = renderers.Aggregate(Vector3.positiveInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Min(cur, renderer.bounds.min));
                Vector3 max = renderers.Aggregate(Vector3.negativeInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Max(cur, renderer.bounds.max));
                Vector3 size = max - min;

                camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);
                float zDist = Mathf.Max(size.x, size.y) * 1.05f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad);
                Transform transform = camera.transform;
                transform.position = (min + max) / 2 + new Vector3(0, 0, -zDist);
                topLight.transform.position = transform.position + new Vector3(-2, 0.2f) / 3 * zDist;

                camera.Render();

                RenderTexture currentRenderTexture = RenderTexture.active;
                RenderTexture.active = camera.targetTexture;

                Texture2D texture = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
                texture.ReadPixels(rect, 0, 0);
                texture.Apply();

                RenderTexture.active = currentRenderTexture;

                item.m_itemData.m_shared.m_icons = new[] { Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f)) };

                Object.DestroyImmediate(visual);
                camera.targetTexture.Release();

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to update icon - {ex.Message}");
            }
            finally
            {
                Object.Destroy(camera);
                Object.Destroy(topLight);
            }
        }

        public static void UpdateMaterialReference(Renderer r, Material m)
        {
            if (m == null)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to retrieve material");
                return;
            }

            Debug.Log($"[{WMRecipeCust.ModName}]: Updating material to: {m.name}");

            if (r.sharedMaterials.Length > 1)
            {
                Material[] materials = r.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = m;
                }

                r.sharedMaterials = materials;
            }
            else
            {
                r.sharedMaterial = m;
            }
        }

        public static void UpdateMaterialReferences(Renderer r, Material[] materials)
        {
            if (r.sharedMaterials.Length > 1)
            {
                r.sharedMaterials = materials;
            }
            else
            {
                r.sharedMaterial = materials[0];
            }
        }
    }
}
