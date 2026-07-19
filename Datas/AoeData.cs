using System;
using JetBrains.Annotations;

namespace wackydatabase.Datas
{
    [Serializable]
    [CanBeNull]
    public class AoeData
    {
        public string aoe_name;
        public string? clonePrefabName;

        public string? m_name;
        public bool? m_useAttackSettings;
        public WDamages? Damage;
        public bool? m_scaleDamageByDistance;
        public bool? m_dodgeable;
        public bool? m_blockable;
        public int? m_toolTier;
        public float? m_attackForce;
        public float? m_backstabBonus;

        public string? StatusEffect;
        public string? StatusEffectIfBoss;
        public string? StatusEffectIfPlayer;
        public WDamages? DamagePerLevel;

        public string? SpawnOnHitTerrain;
        public bool? m_hitTerrainOnlyOnce;
        public FootStep.GroundMaterial? m_spawnOnGroundType;
        public float? m_groundLavaValue;
        public float? m_hitNoise;
        public bool? m_placeOnGround;
        public bool? m_randomRotation;

        public int? m_maxTargetsFromCenter;
        public int? m_multiSpawnMin;
        public int? m_multiSpawnMax;
        public float? m_multiSpawnDistanceMin;
        public float? m_multiSpawnDistanceMax;
        public float? m_multiSpawnScaleMin;
        public float? m_multiSpawnScaleMax;
        public float? m_multiSpawnSpringDelayMax;

        public float? m_chainStartChance;
        public float? m_chainStartChanceFalloff;
        public float? m_chainChancePerTarget;
        public string? ChainObject;
        public float? m_chainStartDelay;
        public int? m_chainMinTargets;
        public int? m_chainMaxTargets;
        public EffectVerse[]? ChainEffects;

        public float? m_damageSelf;
        public bool? m_hitOwner;
        public bool? m_hitParent;
        public bool? m_hitSame;
        public bool? m_hitFriendly;
        public bool? m_hitEnemy;
        public bool? m_hitCharacters;
        public bool? m_hitProps;
        public bool? m_hitTerrain;
        public bool? m_ignorePVP;

        public bool? m_launchCharacters;
        public Vector2Data? m_launchForceMinMax;
        public float? m_launchForceUpFactor;

        public Skills.SkillType? m_skill;
        public bool? m_canRaiseSkill;
        public bool? m_useTriggers;
        public bool? m_triggerEnterOnly;

        public float? m_radius;
        public float? m_activationDelay;
        public float? m_ttl;
        public float? m_ttlMax;
        public bool? m_hitAfterTtl;
        public float? m_hitInterval;
        public bool? m_hitOnEnable;
        public bool? m_attachToCaster;

        public EffectVerse[]? HitEffects;
        public EffectVerse[]? InitiateEffects;
    }

    [Serializable]
    [CanBeNull]
    public class Vector2Data
    {
        public float? x;
        public float? y;
    }

}
