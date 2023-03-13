using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace wackydatabase.Datas
{
    
	[Serializable]
    [CanBeNull]
    public class StatusData:StatusEffect
    {
        public string? Name;
        public string? m_Name;
        public string? Category;
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
        public EffectList? StartEffect;
        public EffectList? StopEffect;
        public float? Cooldown;
        public string? ActivationAnimation;
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
}
