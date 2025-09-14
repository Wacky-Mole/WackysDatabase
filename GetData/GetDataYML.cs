using System.Linq;
using HarmonyLib;
using UnityEngine;
using wackydatabase.Datas;
using BepInEx.Bootstrap;
using System.Reflection;
using static ItemSets;
using System.Collections.Generic;
using System;
using wackydatabase.Util;
using System.Linq.Expressions;
using JetBrains.Annotations;
using static Skills;
using System.IO;
using YamlDotNet.Serialization;
using System.IO.Ports;
using System.Configuration;


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
                        return GetRecip(recipes, tod, false);
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

            return GetRecip(recipe, tod);
        }

        internal  RecipeData GetRecipeDataByNum(int count, ObjectDB tod)
        {
            var rep = tod.m_recipes[count];
            WMRecipeCust.Dbgl($" {rep.name} Item saving");
            
            try
            {
                
                if (rep.name.Contains("Recipe_"))
                {
                    return GetRecip(rep, tod, false);
                } 

                return GetRecip(rep, tod);
            }
            catch { WMRecipeCust.Dbgl($"      Saving Actual Recipe Instead"); }
            return GetRecip(rep, tod, false);

        }

        private RecipeData GetRecip(Recipe data, ObjectDB tod, bool AllowClone = true)
        {
            List<string> reqs2 = new List<string>();
            foreach (Piece.Requirement req in data.m_resources)
            {
                reqs2.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }
            string cloneyesorno = null;
            var temp = data.name;
             

            if (!AllowClone)
            {
                cloneyesorno = "NO";
                data.name = temp;
            }else
            {
                data.name = data.m_item.name;
            }

            RecipeData dataRec = new RecipeData()
            {
                name = data.name,
                amount = data.m_amount,
                clonePrefabName = cloneyesorno,
                craftingStation = data.m_craftingStation?.m_name ?? "",
                repairStation = data.m_repairStation?.m_name ?? null, // maybe
                minStationLevel = data.m_minStationLevel,
                maxStationLevelCap = null,
                disabled = !data.m_enabled,
                disabledUpgrade = false,
                requireOnlyOneIngredient = data.m_requireOnlyOneIngredient,
                reqs = reqs2,

            };


            return dataRec;
        }





        internal StatusData GetStatusEByName(string name, ObjectDB tod)
        {
            // WMRecipeCust.NoNotTheseSEs
            // SEdata stats = new SEdata() ;//= (SEdata)ScriptableObject.CreateInstance<SE_Stats>();
            //var eff= tod.GetStatusEffect(name);
            //stats = Ob.Cast<SEdata>(eff);

            var pass = tod.GetStatusEffect(name.GetStableHashCode());
            if (pass == null)
                return null;

            return GetStatusData(pass);
        }
        internal StatusData GetStatusEByNum(int num, ObjectDB tod)
        {

            // SE_Stats stats = new SE_Stats();

            var count = tod.m_StatusEffects.Count();
            if (num == count)
            {
                return null;
            }
            var nam = tod.m_StatusEffects[num].name;
            foreach (var list in WMRecipeCust.NoNotTheseSEs)
            {
                if (nam == list)
                    return null;
            }
            StatusData John = null;
            try {


                John = GetStatusData(tod.m_StatusEffects[num]); 
            } catch
            {
                WMRecipeCust.WLog.LogWarning($"Something went wrong with a Status Effect {tod.m_StatusEffects[num].name}");
            }
            return John;
        }



        private StatusData GetStatusData(StatusEffect effect)
        {
            //effect.m_icon = effect.m_icon.name;
            var f2 = effect.GetType();
            WMRecipeCust.WLog.LogInfo("             StatusEffect " + effect.name);

            SEShield shield = new SEShield();
            if (Functions.getCast<float>(f2, "m_absorbDamage", effect) > 0)
            { // the Skill levelup could work LevelUpSkillOnBreak
                shield.AbsorbDmg = Functions.getCast<float>(f2, "m_absorbDamage", effect);
                shield.AbsorbDmgWorldLevel = Functions.getCast<float>(f2, "m_absorbDamageWorldLevel", effect);
                shield.LevelUpSkillFactor = Functions.getCast<float>(f2, "m_levelUpSkillFactor", effect);
                shield.TtlPerItemLevel = Functions.getCast<int>(f2, "m_ttlPerItemLevel", effect);
                shield.AbsorbDmgPerSkill = Functions.getCast<float>(f2, "m_absorbDamagePerSkillLevel", effect);

             }

            SEPoison posion = new SEPoison();
            if (Functions.getCast<float>(f2, "m_TTLPerDamagePlayer", effect) > 0)
            { // the Skill levelup could work LevelUpSkillOnBreak
                posion.m_damageInterval = Functions.getCast<float>(f2, "m_damageInterval", effect);
                posion.m_baseTTL = Functions.getCast<float>(f2, "m_baseTTL", effect);
                posion.m_TTLPerDamagePlayer = Functions.getCast<float>(f2, "m_TTLPerDamagePlayer", effect);
                posion.m_TTLPerDamage = Functions.getCast<float>(f2, "m_TTLPerDamage", effect);
                posion.m_TTLPower = Functions.getCast<float>(f2, "m_TTLPower", effect);

            }

            SEFrost frost = new SEFrost();
            if (Functions.getCast<float>(f2, "m_freezeTimeEnemy", effect) > 0)
            { // the Skill levelup could work LevelUpSkillOnBreak
                frost.m_freezeTimeEnemy = Functions.getCast<float>(f2, "m_freezeTimeEnemy", effect);
                frost.m_freezeTimePlayer = Functions.getCast<float>(f2, "m_freezeTimePlayer", effect);
                frost.m_minSpeedFactor = Functions.getCast<float>(f2, "m_minSpeedFactor", effect);
              

            }


            SEdata stats = new SEdata
            {
                //m_tickInterval = f2.GetField("m_tickInterval", BindingFlags.Instance | BindingFlags.Public)?.GetValue(effect)

                m_heatlhUpFront= Functions.getCast<float>(f2, "m_healthUpFront", effect),
                m_tickInterval = Functions.getCast<float>(f2, "m_tickInterval", effect),
                m_healthPerTickMinHealthPercentage = Functions.getCast<float>(f2, "m_healthPerTickMinHealthPercentage", effect),
                m_healthPerTick = Functions.getCast<float>(f2, "m_healthPerTick", effect),
                m_healthOverTime = Functions.getCast<float>(f2, "m_healthOverTime", effect),
                m_healthOverTimeDuration = Functions.getCast<float>(f2, "m_healthOverTimeDuration", effect),
                m_healthOverTimeInterval = Functions.getCast<float>(f2, "m_healthOverTimeInterval", effect),

                m_healthOverTimeTimer = Functions.getCast<float>(f2, "m_healthOverTimeTimer", effect),
                m_healthOverTimeTicks = Functions.getCast<float>(f2, "m_healthOverTimeTicks", effect),
                m_healthOverTimeTickHP = Functions.getCast<float>(f2, "m_healthOverTimeTickHP", effect),

                m_staminaUpFront = Functions.getCast<float>(f2, "m_staminaUpFront", effect),
                m_staminaOverTime = Functions.getCast<float>(f2, "m_staminaOverTime", effect),
                m_staminaOverTimeDuration = Functions.getCast<float>(f2, "m_staminaOverTimeDuration", effect),
                m_staminaDrainPerSec = Functions.getCast<float>(f2, "m_staminaDrainPerSec", effect),
                m_runStaminaDrainModifier = Functions.getCast<float>(f2, "m_runStaminaDrainModifier", effect),
                m_jumpStaminaUseModifier = Functions.getCast<float>(f2, "m_jumpStaminaUseModifier", effect),
                m_attackStaminaUseModifier = Functions.getCast<float>(f2, "m_attackStaminaUseModifier", effect),
                m_blockStaminaUseModifier = Functions.getCast<float>(f2, "m_blockStaminaUseModifier", effect),
                m_dodgeStaminaUseModifier = Functions.getCast<float>(f2, "m_dodgeStaminaUseModifier", effect),
                m_swimStaminaUseModifier = Functions.getCast<float>(f2, "m_swimStaminaUseModifier", effect),
                m_homeItemStaminaUseModifier = Functions.getCast<float>(f2, "m_homeItemStaminaUseModifier", effect),
                m_sneakStaminaUseModifier = Functions.getCast<float>(f2, "m_sneakStaminaUseModifier", effect),
                m_runStaminaUseModifier = Functions.getCast<float>(f2, "m_runStaminaUseModifier", effect),

                m_adrenalineUpFront = Functions.getCast<float>(f2, "m_adrenalineUpFront", effect),
                m_adrenalineModifier = Functions.getCast<float>(f2, "m_adrenalineModifier", effect),

                m_staggerModifier = Functions.getCast<float>(f2, "m_staggerModifier", effect),
                m_staggerTimeBlockBonus = Functions.getCast<float>(f2, "m_timedBlockBonus", effect),

                m_eitrUpFront = Functions.getCast<float>(f2, "m_eitrUpFront", effect),
                m_eitrOverTime = Functions.getCast<float>(f2, "m_eitrOverTime", effect),
                m_eitrOverTimeDuration = Functions.getCast<float>(f2, "m_eitrOverTimeDuration", effect),
                m_healthRegenMultiplier = Functions.getCast<float>(f2, "m_healthRegenMultiplier", effect),
                m_staminaRegenMultiplier = Functions.getCast<float>(f2, "m_staminaRegenMultiplier", effect),
                m_eitrRegenMultiplier = Functions.getCast<float>(f2, "m_eitrRegenMultiplier", effect),

                
                m_armorAdd = Functions.getCast<float>(f2, "m_addArmor", effect),
                m_armorMultiplier = Functions.getCast<float>(f2, "m_armorMultiplier", effect),

                m_raiseSkill = Functions.getCast<Skills.SkillType>(f2, "m_raiseSkill", effect),
                m_raiseSkillModifier = Functions.getCast<float>(f2, "m_raiseSkillModifier", effect),
                m_skillLevel = Functions.getCast<Skills.SkillType>(f2, "m_skillLevel", effect),
                m_skillLevelModifier = Functions.getCast<float>(f2, "m_skillLevelModifier", effect),
                m_skillLevel2 = Functions.getCast<Skills.SkillType>(f2, "m_skillLevel2", effect),
                m_skillLevelModifier2 = Functions.getCast<float>(f2, "m_skillLevelModifier2", effect),
                m_mods = Functions.getCast<List<HitData.DamageModPair>>(f2, "m_mods", effect),
                m_modifyAttackSkill = Functions.getCast<Skills.SkillType>(f2, "m_modifyAttackSkill", effect),
                m_damageModifier = Functions.getCast<float>(f2, "m_damageModifier",effect),
                m_noiseModifier = Functions.getCast<float>(f2, "m_noiseModifier", effect),
                m_stealthModifier = Functions.getCast<float>(f2, "m_stealthModifier", effect),
                m_addMaxCarryWeight = Functions.getCast<float>(f2, "m_addMaxCarryWeight", effect),
                m_speedModifier = Functions.getCast<float>(f2, "m_speedModifier", effect),
                m_jumpModifier = Functions.getCast<Vector3>(f2, "m_jumpModifier", effect),
                m_maxMaxFallSpeed = Functions.getCast<float>(f2, "m_maxMaxFallSpeed", effect),
                m_fallDamageModifier = Functions.getCast<float>(f2, "m_fallDamageModifier", effect),
                m_tickTimer = Functions.getCast<float>(f2, "m_tickTimer", effect),
                m_windMovementModifier = Functions.getCast<float>(f2, "m_windMovementModifier", effect),

            };

        StatusData statusData = new StatusData
            {

                Name = effect.name ?? "",
                Status_m_name = effect.m_name ?? "",
                Category = effect.m_category ?? null,
                IconName = effect.m_icon.name ?? "",
                //CustomIcon = effect.m_icon.name ?? "",
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
                TimeToLive = effect.m_ttl,
                StartEffect_PLUS = ConvertEffectstoVerse(effect.m_startEffects?.m_effectPrefabs) ?? null,
                StopEffect_PLUS = ConvertEffectstoVerse(effect.m_stopEffects?.m_effectPrefabs) ?? null,
                Cooldown = effect.m_cooldown,
                ActivationAnimation = effect.m_activationAnimation ?? "",
                SeData = stats,              
            };
            if(Functions.getCast<float>(f2, "m_absorbDamage", effect) > 0)
            {
                statusData.SeShield = shield; 
            }
            if (Functions.getCast<float>(f2, "m_TTLPerDamagePlayer", effect) > 0)
            {
                statusData.SePoison = posion;
            }            
            if (Functions.getCast<float>(f2, "m_freezeTimeEnemy", effect) > 0)
            {
                statusData.SeFrost = frost;
            }


            /*
            List<HitData.DamageModPair> jsoh = Functions.getCast<List<HitData.DamageModPair>>(f2, "m_mods", effect);
            if (jsoh != null)
            {
                if (jsoh.Count() > 0)
                {
                    foreach (var list in jsoh)
                    {
                        WMRecipeCust.WLog.LogInfo("                      mods " + list.m_modifier + " " + list.m_type);
                    }
                }
            }*/


            return statusData;

        }


        internal GameObject GetJustThePieceRecipeByName(string name, ObjectDB tod, bool warn = true)
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

            return go;
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
            ItemDrop hammer = null;
            if (WMRecipeCust.selectedPiecehammer != null)
            {

                hammer = WMRecipeCust.selectedPiecehammer.GetComponent<ItemDrop>();
                piecehammer = WMRecipeCust.selectedPiecehammer.name;
            }
            else
            {

                if (piecehammer == null)
                    piecehammer = "Hammer"; // default

                hammer = tod.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
                ItemDrop hoe = tod.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
                if (hammer && hammer.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                {
                    piecehammer = "Hammer";

                } else if (hoe && hoe.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                {
                    piecehammer = "Hoe";
                    hammer = hoe;

                }else
                {
                    WMRecipeCust.WLog.LogWarning("Hammer selector needs help! in getdata GetPieceRecipeByName");
                }                

            }

            
            return GetPiece(hammer, piecehammer, go, tod);

        }

        internal PieceData GetPieceRecipeByNum(int count, string hammername, ItemDrop HamerItemdrop, ObjectDB tod, ItemDrop itemD = null)
        {
            GameObject pieceSel = null;

            pieceSel = HamerItemdrop.m_itemData.m_shared.m_buildPieces.m_pieces[count];
                //Piece actPiece = pieceSel.GetComponent<Piece>();


            return GetPiece(HamerItemdrop, hammername, pieceSel, tod);

        }

        internal PieceData GetPiece (ItemDrop HammerID,string Hammername, GameObject PieceID, ObjectDB tod)
        {
            Dictionary<Piece.PieceCategory, string> currentmap = PieceManager.PiecePrefabManager.GetPieceCategoriesMap();

            Piece piece = PieceID.GetComponent<Piece>();
            WMRecipeCust.WLog.LogInfo($"Piece {PieceID.name} in {Hammername}");
            //WMRecipeCust.WLog.LogWarning("Piece start");
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
                piecehammerCategory = currentmap[piece.m_category],//piece.m_category.ToString(),
                sizeMultiplier = "1",
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
                notOnWood = piece.m_notOnWood,

            };
            
           
            if (piece.m_comfort != 0)
            {
                //WMRecipeCust.WLog.LogWarning("Piece Comfort");
                ComfortData comfort = new ComfortData
                {
                    comfort = piece.m_comfort,
                    comfortGroup = piece.m_comfortGroup,
                    comfortObjectName = piece.m_comfortObject?.name,
                };
                data.comfort = comfort;
             }

            if (PieceID.TryGetComponent<WearNTear>(out var wear))
            {
               // var wear = PieceID.GetComponent<WearNTear>();
                //WMRecipeCust.WLog.LogWarning("Piece Wear");
                WearNTearData wearNTearData = new WearNTearData
                {

                 health = wear.m_health,
                 damageModifiers = wear.m_damages,
                 noRoofWear = wear.m_noRoofWear,
                 noSupportWear = wear.m_noSupportWear,
                 supports = wear.m_supports,
                 triggerPrivateArea = wear.m_triggerPrivateArea,
                 materialType = wear.m_materialType,
                 burnable = wear.m_burnable,
                 };

                data.wearNTearData = wearNTearData;
            }

            if (PieceID.TryGetComponent<CraftingStation>(out var station))
            {
                //WMRecipeCust.WLog.LogWarning("Piece CraftingStation");
                //var station = PieceID.GetComponent<CraftingStation>();
                CraftingStationData craftingStationData = new CraftingStationData
                {
                  //  cStationName = station.name,
                 cStationCustomIcon = null,
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

            if (PieceID.TryGetComponent<StationExtension>(out var ex))
            {
                // WMRecipeCust.WLog.LogWarning("Piece StationExtension");
                //var ex = PieceID.GetComponent<StationExtension>();

                if (PieceID.GetComponents<StationExtension>().Count() > 1)
                {
                    //WMRecipeCust.WLog.LogWarning("Station Count is " + PieceID.GetComponents<StationExtension>().Count());
                    data.cSExtensionDataList = new List<CSExtensionData>();
                    foreach (var ext in PieceID.GetComponents<StationExtension>())
                    {
                        CSExtensionData cSExtension2 = new();
                        cSExtension2.MainCraftingStationName = ext.m_craftingStation?.name ?? ext.m_craftingStation.ToString();                                             
                        cSExtension2.maxStationDistance = ext.m_maxStationDistance;
                        cSExtension2.continousConnection = ext.m_continousConnection; 
                        cSExtension2.stack = ext.m_stack;
                        
                        data.cSExtensionDataList.Add(cSExtension2);
                    }
                }
                else
                {
                    data.cSExtensionDataList = new List<CSExtensionData>();
                    CSExtensionData cSExtension = new CSExtensionData
                    {
                        MainCraftingStationName = ex.m_craftingStation.name,
                        maxStationDistance = ex.m_maxStationDistance,
                        continousConnection = ex.m_continousConnection,
                        stack = ex.m_stack,
                    };
                    data.cSExtensionDataList.Add(cSExtension);
                    
                }
            }

            if (PieceID.TryGetComponent<Container>(out var cont))
            {
                ContainerData ContData = new ContainerData
                {
                    Width = cont.m_width,
                    Height = cont.m_height,
                   // Privacy = cont.m_privacy.ToString(),
                    CheckWard = cont.m_checkGuardStone,
                    AutoDestoryIfEmpty = cont.m_autoDestroyEmpty,
                };
                data.contData = ContData;
            }

            if (PieceID.TryGetComponent<Beehive>(out var Bee))
            {
                BeehiveData BeeData = new BeehiveData
                {
                    effectOnlyInDaylight = Bee.m_effectOnlyInDaylight,
                    maxCover = Bee.m_maxCover,
                    biomes = Bee.m_biome,
                    secPerUnit = Bee.m_secPerUnit,
                    maxAmount = Bee.m_maxHoney,
                    dropItem = Bee.m_honeyItem.name,
                    effectsPLUS = ConvertEffectstoVerse(Bee.m_spawnEffect.m_effectPrefabs) ?? null,
                    extractText = Bee.m_extractText,
                    checkText = Bee.m_checkText,
                    areaText = Bee.m_areaText,
                    freespaceText = Bee.m_freespaceText,
                    sleepText = Bee.m_sleepText,
                    happyText = Bee.m_happyText,
                };
                data.beehiveData = BeeData;
            }

            if (PieceID.TryGetComponent<CookingStation>(out var cook))
            {
                List<CookStationConversionList> CookConversionList = new List<CookStationConversionList>();
                foreach (var Item in cook.m_conversion)
                {
                    CookStationConversionList cookl = new CookStationConversionList();
                    cookl.FromName = Item.m_from.name;
                    cookl.ToName = Item.m_to.name;
                    cookl.CookTime = Item.m_cookTime;
                    cookl.Remove = false;
                    CookConversionList.Add(cookl);
                } 
                CookingStationData CookData2 = new CookingStationData();

                if (cook.name == "piece_oven")
                {                
                    CookData2.addItemTooltip = cook.m_addItemTooltip;
                    CookData2.overcookedItem = cook.m_overCookedItem.name;
                    CookData2.fuelItem = cook.m_fuelItem.name;
                    CookData2.requireFire = cook.m_requireFire;
                    CookData2.maxFuel = cook.m_maxFuel;
                    CookData2.secPerFuel = cook.m_secPerFuel;
                    CookData2.cookConversion = CookConversionList;
                    data.cookingStationData = CookData2;
                }
                else
                {                
                    CookData2.addItemTooltip = cook.m_addItemTooltip;
                    CookData2.overcookedItem = cook.m_overCookedItem.name;
                    CookData2.requireFire = cook.m_requireFire;
                    CookData2.cookConversion = CookConversionList;
                    data.cookingStationData = CookData2;

                }
            }

            if (PieceID.TryGetComponent<Fermenter>(out var ferm))
            {
                List<FermenterConversionList> fermenterConversionLists = new List<FermenterConversionList>();
                foreach ( var ferms in ferm.m_conversion)
                {
                    FermenterConversionList temp1 = new FermenterConversionList();
                    temp1.FromName = ferms.m_from.gameObject.name;
                    temp1.ToName = ferms.m_to.gameObject.name;
                    temp1.Amount = ferms.m_producedItems;
                    temp1.Remove = false;
                    fermenterConversionLists.Add(temp1);
                }
                FermenterData fermdata = new FermenterData();

                fermdata.fermDuration = ferm.m_fermentationDuration;
                fermdata.fermConversion = fermenterConversionLists;
                data.fermStationData = fermdata;
                

            }
            if (PieceID.TryGetComponent<Ship>(out var ship))
            {
                ShipData shipdata = new ShipData();
                shipdata.ashlandProof = ship.m_ashlandsReady;
                //shipdata.shipHealth = ship.m_

                data.shipData = shipdata;

            }

            if (PieceID.TryGetComponent<ShieldGenerator>(out var shield))
            {
                ShieldGenData shielddata = new ShieldGenData();
                shielddata.name = shield.m_name;
                shielddata.nameAdd  = shield.m_add;
                shielddata.fuelPerDamage  = shield.m_fuelPerDamage;
                shielddata.offWhenOutofFuel  = shield.m_offWhenNoFuel;
                shielddata.maxFuel  = shield.m_maxFuel;
                shielddata.spawnWithFuel = shield.m_defaultFuel;
                shielddata.maxRadius = shield.m_maxShieldRadius;
                shielddata.minRadius = shield.m_minShieldRadius;
                shielddata.attack = shield.m_enableAttack;
                shielddata.attackChargeTime = shield.m_attackChargeTime;
                shielddata.attackPlayers = shield.m_damagePlayers;
                    
                List<string> sup = new List<string>();
                foreach(var it in shield.m_fuelItems)
                {
                    sup.Add(it.name);
                }
                shielddata.fuel = sup;

                data.shieldGenData = shielddata;

            }

            if (PieceID.TryGetComponent<SiegeMachine>(out var siege))
            {
                BatteringRamData siegeData = new BatteringRamData();
                siegeData.chargeTime = siege.m_chargeTime;
                siegeData.maxFuel = PieceID.GetComponentInChildren<Smelter>().m_maxOre;

                data.batteringRamData = siegeData;
            }

                if (PieceID.TryGetComponent<Plant>(out var plant))
            {
                PlantData plantdata = new PlantData();

                plantdata.m_name = plant.m_name;
                plantdata.GrowTime = plant.m_growTime;
                plantdata.MaxGrowTime = plant.m_growTimeMax;
                plantdata.GrowPrefab = plant.m_grownPrefabs[0].name;
                plantdata.MinSize = plant.m_minScale;
                plantdata.MaxSize = plant.m_maxScale;
                plantdata.GrowRadius = plant.m_growRadius;
                plantdata.GrowRadiusVines = plant.m_growRadiusVines;
                plantdata.CultivatedGround = plant.m_needCultivatedGround;
                plantdata.DestroyIfCantGrow = plant.m_destroyIfCantGrow;
                plantdata.TolerateHeat = plant.m_tolerateHeat;
                plantdata.TolerateCold = plant.m_tolerateCold;
                plantdata.Biomes = plant.m_biome;

                data.plantData = plantdata;

            }
    

            if (PieceID.TryGetComponent<SapCollector>(out var sap))
            {
                SapData sapdata = new SapData();
                sapdata.secPerUnit = sap.m_secPerUnit;
                sapdata.maxLevel = sap.m_maxLevel;
                sapdata.producedItem = sap.m_spawnItem.gameObject.name;
                sapdata.connectedToWhat = sap.m_mustConnectTo.gameObject.name;

                sapdata.extractText = sap.m_extractText;
                sapdata.drainingText = sap.m_drainingText;
                sapdata.drainingSlowText = sap.m_drainingSlowText;
                sapdata.notConnectedText = sap.m_notConnectedText;
                sapdata.fullText = sap.m_fullText;

                data.sapData = sapdata;

            }
            if (PieceID.TryGetComponent<Incinerator>(out var obl))
            {
                ObliteratorData obldata = new ObliteratorData();
                obldata.defaultCostPerDrop = obl.m_defaultCost;
                obldata.defaultDrop = obl.m_defaultResult.name;

                List<ObliteratorList> OblConversionList = new List<ObliteratorList>();
                foreach (var con in obl.m_conversions)
                {
                    ObliteratorList oblitList = new ObliteratorList();
                    oblitList.Priority = con.m_priority;
                    oblitList.Result = con.m_result.name;
                    oblitList.ResultAmount = con.m_resultAmount;
                    oblitList.RequireOnlyOne = con.m_requireOnlyOneIngredient;
                                      
                    List<ObRequirementList> OblReqList = new List<ObRequirementList>();                 
                    foreach (var reqO in con.m_requirements) 
                    {
                        ObRequirementList obRequirementList = new ObRequirementList();
                        obRequirementList.Name = reqO.m_resItem.name;
                        obRequirementList.Amount = reqO.m_amount;
                        OblReqList.Add(obRequirementList);
                    } 
                    oblitList.Requirements = OblReqList;
                    OblConversionList.Add(oblitList);
                }
                data.incineratorData = obldata;
                data.incineratorData.incineratorConversion = OblConversionList;

            }
            if (PieceID.TryGetComponent<Fireplace>(out var FP))
            {
                try
                {
                    FireplaceData fireData = new FireplaceData();
                    fireData.StartFuel = FP.m_startFuel;
                    fireData.MaxFuel = FP.m_maxFuel;
                    fireData.SecPerFuel = FP.m_secPerFuel;
                    fireData.InfiniteFuel = FP.m_infiniteFuel;
                    fireData.FuelType = FP.m_fuelItem.name;
                    fireData.IgniteInterval = FP.m_igniteInterval;
                    fireData.IgniteChance = FP.m_igniteChance;
                    fireData.IgniteSpread = FP.m_igniteSpread;

                    data.fireplaceData = fireData;
                }
                catch { WMRecipeCust.WLog.LogWarning("Error catch in Fireplace"); }
                
            }

            if (PieceID.TryGetComponent<TeleportWorld>(out var tpW))
            {
                TeleportWorldData tpData = new TeleportWorldData();
                tpData.AllowAllItems = tpW.m_allowAllItems;
                data.teleportWorldData = tpData;
            }

            try {
            if (PieceID.TryGetComponent<Smelter>(out var smelt))
            {
                // WMRecipeCust.WLog.LogWarning("Piece Smelter");
                // var smelt = PieceID.GetComponent<Smelter>();
                if (smelt.name == "charcoal_kiln" || smelt.name == "windmill" || smelt.name == "piece_spinningwheel")
                {

                    List<SmelterConversionList> smelterConversionList = new List<SmelterConversionList>();
                    foreach (var Item in smelt.m_conversion)
                    {
                        SmelterConversionList smell = new SmelterConversionList();
                        smell.FromName = Item.m_from.name;
                        smell.ToName = Item.m_to.name;
                        smell.Remove = false;
                        smelterConversionList.Add(smell);
                    }

                    SmelterData smelterData2 = new SmelterData
                    {
                        //smelterName = smelt.name,
                        smelterConversion = smelterConversionList,
                        emptyOreTooltip = smelt.m_emptyOreTooltip,
                        addOreTooltip = smelt.m_addOreTooltip,
                        maxOre = smelt.m_maxOre,
                        secPerProduct = smelt.m_secPerProduct,

                    };
                    data.smelterData = smelterData2;
                }
                else
                {
                    fuelItemData fuelItemData = new fuelItemData
                    {
                        name = smelt.m_fuelItem.name,
                        // ItemNameShared = smelt.m_fuelItem.m_itemData.m_shared.m_name,
                    };

                    List<SmelterConversionList> smelterConversionList = new List<SmelterConversionList>();
                    foreach (var Item in smelt.m_conversion)
                    {
                        SmelterConversionList smell = new SmelterConversionList();
                        smell.FromName = Item.m_from.name;
                        smell.ToName = Item.m_to.name;
                        smell.Remove = false;
                        smelterConversionList.Add(smell);
                    }


                    SmelterData smelterData = new SmelterData
                    {
                        //smelterName = smelt.name,
                        addOreTooltip = smelt.m_addOreTooltip,
                        emptyOreTooltip = smelt.m_emptyOreTooltip,
                        //addFuelSwitch = smelt.m_addWoodSwitch,
                        // addOreSwitch = smelt.m_addOreSwitch,
                        //emptyOreSwitch = smelt.m_emptyOreSwitch,
                        //fuelItem = smelt.m_fuelItem,
                        fuelItem = fuelItemData,
                        maxOre = smelt.m_maxOre,
                        maxFuel = smelt.m_maxFuel,
                        fuelPerProduct = smelt.m_fuelPerProduct,
                        secPerProduct = smelt.m_secPerProduct,
                        spawnStack = smelt.m_spawnStack,
                        requiresRoof = smelt.m_requiresRoof,
                        addOreAnimationLength = smelt.m_addOreAnimationDuration,
                        smelterConversion = smelterConversionList,
                    };
                    data.smelterData = smelterData;
                }
            } 
            } catch  { }

    
            foreach (Piece.Requirement req in piece.m_resources)
            {
                data.build.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
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

        private string[] CheckEffectsArray(EffectList.EffectData[] tosh)
        {
            if (tosh == null) return null;
            string[] temp = null;
            try
            {
                if (tosh.Count() > 0 && tosh != null)
                {
                    return tosh.Select(p => p.m_prefab.name).ToArray();
                }
            }
            catch { }
            return temp;

        }

        private EffectVerse[]? ConvertEffectstoVerse(EffectList.EffectData[] tosh)
        {
            if (tosh == null) return null;
            try
            {
                EffectVerse[] temp = new EffectVerse[tosh.Count()];
                var count = 0;
                foreach (EffectList.EffectData eff in tosh)
                {
                    if (eff != null )
                    {
                        if (eff.m_prefab != null) {
                            //WMRecipeCust.WLog.LogWarning(" Effect Check 1 ");
                            EffectVerse james = new EffectVerse();

                            james.name = eff.m_prefab?.name;
                            //WMRecipeCust.WLog.LogWarning(" Effect Check 2 ");
                            james.m_enabled = eff.m_enabled;
                            james.m_variant = eff.m_variant;
                            james.m_attach = eff.m_attach;
                            james.m_follow = eff.m_follow;
                            james.m_inheritParentRotation = eff.m_inheritParentRotation;
                            james.m_inheritParentScale = eff.m_inheritParentScale;
                            james.m_multiplyParentVisualScale = eff.m_multiplyParentVisualScale;
                            james.m_randomRotation = eff.m_randomRotation;
                            james.m_scale = eff.m_scale;
                            james.m_childTransform = eff.m_childTransform;

                            temp[count] = james;
                            count++;
                        }
                        else
                        {
                            WMRecipeCust.WLog.LogWarning(" effect prefab is null, skipping effect");
                        }
                    }
                    else
                    {
                        WMRecipeCust.WLog.LogWarning(" effect is null, skipping effect" );
                    }
                    
                }

                return temp;
            } catch { WMRecipeCust.WLog.LogWarning(" One Effect returned null - be careful with last item " ); return null; }
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

            StatMods StatModdifers = new StatMods()
            {
                m_movementModifier = data.m_shared.m_movementModifier,
                m_EitrRegen = data.m_shared.m_eitrRegenModifier,
                m_homeItemsStaminaModifier = data.m_shared.m_homeItemsStaminaModifier,
                m_heatResistanceModifier = data.m_shared.m_heatResistanceModifier,
                m_jumpStaminaModifier = data.m_shared.m_jumpStaminaModifier,
                m_attackStaminaModifier = data.m_shared.m_attackStaminaModifier,
                m_blockStaminaModifier = data.m_shared.m_blockStaminaModifier,
                m_dodgeStaminaModifier = data.m_shared.m_dodgeStaminaModifier,
                m_swimStaminaModifier = data.m_shared.m_swimStaminaModifier,
                m_sneakStaminaModifier = data.m_shared.m_sneakStaminaModifier,
                m_runStaminaModifier = data.m_shared.m_runStaminaModifier

    };


            GEffectsPLUS gEffects = new GEffectsPLUS()
            {
                Hit_Effects = ConvertEffectstoVerse(data.m_shared.m_hitEffect?.m_effectPrefabs),
                Hit_Terrain_Effects = ConvertEffectstoVerse(data.m_shared.m_hitTerrainEffect?.m_effectPrefabs),
                Start_Effect = ConvertEffectstoVerse(data.m_shared.m_startEffect?.m_effectPrefabs),
                Hold_Start_Effects = ConvertEffectstoVerse(data.m_shared.m_holdStartEffect?.m_effectPrefabs),
                Trigger_Effect = ConvertEffectstoVerse(data.m_shared.m_triggerEffect?.m_effectPrefabs),
                Trail_Effect = ConvertEffectstoVerse(data.m_shared.m_trailStartEffect?.m_effectPrefabs),
            };
            AEffectsPLUS aEffects = new AEffectsPLUS()
            {
                Hit_Effects = ConvertEffectstoVerse(data.m_shared.m_attack?.m_hitEffect?.m_effectPrefabs) ,
                Hit_Terrain_Effects = ConvertEffectstoVerse(data.m_shared.m_attack?.m_hitTerrainEffect.m_effectPrefabs),
                Start_Effect = ConvertEffectstoVerse(data.m_shared.m_attack?.m_startEffect.m_effectPrefabs),
                Trigger_Effect = ConvertEffectstoVerse(data.m_shared.m_attack?.m_triggerEffect.m_effectPrefabs),
                Trail_Effect = ConvertEffectstoVerse(data.m_shared.m_attack?.m_trailStartEffect.m_effectPrefabs),
                Burst_Effect = ConvertEffectstoVerse(data.m_shared.m_attack?.m_burstEffect?.m_effectPrefabs),

            };

            AEffectsPLUS sEffects = new AEffectsPLUS()
            {
                Hit_Effects = ConvertEffectstoVerse(data.m_shared.m_secondaryAttack?.m_hitEffect?.m_effectPrefabs),
                Hit_Terrain_Effects = ConvertEffectstoVerse(data.m_shared.m_secondaryAttack?.m_hitTerrainEffect.m_effectPrefabs),
                Start_Effect = ConvertEffectstoVerse(data.m_shared.m_secondaryAttack?.m_startEffect.m_effectPrefabs),
                Trigger_Effect = ConvertEffectstoVerse(data.m_shared.m_secondaryAttack?.m_triggerEffect.m_effectPrefabs),
                Trail_Effect = ConvertEffectstoVerse(data.m_shared.m_secondaryAttack?.m_trailStartEffect.m_effectPrefabs),
                Burst_Effect = ConvertEffectstoVerse(data.m_shared.m_secondaryAttack?.m_burstEffect?.m_effectPrefabs),
            };

            WMRecipeCust.Dbgl("Item " + go.GetComponent<ItemDrop>().name + " Main ");
            WItemData ItemData = new WItemData
            {
                name = go.GetComponent<ItemDrop>().name,
                //m_armor = data.m_shared.m_armor,
                //clone = false,
                //clonePrefabName = "",
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
                sizeMultiplier = "1", // default scale
                m_weight = data.m_shared.m_weight,
                m_destroyBroken = data.m_shared.m_destroyBroken,
                m_dodgeable = data.m_shared.m_dodgeable,
                blockable = data.m_shared.m_blockable,
                m_canBeReparied = data.m_shared.m_canBeReparied,
                m_name = data.m_shared.m_name,
                m_questItem = data.m_shared.m_questItem,
                m_teleportable = data.m_shared.m_teleportable,
                m_knockback = data.m_shared.m_attackForce,
                m_skillType = data.m_shared.m_skillType,
                m_animationState = data.m_shared.m_animationState,
                m_itemType = data.m_shared.m_itemType,
                Attach_Override = data.m_shared.m_attachOverride,
                Damage = damages,
                Damage_Per_Level = damagesPerLevel,
                Moddifiers = StatModdifers,
                damageModifiers = data.m_shared.m_damageModifiers.Select(m => m.m_type + ":" + m.m_modifier).ToList(),
                GEffectsPLUS = gEffects,



                //damageModifiers = data.m_shared.m_damageModifiers.Select(m => m.m_type + ":" + m.m_modifier).ToList(),

            };

            if (WMRecipeCust.ClonedPrefabsMap.ContainsKey(go.name))
            {
                ItemData.clonePrefabName = WMRecipeCust.ClonedPrefabsMap[go.name];
            }
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
            if(go.name == "StaffShield" || go.name == "StaffSkeleton") // for the special cases
                hasdmg = true;

            if (go.TryGetComponent<Attack>(out var attackcheck) )
            {
                if (attackcheck != null)
                {
                    if (attackcheck.m_attackAnimation != null || attackcheck.m_attackAnimation != "")
                        hasdmg = true;
                }
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
                    Hit_Friendly = data.m_shared.m_attack.m_hitFriendly,
                    is_HomeItem = data.m_shared.m_attack.m_isHomeItem,
                    Custom_AttackSpeed = 1,

                    m_attackStamina = data.m_shared.m_attack.m_attackStamina,
                    m_attackAdrenaline = data.m_shared.m_attack.m_attackAdrenaline,
                    m_attackUseAdrenaline = data.m_shared.m_attack.m_attackUseAdrenaline,
                    m_eitrCost = data.m_shared.m_attack.m_attackEitr,
                    AttackHealthCost = data.m_shared.m_attack.m_attackHealth,
                    m_attackHealthPercentage = data.m_shared.m_attack.m_attackHealthPercentage,
                    attack_Health_Low_BlockUsage = data.m_shared.m_attack.m_attackHealthLowBlockUse,

                    Attack_Start_Noise = data.m_shared.m_attack.m_attackStartNoise,
                    Attack_Hit_Noise = data.m_shared.m_attack.m_attackHitNoise,
                    Dmg_Multiplier_Per_Missing_Health = data.m_shared.m_attack.m_damageMultiplierPerMissingHP,
                    Damage_Multiplier_By_Health_Deficit_Percent = data.m_shared.m_attack.m_damageMultiplierByTotalHealthMissing,
                    Stamina_Return_Per_Missing_HP = data.m_shared.m_attack.m_staminaReturnPerMissingHP,
                    SelfDamage = data.m_shared.m_attack.m_selfDamage,
                    Attack_Kills_Self = data.m_shared.m_attack.m_attackKillsSelf,

                    SpeedFactor = data.m_shared.m_attack.m_speedFactor,
                    DmgMultiplier = data.m_shared.m_attack.m_damageMultiplier,
                    ForceMultiplier = data.m_shared.m_attack.m_forceMultiplier,
                    StaggerMultiplier = data.m_shared.m_attack.m_staggerMultiplier,
                    RecoilMultiplier = data.m_shared.m_attack.m_recoilPushback,

                    AttackRange = data.m_shared.m_attack.m_attackRange,
                    AttackHeight = data.m_shared.m_attack.m_attackHeight,
                    Spawn_On_Trigger = data.m_shared.m_attack.m_spawnOnTrigger?.name,
                    cant_Use_InDungeon = data.m_shared.m_attack.m_cantUseInDungeon,

                    Requires_Reload = data.m_shared.m_attack.m_requiresReload,
                    Reload_Animation = data.m_shared.m_attack.m_reloadAnimation,
                    ReloadTime = data.m_shared.m_attack.m_reloadTime,
                    ReloadTimeMultiplier = 1.0f,
                    Reload_Stamina_Drain = data.m_shared.m_attack.m_reloadStaminaDrain,
                    Reload_Eitr_Drain = data.m_shared.m_attack.m_reloadEitrDrain,

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
                    Reset_Chain_If_hit = data.m_shared.m_attack.m_resetChainIfHit,

                    SpawnOnHit = data.m_shared.m_attack.m_spawnOnHit?.name,
                    SpawnOnHit_Chance = data.m_shared.m_attack.m_spawnOnHitChance,
                    
                    Raise_Skill_Amount = data.m_shared.m_attack.m_raiseSkillAmount,
                    Skill_Hit_Type = data.m_shared.m_attack.m_skillHitType,
                    Special_Hit_Skill = data.m_shared.m_attack.m_specialHitSkill,
                    Special_Hit_Type = data.m_shared.m_attack.m_specialHitType,
                    


                    Attack_Projectile = data.m_shared.m_attack.m_attackProjectile?.name,
                    Projectile_Vel = data.m_shared.m_attack.m_projectileVel,
                    Projectile_Accuraccy = data.m_shared.m_attack.m_projectileAccuracy,
                    Projectiles = data.m_shared.m_attack.m_projectiles,

                    Skill_Accuracy = data.m_shared.m_attack.m_skillAccuracy,
                    Launch_Angle = data.m_shared.m_attack.m_launchAngle,
                    Projectile_Burst = data.m_shared.m_attack.m_projectileBursts,
                    Burst_Interval = data.m_shared.m_attack.m_burstInterval,
                    Destroy_Previous_Projectile = data.m_shared.m_attack.m_destroyPreviousProjectile,
                    PerBurst_Resource_usage = data.m_shared.m_attack.m_perBurstResourceUsage,
                    Looping_Attack = data.m_shared.m_attack.m_loopingAttack,
                    Consume_Item = data.m_shared.m_attack.m_consumeItem,


                    AEffectsPLUS = aEffects,
                };

                AttackArm Secondary_Attack = new AttackArm
                {
                    AttackType = data.m_shared.m_secondaryAttack.m_attackType,
                    Attack_Animation = data.m_shared.m_secondaryAttack.m_attackAnimation,
                    Attack_Random_Animation = data.m_shared.m_secondaryAttack.m_attackRandomAnimations,
                    Chain_Attacks = data.m_shared.m_secondaryAttack.m_attackChainLevels,
                    Hit_Terrain = data.m_shared.m_secondaryAttack.m_hitTerrain,
                    Hit_Friendly = data.m_shared.m_secondaryAttack.m_hitFriendly,
                    is_HomeItem = data.m_shared.m_secondaryAttack.m_isHomeItem,
                    Custom_AttackSpeed = 1,

                    m_attackStamina = data.m_shared.m_secondaryAttack.m_attackStamina,
                    m_attackAdrenaline = data.m_shared.m_secondaryAttack.m_attackAdrenaline,
                    m_attackUseAdrenaline = data.m_shared.m_secondaryAttack.m_attackUseAdrenaline,
                    m_eitrCost = data.m_shared.m_secondaryAttack.m_attackEitr,
                    AttackHealthCost = data.m_shared.m_secondaryAttack.m_attackHealth,
                    m_attackHealthPercentage = data.m_shared.m_secondaryAttack.m_attackHealthPercentage,
                    attack_Health_Low_BlockUsage = data.m_shared.m_secondaryAttack.m_attackHealthLowBlockUse,

                    Attack_Start_Noise = data.m_shared.m_secondaryAttack.m_attackStartNoise,
                    Attack_Hit_Noise = data.m_shared.m_secondaryAttack.m_attackHitNoise,
                    Dmg_Multiplier_Per_Missing_Health = data.m_shared.m_secondaryAttack.m_damageMultiplierPerMissingHP,
                    Damage_Multiplier_By_Health_Deficit_Percent = data.m_shared.m_secondaryAttack.m_damageMultiplierByTotalHealthMissing,
                    Stamina_Return_Per_Missing_HP = data.m_shared.m_secondaryAttack.m_staminaReturnPerMissingHP,
                    SelfDamage = data.m_shared.m_secondaryAttack.m_selfDamage,
                    Attack_Kills_Self = data.m_shared.m_secondaryAttack.m_attackKillsSelf,

                    SpeedFactor = data.m_shared.m_secondaryAttack.m_speedFactor,
                    DmgMultiplier = data.m_shared.m_secondaryAttack.m_damageMultiplier,
                    ForceMultiplier = data.m_shared.m_secondaryAttack.m_forceMultiplier,
                    StaggerMultiplier = data.m_shared.m_secondaryAttack.m_staggerMultiplier,
                    RecoilMultiplier = data.m_shared.m_secondaryAttack.m_recoilPushback,

                    AttackRange = data.m_shared.m_secondaryAttack.m_attackRange,
                    AttackHeight = data.m_shared.m_secondaryAttack.m_attackHeight,
                    Spawn_On_Trigger = data.m_shared.m_secondaryAttack.m_spawnOnTrigger?.name,
                    cant_Use_InDungeon = data.m_shared.m_secondaryAttack.m_cantUseInDungeon,

                    Requires_Reload = data.m_shared.m_secondaryAttack.m_requiresReload,
                    Reload_Animation = data.m_shared.m_secondaryAttack.m_reloadAnimation,
                    ReloadTimeMultiplier = 1.0f,
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
                    Reset_Chain_If_hit = data.m_shared.m_secondaryAttack.m_resetChainIfHit,

                    SpawnOnHit = data.m_shared.m_secondaryAttack.m_spawnOnHit?.name,
                    SpawnOnHit_Chance = data.m_shared.m_secondaryAttack.m_spawnOnHitChance,
                    
                    Raise_Skill_Amount = data.m_shared.m_secondaryAttack.m_raiseSkillAmount,
                    Skill_Hit_Type = data.m_shared.m_secondaryAttack.m_skillHitType,
                    Special_Hit_Skill = data.m_shared.m_secondaryAttack.m_specialHitSkill,
                    Special_Hit_Type = data.m_shared.m_secondaryAttack.m_specialHitType,
                    

                    Attack_Projectile = data.m_shared.m_secondaryAttack.m_attackProjectile?.name,
                    Projectile_Vel = data.m_shared.m_secondaryAttack.m_projectileVel,
                    Projectile_Accuraccy = data.m_shared.m_secondaryAttack.m_projectileAccuracy,
                    Projectiles = data.m_shared.m_secondaryAttack.m_projectiles,


                    Skill_Accuracy = data.m_shared.m_secondaryAttack.m_skillAccuracy,
                    Launch_Angle = data.m_shared.m_secondaryAttack.m_launchAngle,
                    Projectile_Burst = data.m_shared.m_secondaryAttack.m_projectileBursts,
                    Burst_Interval = data.m_shared.m_secondaryAttack.m_burstInterval,
                    Destroy_Previous_Projectile = data.m_shared.m_secondaryAttack.m_destroyPreviousProjectile,
                    PerBurst_Resource_usage = data.m_shared.m_secondaryAttack.m_perBurstResourceUsage,
                    Looping_Attack = data.m_shared.m_secondaryAttack.m_loopingAttack,
                    Consume_Item = data.m_shared.m_secondaryAttack.m_consumeItem,

                    AEffectsPLUS = sEffects,
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
                    eatAnimationTime = data.m_shared.m_foodEatAnimTime,
                    isDrink = data.m_shared.m_isDrink
                };
                

                if (go.TryGetComponent<Feast>(out var feastme))
                {
                    WMRecipeCust.Dbgl("  Feast Found - Starting complicated fixing scheme");
                    FoodStats.feastStacks = feastme.m_eatStacks;

                    GameObject feastMat = tod.GetItemPrefab(go.name + "_Material");
                    var feastMatItemDrop = feastMat.GetComponent<ItemDrop>().m_itemData.m_shared;
                    ItemData.m_description = feastMatItemDrop.m_description;
                    ItemData.m_maxStackSize = feastMatItemDrop.m_maxStackSize;
                    ItemData.m_weight = feastMatItemDrop.m_weight;

                }

                ItemData.FoodStats = FoodStats;


            }

            if (data.m_shared.m_maxAdrenaline != 0)
            {
                AdrenalineData AdrenalineStats = new AdrenalineData
                {
                    maxAdrenaline = data.m_shared.m_maxAdrenaline,
                    fullAdrenalineSE = data.m_shared.m_fullAdrenalineSE?.name,
                    blockAdrenaline = data.m_shared.m_blockAdrenaline,
                    perfectBlockAdrenaline = data.m_shared.m_perfectBlockAdrenaline,

                };
                ItemData.AdrenalineStats = AdrenalineStats;
            }


            if (data.m_shared.m_blockPower != 0)
            {
                ShieldData ShieldData = new ShieldData
                {
                    m_blockPower = data.m_shared.m_blockPower,
                    m_blockPowerPerLevel = data.m_shared.m_blockPowerPerLevel,
                    m_timedBlockBonus = data.m_shared.m_timedBlockBonus,
                    m_deflectionForce = data.m_shared.m_deflectionForce,
                    m_deflectionForcePerLevel = data.m_shared.m_deflectionForcePerLevel,
                    m_perfectBlockStaminaRegen = data.m_shared.m_perfectBlockStaminaRegen,
                    m_perfectBlockStatusEffect = data.m_shared.m_perfectBlockStatusEffect?.name,
                    m_buildBlockCharges = data.m_shared.m_buildBlockCharges,
                    m_maxBlockCharges = data.m_shared.m_maxBlockCharges,
                    m_blockChargeDecayTime = data.m_shared.m_blockChargeDecayTime

                };
                ItemData.ShieldStats = ShieldData;
            }


            if (data.m_shared.m_attackStatusEffect != null)
                ItemData.Attack_status_effect = data.m_shared.m_attackStatusEffect?.name;

            ItemData.Attack_status_effect_chance = data.m_shared.m_attackStatusEffectChance;

            if (data.m_shared.m_spawnOnHit != null)
                ItemData.spawn_on_hit = data.m_shared.m_spawnOnHit?.name;

            if (data.m_shared.m_spawnOnHitTerrain != null)
                ItemData.spawn_on_terrain_hit = data.m_shared.m_spawnOnHitTerrain?.name;

            ItemData.ConsumableStatusEffect = data.m_shared.m_consumeStatusEffect?.name ?? null;
            ItemData.AppendToolTip = data.m_shared.m_appendToolTip?.name?.ToString() ?? null;

            ItemData.snapshotOnMaterialChange = true;
            //ItemData.snapshotVisualRotation = Quaternion.Euler(90, 30, 30);
            //ItemData.snapshotRotation = new Vector3(33, 22, 44);

            return ItemData;

        }

        internal CreatureData GetCreature(string name)
        {
            GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
            CreatureData creatureData = new CreatureData();

            foreach (GameObject obj in array)
            {
                if (obj.name == (name))
                {
                    creatureData.name = obj.name;
                    creatureData.mob_display_name = obj.GetComponent<Humanoid>().m_name;
                    //creatureData.faction = obj.GetComponent<Humanoid>().m_faction;
                    return creatureData;
                }
            }
            return null;
        }

        internal bool GetAllCreature()
        {
            Humanoid[] array = Resources.FindObjectsOfTypeAll<Humanoid>();
            GameObject cre = null;
            var serializer = new SerializerBuilder().WithNewLine("\n")
                .Build();

            foreach (var obj in array)
            {

                CreatureData creatureData = new CreatureData();
                string Name = obj.name;


                creatureData.name = obj.name;
                creatureData.mob_display_name = obj.GetComponent<Humanoid>().m_name;
                //creatureData.faction = obj.GetComponent<Humanoid>().m_faction;

                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathBulkYMLCreatures, "Creature_" + Name + ".yml"), serializer.Serialize(creatureData));
          
            }
            return true;
        }



        internal PickableData GetPickable(string name, Pickable[] array)
        {
            
            PickableData picData = new PickableData();

            try
            {
                foreach (var obj in array)
                {
                    if (obj.name == (name))
                    {
                        picData.name = obj.name;
                        picData.itemPrefab = obj.m_itemPrefab.name;
                        picData.amount = obj.m_amount;
                        // picData.minAmountScaled = obj.m_minAmountScaled;
                        picData.overrideName = obj.m_overrideName;
                        picData.respawnTimer = obj.m_respawnTimeMinutes;
                        picData.spawnOffset = obj.m_spawnOffset;
                        if (obj.m_hideWhenPicked)
                            picData.hiddenChildWhenPicked = obj.m_hideWhenPicked.name;
                        //picData.enable = obj.enabled;
                        if (obj.TryGetComponent<Destructible>(out var yolo))
                            picData.ifHasHealth = yolo.m_health;


                        if (obj?.m_extraDrops != null && obj?.m_extraDrops.m_drops.Count() > 0)
                        {
                            ExtraDrops ExtraDrops = new ExtraDrops();
                            ExtraDrops.dropChance = obj.m_extraDrops.m_dropChance;
                            ExtraDrops.dropMin = obj.m_extraDrops.m_dropMin;
                            ExtraDrops.dropMax = obj.m_extraDrops.m_dropMax;
                            ExtraDrops.dropOneOfEach = obj.m_extraDrops.m_oneOfEach;
                            List<string> extradropspre = new List<string>();
                            foreach (var d in obj.m_extraDrops.m_drops)
                            {
                                extradropspre.Add(d.m_item.name);
                            }

                            ExtraDrops.drops = extradropspre;
                            picData.extraDrops = ExtraDrops;
                        }


                        return picData;
                    }
                }
            } catch { WMRecipeCust.WLog.LogWarning("An Error happened with " + picData.name); }
            return null;
        }
        internal TreeBaseData GetTreeBase(string name, TreeBase[] array)
        {
            TreeBaseData picData = new TreeBaseData();

            foreach (var obj in array)
            {
                if (obj.name == (name))
                {
                    picData.name = obj.name;
                    picData.treeHealth = obj.m_health;
                    picData.minToolTier = obj.m_minToolTier;

                    return picData;
                }
            }
            return null;
        }

        internal bool GetAllPickables()
        {
            var tod = ObjectDB.instance;
            var serializer = new SerializerBuilder().WithNewLine("\n")
                                        .Build();

            Pickable[] array = Resources.FindObjectsOfTypeAll<Pickable>();
            try
            {
                foreach (var obj in array)
                {
                    if (obj.name.Contains("Clone"))
                        continue;
                    PickableData picData = new PickableData();
                    picData.name = obj?.name;
                    WMRecipeCust.WLog.LogInfo("Saving " + obj?.name);
                    picData.itemPrefab = obj?.m_itemPrefab?.name;
                    picData.amount = obj?.m_amount;
                    //picData.minAmountScaled = obj.m_minAmountScaled;
                    picData.overrideName = obj?.m_overrideName;
                    picData.respawnTimer = obj?.m_respawnTimeMinutes;
                    picData.spawnOffset = obj?.m_spawnOffset;
                    if (obj?.m_hideWhenPicked)
                        picData.hiddenChildWhenPicked = obj?.m_hideWhenPicked?.name;
                    // picData.enable = obj.enabled;
                    if (obj.TryGetComponent<Destructible>(out var yolo))
                        picData.ifHasHealth = yolo?.m_health;

                    if (obj?.m_extraDrops != null && obj?.m_extraDrops.m_drops.Count() > 0)
                    {
                        ExtraDrops ExtraDrops = new ExtraDrops();
                        ExtraDrops.dropChance = obj.m_extraDrops.m_dropChance;
                        ExtraDrops.dropMin = obj.m_extraDrops.m_dropMin;
                        ExtraDrops.dropMax = obj.m_extraDrops.m_dropMax;
                        ExtraDrops.dropOneOfEach = obj.m_extraDrops.m_oneOfEach;
                        List<string> extradropspre = new List<string>();
                        foreach (var d in obj.m_extraDrops.m_drops)
                        {
                            extradropspre.Add(d.m_item.name);
                        }

                        ExtraDrops.drops = extradropspre;
                        picData.extraDrops = ExtraDrops;
                    }


                    File.WriteAllText(Path.Combine(WMRecipeCust.assetPathBulkYMLPickables, "Pickable_" + picData.name + ".yml"), serializer.Serialize(picData));
                }

                TreeBase[] array2 = Resources.FindObjectsOfTypeAll<TreeBase>();
                foreach (var obj2 in array2)
                {
                    if (obj2.name.Contains("Clone"))
                        continue;

                    TreeBaseData picData = new TreeBaseData();
                    WMRecipeCust.WLog.LogInfo("Saving " + obj2.name);
                    picData.name = obj2.name;
                    picData.treeHealth = obj2.m_health;
                    picData.minToolTier = obj2.m_minToolTier;

                    
                    File.WriteAllText(Path.Combine(WMRecipeCust.assetPathBulkYMLPickables, "Treebase_" + picData.name + ".yml"), serializer.Serialize(picData));
                }
            }
            catch (Exception e) { WMRecipeCust.WLog.LogWarning("An Error happened with Getallpickables " + e); }

            return true;
        }


    }
}
