using System.Linq;
using HarmonyLib;
using UnityEngine;
using wackydatabase.Datas;
using BepInEx.Bootstrap;
using System.Reflection;

namespace wackydatabase.GetData
{
    public class GetData     {
        internal static RecipeData_json GetRecipeDataByName(string name)
        {
            GameObject go = DataHelpers.CheckforSpecialObjects(name);// check for special cases
            if (go == null)
                go = ObjectDB.instance.GetItemPrefab(name);

            if (go == null)
            {
                foreach (Recipe recipes in ObjectDB.instance.m_recipes)
                {
                    if (!(recipes.m_item == null) && recipes.name == name)
                    {
                        WMRecipeCust.Dbgl($"An actual Recipe_ {name} has been found!-- Only Modification - No Cloning");
                        var data2 = new RecipeData_json()
                        {
                            name = name,
                            amount = recipes.m_amount,
                            craftingStation = recipes.m_craftingStation?.m_name ?? "",
                            minStationLevel = recipes.m_minStationLevel,
                        };
                        foreach (Piece.Requirement req in recipes.m_resources)
                        {
                            data2.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
                        }

                        return data2;
                    }
                }
            }

            if (go == null)
            {
                WMRecipeCust.Dbgl($"Recipe {name} not found!");
                return null; //GetPieceRecipeByName(name);
            }

            ItemDrop.ItemData item = go.GetComponent<ItemDrop>().m_itemData;
            if (item == null)
            {
                WMRecipeCust.Dbgl("Item data not found!");
                return null;
            }
            Recipe recipe = ObjectDB.instance.GetRecipe(item);
            if (!recipe)
            {
                if (Chainloader.PluginInfos.ContainsKey("com.jotunn.jotunn"))
                {
                    object itemManager = Chainloader.PluginInfos["com.jotunn.jotunn"].Instance.GetType().Assembly.GetType("Jotunn.Managers.ItemManager").GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    object cr = AccessTools.Method(itemManager.GetType(), "GetRecipe").Invoke(itemManager, new[] { item.m_shared.m_name });
                    if (cr != null)
                    {
                        recipe = (Recipe)AccessTools.Property(cr.GetType(), "Recipe").GetValue(cr);
                        WMRecipeCust.Dbgl($"Jotunn recipe: {item.m_shared.m_name} {recipe != null}");
                    }
                }

                if (!recipe)
                {
                    WMRecipeCust.Dbgl($"Recipe not found for item {item.m_shared.m_name}!");
                    return null;
                }
            }

            var data = new RecipeData_json()
            {
                name = name,
                amount = recipe.m_amount,
                craftingStation = recipe.m_craftingStation?.m_name ?? "",
                minStationLevel = recipe.m_minStationLevel,
            };
            foreach (Piece.Requirement req in recipe.m_resources)
            {
                data.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }

            return data;
        }

        internal static PieceData_json GetPieceRecipeByName(string name, bool warn = true)
        {
            Piece piece = null;
            WMRecipeCust.selectedPiecehammer = null; // makes sure doesn't use an old one. 
            GameObject go = DataHelpers.GetPieces().Find(g => Utils.GetPrefabName(g) == name); // vanilla search  replace with FindPieceObjectName(data.name) in the future
            if (go == null)
            {
                go = DataHelpers.GetModdedPieces(name); // known modded Hammer search
                if (go == null)
                {
                    go = DataHelpers.CheckforSpecialObjects(name);// check for special cases
                    if (go == null) // 3th layer
                    {
                        WMRecipeCust.Dbgl($"Piece {name} not found! 3 layer search");
                        return null;
                    }
                }
                else // 2nd layer
                    WMRecipeCust.Dbgl($"Piece {name} from known hammer {WMRecipeCust.selectedPiecehammer}");
            }
            piece = go.GetComponent<Piece>();
            if (piece == null) // final check
            {
                WMRecipeCust.Dbgl("Piece data not found!");
                return null;
            }
            string piecehammer = null;
            if (WMRecipeCust.selectedPiecehammer != null)
                piecehammer = WMRecipeCust.selectedPiecehammer.name;

            if (piecehammer == null)
                piecehammer = "Hammer"; // default

            // these are kind of reduntant. // But are helpful for existing configs
            ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
            if (hammer && hammer.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                piecehammer = "Hammer";

            ItemDrop hoe = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
            if (hoe && hoe.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                piecehammer = "Hoe";

            string wackyname = "";
            string wackydesc = "";
            wackydesc = piece.m_description;
            wackyname = piece.m_name;
            string wackycatSring = piece.m_category.ToString();

            var data = new PieceData_json()
            {
                name = name,
                amount = 1,
                craftingStation = piece.m_craftingStation?.m_name ?? "",
                minStationLevel = 1,
                piecehammer = piecehammer,
                adminonly = false,
                m_name = wackyname,
                m_description = wackydesc,
                piecehammerCategory = wackycatSring,
            };
            foreach (Piece.Requirement req in piece.m_resources)
            {
                data.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }

            return data;
        }

        internal static WItemData_json GetItemDataByName(string name)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(name);
            if (go == null)
            {
                WMRecipeCust.Dbgl("GetItemDataByName data not found!");
                return null;
            }

            ItemDrop.ItemData data = go.GetComponent<ItemDrop>().m_itemData;
            if (data == null)
            {
                WMRecipeCust.Dbgl("Item GetItemDataByName not found! - componets");
                return null;
            }
            WDamages damages = null;
            string damagestring = "";
            // Dbgl("Item "+ name + " data.m_shared.m_damages.mslash" + data.m_shared.m_damages.m_slash);
            if (data.m_shared.m_damages.m_blunt > 0f || data.m_shared.m_damages.m_chop > 0f || data.m_shared.m_damages.m_damage > 0f || data.m_shared.m_damages.m_fire > 0f || data.m_shared.m_damages.m_frost > 0f || data.m_shared.m_damages.m_lightning > 0f || data.m_shared.m_damages.m_pickaxe > 0f || data.m_shared.m_damages.m_pierce > 0f || data.m_shared.m_damages.m_poison > 0f || data.m_shared.m_damages.m_slash > 0f || data.m_shared.m_damages.m_spirit > 0f)
            {
                WMRecipeCust.Dbgl("Item " + name + " damage on ");

                damages = new WDamages // not used
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
                damagestring = $"m_blunt:{data.m_shared.m_damages.m_blunt},"
               + $"m_chop:{data.m_shared.m_damages.m_chop},"
               + $"m_damage:{data.m_shared.m_damages.m_damage},"
               + $"m_fire:{data.m_shared.m_damages.m_fire},"
               + $"m_frost:{data.m_shared.m_damages.m_frost},"
               + $"m_lightning:{data.m_shared.m_damages.m_lightning},"
               + $"m_pickaxe:{data.m_shared.m_damages.m_pickaxe},"
               + $"m_pierce:{data.m_shared.m_damages.m_pierce},"
               + $"m_poison:{data.m_shared.m_damages.m_poison},"
               + $"m_slash:{data.m_shared.m_damages.m_slash},"
               + $"m_spirit:{data.m_shared.m_damages.m_spirit},"

               ;
                damagestring = damagestring.Replace(",", ", ");
            }
            WDamages damagesPerLevel = null;
            string damgelvlstring = "";
            if (data.m_shared.m_damagesPerLevel.m_blunt > 0f || data.m_shared.m_damagesPerLevel.m_chop > 0f || data.m_shared.m_damagesPerLevel.m_damage > 0f || data.m_shared.m_damagesPerLevel.m_fire > 0f || data.m_shared.m_damagesPerLevel.m_frost > 0f || data.m_shared.m_damagesPerLevel.m_lightning > 0f || data.m_shared.m_damagesPerLevel.m_pickaxe > 0f || data.m_shared.m_damagesPerLevel.m_pierce > 0f || data.m_shared.m_damagesPerLevel.m_poison > 0f || data.m_shared.m_damagesPerLevel.m_slash > 0f || data.m_shared.m_damagesPerLevel.m_spirit > 0f)
            {
                damagesPerLevel = new WDamages // not used
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
                damgelvlstring = $"m_blunt:{data.m_shared.m_damagesPerLevel.m_blunt},"
               + $"m_chop:{data.m_shared.m_damagesPerLevel.m_chop},"
               + $"m_damage:{data.m_shared.m_damagesPerLevel.m_damage},"
               + $"m_fire:{data.m_shared.m_damagesPerLevel.m_fire},"
               + $"m_frost:{data.m_shared.m_damagesPerLevel.m_frost},"
               + $"m_lightning:{data.m_shared.m_damagesPerLevel.m_lightning},"
               + $"m_pickaxe:{data.m_shared.m_damagesPerLevel.m_pickaxe},"
               + $"m_pierce:{data.m_shared.m_damagesPerLevel.m_pierce},"
               + $"m_poison:{data.m_shared.m_damagesPerLevel.m_poison},"
               + $"m_slash:{data.m_shared.m_damagesPerLevel.m_slash},"
               + $"m_spirit:{data.m_shared.m_damagesPerLevel.m_spirit},"

               ;
                damgelvlstring = damgelvlstring.Replace(",", ", ");
            }
            /*
            foreach (Piece.Requirement req in piece.m_resources) // maybe use in future
            {
                data.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }*/

            WItemData_json jItemData = new WItemData_json
            {
                name = name,
                m_armor = data.m_shared.m_armor,
                clone = false,
                m_armorPerLevel = data.m_shared.m_armorPerLevel,
                m_blockPower = data.m_shared.m_blockPower,
                m_blockPowerPerLevel = data.m_shared.m_blockPowerPerLevel,
                m_deflectionForce = data.m_shared.m_deflectionForce,
                m_deflectionForcePerLevel = data.m_shared.m_deflectionForcePerLevel,
                m_description = data.m_shared.m_description,
                m_durabilityDrain = data.m_shared.m_durabilityDrain,
                m_durabilityPerLevel = data.m_shared.m_durabilityPerLevel,
                m_backstabbonus = data.m_shared.m_backstabBonus,
                m_equipDuration = data.m_shared.m_equipDuration,
                m_foodHealth = data.m_shared.m_food,
                // m_foodColor = ColorUtil.GetHexFromColor(data.m_shared.m_foodColor),
                m_foodBurnTime = data.m_shared.m_foodBurnTime,
                m_foodRegen = data.m_shared.m_foodRegen,
                m_foodStamina = data.m_shared.m_foodStamina,
                m_FoodEitr = data.m_shared.m_foodEitr,
                m_holdDurationMin = data.m_shared.m_attack.m_drawDurationMin,
                m_holdStaminaDrain = data.m_shared.m_attack.m_drawStaminaDrain,
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
                m_damages = damagestring,
                m_damagesPerLevel = damgelvlstring,
                m_name = data.m_shared.m_name,
                m_questItem = data.m_shared.m_questItem,
                m_teleportable = data.m_shared.m_teleportable,
                m_timedBlockBonus = data.m_shared.m_timedBlockBonus,
                m_movementModifier = data.m_shared.m_movementModifier,
                m_EitrRegen = data.m_shared.m_eitrRegenModifier,
                m_attackStamina = data.m_shared.m_attack.m_attackStamina,
                m_secAttackStamina = data.m_shared.m_secondaryAttack.m_attackStamina,
                m_EitrCost = data.m_shared.m_attack.m_attackEitr,
                m_secEitrCost = data.m_shared.m_secondaryAttack.m_attackEitr,
                m_attackHealthPercentage = data.m_shared.m_attack.m_attackHealthPercentage,
                m_secAttackHealthPercentage = data.m_shared.m_secondaryAttack.m_attackHealthPercentage,
                m_knockback = data.m_shared.m_attackForce,

                damageModifiers = data.m_shared.m_damageModifiers.Select(m => m.m_type + ":" + m.m_modifier).ToList(),

            };
            if (jItemData.m_foodHealth == 0f && jItemData.m_foodRegen == 0f && jItemData.m_foodStamina == 0f)
            {
                jItemData.m_foodColor = null;
            }

            return jItemData;

        }




    }
}
