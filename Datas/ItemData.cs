using System;
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace wackydatabase.Datas
{
	[Serializable]
    [CanBeNull]
	public class WItemData
	{
        #nullable enable
        public string name; // must have

        public string? m_name;

        public string? m_description;

        //public bool? clone;

        public string? clonePrefabName;

        public string? customIcon;

        //public string cloneEffects;

        public string? cloneMaterial;

        public float? m_weight; // must have

        //public string m_skillType;

        //public string m_animationState;

        public AttackArm? Primary_Attack;

        public AttackArm? Secondary_Attack;

        public WDamages? Damage;

        public WDamages? Damage_Per_Level;

        public int? m_maxStackSize;

        public float? m_foodHealth;

        public float? m_foodStamina;

        public float? m_foodRegen;

        public float? m_foodBurnTime;

        public string? m_foodColor;

        public float? m_FoodEitr;

        public float? m_armor;

        public float? m_armorPerLevel;

        public float? m_movementModifier;

        public float? m_EitrRegen;

        public float? m_blockPower;

        public float? m_blockPowerPerLevel;

        public bool? m_canBeReparied;

        public float? m_timedBlockBonus;

        public float? m_deflectionForce;

        public float? m_deflectionForcePerLevel;

        public float? m_backstabbonus;

        public float? m_knockback;

        public bool? m_destroyBroken;

        public bool? m_dodgeable;

        public float? m_maxDurability;

        public float? m_durabilityDrain;

        public float? m_durabilityPerLevel;

        public float? m_equipDuration;

        public float? m_holdDurationMin;

        public float? m_holdStaminaDrain;

        //public string m_holdAnimationState;

        public int? m_maxQuality;

        public bool? m_useDurability;

        public float? m_useDurabilityDrain;

        public bool? m_questItem;

        public bool? m_teleportable;

        public int? m_toolTier;

        public int? m_value;

        public List<string>? damageModifiers = new List<string>();

        #nullable disable
    }


    [Serializable]
    public class AttackArm
        {
			public float? m_attackStamina;
            public float? m_attackHealthPercentage;
            public float? m_eitrCost;

    }


	[Serializable]
	public class ArmorData { 
	
		public string name;

		public float armor;
		public float armorPerLevel;
		public float movementModifier;

		public List<string> damageModifiers = new List<string>();
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

    public class DamageMod //HitData.DamageModifier
    {

    }




    [Serializable]
	public class WIngredients
	{
		public string id;
		public int amount;
		public int amountPerLevel;

	}

}