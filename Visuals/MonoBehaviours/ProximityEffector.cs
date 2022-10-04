using System;
using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    [Serializable]
    [RequireComponent(typeof(SphereCollider))]
    public class ProximityEffector : MonoBehaviour, IRealtimeEffector
    {
        private const int UPDATE_INTERVAL = 15;
        private const int CHARACTER_TRIGGER = 14;
        private const int CHARACTER = 9;

        private const int TRANSITION_DURATION = 1;
        private float _currentRatio = 0.0f;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private SphereCollider _sphere;

        private RealtimeEffectData _effectContext;
        private List<IRealtimeEffect> _effects = new List<IRealtimeEffect>();

        private Collider[] _hits = new Collider[10];
        private Collider[] _targets = new Collider[5];
        private bool _isClear = true;

        public void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _sphere = GetComponent<SphereCollider>();
            _block = new MaterialPropertyBlock();

            int item = _renderer.material.GetInt("_Item");
            VisualData data = VisualController.GetVisualByIndex(item);

            if (data == null)
            {
                return;
            }

            if (data.Effect != null)
            {
                ApplyContext(data.Effect);

                var c = data.Effect;

                if (c.Trigger != null)
                {
                    gameObject.layer = CHARACTER_TRIGGER;
                    _sphere.isTrigger = true;
                    _sphere.radius = c.Trigger.Radius;
                    _sphere.enabled = true;
                }
            }
        }

        public void Start()
        {
            if (!_sphere.enabled)
            {
                _sphere.enabled = true;
            }

            UpdateTargets();
            Run();
        }

        private void Run()
        {
            // TODO Move this to a cached value somewhere. It only updates once per second.
            ValheimTime vt = ValheimTime.Get();

            _currentRatio = Mathf.MoveTowards(_currentRatio, GetRatio(), (TRANSITION_DURATION * UPDATE_INTERVAL) * Time.deltaTime);

            _renderer.GetPropertyBlock(_block);
            _effects.ForEach(e => { e.Apply(_block, _renderer.material, _effectContext, _currentRatio); });
            _renderer.SetPropertyBlock(_block);
        }

        public void LateUpdate()
        {
            if (_effectContext == null)
            {
                return;
            }

            if (!_isClear)
            {
                if (Time.frameCount % UPDATE_INTERVAL == 0)
                {
                    VerifyTargets();
                }
            }

            try
            {
                if (Time.frameCount % UPDATE_INTERVAL == 0)
                {
                    Run();
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                Debug.Log(ex.InnerException);
            }
        }

        private float GetRatio()
        {
            float distance = _sphere.radius;

            for (int i = 0; i < 5; i++)
            {
                if (_targets[i] != null)
                {
                    float d = Vector3.Distance(_targets[i].transform.position, transform.position);

                    if (d < distance)
                    {
                        distance = d;
                    }
                }
            }

            return 1 - (distance / _sphere.radius);
        }

        public void OnTriggerEnter(Collider other)
        {
            if (_effectContext == null)
            {
                return;
            }

            if (_effectContext.Trigger != null && _effectContext.Trigger.Entities.Count > 0)
            {
                if(_effectContext.Trigger.Entities.Contains(other.name))
                {
                    UpdateTargets();
                }
            }
        }

        private void UpdateTargets()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, _sphere.radius, _hits, (1 << CHARACTER));
            int index = 0;

            for (int i = 0; i < hits; i++)
            {
                if (_effectContext.Trigger.Entities.Contains(_hits[i].name))
                {
                    _targets[index++] = _hits[i];
                }
            }
        }

        private void VerifyTargets()
        {
            bool clear = true;

            for (int i = 0; i < 5; i++)
            {
                if (_targets[i] != null)
                {
                    clear = false;
                }
            }

            _isClear = clear;

            if (!_isClear)
            {
                UpdateTargets();
            }
        }

        public void SetVisuals(int item)
        {
            this.GetComponent<Renderer>().material.SetInt("_Item", item);
        }


        public void ApplyContext(RealtimeEffectData data)
        {
            _effectContext = data;

            try
            {
                Debug.Log("Applying Context");

                if (data.Material.Colors != null && data.Material.Colors.Count > 0)
                {
                    foreach (KeyValuePair<string, Color> entity in data.Material.Colors)
                    {
                        Debug.Log("Adding Realtime Color Effect for: " + entity.Key);

                        _effects.Add(new RealtimeColorEffect(entity.Key, entity.Value));

                        Debug.Log("Added Realtime Color Effect for: " + entity.Key);
                    }
                }

                if (data.Material.Floats != null && data.Material.Floats.Count > 0)
                {
                    foreach (KeyValuePair<string, float> entity in data.Material.Floats)
                    {
                        Debug.Log("Adding Realtime Float Effect for: " + entity.Key);

                        _effects.Add(new RealtimeFloatEffect(entity.Key, entity.Value));

                        Debug.Log("Added Realtime Float Effect for: " + entity.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                Debug.LogError(ex.InnerException);
            }
        }
    }
}
