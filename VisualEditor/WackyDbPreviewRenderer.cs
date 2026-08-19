using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase.VisualEditor
{
    internal sealed class WackyDbPreviewRenderer : IDisposable
    {
        private const int PreviewLayer = 30;
        private const int TextureSize = 512;
        private static readonly Vector3 PreviewOrigin = new Vector3(10000f, -10000f, 10000f);

        private GameObject _pivot;
        private GameObject _clone;
        private Camera _camera;
        private Light _light;
        private RenderTexture _texture;
        private Vector3 _center;
        private float _baseDistance;
        private float _yaw = 25f;
        private float _pitch = 15f;
        private float _zoom = 1f;
        private readonly Dictionary<string, PreviewMaterial> _previewMaterials = new Dictionary<string, PreviewMaterial>();
        private Transform _sourceRoot;
        private bool _usingPlayerModel;
        private readonly Dictionary<int, Renderer> _rendererMap = new Dictionary<int, Renderer>();
        private readonly Dictionary<int, Renderer> _sourceRendererMap = new Dictionary<int, Renderer>();

        internal RenderTexture Texture => _texture;
        internal bool HasPreview => _clone && _camera && _texture;
        internal bool CanPickRenderer => !_usingPlayerModel && _sourceRendererMap.Count > 0;

        internal void SetPrefab(GameObject prefab, bool usePlayerModel = false)
        {
            Dispose();
            if (!prefab)
            {
                return;
            }

            try
            {
                _usingPlayerModel = usePlayerModel;
                ResetView();
                CreateRenderEnvironment();
                InstantiatePreview(prefab);
                FramePreview();
                ApplyView();
                Render();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void Rotate(float yawDelta, float pitchDelta = 0f)
        {
            _yaw += yawDelta;
            _pitch = Mathf.Clamp(_pitch + pitchDelta, -80f, 80f);
            ApplyView();
        }

        internal bool TryPickRenderer(Vector2 viewportPosition, out Renderer sourceRenderer)
        {
            sourceRenderer = null;
            if (!CanPickRenderer)
            {
                return false;
            }

            Ray ray = _camera.ViewportPointToRay(viewportPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, _camera.farClipPlane, 1 << PreviewLayer, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            Renderer cloneRenderer = hit.collider.GetComponent<Renderer>();
            return cloneRenderer && _sourceRendererMap.TryGetValue(cloneRenderer.GetInstanceID(), out sourceRenderer);
        }

        internal void ApplyMaterial(Renderer sourceRenderer, int slot, Material sourceMaterial, MaterialData changes)
        {
            if (!_clone || !sourceRenderer || !sourceMaterial)
            {
                return;
            }

            _rendererMap.TryGetValue(sourceRenderer.GetInstanceID(), out Renderer cloneRenderer);
            if (!cloneRenderer && _usingPlayerModel)
            {
                cloneRenderer = FindRendererUsingMaterial(sourceMaterial);
                if (cloneRenderer)
                {
                    slot = FindMaterialSlot(cloneRenderer, sourceMaterial);
                }
            }
            if (!cloneRenderer)
            {
                string relativePath = GetRelativePath(sourceRenderer.transform);
                Transform cloneTransform = relativePath == null
                    ? null
                    : string.IsNullOrEmpty(relativePath) ? _clone.transform : _clone.transform.Find(relativePath);
                cloneRenderer = cloneTransform ? cloneTransform.GetComponent(sourceRenderer.GetType()) as Renderer : null;
            }
            if (!cloneRenderer || slot < 0 || slot >= cloneRenderer.sharedMaterials.Length)
            {
                throw new InvalidOperationException("The selected renderer slot could not be mapped to the preview clone.");
            }

            string key = GetPreviewMaterialKey(cloneRenderer, slot);
            RestorePreviewMaterial(key);

            Material[] materials = cloneRenderer.sharedMaterials;
            Material previewMaterial = UnityEngine.Object.Instantiate(sourceMaterial);
            previewMaterial.name = sourceMaterial.name + " (WackyDB Preview)";
            previewMaterial.hideFlags = HideFlags.HideAndDontSave;

            if (changes != null)
            {
                new MaterialManipulator(changes).Invoke(previewMaterial, _clone);
            }

            _previewMaterials[key] = new PreviewMaterial
            {
                Renderer = cloneRenderer,
                Slot = slot,
                OriginalMaterial = materials[slot],
                Material = previewMaterial
            };
            materials[slot] = previewMaterial;
            cloneRenderer.sharedMaterials = materials;
            Render();
        }

        internal void Zoom(float delta)
        {
            _zoom = Mathf.Clamp(_zoom + delta, 0.45f, 2.5f);
            ApplyView();
        }

        internal void ResetView()
        {
            _yaw = _usingPlayerModel ? 0f : 25f;
            _pitch = _usingPlayerModel ? 0f : 15f;
            _zoom = 1f;
            ApplyView();
        }

        internal void Render()
        {
            if (HasPreview)
            {
                _camera.Render();
            }
        }

        public void Dispose()
        {
            RestorePreviewMaterials();
            if (_camera)
            {
                _camera.targetTexture = null;
            }

            if (_texture)
            {
                _texture.Release();
                UnityEngine.Object.Destroy(_texture);
            }

            if (_pivot)
            {
                UnityEngine.Object.Destroy(_pivot);
            }

            if (_camera)
            {
                UnityEngine.Object.Destroy(_camera.gameObject);
            }

            if (_light)
            {
                UnityEngine.Object.Destroy(_light.gameObject);
            }

            _texture = null;
            _pivot = null;
            _clone = null;
            _camera = null;
            _light = null;
            _sourceRoot = null;
            _rendererMap.Clear();
            _sourceRendererMap.Clear();
        }

        private void RestorePreviewMaterials()
        {
            foreach (string key in _previewMaterials.Keys.ToArray())
            {
                RestorePreviewMaterial(key);
            }
        }

        private void RestorePreviewMaterial(string key)
        {
            if (!_previewMaterials.TryGetValue(key, out PreviewMaterial preview))
            {
                return;
            }

            if (preview.Renderer && preview.Slot >= 0)
            {
                Material[] materials = preview.Renderer.sharedMaterials;
                if (preview.Slot < materials.Length)
                {
                    materials[preview.Slot] = preview.OriginalMaterial;
                    preview.Renderer.sharedMaterials = materials;
                }
            }
            if (preview.Material)
            {
                UnityEngine.Object.Destroy(preview.Material);
            }
            _previewMaterials.Remove(key);
        }

        private static string GetPreviewMaterialKey(Renderer renderer, int slot)
        {
            return renderer.GetInstanceID() + ":" + slot;
        }

        private sealed class PreviewMaterial
        {
            internal Renderer Renderer;
            internal int Slot;
            internal Material OriginalMaterial;
            internal Material Material;
        }

        private void CreateRenderEnvironment()
        {
            _texture = new RenderTexture(TextureSize, TextureSize, 24, RenderTextureFormat.ARGB32)
            {
                name = "WackyDB Creator Preview",
                hideFlags = HideFlags.HideAndDontSave,
                antiAliasing = 2
            };
            _texture.Create();

            GameObject cameraObject = new GameObject("WackyDB Preview Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.fieldOfView = 30f;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 2000f;
            _camera.targetTexture = _texture;

            GameObject lightObject = new GameObject("WackyDB Preview Light");
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.intensity = 1.25f;
            _light.cullingMask = 1 << PreviewLayer;
            _light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            _pivot = new GameObject("WackyDB Preview Pivot");
            _pivot.hideFlags = HideFlags.HideAndDontSave;
            _pivot.transform.position = PreviewOrigin;
            _pivot.layer = PreviewLayer;
        }

        private void InstantiatePreview(GameObject prefab)
        {
            _sourceRoot = prefab.transform;
            bool forceDisableInit = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            try
            {
                GameObject source = _usingPlayerModel ? GetPlayerModelSource() : prefab;
                if (!source)
                {
                    throw new InvalidOperationException("A player model is unavailable. Enter a world before using player preview.");
                }
                _clone = UnityEngine.Object.Instantiate(source, PreviewOrigin, Quaternion.identity);
            }
            finally
            {
                ZNetView.m_forceDisableInit = forceDisableInit;
            }

            _clone.name = prefab.name + " (WackyDB Preview)";
            _clone.hideFlags = HideFlags.HideAndDontSave;
            _clone.transform.SetParent(_pivot.transform, true);

            if (_usingPlayerModel)
            {
                EquipPlayerPreview(prefab.name);
            }

            if (!_usingPlayerModel)
            {
                Renderer[] sourceRenderers = prefab.GetComponentsInChildren<Renderer>(true);
                Renderer[] cloneRenderers = _clone.GetComponentsInChildren<Renderer>(true);
                int rendererCount = Mathf.Min(sourceRenderers.Length, cloneRenderers.Length);
                for (int index = 0; index < rendererCount; index++)
                {
                    if (sourceRenderers[index] && cloneRenderers[index]
                        && sourceRenderers[index].GetType() == cloneRenderers[index].GetType())
                    {
                        _rendererMap[sourceRenderers[index].GetInstanceID()] = cloneRenderers[index];
                        _sourceRendererMap[cloneRenderers[index].GetInstanceID()] = sourceRenderers[index];
                    }
                }
            }

            foreach (Transform child in _clone.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = PreviewLayer;
            }

            foreach (MonoBehaviour behaviour in _clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (Collider collider in _clone.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody rigidbody in _clone.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
            }

            foreach (ParticleSystemRenderer particleRenderer in _clone.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                particleRenderer.enabled = false;
            }

            if (!_usingPlayerModel)
            {
                AddPickingColliders();
            }

            _clone.SetActive(true);
        }

        private void AddPickingColliders()
        {
            foreach (Renderer renderer in _clone.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer meshRenderer)
                    || !_sourceRendererMap.ContainsKey(meshRenderer.GetInstanceID()))
                {
                    continue;
                }

                MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh)
                {
                    continue;
                }

                MeshCollider collider = meshRenderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
        }

        private static GameObject GetPlayerModelSource()
        {
            GameObject playerPrefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab("Player") : null;
            return playerPrefab ? playerPrefab : Player.m_localPlayer ? Player.m_localPlayer.gameObject : null;
        }

        private void EquipPlayerPreview(string itemName)
        {
            Component equipment = _clone.GetComponentInChildren<VisEquipment>(true);
            ItemDrop itemDrop = _sourceRoot ? _sourceRoot.GetComponent<ItemDrop>() : null;
            if (!equipment || !itemDrop)
            {
                return;
            }

            ClearPlayerEquipment(equipment);
            RefreshEquipmentVisuals(equipment);
            string itemType = itemDrop.m_itemData.m_shared.m_itemType.ToString();
            string methodName = itemType.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) >= 0 ? "SetChestItem"
                : itemType.IndexOf("Leg", StringComparison.OrdinalIgnoreCase) >= 0 ? "SetLegItem"
                : itemType.IndexOf("Helmet", StringComparison.OrdinalIgnoreCase) >= 0 ? "SetHelmetItem"
                : itemType.IndexOf("Shoulder", StringComparison.OrdinalIgnoreCase) >= 0 ? "SetShoulderItem"
                : null;
            SetEquipmentItem(equipment, methodName, itemName);
            RefreshEquipmentVisuals(equipment);
        }

        private static void ClearPlayerEquipment(Component equipment)
        {
            string[] equipmentSlots =
            {
                "SetRightItem", "SetLeftItem", "SetHelmetItem", "SetChestItem", "SetLegItem",
                "SetShoulderItem", "SetUtilityItem", "SetBeardItem", "SetHairItem"
            };
            foreach (string slot in equipmentSlots)
            {
                SetEquipmentItem(equipment, slot, string.Empty);
            }
        }

        private static void SetEquipmentItem(Component equipment, string methodName, string itemName)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                return;
            }

            MethodInfo method = equipment.GetType().GetMethod(methodName, new[] { typeof(string) });
            method?.Invoke(equipment, new object[] { itemName });
        }

        private static void RefreshEquipmentVisuals(Component equipment)
        {
            string[] refreshMethods = { "UpdateEquipmentVisuals", "UpdateVisuals" };
            foreach (string methodName in refreshMethods)
            {
                MethodInfo method = equipment.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);
                method?.Invoke(equipment, null);
            }
        }

        private Renderer FindRendererUsingMaterial(Material material)
        {
            foreach (Renderer renderer in _clone.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material candidate in renderer.sharedMaterials)
                {
                    if (candidate && (candidate == material || candidate.name == material.name))
                    {
                        return renderer;
                    }
                }
            }
            return null;
        }

        private static int FindMaterialSlot(Renderer renderer, Material material)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                if (materials[index] && (materials[index] == material || materials[index].name == material.name))
                {
                    return index;
                }
            }
            return -1;
        }

        private void FramePreview()
        {
            Renderer[] renderers = _clone.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && !(renderer is ParticleSystemRenderer))
                .ToArray();
            if (_usingPlayerModel)
            {
                Renderer[] skinnedRenderers = renderers.Where(renderer => renderer is SkinnedMeshRenderer).ToArray();
                if (skinnedRenderers.Length > 0)
                {
                    renderers = skinnedRenderers;
                }
            }
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("The selected prefab has no previewable renderers.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            _clone.transform.position += PreviewOrigin - bounds.center;
            _center = PreviewOrigin;

            float radius = Mathf.Max(bounds.extents.magnitude, 0.1f);
            _baseDistance = radius / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
            _camera.nearClipPlane = Mathf.Max(0.01f, _baseDistance - radius * 2.5f);
            _camera.farClipPlane = _baseDistance + radius * 4f;
        }

        private void ApplyView()
        {
            if (!_pivot || !_camera)
            {
                return;
            }

            _pivot.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            float distance = _baseDistance * _zoom;
            _camera.transform.position = _center + new Vector3(0f, 0f, -distance);
            _camera.transform.rotation = Quaternion.LookRotation(_center - _camera.transform.position, Vector3.up);
            Render();
        }

        private string GetRelativePath(Transform sourceTransform)
        {
            List<string> parts = new List<string>();
            Transform current = sourceTransform;
            while (current && current != _sourceRoot)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            if (current != _sourceRoot)
            {
                return null;
            }

            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }
    }
}
