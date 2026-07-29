using System;
using System.Linq;
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
        private Renderer _selectedRenderer;
        private int _selectedSlot = -1;
        private Material _originalSlotMaterial;
        private Material _previewMaterial;

        internal RenderTexture Texture => _texture;
        internal bool HasPreview => _clone && _camera && _texture;

        internal void SetPrefab(GameObject prefab)
        {
            Dispose();
            if (!prefab)
            {
                return;
            }

            try
            {
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

        internal void ApplyMaterial(Renderer sourceRenderer, int slot, Material sourceMaterial, MaterialData changes)
        {
            RestoreSelectedMaterial();
            if (!_clone || !sourceRenderer || !sourceMaterial)
            {
                return;
            }

            string relativePath = GetRelativePath(sourceRenderer.transform);
            Transform cloneTransform = string.IsNullOrEmpty(relativePath)
                ? _clone.transform
                : _clone.transform.Find(relativePath);
            Renderer cloneRenderer = cloneTransform ? cloneTransform.GetComponent(sourceRenderer.GetType()) as Renderer : null;
            if (!cloneRenderer || slot < 0 || slot >= cloneRenderer.sharedMaterials.Length)
            {
                throw new InvalidOperationException("The selected renderer slot could not be mapped to the preview clone.");
            }

            Material[] materials = cloneRenderer.sharedMaterials;
            _selectedRenderer = cloneRenderer;
            _selectedSlot = slot;
            _originalSlotMaterial = materials[slot];
            _previewMaterial = UnityEngine.Object.Instantiate(sourceMaterial);
            _previewMaterial.name = sourceMaterial.name + " (WackyDB Preview)";
            _previewMaterial.hideFlags = HideFlags.HideAndDontSave;

            if (changes != null)
            {
                new MaterialManipulator(changes).Invoke(_previewMaterial, _clone);
            }

            materials[slot] = _previewMaterial;
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
            _yaw = 25f;
            _pitch = 15f;
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
            RestoreSelectedMaterial();
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
        }

        private void RestoreSelectedMaterial()
        {
            if (_selectedRenderer && _selectedSlot >= 0)
            {
                Material[] materials = _selectedRenderer.sharedMaterials;
                if (_selectedSlot < materials.Length)
                {
                    materials[_selectedSlot] = _originalSlotMaterial;
                    _selectedRenderer.sharedMaterials = materials;
                }
            }

            if (_previewMaterial)
            {
                UnityEngine.Object.Destroy(_previewMaterial);
            }

            _selectedRenderer = null;
            _selectedSlot = -1;
            _originalSlotMaterial = null;
            _previewMaterial = null;
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
            bool forceDisableInit = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            try
            {
                _clone = UnityEngine.Object.Instantiate(prefab, PreviewOrigin, Quaternion.identity);
            }
            finally
            {
                ZNetView.m_forceDisableInit = forceDisableInit;
            }

            _clone.name = prefab.name + " (WackyDB Preview)";
            _clone.hideFlags = HideFlags.HideAndDontSave;
            _clone.transform.SetParent(_pivot.transform, true);

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

            _clone.SetActive(true);
        }

        private void FramePreview()
        {
            Renderer[] renderers = _clone.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && !(renderer is ParticleSystemRenderer))
                .ToArray();
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
            System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
            Transform current = sourceTransform;
            while (current && current.parent)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }
    }
}
