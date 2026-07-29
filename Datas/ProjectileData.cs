using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace wackydatabase.Datas
{
    [Serializable]
    [CanBeNull]
    public class ProjectileData
    {
        public string proj_name;
        public string? clonePrefabName;

        public ProjectileType? m_type;
        public WDamages? Damage;
        public float? m_aoe;
        public bool? m_dodgeable;
        public bool? m_blockable;
        public float? m_adrenaline;
        public float? m_attackForce;
        public float? m_backstabBonus;
        public string? StatusEffect;
        public float? m_healthReturn;
        public bool? m_canHitWater;

        public float? m_ttl;
        public float? m_gravity;
        public float? m_drag;
        public float? m_rayRadius;
        public float? m_hitNoise;
        public bool? m_doOwnerRaytest;

        public bool? m_stayAfterHitStatic;
        public bool? m_stayAfterHitDynamic;
        public float? m_stayTTL;
        public bool? m_attachToRigidBody;
        public bool? m_attachToClosestBone;
        public float? m_attachPenetration;
        public float? m_attachBoneNearify;
        public string? HideOnHit;
        public bool? m_stopEmittersOnHit;

        public EffectVerse[]? HitEffects;
        public EffectVerse[]? HitWaterEffects;

        public bool? m_bounce;
        public bool? m_bounceOnWater;
        public float? m_bouncePower;
        public float? m_bounceRoughness;
        public int? m_maxBounces;
        public float? m_minBounceVel;

        public bool? m_respawnItemOnHit;
        public bool? m_spawnOnTtl;
        public string? SpawnOnHit;
        public float? m_spawnOnHitChance;
        public int? m_spawnCount;
        public List<string>? RandomSpawnOnHit;
        public int? m_randomSpawnOnHitCount;
        public bool? m_randomSpawnSkipLava;
        public bool? m_showBreakMessage;
        public bool? m_staticHitOnly;
        public bool? m_groundHitOnly;
        public Vector3Data? m_spawnOffset;
        public bool? m_copyProjectileRotation;
        public bool? m_spawnRandomRotation;
        public bool? m_spawnFacingRotation;
        public EffectVerse[]? SpawnOnHitEffects;

        public bool? m_spawnProjectileNewVelocity;
        public float? m_spawnProjectileMinVel;
        public float? m_spawnProjectileMaxVel;
        public float? m_spawnProjectileRandomDir;
        public bool? m_spawnProjectileHemisphereDir;
        public bool? m_projectilesInheritHitData;
        public bool? m_onlySpawnedProjectilesDealDamage;
        public bool? m_divideDamageBetweenProjectiles;

        public float? m_rotateVisual;
        public float? m_rotateVisualY;
        public float? m_rotateVisualZ;
        public string? Visual;
        public bool? m_canChangeVisuals;

        public Skills.SkillType? m_skill;
        public float? m_raiseSkillAmount;
    }

    [Serializable]
    [CanBeNull]
    public class Vector3Data
    {
        public float? x;
        public float? y;
        public float? z;
    }
}
