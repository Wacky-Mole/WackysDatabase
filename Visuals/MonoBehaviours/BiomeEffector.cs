using System;
using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    [Serializable]
    public class BiomeEffector : MonoBehaviour, IRealtimeEffector
    {
        private const int UPDATE_INTERVAL = 30;
        private const int TRANSITION_DURATION = 1;

        private Renderer _renderer = null;
        private MaterialPropertyBlock _block = null;

        private List<IRealtimeEffect> _effects = new List<IRealtimeEffect>();

        private float _currentRatio = 0.0f;

        public RealtimeEffectData _effectContext;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            _renderer = GetComponentInChildren<Renderer>();

            if (_renderer == null)
            {
                Debug.LogError("BiomeEffector: Failed to retrieve renderer");
            }

            int item = _renderer.material.GetInt("_Item");

            VisualData data = VisualController.GetVisualByIndex(item);

            if (data == null)
            {
                return;
            }

            if (data.Effect != null)
            {
                ApplyContext(data.Effect);
            }
        }

        void LateUpdate()
        {
            if (_effectContext == null)
            {
                return;
            }

            // Update once every 30 updates, no point updating more often
            if (Time.frameCount % UPDATE_INTERVAL != 0)
            {
                return;
            }

            try
            {
                ValheimTime vt = ValheimTime.Get();

                _currentRatio = Mathf.MoveTowards(_currentRatio, GetRatio(), (TRANSITION_DURATION * UPDATE_INTERVAL) * Time.deltaTime);

                _renderer.GetPropertyBlock(_block);
                _effects.ForEach(e => { e.Apply(_block, _renderer.material, _effectContext, _currentRatio); });
                _renderer.SetPropertyBlock(_block);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                Debug.Log(ex.InnerException);
            }
        }

        private float GetRatio()
        {
            Heightmap.Biome biome = EnvMan.instance.GetBiome();

            return biome == _effectContext.Trigger.Biome ? 1.0f : 0.0f;
        }

        public void ApplyContext(RealtimeEffectData data)
        {
            _effectContext = data;

            try
            {
                if (data.Material.Colors != null && data.Material.Colors.Count > 0)
                {
                    foreach (KeyValuePair<string, Color> entity in data.Material.Colors)
                    {
                        _effects.Add(new RealtimeColorEffect(entity.Key, entity.Value));

                        Debug.Log(string.Format("[BiomeEffector] [Color] {0} ({1},{2},{3},{4})",
                            entity.Key, entity.Value.r, entity.Value.g, entity.Value.b, entity.Value.a));
                    }
                }

                if (data.Material.Floats != null && data.Material.Floats.Count > 0)
                {
                    foreach (KeyValuePair<string, float> entity in data.Material.Floats)
                    {
                        _effects.Add(new RealtimeFloatEffect(entity.Key, entity.Value));

                        Debug.Log(string.Format("[BiomeEffector] [Float] {0} ({1})", entity.Key, entity.Value));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                Debug.LogError(ex.InnerException);
            }

        }
        public void SetVisuals(int item)
        {
            GetComponent<Renderer>().material.SetInt("_Item", item);
        }
    }
}