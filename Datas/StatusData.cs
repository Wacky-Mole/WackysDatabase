using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace wackydatabase.Datas
{

    [Serializable]
    [CanBeNull]
    public class StatusData 
    {
        public string? Name;// required
        public string? Status_m_name;// required
        public string? Category;
        public string? IconName;
        public string? ClonedSE;
        public string? CustomIcon;
        public bool? FlashIcon;
        public bool? CooldownIcon;
        public string? Tooltip;
        public StatusEffect.StatusAttribute? Attributes;
        public MessageHud.MessageType? StartMessageLoc;
        public string? StartMessage;
        public MessageHud.MessageType? StopMessageLoc;
        public string? StopMessage;
        public MessageHud.MessageType? RepeatMessageLoc;
        public string? RepeatMessage;
        public float? RepeatInterval;
        public float? TimeToLive;
        public string? EndingStatusEffect;
        public string[] StartEffect_;
        public string[] StopEffect_;        
        public EffectVerse[] StartEffect_PLUS;
        public EffectVerse[] StopEffect_PLUS;
        public float? Cooldown;
        public string? ActivationAnimation;
        public SEdata? SeData;
        public SEShield SeShield;
        public SEPoison SePoison;
        public SEFrost SeFrost;
        //public FieldInfo? fld = typeof(StatusEffect).GetField("m_tickInterval").GetValue;

    }

    [Serializable]
    [CanBeNull]
    public class SEShield
    {
        public float? AbsorbDmg;
        public float? AbsorbDmgWorldLevel;
        public float? LevelUpSkillFactor;
        public int? TtlPerItemLevel;
        public float? AbsorbDmgPerSkill;

    }

    [Serializable]
    [CanBeNull]
    public class SEPoison
    {
        public float? m_damageInterval;
        public float? m_baseTTL;
        public float? m_TTLPerDamagePlayer;
        public float? m_TTLPerDamage;
        public float? m_TTLPower;

    }    
    [Serializable]
    [CanBeNull]
    public class SEFrost
    {
        public float? m_freezeTimeEnemy;
        public float? m_freezeTimePlayer;
        public float? m_minSpeedFactor;

    }


    [Serializable]
    [CanBeNull]
    public class SEdata 
    {

        //HP Per Tick 
        public float? m_tickTimer;

        public float? m_healthOverTimeTimer;

        public float? m_healthOverTimeTicks;

        public float? m_healthOverTimeTickHP;

        // Health

        public float? m_heatlhUpFront;

        public float? m_tickInterval;

        public float? m_healthPerTickMinHealthPercentage;

        public float? m_healthPerTick;

        //[Header("Health over time")]
        public float? m_healthOverTime;

        public float? m_healthOverTimeDuration;

        public float? m_healthOverTimeInterval;

        //[Header("Stamina")]
        public float? m_staminaUpFront;

        public float? m_staminaOverTime;

        public float? m_staminaOverTimeDuration;

        public float? m_staminaDrainPerSec;

        public float? m_runStaminaDrainModifier;

        public float? m_jumpStaminaUseModifier;

        // new ones
        public float? m_attackStaminaUseModifier;

        public float? m_blockStaminaUseModifier;

        public float? m_dodgeStaminaUseModifier;

        public float? m_swimStaminaUseModifier;

        public float? m_homeItemStaminaUseModifier;

        public float? m_sneakStaminaUseModifier;

        public float? m_runStaminaUseModifier;

        // Adrenaline
        public float? m_adrenalineUpFront;

        public float? m_adrenalineModifier;       
        
        // Stagger
        public float? m_staggerModifier;

        public float? m_staggerTimeBlockBonus;

        //[Header("Eitr")]
        public float? m_eitrUpFront;

        public float? m_eitrOverTime;

        public float? m_eitrOverTimeDuration;

        //[Header("Regen modifiers")]
        public float? m_healthRegenMultiplier;

        public float? m_staminaRegenMultiplier;

        public float? m_eitrRegenMultiplier ;

        // Armor
        public float? m_armorAdd;

        public float? m_armorMultiplier;

        // [Header("Modify raise skill")]
        public Skills.SkillType? m_raiseSkill;

        public float? m_raiseSkillModifier;

       // [Header("Modify skill level")]
        public Skills.SkillType? m_skillLevel;

        public float? m_skillLevelModifier;

        public Skills.SkillType? m_skillLevel2;

        public float? m_skillLevelModifier2;

        //[Header("Hit modifier")]
        public List<HitData.DamageModPair>? m_mods = new List<HitData.DamageModPair>();

        //[Header("Attack")]
        public Skills.SkillType? m_modifyAttackSkill;

        public float? m_damageModifier;

        //[Header("Sneak")]
        public float? m_noiseModifier;

        public float? m_stealthModifier;

        //[Header("Carry weight")]
        public float? m_addMaxCarryWeight;

        //[Header("Speed")]
        public float? m_speedModifier;

        public Vector3? m_jumpModifier;

        //[Header("Fall")]
        public float? m_maxMaxFallSpeed;

        public float? m_fallDamageModifier;


        //[Header("Wind")]

        public float? m_windMovementModifier;
    }




}
