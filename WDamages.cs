using System;

[Serializable]
public class WDamages
{
	public float m_blunt;

	public float m_chop;

	public float m_damage;

	public float m_fire;

	public float m_frost;

	public float m_lightning;

	public float m_pickaxe;

	public float m_pierce;

	public float m_poison;

	public float m_slash;

	public float m_spirit;
}

[Serializable]
public class WIngredients
{
	public string id;
	public int amount;
	public int amountPerLevel;

}