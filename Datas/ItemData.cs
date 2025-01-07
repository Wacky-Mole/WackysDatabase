using System;
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;
using static ItemDrop.ItemData;

namespace wackydatabase.Datas
{
    [Serializable]
    [CanBeNull]
    public class WItemData
    {
#nullable enable
        public string name; // must have

        public float m_weight; // must have

        public string? m_name;

        public string? m_description;

        //public bool? clone;

        public string? clonePrefabName;

        public string? mockName;

        public string? customIcon;

        //public string cloneEffects;

        public string? material;

        public string[]? materials;

        public CustomVisual? customVisual;

        public string? snapshotRotation;

        public bool? snapshotOnMaterialChange;

        public string? sizeMultiplier;

        public float? scale_weight_by_quality;

        public AttackArm? Primary_Attack;

        public AttackArm? Secondary_Attack;

        public WDamages? Damage;

        public WDamages? Damage_Per_Level;

        public ArmorData? Armor;

        public FoodData? FoodStats;

        public StatMods? Moddifiers;

        public SE_Equip? SE_Equip;

        public SE_SET_Equip? SE_SET_Equip;

        public ShieldData? ShieldStats;

        public int? m_maxStackSize;

        public bool? m_canBeReparied;

        public bool? m_destroyBroken;

        public bool? m_dodgeable;

        public string? Attack_status_effect;

        public float? Attack_status_effect_chance;

        public string? spawn_on_hit;

        public string? spawn_on_terrain_hit;

        public bool? m_questItem;

        public bool? m_teleportable;

        public float? m_backstabbonus;

        public float? m_knockback;


        public bool? m_useDurability;

        public float? m_useDurabilityDrain;

        public float? m_durabilityDrain;

        public float? m_maxDurability;

        public float? m_durabilityPerLevel;

        public float? m_equipDuration;

        public ItemDrop.ItemData.ItemType? Attach_Override;


        public Skills.SkillType? m_skillType;

        public ItemDrop.ItemData.AnimationState? m_animationState;

        public ItemDrop.ItemData.ItemType? m_itemType;


        public int? m_toolTier;

        public int? m_maxQuality;

        public int? m_value;

        public string? ConsumableStatusEffect;

        public List<string>? damageModifiers = new List<string>();

        public GEffects? GEffects;

        public GEffectsPLUS? GEffectsPLUS;


#nullable disable
    }


    [Serializable]
    public class AttackArm  //:Attack
    {
        public Attack.AttackType? AttackType;
        public string? Attack_Animation;
        public int? Attack_Random_Animation;
        public int? Chain_Attacks;
        public bool? Hit_Terrain;
        public bool? Hit_Friendly;
        public float? Custom_AttackSpeed;

        public float? m_attackStamina;
        public float? m_eitrCost;
        public float? AttackHealthCost;
        public float? m_attackHealthPercentage;

        public float? SpeedFactor;
        public float? Attack_Start_Noise;
        public float? Attack_Hit_Noise;
        public float? Dmg_Multiplier_Per_Missing_Health;
        public float? Damage_Multiplier_By_Health_Deficit_Percent;
        public float? Stamina_Return_Per_Missing_HP;
        public float? DmgMultiplier;
        public float? ForceMultiplier;
        public float? StaggerMultiplier;
        public float? RecoilMultiplier;
        public int? SelfDamage;
        public bool? Attack_Kills_Self;

        public float? AttackRange;
        public float? AttackHeight;
        public string? Spawn_On_Trigger;

        public bool? Requires_Reload;
        public string? Reload_Animation;
        public float? ReloadTime;
        public float? ReloadTimeMultiplier; // crossbowOnly
        public float? Reload_Stamina_Drain;
        public float? Reload_Eitr_Drain;

        public bool? Bow_Draw;
        public float? Bow_Duration_Min;
        public float? Bow_Stamina_Drain;
        public string? Bow_Animation_State;

        public float? Attack_Angle;
        public float? Attack_Ray_Width;
        public bool? Lower_Dmg_Per_Hit;
        public bool? Hit_Through_Walls;
        public bool? Multi_Hit;
        public bool? Pickaxe_Special;
        public float? Last_Chain_Dmg_Multiplier;
        public DestructibleType? Reset_Chain_If_hit;

        public string? SpawnOnHit;
        public float? SpawnOnHit_Chance;

        public string? Attack_status_effect;
        public float? Attack_status_effect_chance;
     
        public float? Raise_Skill_Amount;
        public DestructibleType? Skill_Hit_Type;
        public Skills.SkillType? Special_Hit_Skill;
        public DestructibleType? Special_Hit_Type;
       
        public string? Attack_Projectile;
        public float? Projectile_Vel;
        public float? Projectile_Accuraccy;
        public int? Projectiles;

        public bool? Skill_Accuracy; // new
        public float? Launch_Angle;
        public int? Projectile_Burst;
        public float? Burst_Interval;
        public bool? Destroy_Previous_Projectile;
        public bool? PerBurst_Resource_usage;
        public bool? Looping_Attack;
        public bool? Consume_Item;

        public AEffects? AEffects;

        public AEffectsPLUS? AEffectsPLUS;

    }

    [Serializable]
    public class CustomVisual
    {
        public string? base_mat;
        public string? chest;
        public string? legs;
        public string? realtime;
    }

    [Serializable]

    public class EffectVerse //: EffectList.EffectData
    {
        public string? name;
        public bool? m_enabled;
        public int? m_variant;
        public bool? m_attach;
        public bool? m_follow;
        public bool? m_inheritParentRotation;
        public bool? m_inheritParentScale;
        public bool? m_multiplyParentVisualScale;
        public bool? m_randomRotation;
        public bool? m_scale;
        public string? m_childTransform;

    }

    public class AEffects
    {
        //EffectList.EffectData EffectData;
        public string[]? Hit_Effects;
        public string[]? Hit_Terrain_Effects;
        public string[]? Start_Effect;
        public string[]? Trigger_Effect;
        public string[]? Trail_Effect;
        public string[]? Burst_Effect;
    }

    public class GEffects
    {
        public string[]? Hit_Effects;
        public string[]? Hit_Terrain_Effects;
        public string[]? Start_Effect;
        public string[]? Hold_Start_Effects;
        public string[]? Trigger_Effect;
        public string[]? Trail_Effect;

    }    
    public class AEffectsPLUS
    {
        //EffectList.EffectData EffectData;
        public EffectVerse[]? Hit_Effects;
        public EffectVerse[]? Hit_Terrain_Effects;
        public EffectVerse[]? Start_Effect;
        public EffectVerse[]? Trigger_Effect;
        public EffectVerse[]? Trail_Effect;
        public EffectVerse[]? Burst_Effect;
    }

    public class GEffectsPLUS
    {
        public EffectVerse[]? Hit_Effects;
        public EffectVerse[]? Hit_Terrain_Effects;
        public EffectVerse[]? Start_Effect;
        public EffectVerse[]? Hold_Start_Effects;
        public EffectVerse[]? Trigger_Effect;
        public EffectVerse[]? Trail_Effect;

    }


    [Serializable]
    public class FoodData
    {
        public int? feastStacks;
        public float? m_foodHealth;
        public float? m_foodStamina;
        public float? m_foodRegen;
        public float? m_foodBurnTime;
        //public string? m_foodColor;
        public float? m_FoodEitr;
    }
    [Serializable]
    public class StatMods
    {
        public float? m_movementModifier;
        public float? m_EitrRegen;
        public float? m_homeItemsStaminaModifier;
        public float? m_heatResistanceModifier;
        public float? m_jumpStaminaModifier;
        public float? m_attackStaminaModifier;
        public float? m_blockStaminaModifier;
        public float? m_dodgeStaminaModifier;
        public float? m_swimStaminaModifier;
        public float? m_sneakStaminaModifier;
        public float? m_runStaminaModifier;

    }

    [Serializable]
    public class SE_Equip
    {
        public string? EffectName;
    }

    [Serializable]
    public class SE_SET_Equip
    {
        public string? SetName;
        public int? Size;
        public string? EffectName;

    }


    [Serializable]
    public class ShieldData
    {
        public float? m_blockPower;
        public float? m_blockPowerPerLevel;
        public float? m_timedBlockBonus;
        public float? m_deflectionForce;
        public float? m_deflectionForcePerLevel;
    }



    [Serializable]
    public class ArmorData
    {

        //public string? name;

        public float? armor;
        public float? armorPerLevel;
        //public float movementModifier;

    }

    [Serializable]
    public class WDamages // can't get the inhertance in json to sterilize
    {
        public float Blunt;

        public float Chop;

        public float Damage;

        public float Fire;

        public float Frost;

        public float Lightning;

        public float Pickaxe;

        public float Pierce;

        public float Poison;

        public float Slash;

        public float Spirit;

    }



    [Serializable]
    public class WIngredients
    {
        public string id;
        public int amount;
        public int amountPerLevel;

    }

}