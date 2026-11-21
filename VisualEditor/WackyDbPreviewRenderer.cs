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
        private float _cameraHeight;
        private readonly Dictionary<string, PreviewMaterial> _previewMaterials = new Dictionary<string, PreviewMaterial>();
        private Transform _sourceRoot;
        private bool _usingPlayerModel;
        private bool _usingPieceModel;
        private GameObject _damagedPieceModel;
        private bool _usingVisualCopy;
        private GameObject _particleStage;
        private int _particleStageFrame;
        private bool _particleComponentsQueued;
        private bool _particlesReady;
        private bool _frameParticles;
        private float _particleFitUntil;
        private readonly List<ParticleSystem> _particles = new();
        private readonly List<ParticleSystem> _particleStarters = new();
        private readonly List<ParticleSystemRenderer> _particleRenderers = new();
        private Renderer _playerBody;
        private readonly List<Material> _playerBodyMaterials = new();
        private readonly ArmorTint _armorTint = new ArmorTint();
        private readonly Dictionary<int, Renderer> _rendererMap = new Dictionary<int, Renderer>();
        private readonly Dictionary<int, Renderer> _sourceRendererMap = new Dictionary<int, Renderer>();

        internal RenderTexture Texture => _texture;
        internal bool HasPreview => _clone && _camera && _texture;
        internal bool CanPickRenderer => !_usingPlayerModel && _sourceRendererMap.Count > 0;
        internal bool UsesMappedRenderers => _usingPlayerModel || _usingVisualCopy;
        internal bool HasParticles => _particlesReady && _particleStarters.Count > 0;
        internal string ParticleWarning { get; private set; } = string.Empty;
        internal bool ShowsRenderer(Renderer renderer) => renderer && _rendererMap.ContainsKey(renderer.GetInstanceID())
            && (!_damagedPieceModel || PrefabAssistant.IsDamagedPieceRenderer(_sourceRoot.gameObject, _damagedPieceModel, renderer));
        internal float CameraHeight
        {
            get => _cameraHeight;
            set
            {
                _cameraHeight = value;
                ApplyView();
            }
        }

        internal void ApplyArmorTint(GameObject prefab, Color32 color)
        {
            var shared = prefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
            if (shared == null) return;

            var restore = new List<Action>();
            try
            {
                IEnumerable<Renderer> meshes = _rendererMap.Values.Where(renderer => renderer && renderer.enabled).Distinct();
                _armorTint.TintMeshes(meshes, color, restore);
                if (_playerBody && VisualController.SupportsEquippedArmor(shared.m_armorMaterial, shared.m_itemType))
                {
                    string[] properties = ArmorTint.Slots(shared.m_itemType);
                    Material equipped = _armorTint.Tint(shared.m_armorMaterial, color, properties[0]);
                    if (equipped)
                    {
                        Material[] originals = _playerBody.sharedMaterials;
                        Material[] materials = (Material[])originals.Clone();
                        for (int i = 0; i < materials.Length; i++)
                        {
                            Material original = originals[i];
                            if (!original || !original.HasTexture(properties[0])) continue;
                            Material copy = new Material(original) { hideFlags = HideFlags.HideAndDontSave };
                            restore.Add(() => UnityEngine.Object.Destroy(copy));
                            foreach (string property in properties)
                                if (copy.HasTexture(property)) copy.SetTexture(property, equipped.GetTexture(property));
                            materials[i] = copy;
                        }
                        restore.Add(() => { if (_playerBody) _playerBody.sharedMaterials = originals; });
                        _playerBody.sharedMaterials = materials;
                    }
                }
                _armorTintRestores.AddRange(restore);
                Render();
            }
            catch
            {
                VisualController.UndoTint(restore);
                throw;
            }
        }

        private readonly List<Action> _armorTintRestores = new();

        internal void SetPrefab(GameObject prefab, bool usePlayerModel = false, bool showDamagedPiece = false, bool visualOnly = false)
        {
            Dispose();
            if (!prefab)
            {
                return;
            }

            try
            {
                _usingPlayerModel = usePlayerModel;
                Piece piece = usePlayerModel ? null : prefab.GetComponent<Piece>();
                _usingPieceModel = piece;
                _usingVisualCopy = !usePlayerModel && (visualOnly || piece || prefab.GetComponentInChildren<ParticleSystem>(true));
                ResetView();
                CreateRenderEnvironment();
                InstantiatePreview(prefab, showDamagedPiece);
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
            if (!cloneRenderer && (_usingPlayerModel || _usingVisualCopy))
            {
                // Other visual states have no corresponding renderer in this preview.
                return;
            }
            if (!cloneRenderer)
            {
                cloneRenderer = FindCloneRenderer(sourceRenderer, slot);
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
            _cameraHeight = 0f;
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
            VisualController.UndoTint(_armorTintRestores);
            _armorTint.Dispose();
            foreach (Material material in _playerBodyMaterials) UnityEngine.Object.Destroy(material);
            _playerBodyMaterials.Clear();
            _playerBody = null;
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
                _pivot.SetActive(false);
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
            _damagedPieceModel = null;
            _particleStage = null;
            _particleComponentsQueued = false;
            _particlesReady = false;
            _frameParticles = false;
            ParticleWarning = string.Empty;
            _particles.Clear();
            _particleStarters.Clear();
            _particleRenderers.Clear();
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

        private void InstantiatePreview(GameObject prefab, bool showDamagedPiece)
        {
            _sourceRoot = prefab.transform;
            GameObject preferred = null;
            var hidden = new HashSet<GameObject>();
            bool forceDisableInit = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            try
            {
                GameObject source = _usingPlayerModel ? GetPlayerModelSource() : prefab;
                if (!source)
                {
                    throw new InvalidOperationException("A player model is unavailable in the current game state.");
                }

                _clone = _usingPlayerModel
                    ? CreateCleanPlayerPreview(source, prefab)
                    : _usingVisualCopy ? CreateVisualPreview(prefab, showDamagedPiece, hidden, out preferred)
                    : UnityEngine.Object.Instantiate(source, PreviewOrigin, Quaternion.identity);
            }
            finally
            {
                ZNetView.m_forceDisableInit = forceDisableInit;
            }

            _clone.name = prefab.name + " (WackyDB Preview)";
            _clone.hideFlags = HideFlags.HideAndDontSave;
            _clone.transform.SetParent(_pivot.transform, true);

            if (!_usingPlayerModel && !_usingVisualCopy)
            {
                Renderer[] sourceRenderers = prefab.GetComponentsInChildren<Renderer>(true);
                bool hasItemRenderer = prefab.GetComponent<ItemDrop>() && sourceRenderers.Any(renderer =>
                    renderer && IsLogRenderer(renderer.transform));
                if (hasItemRenderer)
                {
                    foreach (Renderer cloneRenderer in _clone.GetComponentsInChildren<Renderer>(true))
                    {
                        cloneRenderer.enabled = false;
                    }
                }
                foreach (Renderer sourceRenderer in sourceRenderers)
                {
                    if (!sourceRenderer || sourceRenderer is ParticleSystemRenderer)
                    {
                        continue;
                    }

                    Renderer cloneRenderer = FindCloneRenderer(sourceRenderer, -1);
                    if (!cloneRenderer)
                    {
                        continue;
                    }

                    _rendererMap[sourceRenderer.GetInstanceID()] = cloneRenderer;
                    _sourceRendererMap[cloneRenderer.GetInstanceID()] = sourceRenderer;
                    if (!hasItemRenderer || IsLogRenderer(sourceRenderer.transform))
                    {
                        cloneRenderer.enabled = true;
                    }
                }
            }

            foreach (Transform child in _clone.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = PreviewLayer;
            }

            foreach (MonoBehaviour behaviour in _clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!_usingPlayerModel)
                {
                    behaviour.enabled = false;
                }
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
                StageParticles(prefab, preferred, hidden);
            }

            _clone.SetActive(true);
        }

        private GameObject CreateVisualPreview(GameObject prefab, bool showDamaged, HashSet<GameObject> hidden, out GameObject preferred)
        {
            var transformMap = new Dictionary<Transform, Transform>();
            Transform root = CopyTransformHierarchy(prefab.transform, _pivot.transform, transformMap);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;

            WearNTear wear = _usingPieceModel ? prefab.GetComponent<WearNTear>() : null;
            preferred = null;
            if (wear)
            {
                GameObject damaged = showDamaged ? PrefabAssistant.GetDamagedPieceModels(prefab).FirstOrDefault() : null;
                _damagedPieceModel = damaged;
                preferred = showDamaged && damaged ? damaged : wear.m_new;
                foreach (GameObject alternative in new[] { wear.m_new, wear.m_worn, wear.m_broken })
                {
                    if (preferred && alternative && alternative != preferred && !preferred.transform.IsChildOf(alternative.transform))
                    {
                        hidden.Add(alternative);
                    }
                }
            }

            Renderer[] meshes = prefab.GetComponentsInChildren<Renderer>(true).Where(renderer =>
                (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh)
                || (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh)).ToArray();
            bool isItem = !_usingPieceModel && prefab.GetComponent<ItemDrop>();
            bool preferItemModel = isItem
                && meshes.Any(renderer => IsLogRenderer(renderer.transform)
                    && IsVisualNodeVisible(renderer.transform, prefab.transform, null, hidden));
            GameObject selectedState = preferred;
            var visible = new HashSet<Renderer>(meshes.Where(renderer =>
                (_usingPieceModel || isItem || renderer.enabled)
                && (!preferItemModel || IsLogRenderer(renderer.transform))
                && IsVisualNodeVisible(renderer.transform, prefab.transform, selectedState, hidden)));

            // Older and custom prefabs can omit a health model. Keep their authored visible state.
            if (visible.Count == 0 && preferred && !prefab.GetComponentsInChildren<ParticleSystem>(true).Any(system =>
                IsVisualNodeVisible(system.transform, prefab.transform, selectedState, hidden)))
            {
                hidden.Clear();
                preferred = null;
                visible.UnionWith(meshes.Where(renderer => IsVisualNodeVisible(renderer.transform, prefab.transform, null, hidden)));
            }

            foreach (LODGroup group in prefab.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] levels = group.GetLODs();
                Renderer[] detailed = levels.Select(level => level.renderers).FirstOrDefault(level => level.Any(visible.Contains));
                if (detailed == null)
                {
                    continue;
                }
                foreach (Renderer renderer in levels.SelectMany(level => level.renderers))
                {
                    if (!detailed.Contains(renderer))
                    {
                        visible.Remove(renderer);
                    }
                }
            }

            foreach (Renderer source in visible)
            {
                Renderer copy = CopyRenderer(source, transformMap[source.transform].gameObject, transformMap);
                if (!copy)
                {
                    continue;
                }
                _rendererMap[source.GetInstanceID()] = copy;
                _sourceRendererMap[copy.GetInstanceID()] = source;
            }
            return root.gameObject;
        }

        private static bool IsVisualNodeVisible(Transform current, Transform root, GameObject preferred, HashSet<GameObject> hidden)
        {
            for (; current != root; current = current.parent)
            {
                GameObject node = current.gameObject;
                if (hidden.Contains(node) || node.GetComponent<Piece>()
                    || (current.parent == root && node.name == "_GhostOnly")
                    || (!node.activeSelf && (!preferred || !preferred.transform.IsChildOf(current))))
                {
                    return false;
                }
            }
            return true;
        }

        private void StageParticles(GameObject prefab, GameObject preferred, HashSet<GameObject> hidden)
        {
            if (!prefab.GetComponentsInChildren<ParticleSystem>(true).Any(system =>
                IsVisualNodeVisible(system.transform, prefab.transform, preferred, hidden)))
            {
                return;
            }

            // Preserve Unity's serialized modules and internal references without running prefab Awake methods.
            _particleStage = new GameObject("Preview particles");
            _particleStage.SetActive(false);
            _particleStage.transform.SetParent(_clone.transform, false);
            GameObject copy = UnityEngine.Object.Instantiate(prefab, _particleStage.transform, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;
            SetParticleHierarchy(prefab.transform, copy.transform, prefab.transform, preferred, hidden);

            foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour) UnityEngine.Object.Destroy(behaviour);
            }
            var subEmitters = new HashSet<ParticleSystem>();
            var included = new HashSet<ParticleSystem>(_particles);
            foreach (ParticleSystem system in _particles)
            {
                var main = system.main;
                main.playOnAwake = false;
                main.useUnscaledTime = true;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                main.stopAction = ParticleSystemStopAction.None;
                if (main.simulationSpace == ParticleSystemSimulationSpace.Custom
                    && (!main.customSimulationSpace || !main.customSimulationSpace.IsChildOf(copy.transform)))
                {
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                }

                var collision = system.collision;
                collision.enabled = false;
                var trigger = system.trigger;
                trigger.enabled = false;
                var lights = system.lights;
                lights.enabled = false;
                var forces = system.externalForces;
                forces.enabled = false;

                var sub = system.subEmitters;
                if (!sub.enabled) continue;
                for (int i = sub.subEmittersCount - 1; i >= 0; i--)
                {
                    ParticleSystem child = sub.GetSubEmitterSystem(i);
                    if (included.Contains(child)) subEmitters.Add(child);
                    else sub.RemoveSubEmitter(i);
                }
            }
            _particleStarters.AddRange(_particles.Where(system => !subEmitters.Contains(system)));
            _particleStageFrame = Time.frameCount;
        }

        private void SetParticleHierarchy(Transform source, Transform copy, Transform root, GameObject preferred, HashSet<GameObject> hidden)
        {
            copy.gameObject.layer = PreviewLayer;
            bool visible = IsVisualNodeVisible(source, root, preferred, hidden);
            copy.gameObject.SetActive(visible);
            if (visible && copy.TryGetComponent(out ParticleSystem system))
            {
                _particles.Add(system);
                if (copy.TryGetComponent(out ParticleSystemRenderer renderer) && renderer.enabled)
                {
                    _particleRenderers.Add(renderer);
                }
            }
            for (int i = 0; i < source.childCount; i++)
            {
                SetParticleHierarchy(source.GetChild(i), copy.GetChild(i), root, preferred, hidden);
            }
        }

        private static bool IsParticlePreviewComponent(Component component)
        {
            return component is Transform || component is ParticleSystem || component is ParticleSystemRenderer
                || component is MeshFilter || component is MeshRenderer || component is SkinnedMeshRenderer;
        }

        internal void UpdateParticles()
        {
            if (!_particleStage) return;
            if (!_particlesReady)
            {
                // Destroy is deferred. Never activate the copy until only visual components remain.
                if (Time.frameCount == _particleStageFrame) return;
                Component[] components = _particleStage.GetComponentsInChildren<Component>(true);
                if (!_particleComponentsQueued)
                {
                    // Remove scripts first so RequireComponent dependencies no longer prevent native component removal.
                    foreach (Component component in components)
                    {
                        if (!component) continue;
                        if (!IsParticlePreviewComponent(component)) UnityEngine.Object.Destroy(component);
                        else if (component is Renderer renderer && !(renderer is ParticleSystemRenderer)) renderer.enabled = false;
                    }
                    _particleComponentsQueued = true;
                    _particleStageFrame = Time.frameCount;
                    return;
                }

                if (_particleStarters.Count == 0 || _particleRenderers.Count == 0
                    || components.Any(component => component && !IsParticlePreviewComponent(component)))
                {
                    ParticleWarning = "Preview effects unavailable for this prefab.";
                    WMRecipeCust.WLog.LogWarning($"{ParticleWarning} '{_sourceRoot.name}' has no playable visual emitters or uses components that could not be isolated.");
                    UnityEngine.Object.Destroy(_particleStage);
                    _particleStage = null;
                    return;
                }
                _particleStage.SetActive(true);
                _particlesReady = true;
                _camera.depthTextureMode |= DepthTextureMode.Depth;
                ReplayParticles();
            }

            if (!_frameParticles || Time.unscaledTime > _particleFitUntil) return;
            float radius = 0f;
            foreach (ParticleSystemRenderer renderer in _particleRenderers)
            {
                if (!renderer.GetComponent<ParticleSystem>().IsAlive(false)) continue;
                Bounds bounds = renderer.bounds;
                if (bounds.size == Vector3.zero) continue;
                radius = Mathf.Max(radius, (bounds.center - _center).magnitude + bounds.extents.magnitude);
            }
            float distance = DistanceForRadius(radius);
            if (!float.IsInfinity(distance) && distance > _baseDistance * 1.1f)
            {
                _baseDistance = distance;
                ApplyView();
            }
        }

        internal void ReplayParticles()
        {
            if (!HasParticles) return;
            foreach (ParticleSystem system in _particles) system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (ParticleSystem system in _particleStarters) system.Play(false);
            _particleFitUntil = Time.unscaledTime + 1.5f;
        }

        private GameObject CreateCleanPlayerPreview(GameObject playerSource, GameObject itemPrefab)
        {
            Dictionary<Transform, Transform> transformMap = new Dictionary<Transform, Transform>();
            GameObject cleanRoot = CopyTransformHierarchy(playerSource.transform, null, transformMap).gameObject;
            cleanRoot.transform.position = PreviewOrigin;
            cleanRoot.transform.SetParent(_pivot.transform, true);
            HashSet<int> equippedRendererIds = GetEquippedRendererIds(playerSource);
            VisEquipment equipment = playerSource.GetComponent<VisEquipment>();
            if (!equipment || !equipment.m_bodyModel)
            {
                throw new InvalidOperationException("The player preview has no equipment rig.");
            }

            foreach (Renderer renderer in playerSource.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer
                    || equippedRendererIds.Contains(renderer.GetInstanceID())
                    || !transformMap.TryGetValue(renderer.transform, out Transform targetTransform))
                {
                    continue;
                }

                Renderer copy = CopyRenderer(renderer, targetTransform.gameObject, transformMap);
                if (renderer == equipment.m_bodyModel) _playerBody = copy;
            }
            if (!_playerBody) throw new InvalidOperationException("The player preview could not copy the body mesh.");

            ItemDrop.ItemData.SharedData item = itemPrefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
            ResetPlayerBodyMaterials(equipment, item);
            foreach (Transform attachment in GetWearableRoots(itemPrefab, item?.m_itemType))
            {
                bool skinned = attachment.name == "attach_skin";
                Transform body = _playerBody.transform;
                Transform sourceJoint = skinned ? equipment.m_bodyModel.transform.parent
                    : attachment.name == "attach" ? equipment.m_helmet
                    : Utils.FindChild(equipment.m_visual ? equipment.m_visual.transform : playerSource.transform, attachment.name.Substring(7));
                if (!sourceJoint || !transformMap.TryGetValue(sourceJoint, out Transform targetJoint))
                {
                    WMRecipeCust.WLog.LogWarning($"Player preview: could not attach '{attachment.name}' for '{itemPrefab.name}' to the player rig.");
                    continue;
                }

                var attachmentMap = new Dictionary<Transform, Transform>();
                Transform root = CopyTransformHierarchy(attachment, targetJoint, attachmentMap);
                root.localPosition = skinned ? body.localPosition : Vector3.zero;
                root.localRotation = Quaternion.identity;
                foreach (Renderer sourceRenderer in attachment.GetComponentsInChildren<Renderer>(true))
                {
                    if (!IsWearableMesh(sourceRenderer, attachment)) continue;
                    Renderer copied = CopyRenderer(sourceRenderer, attachmentMap[sourceRenderer.transform].gameObject, attachmentMap);
                    if (skinned && copied is SkinnedMeshRenderer skin && _playerBody is SkinnedMeshRenderer bodySkin)
                    {
                        skin.rootBone = bodySkin.rootBone;
                        skin.bones = bodySkin.bones;
                    }
                    if (copied)
                    {
                        _rendererMap[sourceRenderer.GetInstanceID()] = copied;
                        _sourceRendererMap[copied.GetInstanceID()] = sourceRenderer;
                    }
                }
            }

            return cleanRoot;
        }

        private void ResetPlayerBodyMaterials(VisEquipment equipment, ItemDrop.ItemData.SharedData selected)
        {
            Material[] materials = _playerBody.sharedMaterials;
            int modelIndex = equipment.m_currentModelIndex;
            Material baseline = equipment.m_models != null && modelIndex >= 0 && modelIndex < equipment.m_models.Length
                ? equipment.m_models[modelIndex].m_baseMaterial : null;
            foreach (var type in new[] { ItemDrop.ItemData.ItemType.Chest, ItemDrop.ItemData.ItemType.Legs })
            {
                string[] slots = ArmorTint.Slots(type);
                Material worn = selected?.m_itemType == type && VisualController.SupportsEquippedArmor(selected.m_armorMaterial, type)
                    ? selected.m_armorMaterial : null;
                Texture emptyColor = type == ItemDrop.ItemData.ItemType.Chest ? equipment.m_emptyBodyTexture : equipment.m_emptyLegsTexture;
                Texture emptyNormal = type == ItemDrop.ItemData.ItemType.Chest ? equipment.m_emptyBodyBumpTexture : equipment.m_emptyLegsBumpTexture;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (!source || !source.HasTexture(slots[0])) continue;
                    if (!_playerBodyMaterials.Contains(source))
                    {
                        source = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
                        _playerBodyMaterials.Add(source);
                        materials[i] = source;
                    }
                    for (int slot = 0; slot < slots.Length; slot++)
                    {
                        string property = slots[slot];
                        if (!source.HasTexture(property)) continue;
                        Texture empty = slot == 0 ? emptyColor : slot == 1 ? emptyNormal : null;
                        source.SetTexture(property, worn ? worn.GetTexture(property)
                            : baseline && baseline.HasTexture(property) ? baseline.GetTexture(property) : empty);
                    }
                }
            }
            _playerBody.sharedMaterials = materials;
        }

        private static IEnumerable<Transform> GetWearableRoots(GameObject prefab, ItemDrop.ItemData.ItemType? type)
        {
            bool helmet = type == ItemDrop.ItemData.ItemType.Helmet;
            for (int i = 0; i < prefab.transform.childCount; i++)
            {
                Transform child = prefab.transform.GetChild(i);
                if (helmet)
                {
                    if (child.name != "attach" && child.name != "attach_skin") continue;
                    yield return child;
                    yield break;
                }
                if (child.name.Length > 7 && child.name.StartsWith("attach_", StringComparison.Ordinal)) yield return child;
            }
        }

        private static bool IsWearableMesh(Renderer renderer, Transform root)
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) return false;
            for (Transform current = renderer.transform; current != root; current = current.parent)
            {
                if (!current.gameObject.activeSelf) return false;
            }
            return true;
        }

        private static HashSet<int> GetEquippedRendererIds(GameObject playerSource)
        {
            HashSet<int> rendererIds = new HashSet<int>();
            VisEquipment equipment = playerSource.GetComponentInChildren<VisEquipment>(true);
            if (!equipment)
            {
                return rendererIds;
            }

            foreach (FieldInfo field in equipment.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.Name.IndexOf("instance", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                AddEquippedRendererIds(field.GetValue(equipment), rendererIds);
            }
            return rendererIds;
        }

        private static void AddEquippedRendererIds(object value, HashSet<int> rendererIds)
        {
            GameObject gameObject = value as GameObject;
            if (!gameObject && value is Component component)
            {
                gameObject = component.gameObject;
            }
            if (gameObject)
            {
                foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
                {
                    rendererIds.Add(renderer.GetInstanceID());
                }
                return;
            }

            if (value is System.Collections.IEnumerable values && !(value is string))
            {
                foreach (object entry in values)
                {
                    AddEquippedRendererIds(entry, rendererIds);
                }
            }
        }

        private static Transform CopyTransformHierarchy(
            Transform source,
            Transform parent,
            Dictionary<Transform, Transform> transformMap)
        {
            GameObject copy = new GameObject(source.name);
            Transform target = copy.transform;
            target.SetParent(parent, false);
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            transformMap[source] = target;
            for (int index = 0; index < source.childCount; index++)
            {
                CopyTransformHierarchy(source.GetChild(index), target, transformMap);
            }
            return target;
        }

        private static Renderer CopyRenderer(
            Renderer source,
            GameObject target,
            Dictionary<Transform, Transform> transformMap)
        {
            Renderer result;
            if (source is SkinnedMeshRenderer sourceSkinned)
            {
                SkinnedMeshRenderer targetSkinned = target.AddComponent<SkinnedMeshRenderer>();
                targetSkinned.sharedMesh = sourceSkinned.sharedMesh;
                targetSkinned.localBounds = sourceSkinned.localBounds;
                targetSkinned.updateWhenOffscreen = true;
                targetSkinned.bones = sourceSkinned.bones
                    .Select(bone => ResolveBone(bone, transformMap))
                    .ToArray();
                targetSkinned.rootBone = ResolveBone(sourceSkinned.rootBone, transformMap);
                result = targetSkinned;
            }
            else if (source is MeshRenderer)
            {
                MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
                if (!sourceFilter || !sourceFilter.sharedMesh)
                {
                    return null;
                }

                target.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                result = target.AddComponent<MeshRenderer>();
            }
            else
            {
                return null;
            }

            result.sharedMaterials = source.sharedMaterials;
            result.enabled = true;
            result.shadowCastingMode = source.shadowCastingMode;
            result.receiveShadows = source.receiveShadows;
            return result;
        }

        private static Transform ResolveBone(Transform sourceBone, Dictionary<Transform, Transform> transformMap)
        {
            return sourceBone && transformMap.TryGetValue(sourceBone, out Transform mappedBone) ? mappedBone : null;
        }

        private static bool IsLogRenderer(Transform transform)
        {
            while (transform)
            {
                if (transform.name.StartsWith("log", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                transform = transform.parent;
            }
            return false;
        }

        private Renderer FindCloneRenderer(Renderer sourceRenderer, int slot)
        {
            string relativePath = GetRelativePath(sourceRenderer.transform);
            Transform cloneTransform = relativePath == null
                ? null
                : string.IsNullOrEmpty(relativePath) ? _clone.transform : _clone.transform.Find(relativePath);
            if (cloneTransform)
            {
                Component[] sourceComponents = sourceRenderer.transform.GetComponents(sourceRenderer.GetType());
                Component[] cloneComponents = cloneTransform.GetComponents(sourceRenderer.GetType());
                int componentIndex = Array.IndexOf(sourceComponents, sourceRenderer);
                if (componentIndex >= 0 && componentIndex < cloneComponents.Length)
                {
                    return cloneComponents[componentIndex] as Renderer;
                }
            }

            Material originalMaterial = slot >= 0 && slot < sourceRenderer.sharedMaterials.Length
                ? sourceRenderer.sharedMaterials[slot]
                : null;
            Renderer[] candidates = _clone.GetComponentsInChildren<Renderer>(true)
                .Where(candidate => candidate && candidate.GetType() == sourceRenderer.GetType())
                .ToArray();

            Renderer match = candidates.FirstOrDefault(candidate =>
                candidate.name == sourceRenderer.name
                && MaterialMatchesAtSlot(candidate, slot, originalMaterial));
            if (match)
            {
                return match;
            }

            match = candidates.FirstOrDefault(candidate => MaterialMatchesAtSlot(candidate, slot, originalMaterial));
            if (match)
            {
                return match;
            }

            return candidates.FirstOrDefault(candidate =>
                candidate.name == sourceRenderer.name
                && (slot < 0 || slot < candidate.sharedMaterials.Length));
        }

        private static bool MaterialMatchesAtSlot(Renderer renderer, int slot, Material material)
        {
            if (slot < 0)
            {
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            if (slot >= materials.Length || !materials[slot] || !material)
            {
                return false;
            }

            return materials[slot] == material || materials[slot].name == material.name;
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
                if (!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable)
                {
                    continue;
                }

                MeshCollider collider = meshRenderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
        }

        private static GameObject GetPlayerModelSource()
        {
            if (Player.m_localPlayer)
            {
                return GetPlayerVisualSource(Player.m_localPlayer.gameObject);
            }

            FejdStartup startup = UnityEngine.Object.FindObjectOfType<FejdStartup>();
            if (startup)
            {
                GameObject menuPlayer = GetGameObjectMember(startup, "m_playerInstance");
                if (menuPlayer)
                {
                    GameObject menuVisual = GetPlayerVisualSource(menuPlayer);
                    if (menuVisual)
                    {
                        return menuVisual;
                    }
                }
            }

            GameObject playerPrefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab("Player") : null;
            if (playerPrefab)
            {
                return GetPlayerVisualSource(playerPrefab);
            }

            GameObject menuPrefab = startup ? GetGameObjectMember(startup, "m_playerPrefab") : null;
            return GetPlayerVisualSource(menuPrefab);
        }

        private static GameObject GetPlayerVisualSource(GameObject playerObject)
        {
            if (!playerObject)
            {
                return null;
            }

            VisEquipment equipment = playerObject.GetComponentInChildren<VisEquipment>(true);
            return equipment ? equipment.gameObject : null;
        }

        private static GameObject GetGameObjectMember(object instance, string memberName)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(
                memberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object value = field?.GetValue(instance);
            if (value == null)
            {
                PropertyInfo property = type.GetProperty(
                    memberName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                value = property?.GetValue(instance, null);
            }

            if (value is GameObject gameObject)
            {
                return gameObject;
            }
            if (value is Component component)
            {
                return component.gameObject;
            }
            return null;
        }

        private void EquipPlayerPreview(string itemName)
        {
            Component equipment = _clone.GetComponentInChildren<VisEquipment>(true);
            ItemDrop itemDrop = _sourceRoot ? _sourceRoot.GetComponent<ItemDrop>() : null;
            if (!equipment || !itemDrop)
            {
                return;
            }

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
                if (method != null)
                {
                    method.Invoke(equipment, null);
                    return;
                }
            }
        }

        private void FramePreview()
        {
            Renderer[] renderers = _clone.GetComponentsInChildren<Renderer>()
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
            if (renderers.Length == 0 && _particleRenderers.Count == 0)
            {
                throw new InvalidOperationException("The selected prefab has no previewable renderers.");
            }

            _frameParticles = renderers.Length == 0;
            Bounds bounds;
            if (_frameParticles)
            {
                bounds = new Bounds(_particleRenderers[0].transform.position, Vector3.one * 2f);
                foreach (ParticleSystemRenderer renderer in _particleRenderers)
                {
                    bounds.Encapsulate(renderer.transform.position);
                }
            }
            else
            {
                bounds = renderers[0].bounds;
                for (int index = 1; index < renderers.Length; index++)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }
            }

            _clone.transform.position += PreviewOrigin - bounds.center;
            _center = PreviewOrigin;

            _baseDistance = DistanceForRadius(Mathf.Max(bounds.extents.magnitude, 0.1f));
        }

        private float DistanceForRadius(float radius) => radius / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;

        private void ApplyView()
        {
            if (!_pivot || !_camera)
            {
                return;
            }

            _pivot.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            float distance = _baseDistance * _zoom;
            _camera.nearClipPlane = Mathf.Max(0.01f, distance - _baseDistance * 0.75f);
            _camera.farClipPlane = distance + _baseDistance * 2f;
            // Measure the pan in fractions of the view height so it feels the same at any zoom.
            float viewHeight = 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _camera.transform.position = _center + new Vector3(0f, _cameraHeight * viewHeight, -distance);
            _camera.transform.rotation = Quaternion.identity;
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
