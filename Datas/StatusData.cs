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
        public string? Name;
        public string? m_Name;
        public string? Category;
        public string? IconName;
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
        public List<string>? StartEffect;
        public List<string>? StopEffect;
        public float? Cooldown;
        public string? ActivationAnimation;
        public SEdata? SeData;
        //public FieldInfo? fld = typeof(StatusEffect).GetField("m_tickInterval").GetValue;

    }

    [Serializable]
    [CanBeNull]
    public class SEdata 
    {
        public float? m_tickInterval;

        public float? m_healthPerTickMinHealthPercentage;

        public float? m_healthPerTick;

        //[Header("Health over time")]
        public float? m_healthOverTime;

        public float? m_healthOverTimeDuration;

        public float? m_healthOverTimeInterval;

        //[Header("Stamina")]
        public float? m_staminaOverTime;

        public float? m_staminaOverTimeDuration;

        public float? m_staminaDrainPerSec;

        public float? m_runStaminaDrainModifier;

        public float? m_jumpStaminaUseModifier;

        //[Header("Eitr")]
        public float? m_eitrOverTime;

        public float? m_eitrOverTimeDuration;

        //[Header("Regen modifiers")]
        public float? m_healthRegenMultiplier;

        public float? m_staminaRegenMultiplier;

        public float? m_eitrRegenMultiplier ;

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

        //public Vector3? m_jumpModifier;

        //[Header("Fall")]
        public float? m_maxMaxFallSpeed;

        public float? m_fallDamageModifier;

        public float? m_tickTimer;

        public float? m_healthOverTimeTimer;

        public float? m_healthOverTimeTicks;

        public float? m_healthOverTimeTickHP;


    }




        //The rest are virtual, I wonder if I can override the default layout to remove Icon and just get Icon name. 


        /*
         * 
         * if (type.IsSubclassOf(typeof(TestBaseClass)) // some reflection
{
    var instance = Activator.CreateInstance(type);

    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    {
       var value = prop.GetValue(instance);
    }
}
        m_tickInterval: 0
m_healthPerTickMinHealthPercentage: 0
m_healthPerTick: 0
m_healthOverTime: 0
m_healthOverTimeDuration: 0
m_healthOverTimeInterval: 5
m_staminaOverTime: 0
m_staminaOverTimeDuration: 0
m_staminaDrainPerSec: 0
m_runStaminaDrainModifier: 0
m_jumpStaminaUseModifier: 0
m_eitrOverTime: 0
m_eitrOverTimeDuration: 0
m_healthRegenMultiplier: 1
m_staminaRegenMultiplier: 1
m_eitrRegenMultiplier: 1
m_raiseSkill: None
m_raiseSkillModifier: 0
m_skillLevel: None
m_skillLevelModifier: 0
m_skillLevel2: None
m_skillLevelModifier2: 0
m_mods: []
        m_modifyAttackSkill: None
        m_damageModifier: 1
m_noiseModifier: 0
m_stealthModifier: 0
m_addMaxCarryWeight: 0
m_speedModifier: 0
m_jumpModifier: &o0
  x: 0
  y: 0
  z: 0
  normalized: *o0
  magnitude: 0
  sqrMagnitude: 0
m_maxMaxFallSpeed: 0
m_fallDamageModifier: 0

*/



    
}
