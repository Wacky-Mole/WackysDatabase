using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using wackydatabase.Datas;
using wackydatabase.Util;
using wackydatabase.Armor;
using System.Runtime.CompilerServices;
using System.IO;
using RainbowTrollArmor;
using ItemManager;
using static Attack;
using System.Xml.Schema;
using static ItemSets;
using System.Security.Policy;
using wackydatabase.OBJimporter;
using System.Linq.Expressions;
using static ClutterSystem;
using static EffectList;
using System.Diagnostics.Eventing.Reader;

namespace wackydatabase.SetData
{

    [HarmonyPatch(typeof(Player), "OnSpawned")]
    static class OverrideItemMangerOrVanil
    {
        static void Postfix()
        {
            foreach (var pies in SetData.DisabledPieceandHam)
            {
                if (pies.Value.TryGetComponent<PieceTable>(out var table))
                {
                    WMRecipeCust.Dbgl($"Forcing PieceManger or Vanilla to Disable Piece {pies.Key}");
                    table.m_pieces.Remove(pies.Key);
                }
                else
                {
                    if (pies.Value.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Contains(pies.Key))
                    {
                        
                        pies.Value.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(pies.Key);
                    }
                }
            }
        }
    }

     public class SetData
    {

        public static Component[] renderfinder;
        internal static Renderer[] renderfinder2;
        internal static Dictionary<GameObject , GameObject> DisabledPieceandHam = new Dictionary<GameObject , GameObject>();


        #region Effects
        internal static void SetStatusData(StatusData data, ObjectDB Instant)
        {

            var name = data.Name;
            var go = Instant.GetStatusEffect(name.GetStableHashCode());
            if (go == null) {
                // create new
                go = Instant.GetStatusEffect("SetEffect_TrollArmor".GetStableHashCode());// cloned
                //WMRecipeCust.WLog.LogDebug($"Item CLONE DATA in SetItemData for {tempname} from cache ");
                WMRecipeCust.ClonedE.Add(name);
                Transform RootT = WMRecipeCust.Root.transform; // Root set to inactive to perserve components. 
                StatusEffect newStatus = WMRecipeCust.Instantiate(go, RootT, false);
                newStatus.name = name;
                ObjectDB.instance.m_StatusEffects.Add(newStatus);
                go = Instant.GetStatusEffect(name.GetStableHashCode());

            }
            go.m_name = data.Status_m_name ?? go.m_name;
            go.m_category = data.Category ?? go.m_category;
            if (!DataHelpers.ECheck(data.CustomIcon))
            {
                var pathI = Path.Combine(WMRecipeCust.assetPathIcons, data.CustomIcon);
                var nullcheck = File.ReadAllBytes(pathI);
                if (nullcheck != null)
                {
                    try
                    {
                        var Spri = SpriteTools.LoadNewSprite(pathI);
                        go.m_icon = Spri;
                    }
                    catch { WMRecipeCust.WLog.LogInfo("customIcon failed"); }
                }
                else
                {
                    WMRecipeCust.WLog.LogInfo($"No Img with the name {data.CustomIcon} in Icon Folder - ");
                }
            }
            go.m_flashIcon = data.FlashIcon ?? go.m_flashIcon;
            go.m_cooldownIcon = data.CooldownIcon ?? go.m_cooldownIcon;
            go.m_tooltip = data.Tooltip ?? go.m_tooltip;
            go.m_attributes = data.Attributes ?? go.m_attributes;
            go.m_startMessageType = data.StartMessageLoc ?? go.m_startMessageType;
            go.m_startMessage = data.StartMessage ?? go.m_startMessage;
            go.m_stopMessageType = data.StopMessageLoc ?? go.m_stopMessageType;
            go.m_stopMessage = data.StopMessage ?? go.m_stopMessage;
            go.m_repeatMessageType = data.RepeatMessageLoc ?? go.m_repeatMessageType;
            go.m_repeatMessage = data.RepeatMessage ?? go.m_repeatMessage;
            go.m_ttl = data.TimeToLive ?? go.m_ttl;
            if (data.StartEffect_ != null)
                go.m_startEffects = FindEffect(go.m_startEffects, data.StartEffect_);

            if (data.StopEffect_ != null)
                go.m_stopEffects = FindEffect(go.m_stopEffects, data.StopEffect_);
            
            go.m_cooldown = data.Cooldown ?? go.m_cooldown;
            go.m_activationAnimation = data.ActivationAnimation ?? go.m_activationAnimation;


            Type type = go.GetType();

            Functions.setValue(type, go, "m_tickInterval", data.SeData.m_tickInterval);
            Functions.setValue(type, go, "m_healthPerTickMinHealthPercentage", data.SeData.m_healthPerTickMinHealthPercentage);
            Functions.setValue(type, go, "m_healthPerTick", data.SeData.m_healthPerTick);

            Functions.setValue(type, go, "m_healthOverTime", data.SeData.m_healthOverTime);
            Functions.setValue(type, go, "m_healthOverTimeDuration", data.SeData.m_healthOverTimeDuration);
            Functions.setValue(type, go, "m_healthOverTimeInterval", data.SeData.m_healthOverTimeInterval);

            Functions.setValue(type, go, "m_staminaOverTime", data.SeData.m_staminaOverTime);
            Functions.setValue(type, go, "m_staminaOverTimeDuration", data.SeData.m_staminaOverTimeDuration);
            Functions.setValue(type, go, "m_staminaDrainPerSec", data.SeData.m_staminaDrainPerSec);
            Functions.setValue(type, go, "m_runStaminaDrainModifier", data.SeData.m_runStaminaDrainModifier);
            Functions.setValue(type, go, "m_jumpStaminaUseModifier", data.SeData.m_jumpStaminaUseModifier);

            Functions.setValue(type, go, "m_eitrOverTime", data.SeData.m_eitrOverTime);
            Functions.setValue(type, go, "m_eitrOverTimeDuration", data.SeData.m_eitrOverTimeDuration);
            Functions.setValue(type, go, "m_healthRegenMultiplier", data.SeData.m_healthRegenMultiplier);
            Functions.setValue(type, go, "m_staminaRegenMultiplier", data.SeData.m_staminaRegenMultiplier);
            Functions.setValue(type, go, "m_eitrRegenMultiplier", data.SeData.m_eitrRegenMultiplier);

            Functions.setValue(type, go, "m_raiseSkill", null, null, null, null, data.SeData.m_raiseSkill);
            Functions.setValue(type, go, "m_raiseSkillModifier", data.SeData.m_raiseSkillModifier);

            Functions.setValue(type, go, "m_skillLevel", null, null, null, null, data.SeData.m_skillLevel);
            Functions.setValue(type, go, "m_skillLevelModifier", data.SeData.m_skillLevelModifier);
            Functions.setValue(type, go, "m_skillLevel2", null, null, null, null, data.SeData.m_skillLevel2);
            Functions.setValue(type, go, "m_skillLevelModifier2", data.SeData.m_skillLevelModifier2);


            Functions.setValue(type, go, "m_mods", null, null, null, data.SeData.m_mods);

            Functions.setValue(type, go, "m_modifyAttackSkill", null, null, null, null, data.SeData.m_modifyAttackSkill);
            Functions.setValue(type, go, "m_damageModifier", data.SeData.m_damageModifier);

            Functions.setValue(type, go, "m_noiseModifier", data.SeData.m_noiseModifier);
            Functions.setValue(type, go, "m_stealthModifier", data.SeData.m_stealthModifier);

            Functions.setValue(type, go, "m_addMaxCarryWeight", data.SeData.m_addMaxCarryWeight);

            Functions.setValue(type, go, "m_speedModifier", data.SeData.m_speedModifier);

            Functions.setValue(type, go, "m_maxMaxFallSpeed", data.SeData.m_maxMaxFallSpeed);
            Functions.setValue(type, go, "m_fallDamageModifier", data.SeData.m_fallDamageModifier);
            Functions.setValue(type, go, "m_tickTimer", data.SeData.m_tickTimer);
            Functions.setValue(type, go, "m_healthOverTimeTimer", data.SeData.m_healthOverTimeTimer);
            Functions.setValue(type, go, "m_healthOverTimeTicks", data.SeData.m_healthOverTimeTicks);
            Functions.setValue(type, go, "m_healthOverTimeTickHP", data.SeData.m_healthOverTimeTickHP);
        }



        #endregion





        #region Recipe
        internal static void SetRecipeData(RecipeData data, ObjectDB Instant)
        {
            bool skip = false;
            foreach (string citem in WMRecipeCust.ClonedR)
            {
                //Dbgl($"Recipe clone check {citem} against {data.name}");
                if (citem == data.name)
                    skip = true;
            }

            string tempname = data.name;
            string searchname = data.name;
            if (!string.IsNullOrEmpty(data.clonePrefabName)) // both skip and
            {
                if (data.clonePrefabName == "NO")
                {
                    data.clonePrefabName = null;
                }
                else
                {
                    searchname = data.clonePrefabName;
                }
            }

            GameObject go = DataHelpers.CheckforSpecialObjects(searchname);// check for special cases
            if (go == null)
                go = Instant.GetItemPrefab(searchname);


            Recipe ActualR = null;
            if (go == null)
            {
                foreach (Recipe recipes in Instant.m_recipes)
                {
                    if (!(recipes.m_item == null) && recipes.name == searchname)
                    {
                        //WMRecipeCust.Dbgl($"An actual {data.name} has been found!-- Only modification allowed");
                        ActualR = recipes;
                        break;
                    }
                }
            }

            if (go == null && ActualR == null)
            {
                WMRecipeCust.Dbgl(" null " + searchname);
                return;
            }

            if (go != null)
            {
                if (go.GetComponent<ItemDrop>() == null)
                {
                    WMRecipeCust.WLog.LogWarning($"Item recipe data for {searchname} not found!");
                    return;
                } // it is a prefab and it is an item.
            }


            if (!data.disabled ?? true)
            {
                Recipe RecipeR = null;

                if (!string.IsNullOrEmpty(data.clonePrefabName) && !skip)// only first time clone
                {

                    WMRecipeCust.Dbgl("Setting Cloned Recipe for " + tempname);
                    RecipeR = ScriptableObject.CreateInstance<Recipe>();
                    WMRecipeCust.ClonedR.Add(tempname);
                }
                else if (skip)
                {
                    WMRecipeCust.Dbgl("ReSetting Cloned Recipe for " + tempname);
                    for (int i = Instant.m_recipes.Count - 1; i >= 0; i--)
                    {
                        if (Instant.m_recipes[i].name == tempname)
                        {
                            RecipeR = Instant.m_recipes[i];
                            RecipeR.m_enabled = true;
                            break;
                        }
                    }
                } else if (ActualR != null)
                {
                    WMRecipeCust.Dbgl($"An actual Recipe for {searchname}");
                    RecipeR = ActualR;
                    RecipeR.m_enabled = true;
                }
                else // in game recipe 
                {
                    WMRecipeCust.Dbgl("Setting Recipe for " + tempname);
                    for (int i = Instant.m_recipes.Count - 1; i >= 0; i--)
                    {
                        if (Instant.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                        {
                            RecipeR = Instant.m_recipes[i];
                            RecipeR.m_enabled = true;
                            break;
                        }
                    }
                }


                if (RecipeR == null)
                {
                    WMRecipeCust.Dbgl("Recipe failed inside of " + tempname);
                    return;
                }

                if (ActualR == null)
                    RecipeR.m_item = go.GetComponent<ItemDrop>();

                if (data.craftingStation != null)
                { // null is don't set, '' is only by hand
                    RecipeR.m_craftingStation = DataHelpers.GetCraftingStation(data.craftingStation);
                }
                if (data.repairStation != null)
                {
                    RecipeR.m_repairStation = DataHelpers.GetCraftingStation(data.repairStation);
                }
                RecipeR.m_minStationLevel = data.minStationLevel ?? RecipeR.m_minStationLevel;
                RecipeR.m_amount = data.amount ?? RecipeR.m_amount;
                RecipeR.name = tempname;

                if (data.maxStationLevelCap != null)
                {
                    if (!WMRecipeCust.RecipeMaxStationLvl.ContainsKey(RecipeR.m_item.name))
                    {
                        WMRecipeCust.RecipeMaxStationLvl.Add(RecipeR.m_item.name, data.maxStationLevelCap ?? -1); // -1 no cap
                    }
                }

                List<Piece.Requirement> reqs = new List<Piece.Requirement>();

                RecipeR.m_requireOnlyOneIngredient = data.requireOnlyOneIngredient ?? RecipeR.m_requireOnlyOneIngredient;
                bool alreadyadded = true;
                if (!WMRecipeCust.QualityRecipeReq.ContainsKey(tempname))
                {
                    alreadyadded = false;
                    WMRecipeCust.QualityRecipeReq.Add(tempname, new Dictionary<ItemDrop, int>());
                }
                foreach (string req in data.reqs)
                {
                    if (!string.IsNullOrEmpty(req))
                    {
                        string[] array = req.Split(':'); // safer vewrsion // could add a 5th col for Quality, item must be such and such quality would require a small patch
                        string itemname = array[0];  // and a three tier directonary
                        if (Instant.GetItemPrefab(itemname))
                        {
                            int amount = ((array.Length < 2) ? 1 : int.Parse(array[1]));
                            int amountPerLevel = ((array.Length < 3) ? 1 : int.Parse(array[2]));
                            bool recover = array.Length != 4 || bool.Parse(array[3].ToLower());
                            int quality = ((array.Length < 5) ? 1 : int.Parse(array[4]));
                            Piece.Requirement item = new Piece.Requirement
                            {
                                m_amount = amount,
                                m_recover = recover,
                                m_resItem = Instant.GetItemPrefab(itemname).GetComponent<ItemDrop>(),
                                m_amountPerLevel = amountPerLevel
                            };
                            reqs.Add(item);

                            if (!alreadyadded)
                                WMRecipeCust.QualityRecipeReq[tempname].Add(Instant.GetItemPrefab(itemname).GetComponent<ItemDrop>(), quality);
                        }
                    }
                }// foreach



                int index = 0;
                RecipeR.m_resources = reqs.ToArray();

                if (!string.IsNullOrEmpty(data.clonePrefabName) && !skip) // only first time clone
                {
                    for (int i = Instant.m_recipes.Count - 1; i >= 0; i--)
                    {
                        if (Instant.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                        {
                            index = i++; // some extra resourses, but I think it's worth it
                            break;
                        }
                    }
                    Instant.m_recipes.Insert(index, RecipeR);
                }

                return;
            }
            else // disabled
            {
                if (skip) // has been set before
                {
                    for (int i = Instant.m_recipes.Count - 1; i >= 0; i--)
                    {
                        if (Instant.m_recipes[i].name == tempname)
                        {
                            Recipe clonerecipe = ObjectDB.instance.m_recipes[i];
                            clonerecipe.m_enabled = false;
                            WMRecipeCust.Dbgl("Cloned Recipe that was enabled before is disabled for " + tempname);
                            break;
                        }
                    }

                }
                else if (!skip && !string.IsNullOrEmpty(data.clonePrefabName))
                { // never added so need to disable
                    WMRecipeCust.Dbgl("Cloned Recipe is disabled for " + tempname);
                }
                else if (ActualR != null)
                {
                    ActualR.m_enabled = false;
                    WMRecipeCust.Dbgl("Actual Recipe is disabled for " + ActualR.name);
                }
                else // normal in game
                {
                    for (int i = Instant.m_recipes.Count - 1; i >= 0; i--)
                    {
                        if (Instant.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                        {
                            Instant.m_recipes[i].m_enabled = false;
                            WMRecipeCust.Dbgl("Recipe is disabled for " + tempname);


                        }
                    }
                }
            }
        }




        #endregion





        #region Piece



        internal static void SetPieceRecipeData(PieceData data, ObjectDB Instant, GameObject[] AllObjects = null, bool cloneonly = false)
        {
            bool skip = false;
            foreach (var citem in WMRecipeCust.ClonedP)
            {
                if (citem == data.name)
                    skip = true;
            }

            string tempname = data.name;
            if (!string.IsNullOrEmpty(data.clonePrefabName) && !skip)
            {
                data.name = data.clonePrefabName;
            }

            Piece piece = null;
            GameObject go = DataHelpers.CheckforSpecialObjects(data.name);// check for special cases
            if (go == null)
            {
                go = DataHelpers.GetPieces(Instant).Find(g => Utils.GetPrefabName(g) == data.name); // vanilla search  replace with FindPieceObjectName(data.name) in the future
                if (go == null)
                {
                    go = DataHelpers.GetModdedPieces(data.name); // known modded Hammer search
                    if (go == null)
                    {
                        WMRecipeCust.WLog.LogWarning($"Piece {data.name} not found! 3 layer search");
                        return;
                    }
                    else // 2nd layer
                        WMRecipeCust.Dbgl($"Piece {data.name} from known hammer {WMRecipeCust.selectedPiecehammer}"); // selected piecehammer is set in GetModdedPieces!
                }
            }
            piece = go.GetComponent<Piece>();
            if (piece == null) // final check
            {
                WMRecipeCust.Dbgl("Piece data not found!");
                return;
            }
            if (!string.IsNullOrEmpty(data.clonePrefabName) && !skip) // object is a clone do clonethings
            {
                if (WMRecipeCust.BlacklistClone.Contains(data.clonePrefabName))
                {
                    WMRecipeCust.Dbgl($"Can not clone {data.clonePrefabName} ");
                    return;
                }

                WMRecipeCust.Dbgl($"Item CLONE DATA in SetPiece for {tempname} ");
                Transform RootT = WMRecipeCust.Root.transform; // Root set to inactive to perserve components. 
                GameObject newItem = WMRecipeCust.Instantiate(go, RootT, false);
                Piece NewItemComp = newItem.GetComponent<Piece>();

                WMRecipeCust.ClonedP.Add(tempname); // check against
                newItem.name = tempname; // resets the orginal name- needs to be unquie
                NewItemComp.name = tempname; // ingame name

                var hash = newItem.name.GetStableHashCode();
                ZNetScene znet = ZNetScene.instance;
                if (znet)
                {
                    string name = newItem.name;
                    if (znet.m_namedPrefabs.ContainsKey(hash))
                    {
                        WMRecipeCust.Dbgl($"Prefab {name} already in ZNetScene");
                    }
                    else
                    {
                        if (newItem.GetComponent<ZNetView>() != null)
                        {
                            znet.m_prefabs.Add(newItem);
                        }
                        else
                        {
                            znet.m_nonNetViewPrefabs.Add(newItem);
                        }
                        znet.m_namedPrefabs.Add(hash, newItem);
                        WMRecipeCust.Dbgl($"Added prefab {name}");
                    }
                }
                if (data.craftingStation != null)
                {
                    CraftingStation craft2 = DataHelpers.GetCraftingStation(data.craftingStation);
                    newItem.GetComponent<Piece>().m_craftingStation = craft2; // sets crafing item place
                }

                GameObject piecehammer = Instant.GetItemPrefab(data.piecehammer); // must be set
                skip = true;
                if (piecehammer == null)
                {
                    if (data.piecehammer == "Hoe" || data.piecehammer == "_CultivatorPieceTable") // Hoe and Cultivator
                    {
                        if (!string.IsNullOrEmpty(data.piecehammerCategory))
                        {
                            try
                            { NewItemComp.m_category = (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory); }
                            catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                        }
                       // piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(newItem); // if piecehammer is the actual item and not the PieceTable
                        WMRecipeCust.selectedPiecehammer.m_pieces.Add(newItem);
                    }
                    else if (WMRecipeCust.selectedPiecehammer == null)
                    {
                        WMRecipeCust.Dbgl($"piecehammer named {data.piecehammer} will not be used because the Item prefab was not found and it is not a PieceTable, so setting the piece to Hammer in Misc");
                        piecehammer = Instant.GetItemPrefab("Hammer");

                        NewItemComp.m_category = Piece.PieceCategory.Misc; // set the category
                        piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(newItem);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(data.piecehammerCategory))
                        {
           
                            try
                            { PieceManager.BuildPiece.BuildTableConfigChangedWacky(NewItemComp, data.piecehammerCategory); }
                            catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                        }
                        WMRecipeCust.selectedPiecehammer.m_pieces.Add(newItem); // adding item to PiceTable
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(data.piecehammerCategory))
                    {
                        try
                        { PieceManager.BuildPiece.BuildTableConfigChangedWacky(NewItemComp, data.piecehammerCategory); }
                        catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                    }
                    piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(newItem); // if piecehammer is the actual item and not the PieceTable
                }

                data.name = tempname; // putting back name
                go = DataHelpers.FindPieceObjectName(data.name); // this needs to call to newItem for modifcation otherwise it modifies orginial.
                if (go == null)// just verifying
                {
                    WMRecipeCust.Dbgl($"Item {data.name} not found in SetPiece! after clone");
                    return;
                }
                if (go.GetComponent<Piece>() == null)
                {
                    WMRecipeCust.Dbgl($"Item data for {data.name} not found! after clone");
                    return;
                }
                go.GetComponent<Piece>().m_name = tempname; // set pieces name
            } // end clone 1st pass



            if (!string.IsNullOrEmpty(data.material) || !string.IsNullOrEmpty(data.damagedMaterial)) // allows changing of any piece
            {
                WMRecipeCust.Dbgl($"Material name searching for {data.material} for piece {data.name}"); // need to take in account worn at %50
                try
                {

                    renderfinder = go.GetComponentsInChildren<Renderer>();
                    renderfinder2 = go.GetComponentsInChildren<Renderer>(true); // include inactives
                    if (data.material.Contains("same_mat") || data.material.Contains("no_wear"))
                    {
                        WMRecipeCust.Dbgl($"No Wear set for {data.name}");
                        Material samematerial = null;
                        foreach (Renderer renderitem in renderfinder) // get for piece at full heatlh
                        {
                            if (renderitem.receiveShadows)
                            {
                                samematerial = renderitem.material;
                                break;
                            }
                        }
                        foreach (Renderer renderitem in renderfinder2) // set for Pieces @ 50%
                        {
                            if (renderitem.receiveShadows)
                                renderitem.material = samematerial;
                        }
                    }
                    else
                    {
                        if (data.material.Contains(','))
                        {
                            string[] materialstr = data.material.Split(',');
                            Material mat = WMRecipeCust.originalMaterials[materialstr[0]];
                            Material part = WMRecipeCust.originalMaterials[materialstr[1]];

                            foreach (Renderer renderitem in renderfinder2) // for Pieces @ 50%
                            {
                                if (renderitem.receiveShadows)
                                    renderitem.material = part;
                            }

                            foreach (Renderer renderitem in renderfinder) // set after all of the piece for %50
                            {
                                if (renderitem.receiveShadows)
                                    renderitem.material = mat;
                            }

                        }
                        else
                        {

                            Material mat = WMRecipeCust.originalMaterials[data.material];
                            foreach (Renderer renderitem in renderfinder2)
                            {
                                if (renderitem.receiveShadows)
                                {
                                    renderitem.material = mat;
                                }
                            }

                        }
                    }
                }
                catch { WMRecipeCust.WLog.LogWarning("Material was not found or was not set correctly"); }


            } // mats

            bool usecustom = false;
            if (!DataHelpers.ECheck(data.customIcon))
            {
                var pathI = Path.Combine(WMRecipeCust.assetPathIcons, data.customIcon);
                if (File.Exists(pathI))
                {
                    var nullcheck = File.ReadAllBytes(pathI);
                    if (nullcheck != null)
                    {
                        try
                        {

                            var Spri = SpriteTools.LoadNewSprite(pathI);
                            go.GetComponent<Piece>().m_icon = Spri;
                            usecustom = true;

                        }
                        catch { WMRecipeCust.WLog.LogInfo("customIcon failed"); }
                    }
                    else
                    {
                        WMRecipeCust.WLog.LogInfo($"No Img with the name {data.customIcon} in Icon Folder - ");
                    }
                }
                else
                {
                    WMRecipeCust.WLog.LogInfo($"No Img with the name {data.customIcon} in Icon Folder - ");
                }
            }


            if (!DataHelpers.ECheck(data.material) && !usecustom)
            {

                try
                {
                  //  Functions.SnapshotPiece(go); // snapshot go
                }
                catch { WMRecipeCust.WLog.LogInfo("Piece snapshot  failed"); }
            }


            if (data.craftingStation != null)
            {
                CraftingStation craft = DataHelpers.GetCraftingStation(data.craftingStation);
                go.GetComponent<Piece>().m_craftingStation = craft;
            }

            if (!skip)
            { // Cats // if just added cloned doesn't need to be category changed.
                Piece ItemComp = go.GetComponent<Piece>();

                GameObject piecehammer = Instant.GetItemPrefab(data.piecehammer); // need to check to make sure hammer didn't change, if it did then needs to disable piece in certain cat before moving to next
                                                                                  // Can't check the hammer easily, so checking the PieceCategory, hopefully someone doesn't make two Misc
                if (data.piecehammerCategory != null && data.piecehammer != null) // check that category and hammer is actually set
                {
                    if (data.piecehammer == "Hoe" || data.piecehammer == "_CultivatorPieceTable") // Hoe and Cultivator
                    {
                        if (!string.IsNullOrEmpty(data.piecehammerCategory))
                        {
                            try
                            { ItemComp.m_category = (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory); }
                            catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                        }
                        piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(go); // if piecehammer is the actual item and not the PieceTable
                    }
                    else if (ItemComp.m_category != PieceManager.PiecePrefabManager.GetCategory(data.piecehammerCategory))// (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory))
                    { // now disable old 
                        WMRecipeCust.Dbgl($"Category change has been detected for {data.name}, disabling old piece and setting new piece location");
                        if (piecehammer == null)
                        {
                            if (WMRecipeCust.selectedPiecehammer == null) // selectedPiecehammer is set in 
                            {
                                piecehammer = ObjectDB.instance.GetItemPrefab("Hammer"); // default add // default delete
                                piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go);
                            }
                            else WMRecipeCust.selectedPiecehammer.m_pieces.Remove(go); // found in modded hammers
                        }
                        else piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go); // if piecehammer is the actual item and not the PieceTable

                        // Now add to new Cat and hammer
                        if (piecehammer == null)
                        {
                            if (WMRecipeCust.selectedPiecehammer == null)
                            {
                                WMRecipeCust.Dbgl($"piecehammer named {data.piecehammer} will not be used because the Item prefab was not found and it is not a PieceTable, so setting the piece to Hammer in Misc");
                                piecehammer = Instant.GetItemPrefab("Hammer");

                                ItemComp.m_category = Piece.PieceCategory.Misc; // set the category
                                piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(go);
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(data.piecehammerCategory))
                                {
                                    try
                                    { PieceManager.BuildPiece.BuildTableConfigChangedWacky(ItemComp, data.piecehammerCategory); }
                                    catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                                }
                                WMRecipeCust.selectedPiecehammer.m_pieces.Add(go); // adding item to PiceTable
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(data.piecehammerCategory))
                            {
                                try
                                { PieceManager.BuildPiece.BuildTableConfigChangedWacky(ItemComp, data.piecehammerCategory); }
                                catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                            }
                            piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(go); // if piecehammer is the actual item and not the PieceTable
                        }
                    }
                }
            } //end Cat
            if (data.adminonly ?? false)
            {
                if (WMRecipeCust.Admin)
                {
                    WMRecipeCust.Dbgl($"{data.name} is set for Adminonly, and you are admin, enjoy this exclusive Piece");
                }
                else
                {
                    data.disabled = true;
                    WMRecipeCust.Dbgl($"{data.name} is set for Adminonly, you are not an admin");
                }
            }

            if (data.disabled ?? false)
            {
                GameObject piecehammer = Instant.GetItemPrefab(data.piecehammer);
                if (piecehammer == null)
                    piecehammer = WMRecipeCust.selectedPiecehammer.gameObject;
                WMRecipeCust.Dbgl($"Disabling Piece {data.name} with hammer {piecehammer}");

                if (piecehammer.TryGetComponent<PieceTable>(out var table))
                {
                    table.m_pieces.Remove(go);

                    if (!DisabledPieceandHam.ContainsKey(go))
                        DisabledPieceandHam.Add(go, piecehammer);
                }
                else
                {

                    if (piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                    {
                        WMRecipeCust.Dbgl($"removing from {piecehammer.name} Piece {data.name}");
                        piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go);

                        if (!DisabledPieceandHam.ContainsKey(go))
                            DisabledPieceandHam.Add(go, piecehammer);
                    }
                }
            }
            else
            {
                GameObject piecehammer = Instant.GetItemPrefab(data.piecehammer);
                if (piecehammer == null)
                    piecehammer = WMRecipeCust.selectedPiecehammer.gameObject;

                go.GetComponent<Piece>().m_enabled = true;

                if (piecehammer == null || data.piecehammer == "_CultivatorPieceTable") { 
                
                } // no change?
                else
                {
                    if (DisabledPieceandHam.ContainsKey(go))
                    {
                        if (!piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                        {
                            //WMRecipeCust.Dbgl($"Force adding to selectedPiecehammer Piece {data.name}");
                            piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(go);
                        }

                        DisabledPieceandHam.Remove(go);
                    }
                }
            }
            WMRecipeCust.Dbgl("Setting Piece data for " + data.name);

            if (!string.IsNullOrEmpty(data.m_name))
            {
                go.GetComponent<Piece>().m_name = data.m_name;
                go.GetComponent<Piece>().m_description = data.m_description ?? go.GetComponent<Piece>().m_description;
            }

            bool CStationAdded = false;
            if (!string.IsNullOrEmpty(data.clonePrefabName) && go.TryGetComponent<CraftingStation>(out var station3))
            {
                if (!WMRecipeCust.NewCraftingStations.Contains(station3))
                {
                    WMRecipeCust.NewCraftingStations.Add(go.GetComponent<CraftingStation>()); // keeping track of them is hard
                }
                go.GetComponent<CraftingStation>().name = data.name; // must be set
                go.GetComponent<CraftingStation>().m_name = data.m_name ?? go.GetComponent<CraftingStation>().m_name;

                WMRecipeCust.Dbgl($"  new CraftingStation named {data.name} ");
            }



            if (data.minStationLevel > 1)
            {
                WMRecipeCust.pieceWithLvl.Add(go.name + "." + data.minStationLevel);
            }


            if (data.build != null)
            {
                List<Piece.Requirement> reqs = new List<Piece.Requirement>();
                foreach (string req in data.build)
                {
                    // WMRecipeCust.Dbgl(req);
                    string[] parts = req.Split(':');
                    reqs.Add(new Piece.Requirement() { m_resItem = Instant.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
                    // WMRecipeCust.Dbgl(reqs.Last().ToString() ) ;
                }
                go.GetComponent<Piece>().m_resources = reqs.ToArray();
            }
            var pi = go.GetComponent<Piece>();

            pi.m_name = data.m_name ?? pi.m_name;
            pi.m_description = data.m_description ?? pi.m_description;

            if (pi.gameObject.TryGetComponent<Door>(out Door wpoo))
                wpoo.m_name = pi.m_name;

            if (data.sizeMultiplier != 1 && data.sizeMultiplier != null)
            {
                Vector3 NewScale = new Vector3((float)data.sizeMultiplier, (float)data.sizeMultiplier, (float)data.sizeMultiplier);
                go.transform.localScale = NewScale;
            }

            pi.m_groundPiece = data.groundPiece ?? pi.m_groundPiece;
            pi.m_groundOnly = data.ground ?? pi.m_groundOnly;
            pi.m_waterPiece = data.waterPiece ?? pi.m_waterPiece;
            pi.m_noInWater = data.noInWater ?? pi.m_noInWater;
            pi.m_notOnFloor = data.notOnFloor ?? pi.m_notOnFloor;
            pi.m_onlyInTeleportArea = data.onlyinTeleportArea ?? pi.m_onlyInTeleportArea;
            pi.m_allowedInDungeons = data.allowedInDungeons ?? pi.m_allowedInDungeons;
            pi.m_canBeRemoved = data.canBeRemoved ?? pi.m_canBeRemoved;

            if (data.comfort != null)
            {
                pi.m_comfort = data.comfort.comfort ?? pi.m_comfort;
                pi.m_comfortGroup = data.comfort.comfortGroup ?? pi.m_comfortGroup;
                pi.m_comfortObject = data.comfort.comfortObject ?? pi.m_comfortObject;
            }

            if (data.wearNTearData != null)
            {
                go.TryGetComponent<WearNTear>(out var wear);
                wear.m_health = data.wearNTearData.health ?? wear.m_health;
                if (!string.IsNullOrEmpty(data.wearNTearData.damageModifiers.ToString()))
                {
                    wear.m_damages = data.wearNTearData.damageModifiers;
                }

                wear.m_noRoofWear = data.wearNTearData.noRoofWear ?? wear.m_noRoofWear;
                wear.m_noSupportWear = data.wearNTearData.noSupportWear ?? wear.m_noSupportWear;
                wear.m_supports = data.wearNTearData.supports ?? wear.m_supports;
                wear.m_triggerPrivateArea = data.wearNTearData.triggerPrivateArea ?? wear.m_triggerPrivateArea;
            }


            if (data.craftingStationData != null)
            {
                go.TryGetComponent<CraftingStation>(out var station);

                //station.name = data.craftingStationData.cStationName ?? station.m_name;
                station.m_discoverRange = data.craftingStationData.discoveryRange ?? station.m_discoverRange;
                station.m_rangeBuild = data.craftingStationData.buildRange ?? station.m_rangeBuild;
                station.m_craftRequireRoof = data.craftingStationData.craftRequiresRoof ?? station.m_craftRequireRoof;
                station.m_craftRequireFire = data.craftingStationData.craftRequiresFire ?? station.m_craftRequireFire;
                station.m_showBasicRecipies = data.craftingStationData.showBasicRecipes ?? station.m_showBasicRecipies;
                station.m_useDistance = data.craftingStationData.useDistance ?? station.m_useDistance;
                station.m_useAnimation = data.craftingStationData.useAnimation ?? station.m_useAnimation;


            }
            if (data.cSExtensionData != null)
            {
                go.TryGetComponent<StationExtension>(out var ex);

                //ex.m_craftingStation.name = data.cSExtensionData.MainCraftingStationName ?? ex.m_craftingStation.name;
                ex.m_craftingStation = DataHelpers.GetCraftingStation(data.cSExtensionData.MainCraftingStationName) ?? ex.m_craftingStation;
                ex.m_maxStationDistance = data.cSExtensionData.maxStationDistance ?? ex.m_maxStationDistance;
                ex.m_continousConnection = data.cSExtensionData.continousConnection ?? ex.m_continousConnection;
                ex.m_stack = data.cSExtensionData.stack ?? ex.m_stack;

            }

            if (data.contData != null)
            {
                go.TryGetComponent<Container>(out var cont);
                cont.m_autoDestroyEmpty = data.contData.AutoDestoryIfEmpty ?? cont.m_autoDestroyEmpty;
                cont.m_height = data.contData.Height ?? cont.m_height;
                cont.m_width = data.contData.Width ?? cont.m_width;

            }

            if (data.sapData != null)
            {
                go.TryGetComponent<SapCollector>(out var sap);
                sap.m_secPerUnit = data.sapData.secPerUnit ?? sap.m_secPerUnit;
                sap.m_maxLevel = data.sapData.maxLevel ?? sap.m_maxLevel;

                if (data.sapData.producedItem != null)
                {
                    sap.m_spawnItem = Instant.GetItemPrefab(data.sapData.producedItem).GetComponent<ItemDrop>();
                }
                if (data.sapData.connectedToWhat != null)
                {
                    foreach (var pie in AllObjects)
                    {
                        if (pie.GetComponent<Piece>() != null && pie.name == data.sapData.connectedToWhat)
                        {
                            sap.m_mustConnectTo = pie.GetComponent<ZNetView>();
                            break;
                        }
                    }
                }
                sap.m_extractText = data.sapData.extractText;
                sap.m_drainingText = data.sapData.drainingText;
                sap.m_drainingSlowText = data.sapData.drainingSlowText;
                sap.m_notConnectedText = data.sapData.notConnectedText;
                sap.m_fullText = data.sapData.fullText;

            }

            if (data.fermStationData  != null)
            {
                go.TryGetComponent<Fermenter>(out var ferm);

                ferm.m_fermentationDuration = data.fermStationData.fermDuration ?? ferm.m_fermentationDuration;

                if (data.fermStationData.fermConversion != null)
                {
                    ferm.m_conversion.Clear();
                    List<FermenterConversionList> list = new List<FermenterConversionList>();
                    foreach (var conv in data.fermStationData.fermConversion)
                    {
                        Fermenter.ItemConversion conversion = new Fermenter.ItemConversion();
                        if (conv != null)
                        {
                            if (conv.FromName != null)
                                conversion.m_from = Instant.GetItemPrefab(conv.FromName).GetComponent<ItemDrop>();

                            if (conv.ToName != null)
                                conversion.m_to = Instant.GetItemPrefab(conv.ToName).GetComponent<ItemDrop>();

                            conversion.m_producedItems = conv.Amount ?? conversion.m_producedItems;

                        }
                        ferm.m_conversion.Add(conversion);
                    }
                }
                

            }

            if (data.cookingStationData != null)
            {
                go.TryGetComponent<CookingStation>(out var cook);

                //cook.name = data.cookingStationData.stationName ?? cook.name;
                //cook.m_name = data.cookingStationData.displayName ?? cook.m_name;
                cook.m_addItemTooltip = data.cookingStationData.addItemTooltip ?? cook.m_addItemTooltip;

                if (data.cookingStationData.overcookedItem != null)
                {
                    cook.m_overCookedItem = Instant.GetItemPrefab(data.cookingStationData.overcookedItem).GetComponent<ItemDrop>();
                }
                if (data.cookingStationData.fuelItem != null)
                {
                    cook.m_fuelItem = Instant.GetItemPrefab(data.cookingStationData.fuelItem).GetComponent<ItemDrop>();
                }
                cook.m_requireFire = data.cookingStationData.requireFire ?? cook.m_requireFire;
                cook.m_maxFuel = data.cookingStationData.maxFuel ?? cook.m_maxFuel;
                cook.m_secPerFuel = data.cookingStationData.secPerFuel ?? cook.m_secPerFuel;

                if (data.cookingStationData.cookConversion != null)
                {
                    cook.m_conversion.Clear();
                    foreach (var list in data.cookingStationData.cookConversion)
                    {
                        CookingStation.ItemConversion paul = new CookingStation.ItemConversion();

                        //smelt.m_conversion[0].; // has to have 1 set before 
                        if (list != null)
                        {
                            if (list.FromName != null)
                            {
                                paul.m_from = Instant.GetItemPrefab(list.FromName).GetComponent<ItemDrop>();
                            }

                            if (list.ToName != null)
                            {
                                paul.m_to = Instant.GetItemPrefab(list.ToName).GetComponent<ItemDrop>();
                            }
                            paul.m_cookTime = list.CookTime ?? 10;// overwise 10 secs
                        }
                        cook.m_conversion.Add(paul);
                    }
                }
            }

            Type type = go.GetType();
            if (data.smelterData != null && go.TryGetComponent<Smelter>(out var smelt))
            {
                WMRecipeCust.WLog.LogInfo("       Setting Smelt");
                smelt.name = data.smelterData.smelterName ?? smelt.name;
                smelt.m_addOreTooltip = data.smelterData.addOreTooltip ?? smelt.m_addOreTooltip;
                smelt.m_emptyOreTooltip = data.smelterData.emptyOreTooltip ?? smelt.m_emptyOreTooltip;
                // smelt.m_addOreSwitch = data.smelterData.addOreSwitch ?? smelt.m_addOreSwitch;
                //smelt.m_addWoodSwitch = data.smelterData.addFuelSwitch ?? smelt.m_addWoodSwitch;
                // smelt.m_emptyOreSwitch = data.smelterData.emptyOreSwitch  ?? smelt?.m_emptyOreSwitch;

                if (data.smelterData.fuelItem != null)
                {
                    smelt.m_fuelItem = Instant.GetItemPrefab(data.smelterData.fuelItem.name).GetComponent<ItemDrop>();
                }

                smelt.m_maxOre = data.smelterData.maxOre ?? smelt.m_maxOre;
                smelt.m_maxFuel = data.smelterData.maxFuel ?? smelt.m_maxFuel;
                smelt.m_fuelPerProduct = data.smelterData.fuelPerProduct ?? smelt.m_fuelPerProduct;
                smelt.m_secPerProduct = data.smelterData.secPerProduct ?? smelt.m_secPerProduct;
                smelt.m_spawnStack = data.smelterData.spawnStack ?? smelt.m_spawnStack;
                smelt.m_requiresRoof = data.smelterData.requiresRoof ?? smelt.m_requiresRoof;
                smelt.m_addOreAnimationDuration = data.smelterData.addOreAnimationLength ?? smelt.m_addOreAnimationDuration;


                if (data.smelterData.smelterConversion != null)
                {
                    smelt.m_conversion.Clear();
                    foreach (var list in data.smelterData.smelterConversion)
                    {
                        Smelter.ItemConversion paul = new Smelter.ItemConversion();

                        //smelt.m_conversion[0].; // has to have 1 set before 
                        if (list != null)
                        {
                            if (list.FromName != null)
                            {
                                //paul.m_from.name =list.FromName;
                                paul.m_from = Instant.GetItemPrefab(list.FromName).GetComponent<ItemDrop>();
                            }

                            if (list.ToName != null)
                            {
                                //paul.m_to.name = list.ToName;
                                paul.m_to = Instant.GetItemPrefab(list.ToName).GetComponent<ItemDrop>();
                            }
                        }
                        smelt.m_conversion.Add(paul);
                    }
                }
            }
        }


        #endregion



        #region Items

        internal static GameObject SetClonedItemsDataCache(WItemData data, ObjectDB Instant, bool WithZDO =false) // need to add mock items as well I guess
        {

            bool skip = false;
            bool skipmock = false;
            foreach (var citem in WMRecipeCust.ClonedI)
            {
                if (citem == data.name)
                    skip = true;
            }

            foreach (var citem in WMRecipeCust.MockI)
            {
                if (citem == data.name)
                    skipmock = true;
            }

            
            if (data.mockName != null && !skipmock)
            {
                if (ObjModelLoader._loadedModels.ContainsKey(data.mockName))
                {
                    WMRecipeCust.Dbgl("Mock Model is loaded" + data.name);                   
                    LayerMask itemLayer = LayerMask.NameToLayer("item");
                    GameObject inactive = new GameObject("Inactive_MockerBase");
                    inactive.SetActive(false);
                    GameObject newObj = UnityEngine.Object.Instantiate(ObjModelLoader.MockItemBase, inactive.transform);
                    newObj.name = data.name;
                    ItemDrop itemDrop = newObj.GetComponent<ItemDrop>();
                    itemDrop.name = data.name;
                    itemDrop.m_itemData.m_shared.m_name = data.m_name ?? "Cube";


                    if (ObjModelLoader._loadedModels.TryGetValue(data.mockName, out var model))
                    {
                        newObj.transform.Find("Cube").gameObject.SetActive(false);
                        var newModel = UnityEngine.Object.Instantiate(model, newObj.transform);
                        newModel.SetActive(true);
                        newModel.name = "attach";
                        newModel.transform.localScale = Vector3.one * 1; // default scale
                        newModel.layer = itemLayer;
                        foreach (var transform in newModel.GetComponentsInChildren<Transform>())
                        {
                            transform.gameObject.layer = itemLayer;
                        }
                    }
                    else
                    {
                        WMRecipeCust.Dbgl("New Mock failed for some reason" + data.name);
                        return null;
                    }
                    Instant.m_items.Add(newObj);
                    WMRecipeCust.MockI.Add(data.name);

                    if (!string.IsNullOrEmpty(data.material))
                    {
                        WMRecipeCust.Dbgl($"Item {data.name} searching for mat {data.material}");
                        try
                        {

                            if (data.material.Contains(','))
                            {
                                renderfinder = newObj.GetComponentsInChildren<Renderer>();// "weapons1_fire" glowing orange
                                string[] materialstr = data.material.Split(',');
                                Material mat = WMRecipeCust.originalMaterials[materialstr[0]];
                                Material part = WMRecipeCust.originalMaterials[materialstr[1]];

                                foreach (Renderer renderitem in renderfinder)
                                {
                                    if (renderitem.receiveShadows && materialstr[0] != "none")
                                        renderitem.sharedMaterial = mat;
                                    else if (!renderitem.receiveShadows)
                                        renderitem.sharedMaterial = part;
                                }
                            }
                            else
                            {
                                Material mat = WMRecipeCust.originalMaterials[data.material];

                                foreach (Renderer r in PrefabAssistant.GetRenderers(newObj))
                                {
                                    PrefabAssistant.UpdateMaterialReference(r, mat);
                                }
                            }
                        }
                        catch { WMRecipeCust.WLog.LogWarning("Material was not found or was not set correctly"); }
                    }
                    return newObj;
                }
                else
                {
                    WMRecipeCust.Dbgl("Mock Model is not loaded, please redownload file or rename or goodluck! " + data.name);
                    return null;
                }
            }



            if (!skip)
            {
                string tempname = data.name;
                if (!string.IsNullOrEmpty(data.clonePrefabName))
                {
                    data.name = data.clonePrefabName;
                }
                else
                {
                    return null;
                }

                GameObject go = DataHelpers.CheckforSpecialObjects(data.name);// check for special cases
                if (go == null)
                    go = Instant.GetItemPrefab(data.name); // normal check

                if (go == null)
                {
                    WMRecipeCust.WLog.LogWarning(" item in SetItemData null " + data.name);
                    return null;
                }
                if (go.GetComponent<ItemDrop>() == null)
                {
                    WMRecipeCust.Dbgl($"Item data in SetItemData for {data.name} not found!");
                    return null;
                } // it is a prefab and it is an item.
                if (string.IsNullOrEmpty(tempname) && !string.IsNullOrEmpty(data.clonePrefabName))
                {
                    WMRecipeCust.WLog.LogWarning($"Item cloned name is empty!");
                    return null;
                }
             
                ItemDrop.ItemData PrimaryItemData = go.GetComponent<ItemDrop>().m_itemData;

                WMRecipeCust.Dbgl($"Item CLONE DATA in SetItemData for {tempname} from cache ");
                Transform RootT = WMRecipeCust.Root.transform; // Root set to inactive to perserve components. 
                GameObject newItem = WMRecipeCust.Instantiate(go, RootT, false);
                ItemDrop NewItemComp = newItem.GetComponent<ItemDrop>();

                NewItemComp.name = tempname; // added and seems to be the issue
                newItem.name = tempname; // resets the orginal name- needs to be unquie
                NewItemComp.m_itemData.m_shared.m_name = DataHelpers.ECheck(data.m_name) ? PrimaryItemData.m_shared.m_name : data.m_name; // ingame name
                var hash = newItem.name.GetStableHashCode();
                Instant.m_items.Add(newItem);
                Instant.m_itemByHash.Add(hash, newItem);

                if (data.sizeMultiplier != 1 && data.sizeMultiplier != null)
                {
                    Vector3 NewScale = new Vector3((float)data.sizeMultiplier, (float)data.sizeMultiplier, (float)data.sizeMultiplier);
                    newItem.transform.GetChild(0).localScale = NewScale;
                }

                if (!string.IsNullOrEmpty(data.material))
                {
                    WMRecipeCust.Dbgl($"Item {tempname} searching for mat {data.material}");
                    try
                    {
                                    
                        if (data.material.Contains(','))
                        {
                            renderfinder = newItem.GetComponentsInChildren<Renderer>();// "weapons1_fire" glowing orange
                            string[] materialstr = data.material.Split(',');
                            Material mat = WMRecipeCust.originalMaterials[materialstr[0]];
                            Material part = WMRecipeCust.originalMaterials[materialstr[1]];

                            foreach (Renderer renderitem in renderfinder)
                            {
                                if (renderitem.receiveShadows && materialstr[0] != "none")
                                    renderitem.sharedMaterial = mat;
                                else if (!renderitem.receiveShadows)
                                    renderitem.sharedMaterial = part;
                            }
                        }
                        else
                        {
                            Material mat = WMRecipeCust.originalMaterials[data.material];

                            foreach (Renderer r in PrefabAssistant.GetRenderers(newItem))
                            {
                                PrefabAssistant.UpdateMaterialReference(r, mat);
                            }
                        }
                    }
                    catch { WMRecipeCust.WLog.LogWarning("Material was not found or was not set correctly"); }
                }
                WMRecipeCust.ClonedI.Add(tempname); 
                data.name = tempname; // putting back name
                return newItem;                   
            }
            return null;
        }



        internal static void SetItemData(WItemData data, ObjectDB Instant, GameObject[] AllObjects = null, bool ZnetNow = true)
        {
            // Dbgl("Loaded SetItemData!");

            bool skip = false;
            bool mockskip = false;

            foreach (var citem in WMRecipeCust.ClonedI)
            {
                if (citem == data.name)
                    skip = true;
            }

            bool mock = false;
            if (data.mockName != null)
            {
                if (ObjModelLoader._loadedModels.ContainsKey(data.mockName))
                {
                    WMRecipeCust.Dbgl("Mock Model is loaded" + data.name);
                    mock = true;
                    
                    foreach (var citem in WMRecipeCust.MockI)
                    {
                        if (citem == data.name)
                            mockskip = true;
                    }
                    if (!mockskip)
                    {
                        WMRecipeCust.Dbgl("Mock Model is loading part 1 " + data.name);
                        LayerMask itemLayer = LayerMask.NameToLayer("item");
                        GameObject inactive = new GameObject("Inactive_MockerBase");
                        inactive.SetActive(false);
                        GameObject newObj = UnityEngine.Object.Instantiate(ObjModelLoader.MockItemBase, inactive.transform);
                        newObj.name = data.name;
                        ItemDrop itemDrop = newObj.GetComponent<ItemDrop>();
                        itemDrop.name = data.name;
                        itemDrop.m_itemData.m_shared.m_name = data.m_name ?? "Cube";


                        if (ObjModelLoader._loadedModels.TryGetValue(data.mockName, out var model))
                        {
                            WMRecipeCust.Dbgl("Mock Model is loading part 2 " + data.name);
                            newObj.transform.Find("Cube").gameObject.SetActive(false);
                            var newModel = UnityEngine.Object.Instantiate(model, newObj.transform);
                            newModel.SetActive(true);
                            newModel.name = "attach";
                            newModel.transform.localScale = Vector3.one * 1; // default scale
                            newModel.layer = itemLayer;
                            foreach (var transform in newModel.GetComponentsInChildren<Transform>())
                            {
                                transform.gameObject.layer = itemLayer;
                            }
                        }
                        else
                        {
                            
                            WMRecipeCust.Dbgl("New Mock failed for some reason" + data.name);
                            return;
                        }
                        WMRecipeCust.Dbgl("Mock Model is loaded 3 " + data.name);
                        GameObject go2 = DataHelpers.CheckforSpecialObjects(data.name);// check for special cases
                        if (go2 == null)
                            go2 = Instant.GetItemPrefab(data.name); // normal check
                        if (go2 == null)
                        {
                            WMRecipeCust.Dbgl("Mock Model is loaded 4 " + data.name);
                            //if (Instant.m_items.Contains(newObj))
                            //   Instant.m_items.Remove(newObj);
                            WMRecipeCust.Dbgl("Mock Model is loaded 4.5 " + data.name);
                            Instant.m_items.Add(newObj);

                            /*
                            ZNetScene znet = ZNetScene.instance;
                            if (znet && ZnetNow)
                            {
                                string name = itemDrop.name;
                                int hash = name.GetStableHashCode();
                                if (znet.m_namedPrefabs.ContainsKey(hash))
                                    WMRecipeCust.WLog.LogWarning($"Prefab {name} already in ZNetScene");
                                else
                                {
                                    if (itemDrop.GetComponent<ZNetView>() != null)
                                        znet.m_prefabs.Add(go2);
                                    else
                                        znet.m_nonNetViewPrefabs.Add(go2);

                                    znet.m_namedPrefabs.Add(hash, go2);
                                    WMRecipeCust.Dbgl($"Added prefab {name}");
                                }
                                znet.m_namedPrefabs[hash].gameObject.SetActive(false); //why?
                            }
                            */
                            WMRecipeCust.Dbgl("Mock Model is loaded 4.6 " + data.name);
                            ZNetScene.instance.m_namedPrefabs[data.name.GetStableHashCode()] = newObj;
                            WMRecipeCust.MockI.Add(data.name);
                            newObj.SetActive(true);
                            

                            if (string.IsNullOrEmpty(data.customIcon))
                            {
                                try
                                {
                                    Functions.SnapshotItem(newObj.GetComponent<ItemDrop>()); // snapshot go
                                }
                                catch { WMRecipeCust.WLog.LogInfo("Icon cloned failed"); }
                            }
                            WMRecipeCust.Dbgl("New Mock Model with New Gameobject, loaded " + data.name);
                        }
                        else
                        {
                            WMRecipeCust.Dbgl("New Mock Model with an existing Gameobject, doesn't work right now, please create name for mock item " + data.name);
                        }

                    }
                    ///skip to normal editing
                }
                else
                {
                    WMRecipeCust.Dbgl("Mock Model is not loaded, please redownload file or rename or goodluck! " + data.name);
                    return;
                }
                WMRecipeCust.Dbgl("Mock Model is loaded 5 " + data.name);
            }

            string tempname = data.name;
            if (!string.IsNullOrEmpty(data.clonePrefabName) && !skip)
            {
                data.name = data.clonePrefabName;
            }


            GameObject go = DataHelpers.CheckforSpecialObjects(data.name);// check for special cases
            if (go == null)
                go = Instant.GetItemPrefab(data.name); // normal check

            if(go == null && !string.IsNullOrEmpty(data.clonePrefabName))
            {            
                go = Instant.GetItemPrefab(data.clonePrefabName);
                if(go != null)
                {
                    WMRecipeCust.WLog.LogWarning($"Last ditch effort to catch {data.name} worked, restoring clone");
                    skip = false;
                    WMRecipeCust.ClonedI.Remove(data.name);
                    data.name = data.clonePrefabName;                  
                }
            }

            if (go == null)
            {
                WMRecipeCust.Dbgl(" item in SetItemData null " + data.name);
                return;
            }
            if (go.GetComponent<ItemDrop>() == null)
            {
                WMRecipeCust.Dbgl($"Item data in SetItemData for {data.name} not found!");
                return;
            } // it is a prefab and it is an item.
            if (string.IsNullOrEmpty(tempname) && !string.IsNullOrEmpty(data.clonePrefabName))
            {
                WMRecipeCust.Dbgl($"Item cloned name is empty!");
                return;
            }


            for (int i = Instant.m_items.Count - 1; i >= 0; i--)  // need to handle clones
            {
                if (Instant.m_items[i]?.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name) // Not sure why I am doing this, New Items with the Same name wouldn't make sense in Object DB Should Probably Just change it to GetItemPrefab
                {
                    ItemDrop.ItemData PrimaryItemData = Instant.m_items[i].GetComponent<ItemDrop>().m_itemData;
                    if (!string.IsNullOrEmpty(data.clonePrefabName) && !skip) // clone setup for new clones
                    {
                        if (WMRecipeCust.BlacklistClone.Contains(data.clonePrefabName))
                        {
                            WMRecipeCust.Dbgl($"Can not clone {data.clonePrefabName} ");
                            return;
                        }

                        WMRecipeCust.Dbgl($"Item CLONE DATA in SetItemData for {tempname} ");
                        WMRecipeCust.ClonedI.Add(tempname);
                        Transform RootT = WMRecipeCust.Root.transform; // Root set to inactive to perserve components. 
                        GameObject newItem = WMRecipeCust.Instantiate(go, RootT, false);
                        ItemDrop NewItemComp = newItem.GetComponent<ItemDrop>();
                        ItemDrop.ItemData NewItemData = newItem.GetComponent<ItemDrop.ItemData>();


                        NewItemComp.name = tempname; // added and seems to be the issue
                        newItem.name = tempname; // resets the orginal name- needs to be unquie
                        NewItemComp.m_itemData.m_shared.m_name = DataHelpers.ECheck(data.m_name) ? PrimaryItemData.m_shared.m_name : data.m_name; // ingame name
                        var hash = newItem.name.GetStableHashCode();
                        ObjectDB.instance.m_items.Add(newItem);
                        ObjectDB.instance.m_itemByHash.Add(hash, newItem);
                        WMRecipeCust.WLog.LogDebug("hash " + hash);

                        ZNetScene znet = ZNetScene.instance;
                        if (znet && ZnetNow)
                        {
                            string name = newItem.name;
                            if (znet.m_namedPrefabs.ContainsKey(hash))
                                WMRecipeCust.WLog.LogWarning($"Prefab {name} already in ZNetScene");
                            else
                            {
                                if (newItem.GetComponent<ZNetView>() != null)
                                    znet.m_prefabs.Add(newItem);
                                else
                                    znet.m_nonNetViewPrefabs.Add(newItem);

                                znet.m_namedPrefabs.Add(hash, newItem);
                                WMRecipeCust.Dbgl($"Added prefab {name}");
                            }
                            znet.m_namedPrefabs[hash].gameObject.SetActive( false ); //why?
                        }

                        //ObjectDB.instance.UpdateItemHashes();

                        go = Instant.GetItemPrefab(tempname);
                        PrimaryItemData = go.GetComponent<ItemDrop>().m_itemData; // get ready to set stuff
                        PrimaryItemData.m_dropPrefab = go;
                        data.name = tempname; // putting back name
                        go.SetActive(true);

                    } // end clone creation
                    if (skip || mockskip)
                    {
                        go.SetActive(true); // for clones and mocks // They are false to make sure non approved cache gameobjects don't get added to Networked Game
                    }

                    if (!string.IsNullOrEmpty(data.material))
                    {
                        WMRecipeCust.Dbgl($"Item {data.name} searching for {data.material}");
                        try
                        {
                            renderfinder = go.GetComponentsInChildren<Renderer>();// "weapons1_fire" glowing orange
                            if (data.material.Contains(','))
                            {
                                string[] materialstr = data.material.Split(',');
                                Material mat = WMRecipeCust.originalMaterials[materialstr[0]];
                                Material part = WMRecipeCust.originalMaterials[materialstr[1]];

                                foreach (Renderer renderitem in renderfinder)
                                {
                                    if (renderitem.receiveShadows && materialstr[0] != "none")
                                        renderitem.material = mat;
                                    else if (!renderitem.receiveShadows)
                                        renderitem.material = part;
                                }
                            }
                            else
                            {
                                if (WMRecipeCust.originalMaterials.TryGetValue(data.material, out Material mat))
                                {

                                    foreach (Renderer r in PrefabAssistant.GetRenderers(go))
                                    {
                                        PrefabAssistant.UpdateMaterialReference(r, mat);
                                    }
                                }else
                                {
                                    WMRecipeCust.WLog.LogWarning(data.material + " was not found");
                                }
                            }
                        }
                        catch { WMRecipeCust.WLog.LogWarning("Material was not found or was not set correctly"); }
                    }

                    var ItemDr = Instant.GetItemPrefab(data.name).GetComponent<ItemDrop>(); // like what is the poitn of the loop
                    Reload.lastItemSet = ItemDr;

                    bool usecustom = false;
                    if (!DataHelpers.ECheck(data.customIcon))
                    {
                        var pathI = Path.Combine(WMRecipeCust.assetPathIcons, data.customIcon);
                        if (File.Exists(pathI))
                        {
                            var nullcheck = File.ReadAllBytes(pathI);
                            if (nullcheck != null)
                            {
                                try
                                {

                                    var Spri = SpriteTools.LoadNewSprite(pathI);
                                    ItemDr.m_itemData.m_shared.m_icons[0] = Spri;
                                    usecustom = true;

                                }
                                catch { WMRecipeCust.WLog.LogInfo("customIcon failed"); }
                            }
                            else
                            {
                                WMRecipeCust.WLog.LogInfo($"No Img with the name {data.customIcon} in Icon Folder - ");
                            }
                        } else
                        {
                            WMRecipeCust.WLog.LogInfo($"No Img with the name {data.customIcon} in Icon Folder - ");
                        }
                    }


                    if (!DataHelpers.ECheck(data.material) && !usecustom)
                    {

                        try
                        {
                            Functions.SnapshotItem(ItemDr); // snapshot go
                        }
                        catch { WMRecipeCust.WLog.LogInfo("Icon cloned failed"); }
                    }

                    WMRecipeCust.Dbgl($"Item being Set in SetItemData for {data.name} ");

                    if (data.Damage != null)
                    {
                        WMRecipeCust.Dbgl($"   {data.name} Item has damage values ");

                        PrimaryItemData.m_shared.m_damages = WeaponDamage.ParseDamageTypes(data.Damage);
                    }

                    if (data.Damage_Per_Level != null)
                    {
                        PrimaryItemData.m_shared.m_damagesPerLevel = WeaponDamage.ParseDamageTypes(data.Damage_Per_Level);
                    }

                    PrimaryItemData.m_shared.m_name = data.m_name ?? PrimaryItemData.m_shared.m_name;
                    PrimaryItemData.m_shared.m_description = data.m_description ?? PrimaryItemData.m_shared.m_description;
                    PrimaryItemData.m_shared.m_weight = data.m_weight;
                    PrimaryItemData.m_shared.m_scaleWeightByQuality = data.scale_weight_by_quality ?? PrimaryItemData.m_shared.m_scaleWeightByQuality;
                    if (data.sizeMultiplier != 1 && data.sizeMultiplier != null)
                    {
                        Vector3 NewScale = new Vector3((float)data.sizeMultiplier, (float)data.sizeMultiplier, (float)data.sizeMultiplier);
                        go.transform.GetChild(0).localScale = NewScale;
                    }

                    if (data.Primary_Attack != null)
                    {
                        WMRecipeCust.Dbgl($"   {data.name} Item attacks ");
                        PrimaryItemData.m_shared.m_attack.m_attackType = data.Primary_Attack.AttackType ?? PrimaryItemData.m_shared.m_attack.m_attackType;
                        PrimaryItemData.m_shared.m_attack.m_attackAnimation = data.Primary_Attack.Attack_Animation ?? PrimaryItemData.m_shared.m_attack.m_attackAnimation;
                        PrimaryItemData.m_shared.m_attack.m_attackRandomAnimations = data.Primary_Attack.Attack_Random_Animation ?? PrimaryItemData.m_shared.m_attack.m_attackRandomAnimations;
                        PrimaryItemData.m_shared.m_attack.m_attackChainLevels = data.Primary_Attack.Chain_Attacks ?? PrimaryItemData.m_shared.m_attack.m_attackChainLevels;
                        PrimaryItemData.m_shared.m_attack.m_hitTerrain = data.Primary_Attack.Hit_Terrain ?? PrimaryItemData.m_shared.m_attack.m_hitTerrain;
                        if (!WMRecipeCust.AttackSpeed.ContainsKey(tempname))
                        {
                            WMRecipeCust.AttackSpeed.Add(tempname, new Dictionary<bool, float>());
                            WMRecipeCust.AttackSpeed[tempname].Add(false, 1); // Just go ahead and add both of them. 
                            WMRecipeCust.AttackSpeed[tempname].Add(true, 1);
                        }
                        if (data.Primary_Attack.Custom_AttackSpeed != null)
                            WMRecipeCust.AttackSpeed[tempname][false] = (float)data.Primary_Attack.Custom_AttackSpeed;

                        PrimaryItemData.m_shared.m_attack.m_attackStamina = data.Primary_Attack.m_attackStamina ?? PrimaryItemData.m_shared.m_attack.m_attackStamina;
                        PrimaryItemData.m_shared.m_attack.m_attackEitr = data.Primary_Attack.m_eitrCost ?? PrimaryItemData.m_shared.m_attack.m_attackEitr;
                        PrimaryItemData.m_shared.m_attack.m_attackHealth = data.Primary_Attack.AttackHealthCost ?? PrimaryItemData.m_shared.m_attack.m_attackHealth;
                        PrimaryItemData.m_shared.m_attack.m_attackHealthPercentage = data.Primary_Attack.m_attackHealthPercentage ?? PrimaryItemData.m_shared.m_attack.m_attackHealthPercentage;

                        PrimaryItemData.m_shared.m_attack.m_speedFactor = data.Primary_Attack.SpeedFactor ?? PrimaryItemData.m_shared.m_attack.m_speedFactor;
                        PrimaryItemData.m_shared.m_attack.m_damageMultiplier = data.Primary_Attack.DmgMultiplier ?? PrimaryItemData.m_shared.m_attack.m_damageMultiplier;
                        PrimaryItemData.m_shared.m_attack.m_forceMultiplier = data.Primary_Attack.ForceMultiplier ?? PrimaryItemData.m_shared.m_attack.m_forceMultiplier;
                        PrimaryItemData.m_shared.m_attack.m_staggerMultiplier = data.Primary_Attack.StaggerMultiplier ?? PrimaryItemData.m_shared.m_attack.m_staggerMultiplier;
                        PrimaryItemData.m_shared.m_attack.m_recoilPushback = data.Primary_Attack.RecoilMultiplier ?? PrimaryItemData.m_shared.m_attack.m_recoilPushback;

                        PrimaryItemData.m_shared.m_attack.m_attackRange = data.Primary_Attack.AttackRange ?? PrimaryItemData.m_shared.m_attack.m_attackRange;
                        PrimaryItemData.m_shared.m_attack.m_attackHeight = data.Primary_Attack.AttackHeight ?? PrimaryItemData.m_shared.m_attack.m_attackHeight;

                        if (!string.IsNullOrEmpty(data.Primary_Attack.Spawn_On_Trigger) && (data.Primary_Attack.Spawn_On_Trigger != PrimaryItemData.m_shared.m_attack.m_spawnOnTrigger?.name))
                        {
                            if (data.Primary_Attack.Spawn_On_Trigger == "delete" || data.Primary_Attack.Spawn_On_Trigger == "-")
                            {
                                PrimaryItemData.m_shared.m_attack.m_spawnOnTrigger = null;
                            }
                            GameObject found = null;
                            foreach (var ob in AllObjects)
                            {
                                if (ob.name == data.Primary_Attack.Spawn_On_Trigger)
                                {
                                    if (found == null)
                                        found = ob;
                                    else if (ob.TryGetComponent<MonsterAI>(out var an1) || ob.TryGetComponent<AnimalAI>(out var an2))
                                        found = ob;
                                    else { }
                                }
                            }
                            PrimaryItemData.m_shared.m_attack.m_spawnOnTrigger = found ?? PrimaryItemData.m_shared.m_attack.m_spawnOnTrigger;
                        }
                            

                        PrimaryItemData.m_shared.m_attack.m_requiresReload = data.Primary_Attack.Requires_Reload ?? PrimaryItemData.m_shared.m_attack.m_requiresReload;
                        PrimaryItemData.m_shared.m_attack.m_reloadAnimation = data.Primary_Attack.Reload_Animation ?? PrimaryItemData.m_shared.m_attack.m_reloadAnimation;
                        PrimaryItemData.m_shared.m_attack.m_reloadTime = data.Primary_Attack.ReloadTime ?? PrimaryItemData.m_shared.m_attack.m_reloadTime;
                        PrimaryItemData.m_shared.m_attack.m_reloadStaminaDrain = data.Primary_Attack.Reload_Stamina_Drain ?? PrimaryItemData.m_shared.m_attack.m_reloadStaminaDrain;

                        PrimaryItemData.m_shared.m_attack.m_bowDraw = data.Primary_Attack.Bow_Draw ?? PrimaryItemData.m_shared.m_attack.m_bowDraw;
                        PrimaryItemData.m_shared.m_attack.m_drawDurationMin = data.Primary_Attack.Bow_Duration_Min ?? PrimaryItemData.m_shared.m_attack.m_drawDurationMin;
                        PrimaryItemData.m_shared.m_attack.m_drawStaminaDrain = data.Primary_Attack.Bow_Stamina_Drain ?? PrimaryItemData.m_shared.m_attack.m_drawStaminaDrain;
                        PrimaryItemData.m_shared.m_attack.m_drawAnimationState = data.Primary_Attack.Bow_Animation_State ?? PrimaryItemData.m_shared.m_attack.m_drawAnimationState;

                        PrimaryItemData.m_shared.m_attack.m_attackAngle = data.Primary_Attack.Attack_Angle ?? PrimaryItemData.m_shared.m_attack.m_attackAngle;
                        PrimaryItemData.m_shared.m_attack.m_attackRayWidth = data.Primary_Attack.Attack_Ray_Width ?? PrimaryItemData.m_shared.m_attack.m_attackRayWidth;
                        PrimaryItemData.m_shared.m_attack.m_lowerDamagePerHit = data.Primary_Attack.Lower_Dmg_Per_Hit ?? PrimaryItemData.m_shared.m_attack.m_lowerDamagePerHit;
                        PrimaryItemData.m_shared.m_attack.m_hitThroughWalls = data.Primary_Attack.Hit_Through_Walls ?? PrimaryItemData.m_shared.m_attack.m_hitThroughWalls;
                        PrimaryItemData.m_shared.m_attack.m_multiHit = data.Primary_Attack.Multi_Hit ?? PrimaryItemData.m_shared.m_attack.m_multiHit;
                        PrimaryItemData.m_shared.m_attack.m_pickaxeSpecial = data.Primary_Attack.Pickaxe_Special ?? PrimaryItemData.m_shared.m_attack.m_pickaxeSpecial;
                        PrimaryItemData.m_shared.m_attack.m_lastChainDamageMultiplier = data.Primary_Attack.Last_Chain_Dmg_Multiplier ?? PrimaryItemData.m_shared.m_attack.m_lastChainDamageMultiplier;

                        /*
                        if (!string.IsNullOrEmpty(data.Primary_Attack.Attack_Projectile)  && (data.Primary_Attack.Spawn_On_Trigger != PrimaryItemData.m_shared.m_attack.m_spawnOnTrigger.name))
                        {
                            GameObject found = null;
                            foreach (var ob in AllObjects)
                            {
                                if (ob.name == data.Primary_Attack.Attack_Projectile)
                                {
                                    if (found == null)
                                        found = ob;
                                    else if (ob.TryGetComponent<MonsterAI>(out var an1) || ob.TryGetComponent<AnimalAI>(out var an2))
                                        found = ob;
                                    else { }
                                }
                            }
                            PrimaryItemData.m_shared.m_attack.m_attackProjectile = found ?? PrimaryItemData.m_shared.m_attack.m_attackProjectile;
                        } */ // disabled attack projectile for now


                        PrimaryItemData.m_shared.m_attack.m_projectileVel = data.Primary_Attack.Projectile_Vel ?? PrimaryItemData.m_shared.m_attack.m_projectileVel;
                        PrimaryItemData.m_shared.m_attack.m_projectileAccuracy = data.Primary_Attack.Projectile_Accuraccy ?? PrimaryItemData.m_shared.m_attack.m_projectileAccuracy;
                        PrimaryItemData.m_shared.m_attack.m_projectiles = data.Primary_Attack.Projectiles ?? PrimaryItemData.m_shared.m_attack.m_projectiles;

                        if (data.Primary_Attack.AEffects.Hit_Effects != null)
                            PrimaryItemData.m_shared.m_attack.m_hitEffect = FindEffect(PrimaryItemData.m_shared.m_attack.m_hitEffect, data.Primary_Attack.AEffects.Hit_Effects);
                        if (data.Primary_Attack.AEffects.Hit_Terrain_Effects != null)
                            PrimaryItemData.m_shared.m_attack.m_hitTerrainEffect = FindEffect(PrimaryItemData.m_shared.m_attack.m_hitTerrainEffect, data.Primary_Attack.AEffects.Hit_Terrain_Effects);
                        if (data.Primary_Attack.AEffects.Start_Effect != null)
                            PrimaryItemData.m_shared.m_attack.m_startEffect = FindEffect(PrimaryItemData.m_shared.m_attack.m_startEffect, data.Primary_Attack.AEffects.Start_Effect);
                        if (data.Primary_Attack.AEffects.Trigger_Effect != null)
                            PrimaryItemData.m_shared.m_attack.m_triggerEffect = FindEffect(PrimaryItemData.m_shared.m_attack.m_triggerEffect, data.Primary_Attack.AEffects.Trigger_Effect);
                        if (data.Primary_Attack.AEffects.Trail_Effect != null)
                            PrimaryItemData.m_shared.m_attack.m_trailStartEffect = FindEffect(PrimaryItemData.m_shared.m_attack.m_trailStartEffect, data.Primary_Attack.AEffects.Trail_Effect);
                        if (data.Primary_Attack.AEffects.Burst_Effect != null)
                            PrimaryItemData.m_shared.m_attack.m_burstEffect = FindEffect(PrimaryItemData.m_shared.m_attack.m_burstEffect, data.Primary_Attack.AEffects.Burst_Effect);


                        // Secondary
                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackType = data.Secondary_Attack.AttackType ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackType;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackAnimation = data.Secondary_Attack.Attack_Animation ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackAnimation;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackRandomAnimations = data.Secondary_Attack.Attack_Random_Animation ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackRandomAnimations;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackChainLevels = data.Secondary_Attack.Chain_Attacks ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackChainLevels;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_hitTerrain = data.Secondary_Attack.Hit_Terrain ?? PrimaryItemData.m_shared.m_secondaryAttack.m_hitTerrain;
                        if (data.Primary_Attack.Custom_AttackSpeed != null)
                            WMRecipeCust.AttackSpeed[tempname][true] = (float)data.Secondary_Attack.Custom_AttackSpeed;

                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackStamina = data.Secondary_Attack.m_attackStamina ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackStamina;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackEitr = data.Secondary_Attack.m_eitrCost ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackEitr;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackHealth = data.Secondary_Attack.AttackHealthCost ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackHealth;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackHealthPercentage = data.Secondary_Attack.m_attackHealthPercentage ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackHealthPercentage;

                        PrimaryItemData.m_shared.m_secondaryAttack.m_speedFactor = data.Secondary_Attack.SpeedFactor ?? PrimaryItemData.m_shared.m_secondaryAttack.m_speedFactor;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_damageMultiplier = data.Secondary_Attack.DmgMultiplier ?? PrimaryItemData.m_shared.m_secondaryAttack.m_damageMultiplier;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_forceMultiplier = data.Secondary_Attack.ForceMultiplier ?? PrimaryItemData.m_shared.m_secondaryAttack.m_forceMultiplier;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_staggerMultiplier = data.Secondary_Attack.StaggerMultiplier ?? PrimaryItemData.m_shared.m_secondaryAttack.m_staggerMultiplier;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_recoilPushback = data.Secondary_Attack.RecoilMultiplier ?? PrimaryItemData.m_shared.m_secondaryAttack.m_recoilPushback;

                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackRange = data.Secondary_Attack.AttackRange ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackRange;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackHeight = data.Secondary_Attack.AttackHeight ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackHeight;

                        if (!string.IsNullOrEmpty(data.Secondary_Attack.Spawn_On_Trigger) && (data.Secondary_Attack.Spawn_On_Trigger != PrimaryItemData.m_shared.m_secondaryAttack.m_spawnOnTrigger?.name))
                        {
                            if (data.Secondary_Attack.Spawn_On_Trigger == "delete" || data.Secondary_Attack.Spawn_On_Trigger == "-")
                            {
                                PrimaryItemData.m_shared.m_secondaryAttack.m_spawnOnTrigger = null;
                            }
                            GameObject found = null;
                            foreach (var ob in AllObjects)
                            {
                                if (ob.name == data.Secondary_Attack.Spawn_On_Trigger)
                                {
                                    if (found == null)
                                        found = ob;
                                    else if (ob.TryGetComponent<MonsterAI>(out var an1) || ob.TryGetComponent<AnimalAI>(out var an2))
                                        found = ob;
                                    else { }
                                }
                            }
                            PrimaryItemData.m_shared.m_secondaryAttack.m_spawnOnTrigger = found ?? PrimaryItemData.m_shared.m_secondaryAttack.m_spawnOnTrigger;
                        }
                            

                        PrimaryItemData.m_shared.m_secondaryAttack.m_requiresReload = data.Secondary_Attack.Requires_Reload ?? PrimaryItemData.m_shared.m_secondaryAttack.m_requiresReload;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_reloadAnimation = data.Secondary_Attack.Reload_Animation ?? PrimaryItemData.m_shared.m_secondaryAttack.m_reloadAnimation;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_reloadTime = data.Secondary_Attack.ReloadTime ?? PrimaryItemData.m_shared.m_secondaryAttack.m_reloadTime;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_reloadStaminaDrain = data.Secondary_Attack.Reload_Stamina_Drain ?? PrimaryItemData.m_shared.m_secondaryAttack.m_reloadStaminaDrain;

                        PrimaryItemData.m_shared.m_secondaryAttack.m_bowDraw = data.Secondary_Attack.Bow_Draw ?? PrimaryItemData.m_shared.m_secondaryAttack.m_bowDraw;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_drawDurationMin = data.Secondary_Attack.Bow_Duration_Min ?? PrimaryItemData.m_shared.m_secondaryAttack.m_drawDurationMin;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_drawStaminaDrain = data.Secondary_Attack.Bow_Stamina_Drain ?? PrimaryItemData.m_shared.m_secondaryAttack.m_drawStaminaDrain;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_drawAnimationState = data.Secondary_Attack.Bow_Animation_State ?? PrimaryItemData.m_shared.m_secondaryAttack.m_drawAnimationState;

                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackAngle = data.Secondary_Attack.Attack_Angle ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackAngle;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_attackRayWidth = data.Secondary_Attack.Attack_Ray_Width ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackRayWidth;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_lowerDamagePerHit = data.Secondary_Attack.Lower_Dmg_Per_Hit ?? PrimaryItemData.m_shared.m_secondaryAttack.m_lowerDamagePerHit;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_hitThroughWalls = data.Secondary_Attack.Hit_Through_Walls ?? PrimaryItemData.m_shared.m_secondaryAttack.m_hitThroughWalls;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_multiHit = data.Secondary_Attack.Multi_Hit ?? PrimaryItemData.m_shared.m_secondaryAttack.m_multiHit;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_pickaxeSpecial = data.Secondary_Attack.Pickaxe_Special ?? PrimaryItemData.m_shared.m_secondaryAttack.m_pickaxeSpecial;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_lastChainDamageMultiplier = data.Secondary_Attack.Last_Chain_Dmg_Multiplier ?? PrimaryItemData.m_shared.m_secondaryAttack.m_lastChainDamageMultiplier;


                        /*
                        if (!string.IsNullOrEmpty(data.Secondary_Attack.Attack_Projectile))
                        {
                            GameObject found = null;
                            foreach (var ob in AllObjects)
                            {
                                if (ob.name == data.Secondary_Attack.Attack_Projectile)
                                {
                                    if (found == null)
                                        found = ob;
                                    else if (ob.TryGetComponent<MonsterAI>(out var an1) || ob.TryGetComponent<AnimalAI>(out var an2))
                                        found = ob;
                                    else { }
                                }
                            }
                            PrimaryItemData.m_shared.m_secondaryAttack.m_attackProjectile = found ?? PrimaryItemData.m_shared.m_secondaryAttack.m_attackProjectile;
                        } */ // disabled for now
                            

                        PrimaryItemData.m_shared.m_secondaryAttack.m_projectileVel = data.Secondary_Attack.Projectile_Vel ?? PrimaryItemData.m_shared.m_secondaryAttack.m_projectileVel;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_projectileAccuracy = data.Secondary_Attack.Projectile_Accuraccy ?? PrimaryItemData.m_shared.m_secondaryAttack.m_projectileAccuracy;
                        PrimaryItemData.m_shared.m_secondaryAttack.m_projectiles = data.Secondary_Attack.Projectiles ?? PrimaryItemData.m_shared.m_secondaryAttack.m_projectiles;

                        if (data.Secondary_Attack.AEffects.Hit_Effects != null)
                            PrimaryItemData.m_shared.m_secondaryAttack.m_hitEffect = FindEffect(PrimaryItemData.m_shared.m_secondaryAttack.m_hitEffect, data.Secondary_Attack.AEffects.Hit_Effects);
                        if (data.Secondary_Attack.AEffects.Hit_Terrain_Effects != null)
                            PrimaryItemData.m_shared.m_secondaryAttack.m_hitTerrainEffect = FindEffect(PrimaryItemData.m_shared.m_secondaryAttack.m_hitTerrainEffect, data.Secondary_Attack.AEffects.Hit_Terrain_Effects);
                        if (data.Secondary_Attack.AEffects.Start_Effect != null)
                            PrimaryItemData.m_shared.m_secondaryAttack.m_startEffect = FindEffect(PrimaryItemData.m_shared.m_secondaryAttack.m_startEffect, data.Secondary_Attack.AEffects.Start_Effect);
                        if (data.Secondary_Attack.AEffects.Trigger_Effect != null)
                            PrimaryItemData.m_shared.m_secondaryAttack.m_triggerEffect = FindEffect(PrimaryItemData.m_shared.m_secondaryAttack.m_triggerEffect, data.Secondary_Attack.AEffects.Trigger_Effect);
                        if (data.Secondary_Attack.AEffects.Trail_Effect != null)
                            PrimaryItemData.m_shared.m_secondaryAttack.m_trailStartEffect = FindEffect(PrimaryItemData.m_shared.m_secondaryAttack.m_trailStartEffect, data.Secondary_Attack.AEffects.Trail_Effect);
                        if (data.Secondary_Attack.AEffects.Burst_Effect != null)
                            PrimaryItemData.m_shared.m_secondaryAttack.m_burstEffect = FindEffect(PrimaryItemData.m_shared.m_secondaryAttack.m_burstEffect, data.Secondary_Attack.AEffects.Burst_Effect);
                    }

                    if (data.Armor != null)
                    {
                        WMRecipeCust.Dbgl($"   {data.name} Item armor ");
                        PrimaryItemData.m_shared.m_armor = data.Armor.armor ?? PrimaryItemData.m_shared.m_armor;
                        PrimaryItemData.m_shared.m_armorPerLevel = data.Armor.armorPerLevel ?? PrimaryItemData.m_shared.m_armorPerLevel;

                    }
                    if (data.FoodStats != null)
                    {
                        WMRecipeCust.Dbgl($"   {data.name} Item food ");
                        PrimaryItemData.m_shared.m_food = data.FoodStats.m_foodHealth ?? PrimaryItemData.m_shared.m_food;
                        PrimaryItemData.m_shared.m_foodStamina = data.FoodStats.m_foodStamina ?? PrimaryItemData.m_shared.m_foodStamina;
                        PrimaryItemData.m_shared.m_foodRegen = data.FoodStats.m_foodRegen ?? PrimaryItemData.m_shared.m_foodRegen;
                        PrimaryItemData.m_shared.m_foodBurnTime = data.FoodStats.m_foodBurnTime ?? PrimaryItemData.m_shared.m_foodBurnTime;
                        PrimaryItemData.m_shared.m_foodEitr = data.FoodStats.m_FoodEitr ?? PrimaryItemData.m_shared.m_foodEitr;
                    }
                    if (data.Moddifiers != null)
                    {
                        WMRecipeCust.Dbgl($"   {data.name} Item movement ");
                        PrimaryItemData.m_shared.m_movementModifier = data.Moddifiers.m_movementModifier ?? PrimaryItemData.m_shared.m_movementModifier;
                        PrimaryItemData.m_shared.m_eitrRegenModifier = data.Moddifiers.m_EitrRegen ?? PrimaryItemData.m_shared.m_eitrRegenModifier;

                    }
                    if (data.SE_Equip != null)
                    {
                        if (data.SE_Equip.EffectName == "delete" || data.SE_Equip.EffectName == "-")
                        {
                            PrimaryItemData.m_shared.m_equipStatusEffect = null;
                            WMRecipeCust.Dbgl($"   {data.name} Item equip effects removed");
                        }
                        else
                        {

                            WMRecipeCust.Dbgl($"   {data.name} Item equip effects ");
                            PrimaryItemData.m_shared.m_equipStatusEffect = Instant.GetStatusEffect(data.SE_Equip.EffectName.GetStableHashCode()) ?? PrimaryItemData.m_shared.m_equipStatusEffect;
                        }
                    }
                    if (data.SE_SET_Equip != null)
                    {
                        if (data.SE_SET_Equip.EffectName == "delete" || data.SE_SET_Equip.EffectName == "-")
                        {
                            PrimaryItemData.m_shared.m_setName = null;
                            PrimaryItemData.m_shared.m_setSize = 0;
                            PrimaryItemData.m_shared.m_setStatusEffect = null;
                            WMRecipeCust.Dbgl($"   {data.name} Item seteffects removed");
                        }
                        else
                        {
                            WMRecipeCust.Dbgl($"   {data.name} Item seteffects ");
                            PrimaryItemData.m_shared.m_setName = data.SE_SET_Equip.SetName ?? PrimaryItemData.m_shared.m_setName;
                            PrimaryItemData.m_shared.m_setSize = data.SE_SET_Equip.Size ?? PrimaryItemData.m_shared.m_setSize;
                            PrimaryItemData.m_shared.m_setStatusEffect = Instant.GetStatusEffect(data.SE_SET_Equip.EffectName.GetStableHashCode()) ?? PrimaryItemData.m_shared.m_setStatusEffect;
                        }
                    }

                    if (data.ShieldStats != null)
                    {
                        WMRecipeCust.Dbgl($"   {data.name} Item block ");
                        PrimaryItemData.m_shared.m_blockPower = data.ShieldStats.m_blockPower ?? PrimaryItemData.m_shared.m_blockPower;
                        PrimaryItemData.m_shared.m_blockPowerPerLevel = data.ShieldStats.m_blockPowerPerLevel ?? PrimaryItemData.m_shared.m_blockPowerPerLevel;
                        PrimaryItemData.m_shared.m_timedBlockBonus = data.ShieldStats.m_timedBlockBonus ?? PrimaryItemData.m_shared.m_timedBlockBonus;
                        PrimaryItemData.m_shared.m_deflectionForce = data.ShieldStats.m_deflectionForce ?? PrimaryItemData.m_shared.m_deflectionForce;
                        PrimaryItemData.m_shared.m_deflectionForcePerLevel = data.ShieldStats.m_deflectionForcePerLevel ?? PrimaryItemData.m_shared.m_deflectionForcePerLevel;
                    }


                    PrimaryItemData.m_shared.m_maxStackSize = data.m_maxStackSize ?? PrimaryItemData.m_shared.m_maxStackSize;
                    PrimaryItemData.m_shared.m_canBeReparied = data.m_canBeReparied ?? PrimaryItemData.m_shared.m_canBeReparied;
                    PrimaryItemData.m_shared.m_destroyBroken = data.m_destroyBroken ?? PrimaryItemData.m_shared.m_destroyBroken;
                    PrimaryItemData.m_shared.m_dodgeable = data.m_dodgeable ?? PrimaryItemData.m_shared.m_dodgeable;
                    PrimaryItemData.m_shared.m_questItem = data.m_questItem ?? PrimaryItemData.m_shared.m_questItem;
                    PrimaryItemData.m_shared.m_teleportable = data.m_teleportable ?? PrimaryItemData.m_shared.m_teleportable;
                    PrimaryItemData.m_shared.m_backstabBonus = data.m_backstabbonus ?? PrimaryItemData.m_shared.m_backstabBonus;
                    PrimaryItemData.m_shared.m_attackForce = data.m_knockback ?? PrimaryItemData.m_shared.m_attackForce;


                    if (data.Attack_status_effect != null)
                        PrimaryItemData.m_shared.m_attackStatusEffect = Instant.GetStatusEffect(data.Attack_status_effect.GetStableHashCode()) ?? PrimaryItemData.m_shared.m_attackStatusEffect;

                    if (!string.IsNullOrEmpty(data.spawn_on_hit) && (data.spawn_on_hit != PrimaryItemData.m_shared.m_spawnOnHit?.name))
                    {
                        GameObject found = null;
                        foreach (var ob in AllObjects)
                        {
                            if(ob.name == data.spawn_on_hit)
                            {
                                if (found == null)
                                    found = ob;
                                else if (ob.TryGetComponent<MonsterAI>(out var an1) || ob.TryGetComponent<AnimalAI>(out var an2))
                                    found = ob;
                                else { }
                            }
                        }
                        PrimaryItemData.m_shared.m_spawnOnHit = found ?? PrimaryItemData.m_shared.m_spawnOnHit;                                          
                    }

                    if (!string.IsNullOrEmpty(data.spawn_on_terrain_hit) && (data.spawn_on_terrain_hit != PrimaryItemData.m_shared.m_spawnOnHitTerrain?.name))
                    {
                        GameObject found = null;
                        foreach (var ob in AllObjects)
                        {
                            if (ob.name == data.spawn_on_terrain_hit)
                            {
                                if (found == null)
                                    found = ob;
                                else if (ob.TryGetComponent<MonsterAI>(out var an1) || ob.TryGetComponent<AnimalAI>(out var an2))
                                    found = ob;
                                else { }
                            }
                        }
                        PrimaryItemData.m_shared.m_spawnOnHitTerrain = found ?? PrimaryItemData.m_shared.m_spawnOnHitTerrain;
                    }
                        

                    PrimaryItemData.m_shared.m_useDurability = data.m_useDurability ?? PrimaryItemData.m_shared.m_useDurability;
                    PrimaryItemData.m_shared.m_useDurabilityDrain = data.m_useDurabilityDrain ?? PrimaryItemData.m_shared.m_useDurabilityDrain;
                    // WMRecipeCust.WLog.LogWarning($"use Durabilty is " + data.m_useDurability); // test temp
                    PrimaryItemData.m_shared.m_durabilityDrain = data.m_durabilityDrain ?? PrimaryItemData.m_shared.m_durabilityDrain;
                    PrimaryItemData.m_shared.m_maxDurability = data.m_maxDurability ?? PrimaryItemData.m_shared.m_maxDurability;
                    PrimaryItemData.m_shared.m_durabilityPerLevel = data.m_durabilityPerLevel ?? PrimaryItemData.m_shared.m_durabilityPerLevel;

                    PrimaryItemData.m_shared.m_equipDuration = data.m_equipDuration ?? PrimaryItemData.m_shared.m_equipDuration;

                    PrimaryItemData.m_shared.m_skillType = data.m_skillType ?? PrimaryItemData.m_shared.m_skillType;
                    PrimaryItemData.m_shared.m_animationState = data.m_animationState ?? PrimaryItemData.m_shared.m_animationState;
                    PrimaryItemData.m_shared.m_itemType = data.m_itemType ?? PrimaryItemData.m_shared.m_itemType;

                    PrimaryItemData.m_shared.m_toolTier = data.m_toolTier ?? PrimaryItemData.m_shared.m_toolTier;
                    PrimaryItemData.m_shared.m_maxQuality = data.m_maxQuality ?? PrimaryItemData.m_shared.m_maxQuality;
                    PrimaryItemData.m_shared.m_value = data.m_value ?? PrimaryItemData.m_shared.m_value;

                    if (data.GEffects.Hit_Effects != null)
                        PrimaryItemData.m_shared.m_hitEffect = FindEffect(PrimaryItemData.m_shared.m_hitEffect, data.GEffects.Hit_Effects, "m_hitEffect");
                    if (data.GEffects.Hit_Terrain_Effects != null)
                        PrimaryItemData.m_shared.m_hitTerrainEffect = FindEffect(PrimaryItemData.m_shared.m_hitTerrainEffect, data.GEffects.Hit_Terrain_Effects, "m_hitTerrainEffect");
                    if (data.GEffects.Start_Effect != null)
                        PrimaryItemData.m_shared.m_startEffect = FindEffect(PrimaryItemData.m_shared.m_startEffect, data.GEffects.Start_Effect, "m_startEffect");
                    if (data.GEffects.Hold_Start_Effects != null)
                        PrimaryItemData.m_shared.m_holdStartEffect = FindEffect(PrimaryItemData.m_shared.m_holdStartEffect, data.GEffects.Hold_Start_Effects, "m_holdStartEffect");
                    if (data.GEffects.Trigger_Effect != null)
                        PrimaryItemData.m_shared.m_triggerEffect = FindEffect(PrimaryItemData.m_shared.m_triggerEffect, data.GEffects.Trigger_Effect, "m_triggerEffect");
                    if (data.GEffects.Trail_Effect != null)
                        PrimaryItemData.m_shared.m_trailStartEffect = FindEffect(PrimaryItemData.m_shared.m_trailStartEffect, data.GEffects.Trail_Effect, "m_trailStartEffect");





                    if (!DataHelpers.ECheck(data.damageModifiers))
                    {
                        PrimaryItemData.m_shared.m_damageModifiers.Clear(); // from aedenthorn start -  thx
                        if (data.damageModifiers[0] == "-" || data.damageModifiers[0] == "delete" || data.damageModifiers[0] == " -" ) { // clear it
                            WMRecipeCust.Dbgl("     clearing dmg Modifiers");
                            //PrimaryItemData.m_shared.m_damageModifiers.Clear();
                        }
                        else
                        {

                            foreach (string modString in data.damageModifiers)
                            {
                                string[] mod = modString.Split(':');
                                int modType = Enum.TryParse<ArmorHelpers.NewDamageTypes>(mod[0], out ArmorHelpers.NewDamageTypes result) ? (int)result : (int)Enum.Parse(typeof(HitData.DamageType), mod[0]);
                                PrimaryItemData.m_shared.m_damageModifiers.Add(new HitData.DamageModPair() { m_type = (HitData.DamageType)modType, m_modifier = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), mod[1]) }); // end aedenthorn code
                            }
                        }
                    }
                    if (PrimaryItemData.m_shared.m_value > 0)
                    {
                        string valu = "              <color=#edd221>Valuable</color>";
                        PrimaryItemData.m_shared.m_description = data.m_description + valu;
                    }

                    if (data.ConsumableStatusEffect != null)
                    {
                        PrimaryItemData.m_shared.m_consumeStatusEffect = Instant.GetStatusEffect(data.ConsumableStatusEffect.GetStableHashCode()) ?? PrimaryItemData.m_shared.m_consumeStatusEffect;
                    }
                    return;
                }


            }

        }


        private static EffectList FindEffect(EffectList current, string[] userlist, string name = "")
        {
            try
            {
                if (current != null && current.m_effectPrefabs != null) // has existing effectlist
                {
                    if (userlist[0] == null) {
                        EffectList paul = new EffectList();
                        return paul;
                    }

                    var copy = current;
                    var count = 0;
                    var currentcount = copy.m_effectPrefabs.Count();
                    List<string> copyuserlist = userlist.ToList<string>();
                    int userlistcount = userlist.Count();

                    EffectData[] newEffectData = new EffectData[userlistcount];

                    //List<string> currentList = new List<string>();
                    Dictionary<string, int> removeList = new Dictionary<string, int>();

                    var effectprecount = 0;
                    foreach (var eff in copy.m_effectPrefabs)
                    {
                        //currentList.Add(eff.m_prefab.name);
                        if (copyuserlist.Contains(eff.m_prefab.name))
                        {
                            eff.m_enabled = true;
                            newEffectData[count] = eff;
                            count++;
                            copyuserlist.Remove(eff.m_prefab.name);
                        }
                        else
                        {
                            //removeList.Add(eff.m_prefab.name, effectprecount);
                        }
                        effectprecount++;
                    }
                    foreach (var userEff in removeList)
                    {
                        //copy.m_effectPrefabs[userEff.Value].m_enabled = false; // make it false
                    }

                    foreach (var userEffKey in copyuserlist) // the list left to add to end of effectlist
                    {
                        EffectList.EffectData effectDataone = new EffectList.EffectData();



                        if (WMRecipeCust.originalVFX.TryGetValue(userEffKey, out GameObject list1))
                        {
                            effectDataone.m_prefab = list1;
                            effectDataone.m_enabled = true;
                            effectDataone.m_childTransform = "";
                            newEffectData[count] = effectDataone;
                            count++;

                        }
                        else if (WMRecipeCust.originalSFX.TryGetValue(userEffKey, out GameObject list2))
                        {
                            effectDataone.m_prefab = list2;
                            effectDataone.m_enabled = true;
                            effectDataone.m_childTransform = "";
                            newEffectData[count] = effectDataone;
                            count++;

                        }
                        else if (WMRecipeCust.originalFX.TryGetValue(userEffKey, out GameObject list3))
                        {
                            effectDataone.m_prefab = list3;
                            effectDataone.m_enabled = true;
                            effectDataone.m_childTransform = "";
                            newEffectData[count] = effectDataone;
                            count++;

                        }
                        else if (WMRecipeCust.extraEffects.TryGetValue(userEffKey, out GameObject list4))
                        {
                            effectDataone.m_prefab = list4;
                            effectDataone.m_enabled = true;
                            effectDataone.m_childTransform = "";
                            newEffectData[count] = effectDataone;
                            count++;
                        }
                        else
                        { // failure to find
                            WMRecipeCust.WLog.LogError("Didn't find effect " + userEffKey + " This will cause an error when the effect is used - please remove");
                        }

                    } // end of foreach
                    copy.m_effectPrefabs = newEffectData;
                    return copy;

                }
                else if (userlist.Count() > 0)
                {
                    EffectList effectList = new EffectList();
                    EffectList.EffectData[] effectData = new EffectList.EffectData[userlist.Count()];

                    var count = 0;
                    foreach (var userEffe in userlist)
                    {

                        if (WMRecipeCust.originalVFX.TryGetValue(userEffe, out GameObject list1))
                        {
                            effectData[count].m_prefab = list1;
                            effectData[count].m_enabled = true;

                            count++;
                        }
                        else if (WMRecipeCust.originalSFX.TryGetValue(userEffe, out GameObject list2))
                        {
                            effectData[count].m_prefab = list2;
                            effectData[count].m_enabled = true;
                            count++;
                        }
                        else if (WMRecipeCust.originalFX.TryGetValue(userEffe, out GameObject list3))
                        {
                            effectData[count].m_prefab = list3;
                            effectData[count].m_enabled = true;
                            count++;
                        }else if (WMRecipeCust.extraEffects.TryGetValue(userEffe, out GameObject list4))
                        {
                            effectData[count].m_prefab = list4;
                            effectData[count].m_enabled = true;
                            count++;
                        }
                        else
                        { // failure to find
                            WMRecipeCust.WLog.LogError("Didn't find effect " + userEffe + " This will cause an error when the effect is used - please remove");
                        }
                    }
                    effectList.m_effectPrefabs = effectData;
                    return effectList;

                }
                else { return current; }
            }
            catch (System.Exception e) { WMRecipeCust.WLog.LogWarning($"Effect {name} had problems  {e.Message}"); return current; }
        }

        #endregion



        #region Creatures

        internal static void SetCreature(CreatureData data, GameObject[] arrayCreature)
        {
            var count = 0;
            GameObject replacermodel = null;
            GameObject clonecreature = null;
            bool skip = false;
            bool skipReplacer = false;

            foreach (var citem in WMRecipeCust.ClonedC)
            {
                if (citem == data.name)
                    skip = true;
            }


            foreach (var citem in WMRecipeCust.ClonedCR)
            {
                if (citem == data.name)
                    skipReplacer = true;
            }



            foreach (GameObject obj in arrayCreature) // clone
            {
                var currentCreature = obj;
                if (data.clone_creature != null && !skip && obj.name == data.clone_creature && (obj.TryGetComponent<Humanoid>(out Humanoid dontuse)
                    || obj.TryGetComponent<AnimalAI>(out AnimalAI dontuse1) || obj.TryGetComponent<MonsterAI>(out MonsterAI dontuse2)))
                {
                    clonecreature = WMRecipeCust.Instantiate(obj, WMRecipeCust.Root.transform, false);
                    clonecreature.name = data.name;
                    clonecreature.GetComponent<ZNetView>().name = data.name;
                    var clonepiggy = clonecreature.GetComponent<Humanoid>();

                    ZNetScene znet = ZNetScene.instance;
                    if (znet)
                    {

                        string name = clonecreature.name;
                        var hash = clonecreature.name.GetStableHashCode();
                        if (znet.m_namedPrefabs.ContainsKey(hash))
                            WMRecipeCust.WLog.LogWarning($"Prefab {name} already in ZNetScene");
                        else
                        {
                            if (clonecreature.GetComponent<ZNetView>() != null)
                                znet.m_prefabs.Add(clonecreature);
                            else
                                znet.m_nonNetViewPrefabs.Add(clonecreature);

                            znet.m_namedPrefabs.Add(hash, clonecreature);
                            WMRecipeCust.Dbgl($"Added cloned Creature {name}");
                        }
                    }// end znet

                    clonepiggy.m_name = data.mob_display_name; // clone won't go through normal setting this round, so needs to all be set in here

                    WMRecipeCust.ClonedC.Add(data.name);
                    WMRecipeCust.ClonedCC.Add(data.name, clonecreature);
                    currentCreature = clonecreature;

                    //return; // end loop

                } // end clone

                if (obj.name == data.name && (obj.TryGetComponent<Humanoid>(out Humanoid high1) // replacer
                    || obj.TryGetComponent<AnimalAI>(out AnimalAI high2) || obj.TryGetComponent<MonsterAI>(out MonsterAI high3)))
                {
                    if (data.creature_replacer != null && !skipReplacer)// creature replacer
                    {
                        foreach (GameObject obj2 in arrayCreature)
                        {
                            if (obj2.name == data.creature_replacer && obj2.TryGetComponent<Humanoid>(out Humanoid piggy2))
                            {
                                obj.SetActive(false); // deactives current mob
                                var del = obj.gameObject;
                                replacermodel = WMRecipeCust.Instantiate(obj2, WMRecipeCust.Root.transform, false);
                                replacermodel.name = data.name;

                                var copyznet = obj.GetComponent<ZNetView>();
                                var repl = replacermodel.GetComponent<ZNetView>();
                                repl = copyznet;


                                var piggy3 = replacermodel.GetComponent<Humanoid>();
                                piggy3.m_name = data.mob_display_name; // replacer editing

                                ZNetScene znet = ZNetScene.instance;
                                if (znet)
                                {

                                    string name = replacermodel.name;
                                    var hash = replacermodel.name.GetStableHashCode();
                                    if (znet.m_namedPrefabs.ContainsKey(hash))
                                    {

                                        WMRecipeCust.Dbgl($"Prefab {name} already in ZNetScene");

                                        znet.m_namedPrefabs.Remove(hash);
                                        znet.m_prefabs.Remove(obj);

                                        znet.m_prefabs.Add(replacermodel);
                                        znet.m_namedPrefabs.Add(hash, replacermodel);
                                        WMRecipeCust.Dbgl($"Removed old {name} and replaced prefab with {obj2.name}");

                                    }

                                }
                                WMRecipeCust.ClonedCR.Add(data.name);
                                // currentCreature = obj; already set
                                //return; 
                            }
                        }
                    } // end creature replacer
                   // return; // end loop
                } // end name match

                if (currentCreature.name == data.name && (obj.TryGetComponent<Humanoid>(out Humanoid mob))){ 
                // Normal editing
                    WMRecipeCust.Dbgl($"Setting {data.name} "); // normal edit

                    mob.m_name = data.mob_display_name;
                    /*

                    if (!string.IsNullOrEmpty(data.custom_material)) // material changer
                    {
                        WMRecipeCust.Dbgl($"Material name searching for {data.custom_material}");
                        try
                        {
                            renderfinder = obj.GetComponentsInChildren<SkinnedMeshRenderer>();// "weapons1_fire" glowing orange
                            //if (data.custom_material.Contains(','))
                            {
                                string[] materialstr = data.custom_material.Split(',');
                                Material mat = WMRecipeCust.originalMaterials[materialstr[0]];
                                Material part = WMRecipeCust.originalMaterials[materialstr[1]];

                                foreach (SkinnedMeshRenderer renderitem in renderfinder)
                                {
                                    if (renderitem.receiveShadows && materialstr[0] != "none")
                                        renderitem.material = mat;
                                    else if (!renderitem.receiveShadows)
                                        renderitem.material = part;
                                }
                            }
                           // else
                           // {
                               // Material mat = WMRecipeCust.originalMaterials[data.custom_material];

                               // foreach (Renderer r in PrefabAssistant.GetRenderers(obj))
                               // {
                                //    PrefabAssistant.UpdateMaterialReference(r, mat);
                               // }
                           // }
                        }
                        catch { WMRecipeCust.WLog.LogWarning("Material was not found or was not set correctly"); }
                    } */
                } // nromal edit
                count++;
            } 
        }

    }
    #endregion

}
