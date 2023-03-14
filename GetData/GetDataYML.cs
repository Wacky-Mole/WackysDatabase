using System.Linq;
using HarmonyLib;
using UnityEngine;
using wackydatabase.Datas;
using BepInEx.Bootstrap;
using System.Reflection;
using static ItemSets;
using System.Collections.Generic;

namespace wackydatabase.GetData
{
    public class GetDataYML     {
        internal  RecipeData GetRecipeDataByName(string name, ObjectDB tod)
        {
            GameObject go = DataHelpers.CheckforSpecialObjects(name);// check for special cases
            if (go == null)
                go = tod.GetItemPrefab(name);

            if (go == null)
            {
                foreach (Recipe recipes in tod.m_recipes)
                {
                    if (!(recipes.m_item == null) && recipes.name == name)
                    {
                        WMRecipeCust.Dbgl($"An actual Recipe_ {name} has been found!-- Only Modification - No Cloning");
                        var dataRec = new RecipeData()
                        {
                            name = name,
                            amount = recipes.m_amount,
                            craftingStation = recipes.m_craftingStation?.m_name ?? "",
                            minStationLevel = recipes.m_minStationLevel,
                        };
                        foreach (Piece.Requirement req in recipes.m_resources)
                        {
                            dataRec.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
                        }

                        return dataRec;
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
            Recipe recipe = tod.GetRecipe(item);
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

            var data = new RecipeData()
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

        internal  RecipeData GetRecipeDataByNum(int count, ObjectDB tod)
        {
            var rep = tod.m_recipes[count];
            var dataRec = new RecipeData()
            {
                name = rep.name,
                amount = rep.m_amount,
                craftingStation = rep.m_craftingStation?.m_name ?? "",
                minStationLevel = rep.m_minStationLevel,
            };
            return dataRec;
        }





        internal PieceData GetPieceRecipeByName(string name, ObjectDB tod, bool warn = true)
        {
            Piece piece = null;
            WMRecipeCust.selectedPiecehammer = null; // makes sure doesn't use an old one. 
            GameObject go = DataHelpers.GetPieces(tod).Find(g => Utils.GetPrefabName(g) == name); // vanilla search  replace with FindPieceObjectName(data.name) in the future
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
            ItemDrop hammer = tod.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
            if (hammer && hammer.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                piecehammer = "Hammer";

            ItemDrop hoe = tod.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
            if (hoe && hoe.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                piecehammer = "Hoe";


            WMRecipeCust.WLog.LogWarning("Hammer selector needs helkp! in getdata GetPieceRecipeByName");
            return GetPiece(hammer, piecehammer, go, tod);
            

            string wackyname = "";
            string wackydesc = "";
            wackydesc = piece.m_description;
            wackyname = piece.m_name;
            string wackycatSring = piece.m_category.ToString();

            var data = new PieceData()
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

        internal StatusData GetStatusEByName(string name, ObjectDB tod)
        {
           return GetStatusData(tod.GetStatusEffect(name));
        }
        internal StatusData GetStatusEByNum(int num, ObjectDB tod)
        {
            var count = tod.m_StatusEffects.Count();
            if (num == count)
            {
                return null;
            }
            StatusData John = null;
             try { John = GetStatusData(tod.m_StatusEffects[num]); } catch
            {
                WMRecipeCust.WLog.LogWarning("Something went wrong with a Status Effect ");
            }
            return John;
        }
        private StatusData GetStatusData(StatusEffect effect)
        {

            //effect.m_icon = effect.m_icon.name;
            //effect.

            
            StatusData statusData =  new StatusData
            {


                Name = effect.name ?? "",
                m_Name = effect.m_name ?? "",
                Category = effect.m_category ?? "",
                CustomIcon = effect.m_icon.name ?? "",
                FlashIcon = effect.m_flashIcon,
                CooldownIcon = effect.m_cooldownIcon,
                Tooltip = effect.m_tooltip ?? "",
                Attributes = effect.m_attributes,
                StartMessageLoc = effect.m_startMessageType,
                StartMessage = effect.m_startMessage ?? "",
                StopMessageLoc = effect.m_stopMessageType, 
                StopMessage = effect.m_stopMessage ?? "",
                RepeatMessageLoc = effect.m_repeatMessageType,
                RepeatMessage = effect.m_repeatMessage ?? "",
                RepeatInterval = effect.m_repeatInterval,
                TimeToLive = effect.m_ttl ,
                StartEffect = effect.m_startEffects,
                StopEffect = effect.m_stopEffects,
                Cooldown = effect.m_cooldown,
                ActivationAnimation = effect.m_activationAnimation ?? "",


            }; 
            /*

            effect = statusData;
            
            var data = ScriptableObject.CreateInstance<StatusEffect>();

            data.Name = effect.name;
            data.m_Name = effect.m_name;
            data.Category = effect.m_category;
            data.CustomIcon = effect.m_icon.name;
            data.FlashIcon = effect.m_flashIcon;
            data.CooldownIcon = effect.m_cooldownIcon;
            data.Tooltip = effect.m_tooltip;
            data.Attributes = effect.m_attributes;
            data.StartMessageLoc = effect.m_startMessageType;
            data.StartMessage = effect.m_startMessage;
            data.StopMessageLoc = effect.m_stopMessageType;
            data.StopMessage = effect.m_stopMessage;
            data.RepeatMessageLoc = effect.m_repeatMessageType;
            data.RepeatMessage = effect.m_repeatMessage;
            data.RepeatInterval = effect.m_repeatInterval;
            data.TimeToLive = effect.m_ttl;
            data.StartEffect = effect.m_startEffects;
            data.StopEffect = effect.m_stopEffects;
            data.Cooldown = effect.m_cooldown;
            data.ActivationAnimation = effect.m_activationAnimation;
           */

            return statusData;

        }

        internal PieceData GetPieceRecipeByNum(int count, string hammer, ObjectDB tod, ItemDrop itemD = null)
        {
            ItemDrop HamerItemdrop = null;
            if (itemD == null)
            {
                HamerItemdrop = tod.GetItemPrefab(hammer).GetComponent<ItemDrop>();
            }else
            {
                HamerItemdrop = itemD;
            }


            int PCount = HamerItemdrop.m_itemData.m_shared.m_buildPieces.m_pieces.Count();

            GameObject pieceSel = HamerItemdrop.m_itemData.m_shared.m_buildPieces.m_pieces[count];
            Piece actPiece = pieceSel.GetComponent<Piece>();

            return GetPiece(HamerItemdrop, hammer, pieceSel, tod);

        }

        private PieceData GetPiece (ItemDrop HammerID,string Hammername, GameObject PieceID, ObjectDB tod)
        {

            Piece piece = PieceID.GetComponent<Piece>();
            WMRecipeCust.WLog.LogWarning("Piece sTart");
            var data = new PieceData()
            {
                name = PieceID.name, // required
                piecehammer = Hammername, // required
                amount = 1,
                craftingStation = piece.m_craftingStation?.m_name ?? "",
                minStationLevel = 1,
                adminonly = false,
                m_name = piece.m_name,
                m_description = piece.m_description,
                piecehammerCategory = piece.m_category.ToString(),
                sizeMultiplier = 1,
                customIcon = null,
                clonePrefabName = null,
                material = null,
                damagedMaterial = null,
                disabled = !piece.enabled,
                //cloneEffects

                groundPiece = piece.m_groundPiece,
                ground = piece.m_groundOnly,
                waterPiece = piece.m_waterPiece,
                noInWater = piece.m_noInWater,
                notOnFloor = piece.m_notOnFloor,
                onlyinTeleportArea = piece.m_onlyInTeleportArea,
                allowedInDungeons = piece.m_allowedInDungeons,
                canBeRemoved = piece.m_canBeRemoved,

            };

            WMRecipeCust.WLog.LogWarning("Piece Comfort");
            if (piece.m_comfort != 0)
            {
                ComfortData comfort = new ComfortData
                {
                    confort = piece.m_comfort,
                    confortGroup = piece.m_comfortGroup,
                    comfortObject = piece.m_comfortObject,
                };
                data.comfort = comfort;
             }

            if (PieceID.GetComponent<WearNTear> != null)
            {
                var wear = PieceID.GetComponent<WearNTear>();
                WMRecipeCust.WLog.LogWarning("Piece Wear");
                WearNTearData wearNTearData = new WearNTearData
                {

                 health = wear.m_health,
                 damageModifiers = wear.m_damages,
                 noRoofWear = wear.m_noRoofWear,
                 noSupportWear = wear.m_noSupportWear,
                 supports = wear.m_supports,
                 triggerPrivateArea = wear.m_triggerPrivateArea,
                 };

                data.wearNTearData = wearNTearData;
            }

            if (PieceID.GetComponent<CraftingStation> != null)
            {
                var station = PieceID.GetComponent<CraftingStation>();
                CraftingStationData craftingStationData = new CraftingStationData
                {
                    cStationName = station.name,
                 cStationCustionIcon = null,
                 discoveryRange = station.m_discoverRange,
                 buildRange = station.m_rangeBuild,
                 craftRequiresRoof = station.m_craftRequireRoof,
                 craftRequiresFire = station.m_craftRequireFire,
                 showBasicRecipes = station.m_showBasicRecipies,
                 useDistance = station.m_useDistance,
                 useAnimation = station.m_useAnimation,
                 };
                data.craftingStationData = craftingStationData;
            }

            if (PieceID.GetComponent<StationExtension> != null)
            {
                var ex = PieceID.GetComponent<StationExtension>();
                CSExtension cSExtension = new CSExtension
                {
                     stationExtensionCraftingStation = ex.m_craftingStation,
                     maxStationDistance = ex.m_maxStationDistance,
                     continousConnection = ex.m_continousConnection,
                     stack = ex.m_stack,
                 };
                 data.cSExtension = cSExtension;
            }

            if (PieceID.GetComponent<Smelter> != null)
            {
                var smelt = PieceID.GetComponent<Smelter>();
                SmelterData smelterData = new SmelterData
                {
                     smelterName = smelt.name,
                     addOreTooltip = smelt.m_addOreTooltip,
                     emptyOreTooltip = smelt.m_emptyOreTooltip,
                     addFuelSwitch = smelt.m_addOreSwitch,
                     addOreSwitch = smelt.m_addOreSwitch,
                     emptyOreSwitch = smelt.m_emptyOreSwitch,
                     fuelItem = smelt.m_fuelItem,
                     maxOre = smelt.m_maxOre,
                     maxFuel = smelt.m_maxFuel,
                     fuelPerProduct = smelt.m_fuelPerProduct,
                     secPerProduct = smelt.m_secPerProduct,
                     spawnStack = smelt.m_spawnStack,
                     requiresRoof = smelt.m_requiresRoof,
                     addOreAnimationLength = smelt.m_addOreAnimationDuration,
                     smelterConversion = smelt.m_conversion,
                };
                data.smelterData = smelterData;
            }

    
            foreach (Piece.Requirement req in piece.m_resources)
            {
                data.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }

            return data;


        }


        internal WItemData GetItemDataByName(string name, ObjectDB tod)
        {
            GameObject go = tod.GetItemPrefab(name);
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

            return GetItem(go, tod);
            
        }

        internal WItemData GetItemDataByCount(int count, ObjectDB tod)
        {
            var go = tod.m_items[count];
            return GetItem(go, tod);

        }

        private WItemData GetItem(GameObject go, ObjectDB tod) {
            ItemDrop.ItemData data = go.GetComponent<ItemDrop>().m_itemData;
            if (data == null)
            {
                WMRecipeCust.Dbgl("Item GetItemDataByName not found! - componets");
                return null;
            }
            bool hasdmg = false;
            WDamages damages = null;
            if (data.m_shared.m_damages.m_blunt > 0f || data.m_shared.m_damages.m_chop > 0f || data.m_shared.m_damages.m_damage > 0f || data.m_shared.m_damages.m_fire > 0f || data.m_shared.m_damages.m_frost > 0f || data.m_shared.m_damages.m_lightning > 0f || data.m_shared.m_damages.m_pickaxe > 0f || data.m_shared.m_damages.m_pierce > 0f || data.m_shared.m_damages.m_poison > 0f || data.m_shared.m_damages.m_slash > 0f || data.m_shared.m_damages.m_spirit > 0f)
            {
                WMRecipeCust.Dbgl("Item " + go.GetComponent<ItemDrop>().name + " damage on ");
                hasdmg = true;
                damages = new WDamages
                {

                    Blunt = data.m_shared.m_damages.m_blunt,
                    Chop = data.m_shared.m_damages.m_chop,
                    Damage = data.m_shared.m_damages.m_damage,
                    Fire = data.m_shared.m_damages.m_fire,
                    Frost = data.m_shared.m_damages.m_frost,
                    Lightning = data.m_shared.m_damages.m_lightning,
                    Pickaxe = data.m_shared.m_damages.m_pickaxe,
                    Pierce = data.m_shared.m_damages.m_pierce,
                    Poison = data.m_shared.m_damages.m_poison,
                    Slash = data.m_shared.m_damages.m_slash,
                    Spirit = data.m_shared.m_damages.m_spirit
                };
            }
            WDamages damagesPerLevel = null;
            if (data.m_shared.m_damagesPerLevel.m_blunt > 0f || data.m_shared.m_damagesPerLevel.m_chop > 0f || data.m_shared.m_damagesPerLevel.m_damage > 0f || data.m_shared.m_damagesPerLevel.m_fire > 0f || data.m_shared.m_damagesPerLevel.m_frost > 0f || data.m_shared.m_damagesPerLevel.m_lightning > 0f || data.m_shared.m_damagesPerLevel.m_pickaxe > 0f || data.m_shared.m_damagesPerLevel.m_pierce > 0f || data.m_shared.m_damagesPerLevel.m_poison > 0f || data.m_shared.m_damagesPerLevel.m_slash > 0f || data.m_shared.m_damagesPerLevel.m_spirit > 0f)
            {
                damagesPerLevel = new WDamages
                {
                    Blunt = data.m_shared.m_damagesPerLevel.m_blunt,
                    Chop = data.m_shared.m_damagesPerLevel.m_chop,
                    Damage = data.m_shared.m_damagesPerLevel.m_damage,
                    Fire = data.m_shared.m_damagesPerLevel.m_fire,
                    Frost = data.m_shared.m_damagesPerLevel.m_frost,
                    Lightning = data.m_shared.m_damagesPerLevel.m_lightning,
                    Pickaxe = data.m_shared.m_damagesPerLevel.m_pickaxe,
                    Pierce = data.m_shared.m_damagesPerLevel.m_pierce,
                    Poison = data.m_shared.m_damagesPerLevel.m_poison,
                    Slash = data.m_shared.m_damagesPerLevel.m_slash,
                    Spirit = data.m_shared.m_damagesPerLevel.m_spirit
                };

            }

            
           
            StatMods StatModdifers = new StatMods
            {
                m_movementModifier = data.m_shared.m_movementModifier,
                m_EitrRegen = data.m_shared.m_eitrRegenModifier,
            };



            WMRecipeCust.Dbgl("Item " + go.GetComponent<ItemDrop>().name + " Main ");
            WItemData ItemData = new WItemData
            {
                name = go.GetComponent<ItemDrop>().name,
                //m_armor = data.m_shared.m_armor,
                //clone = false,
                clonePrefabName = "",
                //m_armorPerLevel = data.m_shared.m_armorPerLevel,
                m_description = data.m_shared.m_description,
                m_durabilityDrain = data.m_shared.m_durabilityDrain,
                m_durabilityPerLevel = data.m_shared.m_durabilityPerLevel,
                m_backstabbonus = data.m_shared.m_backstabBonus,
                m_equipDuration = data.m_shared.m_equipDuration,
                m_maxDurability = data.m_shared.m_maxDurability,
                m_maxQuality = data.m_shared.m_maxQuality,
                m_maxStackSize = data.m_shared.m_maxStackSize,
                m_toolTier = data.m_shared.m_toolTier,
                m_useDurability = data.m_shared.m_useDurability,
                m_useDurabilityDrain = data.m_shared.m_useDurabilityDrain,
                m_value = data.m_shared.m_value,
                scale_weight_by_quality = data.m_shared.m_scaleWeightByQuality,
                sizeMultiplier = 1, // default scale
                m_weight = data.m_shared.m_weight,
                m_destroyBroken = data.m_shared.m_destroyBroken,
                m_dodgeable = data.m_shared.m_dodgeable,
                m_canBeReparied = data.m_shared.m_canBeReparied,
                m_name = data.m_shared.m_name,
                m_questItem = data.m_shared.m_questItem,
                m_teleportable = data.m_shared.m_teleportable,
                m_knockback = data.m_shared.m_attackForce,
                m_skillType = data.m_shared.m_skillType,
                m_animationState = data.m_shared.m_animationState,
                Damage = damages,
                Damage_Per_Level = damagesPerLevel,
                Moddifiers = StatModdifers,
                damageModifiers = data.m_shared.m_damageModifiers.Select(m => m.m_type + ":" + m.m_modifier).ToList(),

                //damageModifiers = data.m_shared.m_damageModifiers.Select(m => m.m_type + ":" + m.m_modifier).ToList(),

            };
            if (data.m_shared.m_equipStatusEffect != null)
            {
                WMRecipeCust.Dbgl("Item " + go.GetComponent<ItemDrop>().name + " SEs ");
                SE_Equip SE_Equip2 = new Datas.SE_Equip
                {
                    EffectName = data.m_shared.m_equipStatusEffect.name,
                };
                ItemData.SE_Equip = SE_Equip2; // actual adding
            }
            if (data.m_shared.m_setStatusEffect != null)
            {
                WMRecipeCust.Dbgl("Item " + go.GetComponent<ItemDrop>().name + " SEset ");
                SE_SET_Equip SE_SET_Equip2 = new Datas.SE_SET_Equip
                {
                    SetName = data.m_shared.m_setName ?? "",
                    Size = data.m_shared.m_setSize,
                    EffectName = data.m_shared.m_setStatusEffect.name ?? "",
                };
                ItemData.SE_SET_Equip = SE_SET_Equip2;
            }

            if (hasdmg)
            {
                WMRecipeCust.Dbgl("Item " + go.GetComponent<ItemDrop>().name + " attack ");
                AttackArm Primary_Attack = new AttackArm
                {

                    AttackType = data.m_shared.m_attack.m_attackType,
                    Attack_Animation = data.m_shared.m_attack.m_attackAnimation,
                    Attack_Random_Animation = data.m_shared.m_attack.m_attackRandomAnimations,
                    Chain_Attacks = data.m_shared.m_attack.m_attackChainLevels,
                    Hit_Terrain = data.m_shared.m_attack.m_hitTerrain,

                    m_attackStamina = data.m_shared.m_attack.m_attackStamina,
                    m_eitrCost = data.m_shared.m_attack.m_attackEitr,
                    AttackHealthCost = data.m_shared.m_attack.m_attackHealth,
                    m_attackHealthPercentage = data.m_shared.m_attack.m_attackHealthPercentage,

                    SpeedFactor = data.m_shared.m_attack.m_speedFactor,
                    DmgMultiplier = data.m_shared.m_attack.m_damageMultiplier,
                    ForceMultiplier = data.m_shared.m_attack.m_forceMultiplier,
                    StaggerMultiplier = data.m_shared.m_attack.m_staggerMultiplier,
                    RecoilMultiplier = data.m_shared.m_attack.m_recoilPushback,

                    AttackRange = data.m_shared.m_attack.m_attackRange,
                    AttackHeight = data.m_shared.m_attack.m_attackHeight,
                    Spawn_On_Trigger = data.m_shared.m_attack.m_spawnOnTrigger,

                    Requires_Reload = data.m_shared.m_attack.m_requiresReload,
                    Reload_Animation = data.m_shared.m_attack.m_reloadAnimation,
                    ReloadTime = data.m_shared.m_attack.m_reloadTime,
                    Reload_Stamina_Drain = data.m_shared.m_attack.m_reloadStaminaDrain,

                    Bow_Draw = data.m_shared.m_attack.m_bowDraw,
                    Bow_Duration_Min = data.m_shared.m_attack.m_drawDurationMin,
                    Bow_Stamina_Drain = data.m_shared.m_attack.m_drawStaminaDrain,
                    Bow_Animation_State = data.m_shared.m_attack.m_drawAnimationState,

                    Attack_Angle = data.m_shared.m_attack.m_attackAngle,
                    Attack_Ray_Width = data.m_shared.m_attack.m_attackRayWidth,
                    Lower_Dmg_Per_Hit = data.m_shared.m_attack.m_lowerDamagePerHit,
                    Hit_Through_Walls = data.m_shared.m_attack.m_hitThroughWalls,
                    Multi_Hit = data.m_shared.m_attack.m_multiHit,
                    Pickaxe_Special = data.m_shared.m_attack.m_pickaxeSpecial,
                    Last_Chain_Dmg_Multiplier = data.m_shared.m_attack.m_lastChainDamageMultiplier,

                    Attack_Projectile = data.m_shared.m_attack.m_attackProjectile,
                    Projectile_Vel = data.m_shared.m_attack.m_projectileVel,
                    Projectile_Accuraccy = data.m_shared.m_attack.m_projectileAccuracy,
                    Projectiles = data.m_shared.m_attack.m_projectiles,
                };

                AttackArm Secondary_Attack = new AttackArm
                {
                    AttackType = data.m_shared.m_secondaryAttack.m_attackType,
                    Attack_Animation = data.m_shared.m_secondaryAttack.m_attackAnimation,
                    Attack_Random_Animation = data.m_shared.m_secondaryAttack.m_attackRandomAnimations,
                    Chain_Attacks = data.m_shared.m_secondaryAttack.m_attackChainLevels,
                    Hit_Terrain = data.m_shared.m_secondaryAttack.m_hitTerrain,

                    m_attackStamina = data.m_shared.m_secondaryAttack.m_attackStamina,
                    m_eitrCost = data.m_shared.m_secondaryAttack.m_attackEitr,
                    AttackHealthCost = data.m_shared.m_secondaryAttack.m_attackHealth,
                    m_attackHealthPercentage = data.m_shared.m_secondaryAttack.m_attackHealthPercentage,

                    SpeedFactor = data.m_shared.m_secondaryAttack.m_speedFactor,
                    DmgMultiplier = data.m_shared.m_secondaryAttack.m_damageMultiplier,
                    ForceMultiplier = data.m_shared.m_secondaryAttack.m_forceMultiplier,
                    StaggerMultiplier = data.m_shared.m_secondaryAttack.m_staggerMultiplier,
                    RecoilMultiplier = data.m_shared.m_secondaryAttack.m_recoilPushback,

                    AttackRange = data.m_shared.m_secondaryAttack.m_attackRange,
                    AttackHeight = data.m_shared.m_secondaryAttack.m_attackHeight,
                    Spawn_On_Trigger = data.m_shared.m_secondaryAttack.m_spawnOnTrigger,

                    Requires_Reload = data.m_shared.m_secondaryAttack.m_requiresReload,
                    Reload_Animation = data.m_shared.m_secondaryAttack.m_reloadAnimation,
                    ReloadTime = data.m_shared.m_secondaryAttack.m_reloadTime,
                    Reload_Stamina_Drain = data.m_shared.m_secondaryAttack.m_reloadStaminaDrain,

                    Bow_Draw = data.m_shared.m_secondaryAttack.m_bowDraw,
                    Bow_Duration_Min = data.m_shared.m_secondaryAttack.m_drawDurationMin,
                    Bow_Stamina_Drain = data.m_shared.m_secondaryAttack.m_drawStaminaDrain,
                    Bow_Animation_State = data.m_shared.m_secondaryAttack.m_drawAnimationState,

                    Attack_Angle = data.m_shared.m_secondaryAttack.m_attackAngle,
                    Attack_Ray_Width = data.m_shared.m_secondaryAttack.m_attackRayWidth,
                    Lower_Dmg_Per_Hit = data.m_shared.m_secondaryAttack.m_lowerDamagePerHit,
                    Hit_Through_Walls = data.m_shared.m_secondaryAttack.m_hitThroughWalls,
                    Multi_Hit = data.m_shared.m_secondaryAttack.m_multiHit,
                    Pickaxe_Special = data.m_shared.m_secondaryAttack.m_pickaxeSpecial,
                    Last_Chain_Dmg_Multiplier = data.m_shared.m_secondaryAttack.m_lastChainDamageMultiplier,

                    Attack_Projectile = data.m_shared.m_secondaryAttack.m_attackProjectile,
                    Projectile_Vel = data.m_shared.m_secondaryAttack.m_projectileVel,
                    Projectile_Accuraccy = data.m_shared.m_secondaryAttack.m_projectileAccuracy,
                    Projectiles = data.m_shared.m_secondaryAttack.m_projectiles,
                };
                ItemData.Primary_Attack = Primary_Attack;
                ItemData.Secondary_Attack = Secondary_Attack;

            }
            if (data.m_shared.m_armor != 0)
            {
                WMRecipeCust.Dbgl("Item " + go.GetComponent<ItemDrop>().name + " armor ");
                ArmorData Armor = new ArmorData
                {
                    armor = data.m_shared.m_armor,
                    armorPerLevel = data.m_shared.m_armorPerLevel,
                    

                };
                ItemData.Armor = Armor;
            }
            if (data.m_shared.m_food != 0)
            {
                WMRecipeCust.Dbgl("Item " + go.GetComponent<ItemDrop>().name + " food ");
                FoodData FoodStats = new FoodData
                {
                    m_foodHealth = data.m_shared.m_food,
                    // m_foodColor = ColorUtil.GetHexFromColor(data.m_shared.m_foodColor),
                    m_foodBurnTime = data.m_shared.m_foodBurnTime,
                    m_foodRegen = data.m_shared.m_foodRegen,
                    m_foodStamina = data.m_shared.m_foodStamina,
                    m_FoodEitr = data.m_shared.m_foodEitr,
                };
                ItemData.FoodStats = FoodStats;
            }


            //WMRecipeCust.Dbgl("Item " + go.GetComponent<ItemDrop>().name + " shield "); Maybe everything deflects a bit
            ShieldData ShieldData = new ShieldData
            {
                m_blockPower = data.m_shared.m_blockPower,
                m_blockPowerPerLevel = data.m_shared.m_blockPowerPerLevel,
                m_timedBlockBonus = data.m_shared.m_timedBlockBonus,
                m_deflectionForce = data.m_shared.m_deflectionForce,
                m_deflectionForcePerLevel = data.m_shared.m_deflectionForcePerLevel,
            };


            return ItemData;

        }




    }
}
