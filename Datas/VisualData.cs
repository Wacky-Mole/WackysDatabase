using System;
using System.Collections.Generic;
using UnityEngine;

namespace wackydatabase.Datas
{
    [Serializable]
    public class VisualData
    {
        public string PrefabName;
        public ShaderData Shader;
        public MaterialData Material;
        public MaterialData Particle;
        public LightData Light;
        public RealtimeEffectData Effect;
    }
    public class LightData
    {
        public Color Color;
        public float Range;
    }

    [Serializable]
    public class ShaderData
    {
        public string Material;
        public string Override;
    }

    [Serializable]
    public class MaterialData
    {
        public Dictionary<string, Color> Colors;
        public Dictionary<string, float> Floats;
    }

    [Serializable]
    public enum RealtimeEffectType
    {
        Proximity,
        Time,
        Biome
    }

    [Serializable]
    public class RealtimeEffectTrigger
    {
        public ValheimTime Time;
        public ValheimTime TimeSpan;
        public List<string> Entities;
        public float Radius;
        public Heightmap.Biome Biome;
    }

    [Serializable]
    public class RealtimeEffectData
    {
        public RealtimeEffectType Type;
        public RealtimeEffectTrigger Trigger;
        public MaterialData Material;
    }
}