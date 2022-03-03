using System;
using recipecustomization;

[Serializable]
public class WItemData
{
	public string name;

	public string m_name;

	public string m_description;

	public bool clone;

	public string clonePrefabName;

	public string cloneEffects;

	public string cloneColor;

	public float m_weight;

	public int m_maxStackSize;

	public float m_foodHealth;

	public float m_foodStamina;

	public float m_foodRegen;

	public float m_foodBurnTime;

	public string m_foodColor;

	public float m_armor;

	public float m_armorPerLevel;

	public float m_blockPower;

	public float m_blockPowerPerLevel;

	public bool m_canBeReparied;

	public WDamages m_damages;

	public WDamages m_damagesPerLevel;

	public float m_timedBlockBonus;

	public float m_deflectionForce;

	public float m_deflectionForcePerLevel;

	public bool m_destroyBroken;

	public bool m_dodgeable;

	public float m_maxDurability;

	public float m_durabilityDrain;

	public float m_durabilityPerLevel;

	public float m_equipDuration;

	public float m_holdDurationMin;

	public float m_holdStaminaDrain;

	public int m_maxQuality;

	public bool m_useDurability;

	public float m_useDurabilityDrain;

	public bool m_questItem;

	public bool m_teleportable;

	public int m_toolTier;

	public int m_value;
}