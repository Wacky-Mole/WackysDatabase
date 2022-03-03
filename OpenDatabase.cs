using System.Globalization;
using UnityEngine;

namespace recipecustomization
{

	public class Helper
	{
		public static ItemDrop.ItemData.SharedData GetSharedDataBySharedName(string name)
		{
			foreach (GameObject item in ObjectDB.instance.m_items)
			{
				ItemDrop component = item.GetComponent<ItemDrop>();
				if ((Object)(object)component != (Object)null && component.m_itemData.m_shared.m_name == name)
				{
					return component.m_itemData.m_shared;
				}
			}
			return null;
		}

		public static JItemDrop GetItemDataFromItemDrop(ItemDrop drop)
		{
			return new JItemDrop
			{
				name = ((Object)drop).get_name(),
				itemData = GetItemDataFromItemDrop(drop.m_itemData)
			};
		}

		public static JItemData GetItemDataFromItemDrop(ItemDrop.ItemData data)
		{
			//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
			JDamages damages = null;
			if (data.m_shared.m_damages.m_blunt > 0f || data.m_shared.m_damages.m_chop > 0f || data.m_shared.m_damages.m_damage > 0f || data.m_shared.m_damages.m_fire > 0f || data.m_shared.m_damages.m_frost > 0f || data.m_shared.m_damages.m_lightning > 0f || data.m_shared.m_damages.m_pickaxe > 0f || data.m_shared.m_damages.m_pierce > 0f || data.m_shared.m_damages.m_poison > 0f || data.m_shared.m_damages.m_slash > 0f || data.m_shared.m_damages.m_spirit > 0f)
			{
				damages = new JDamages
				{
					m_blunt = data.m_shared.m_damages.m_blunt,
					m_chop = data.m_shared.m_damages.m_chop,
					m_damage = data.m_shared.m_damages.m_damage,
					m_fire = data.m_shared.m_damages.m_fire,
					m_frost = data.m_shared.m_damages.m_frost,
					m_lightning = data.m_shared.m_damages.m_lightning,
					m_pickaxe = data.m_shared.m_damages.m_pickaxe,
					m_pierce = data.m_shared.m_damages.m_pierce,
					m_poison = data.m_shared.m_damages.m_poison,
					m_slash = data.m_shared.m_damages.m_slash,
					m_spirit = data.m_shared.m_damages.m_spirit
				};
			}
			JDamages damagesPerLevel = null;
			if (data.m_shared.m_damagesPerLevel.m_blunt > 0f || data.m_shared.m_damagesPerLevel.m_chop > 0f || data.m_shared.m_damagesPerLevel.m_damage > 0f || data.m_shared.m_damagesPerLevel.m_fire > 0f || data.m_shared.m_damagesPerLevel.m_frost > 0f || data.m_shared.m_damagesPerLevel.m_lightning > 0f || data.m_shared.m_damagesPerLevel.m_pickaxe > 0f || data.m_shared.m_damagesPerLevel.m_pierce > 0f || data.m_shared.m_damagesPerLevel.m_poison > 0f || data.m_shared.m_damagesPerLevel.m_slash > 0f || data.m_shared.m_damagesPerLevel.m_spirit > 0f)
			{
				damagesPerLevel = new JDamages
				{
					m_blunt = data.m_shared.m_damagesPerLevel.m_blunt,
					m_chop = data.m_shared.m_damagesPerLevel.m_chop,
					m_damage = data.m_shared.m_damagesPerLevel.m_damage,
					m_fire = data.m_shared.m_damagesPerLevel.m_fire,
					m_frost = data.m_shared.m_damagesPerLevel.m_frost,
					m_lightning = data.m_shared.m_damagesPerLevel.m_lightning,
					m_pickaxe = data.m_shared.m_damagesPerLevel.m_pickaxe,
					m_pierce = data.m_shared.m_damagesPerLevel.m_pierce,
					m_poison = data.m_shared.m_damagesPerLevel.m_poison,
					m_slash = data.m_shared.m_damagesPerLevel.m_slash,
					m_spirit = data.m_shared.m_damagesPerLevel.m_spirit
				};
			}
			JItemData jItemData = new JItemData
			{
				m_armor = data.m_shared.m_armor,
				m_armorPerLevel = data.m_shared.m_armorPerLevel,
				m_blockPower = data.m_shared.m_blockPower,
				m_blockPowerPerLevel = data.m_shared.m_blockPowerPerLevel,
				m_deflectionForce = data.m_shared.m_deflectionForce,
				m_deflectionForcePerLevel = data.m_shared.m_deflectionForcePerLevel,
				m_description = data.m_shared.m_description,
				m_durabilityDrain = data.m_shared.m_durabilityDrain,
				m_durabilityPerLevel = data.m_shared.m_durabilityPerLevel,
				m_equipDuration = data.m_shared.m_equipDuration,
				m_food = data.m_shared.m_food,
				m_foodColor = ColorUtil.GetHexFromColor(data.m_shared.m_foodColor),
				m_foodBurnTime = data.m_shared.m_foodBurnTime,
				m_foodRegen = data.m_shared.m_foodRegen,
				m_foodStamina = data.m_shared.m_foodStamina,
				m_holdDurationMin = data.m_shared.m_holdDurationMin,
				m_holdStaminaDrain = data.m_shared.m_holdStaminaDrain,
				m_maxDurability = data.m_shared.m_maxDurability,
				m_maxQuality = data.m_shared.m_maxQuality,
				m_maxStackSize = data.m_shared.m_maxStackSize,
				m_toolTier = data.m_shared.m_toolTier,
				m_useDurability = data.m_shared.m_useDurability,
				m_useDurabilityDrain = data.m_shared.m_useDurabilityDrain,
				m_value = data.m_shared.m_value,
				m_weight = data.m_shared.m_weight,
				m_destroyBroken = data.m_shared.m_destroyBroken,
				m_dodgeable = data.m_shared.m_dodgeable,
				m_canBeReparied = data.m_shared.m_canBeReparied,
				m_damages = damages,
				m_damagesPerLevel = damagesPerLevel,
				m_name = data.m_shared.m_name,
				m_questItem = data.m_shared.m_questItem,
				m_teleportable = data.m_shared.m_teleportable,
				m_timedBlockBonus = data.m_shared.m_timedBlockBonus
			};
			if (jItemData.m_food == 0f && jItemData.m_foodRegen == 0f && jItemData.m_foodStamina == 0f)
			{
				jItemData.m_foodColor = null;
			}
			return jItemData;
		}

		public static void SetItemDropDataFromJItemData(ref ItemDrop.ItemData itemData, JItemData data)
		{
			//IL_0295: Unknown result type (might be due to invalid IL or missing references)
			//IL_029a: Unknown result type (might be due to invalid IL or missing references)
			if (data.m_damages != null)
			{
				HitData.DamageTypes damages = default(HitData.DamageTypes);
				damages.m_blunt = data.m_damages.m_blunt;
				damages.m_chop = data.m_damages.m_chop;
				damages.m_damage = data.m_damages.m_damage;
				damages.m_fire = data.m_damages.m_fire;
				damages.m_frost = data.m_damages.m_frost;
				damages.m_lightning = data.m_damages.m_lightning;
				damages.m_pickaxe = data.m_damages.m_pickaxe;
				damages.m_pierce = data.m_damages.m_pierce;
				damages.m_poison = data.m_damages.m_poison;
				damages.m_slash = data.m_damages.m_slash;
				damages.m_spirit = data.m_damages.m_spirit;
				itemData.m_shared.m_damages = damages;
			}
			if (data.m_damagesPerLevel != null)
			{
				HitData.DamageTypes damagesPerLevel = default(HitData.DamageTypes);
				damagesPerLevel.m_blunt = data.m_damagesPerLevel.m_blunt;
				damagesPerLevel.m_chop = data.m_damagesPerLevel.m_chop;
				damagesPerLevel.m_damage = data.m_damagesPerLevel.m_damage;
				damagesPerLevel.m_fire = data.m_damagesPerLevel.m_fire;
				damagesPerLevel.m_frost = data.m_damagesPerLevel.m_frost;
				damagesPerLevel.m_lightning = data.m_damagesPerLevel.m_lightning;
				damagesPerLevel.m_pickaxe = data.m_damagesPerLevel.m_pickaxe;
				damagesPerLevel.m_pierce = data.m_damagesPerLevel.m_pierce;
				damagesPerLevel.m_poison = data.m_damagesPerLevel.m_poison;
				damagesPerLevel.m_slash = data.m_damagesPerLevel.m_slash;
				damagesPerLevel.m_spirit = data.m_damagesPerLevel.m_spirit;
				itemData.m_shared.m_damagesPerLevel = damagesPerLevel;
			}
			itemData.m_shared.m_name = data.m_name;
			itemData.m_shared.m_description = data.m_description;
			itemData.m_shared.m_weight = data.m_weight;
			itemData.m_shared.m_maxStackSize = data.m_maxStackSize;
			itemData.m_shared.m_food = data.m_food;
			itemData.m_shared.m_foodStamina = data.m_foodStamina;
			itemData.m_shared.m_foodRegen = data.m_foodRegen;
			itemData.m_shared.m_foodBurnTime = data.m_foodBurnTime;
			if (data.m_foodColor != null && data.m_foodColor != "" && data.m_foodColor.StartsWith("#"))
			{
				itemData.m_shared.m_foodColor = ColorUtil.GetColorFromHex(data.m_foodColor);
			}
			itemData.m_shared.m_armor = data.m_armor;
			itemData.m_shared.m_armorPerLevel = data.m_armorPerLevel;
			itemData.m_shared.m_blockPower = data.m_blockPower;
			itemData.m_shared.m_blockPowerPerLevel = data.m_blockPowerPerLevel;
			itemData.m_shared.m_canBeReparied = data.m_canBeReparied;
			itemData.m_shared.m_timedBlockBonus = data.m_timedBlockBonus;
			itemData.m_shared.m_deflectionForce = data.m_deflectionForce;
			itemData.m_shared.m_deflectionForcePerLevel = data.m_deflectionForcePerLevel;
			itemData.m_shared.m_destroyBroken = data.m_destroyBroken;
			itemData.m_shared.m_dodgeable = data.m_dodgeable;
			itemData.m_shared.m_maxDurability = data.m_maxDurability;
			itemData.m_shared.m_durabilityDrain = data.m_durabilityDrain;
			itemData.m_shared.m_durabilityPerLevel = data.m_durabilityPerLevel;
			itemData.m_shared.m_equipDuration = data.m_equipDuration;
			itemData.m_shared.m_holdDurationMin = data.m_holdDurationMin;
			itemData.m_shared.m_holdStaminaDrain = data.m_holdStaminaDrain;
			itemData.m_shared.m_maxQuality = data.m_maxQuality;
			itemData.m_shared.m_useDurability = data.m_useDurability;
			itemData.m_shared.m_useDurabilityDrain = data.m_useDurabilityDrain;
			itemData.m_shared.m_questItem = data.m_questItem;
			itemData.m_shared.m_teleportable = data.m_teleportable;
			itemData.m_shared.m_toolTier = data.m_toolTier;
			itemData.m_shared.m_value = data.m_value;
		}
	}





















}
