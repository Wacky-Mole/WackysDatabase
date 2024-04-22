using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.IO;
using System.Security.Cryptography;
using wackydatabase.Datas;
using wackydatabase.Util;
using wackydatabase.GetData;
using wackydatabase.Startup;
using wackydatabase.SetData;
using UnityEngine.UI;
using System.Runtime.CompilerServices;
using TMPro;
using RainbowTrollArmor;
using System.Reflection;
using System.Diagnostics.Eventing.Reader;

namespace wackydatabase.PatchClasses
{

    /* Cookie Project
     [HarmonyPatch(typeof(Hud), "UpdateMount")]
     static class MountIconPatch 
     {
         public static string playerCurrentSaddle = "";
         private static void Postfix(ref Player player, Hud __instance,  GameObject ___m_mountPanel)
         {
             Sadle sadle = player.GetDoodadController() as Sadle;
             if (sadle != null)
             {             
                 Character character = sadle.GetCharacter(); // use this to select between your riding mobs
                 var Icon = ___m_mountPanel;
                 if (Icon == null) return; 
                 var icon = Icon.transform.GetChild(0).Find("MountIcon")?.gameObject.GetComponent<UnityEngine.UI.Image>();
                 if (icon != null && playerCurrentSaddle != character.name)
                 {
                     WMRecipeCust.WLog.LogWarning($"Mob with saddle is {character.name}"); // remove this just for testing
                     SpriteToolsCombined spriteTool = new SpriteToolsCombined();
                     switch (character.name)
                     {
                         case "Lox(Clone)":
                             WMRecipeCust.WLog.LogInfo("Setting Wolf Icon");
                             icon.sprite = spriteTool.CreateSprite(spriteTool.loadTexture("Wolf.png"), false);
                             //icon.sprite = WMRecipeCust.Wolf; or better yet load this in your main awake once
                             playerCurrentSaddle = character.name;
                             break;

                         case "Wolf(Clone)":
                             icon.sprite = spriteTool.CreateSprite(spriteTool.loadTexture("Wolf.png"), false);
                             playerCurrentSaddle = character.name;
                             break;

                         default:
                             playerCurrentSaddle = character.name;
                             break;
                     }
                 }
             }
         }
     }

     */

    [HarmonyPatch(typeof(ItemDrop.ItemData))]
    internal static class ItemDataPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
        public static void GetWeaponLoadingTimePrefixWM(ItemDrop.ItemData __instance, out float __state)
        {
            if (__instance.m_shared.m_attack.m_requiresReload)
            {
                string name = __instance.m_shared.m_name ?? "none";
                __state = __instance.m_shared.m_attack.m_reloadTime;
                if (WMRecipeCust.crossbowReloadingTime.TryGetValue(name + "P", out float value))
                {  
                    if (value == 1.0f)
                    {
                        __state = -1f;
                        return;
                    }                    
                    __instance.m_shared.m_attack.m_reloadTime *= value;
                }
                else if (WMRecipeCust.crossbowReloadingTime.TryGetValue(name + "S", out float value2)) // for future// not quite setup
                {
                    if (value2 == 1.0f)
                    {
                        __state = -1f;
                        return;
                    }
                    __instance.m_shared.m_secondaryAttack.m_reloadTime *= value2;
                }           
                return;
            }
            __state = -1f;
        }


        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
        public static void GetWeaponLoadingTimePostfixWM(ItemDrop.ItemData __instance, ref float __state)
        {
            if (__instance.m_shared.m_attack.m_requiresReload && __state != -1f)
            {
                __instance.m_shared.m_attack.m_reloadTime = __state;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "PlacePiece")]
    static class Player_MessageforPortal_Patch
    {
        private static Vector3 tempvalue;

        [HarmonyPrefix]
        private static bool Prefix(ref Player __instance, ref Piece piece)

        {
            if (piece == null) return true;
            foreach (var item in wackydatabase.WMRecipeCust.pieceWithLvl)
            {
                var stringwithnumber = item.Split('.');
                var PiecetoLookFor = stringwithnumber[0];
                int CraftingStationlvl = int.Parse(stringwithnumber[1]);

                if (piece.name == PiecetoLookFor && !__instance.m_noPlacementCost) // check for piece name
                {
                    if (__instance.transform.position != null)
                        tempvalue = __instance.transform.position; // save position //must be assigned
                    else
                        tempvalue = new Vector3(0, 0, 0); // shouldn't ever be called 

                    var paulstation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, tempvalue);
                    var paullvl = paulstation.GetLevel();

                    if (paullvl + 1 > CraftingStationlvl) // just for testing
                    {
                        // piecehaslvl = true;
                    }
                    else
                    {
                        string worktablename = piece.m_craftingStation.name;
                        GameObject temp = DataHelpers.GetPieces().Find(g => Utils.GetPrefabName(g) == worktablename);
                        var name = temp.GetComponent<Piece>().m_name;
                        __instance.Message(MessageHud.MessageType.Center, "Need a Level " + CraftingStationlvl + " " + name + " for placement");
                        return false;
                    }
                }
            }
            return true;
        }

    }

    [HarmonyPatch(typeof(Recipe), "GetRequiredStationLevel")]
    static class RecipeStationPatch
    {
        private static void Postfix(Recipe __instance, ref int __result)
        {


            if (__instance == null) return;
            if (__instance.m_item == null) return;
            //if (__instance.m_item.name == null) return;

            //var level2 = WMRecipeCust.RecipeMaxStationLvl[__instance.m_item.m_itemData.m_shared.m_name];
            /*
            foreach (KeyValuePair<string, int> kvp in WMRecipeCust.RecipeMaxStationLvl)
            {
                //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                WMRecipeCust.WLog.LogInfo("Key and Value "+ kvp.Key + " "  +kvp.Value);
            
            } */

            if (WMRecipeCust.RecipeMaxStationLvl.TryGetValue(__instance.m_item.name, out int level))
            {
                if (level == -1)
                {

                }
                else
                {
                    __result = Math.Min(__result, level);

                }

            }

            /*
            if (___recipe == null )
                return;
            if (___recipe.name == null)
                return;

            string name = ___recipe.name;
            if (WMRecipeCust.RecipeMaxStationLvl.ContainsKey(name))
            {
                int level = WMRecipeCust.RecipeMaxStationLvl[name];
                if (level == -1 || level == 0)
                {
                    return;
                }else
                {
                    __result = Mathf.Min(__result, level);
                }
            }
            */
        }
    }

    /*  I didn't like all these patches to do something simple. It was a good idea, but could be done better

    [HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
    static class HideQuality
    {
        private static void Prefix(Transform elementRoot)
        {
            Transform component5 = elementRoot.transform.Find("res_quality");
            if (component5 != null)
            {
                component5.GetComponent<TMP_Text>().gameObject.SetActive(value: false);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
    static class ApplyQuality
    {

        public static Transform root;
        public static Piece.Requirement reqh;
        public static Player playerh;
        public static int qualhold;
        public static InventoryGui instance;
        private static bool Prefix(Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality)
        {
            root = elementRoot;
            reqh = req;
            playerh = player;
            qualhold = quality;
            return true;
        }

        private static void Postfix(bool __result)
        {       
            InventoryGui __instance = InventoryGui.m_instance;           
            if ( reqh.m_resItem != null && __instance != null && __instance.m_selectedRecipe.Key != null)
            {
                Transform component5T = root.transform.Find("res_quality");
                GameObject component5 = null;
                if (component5T == null)
                {
                    Transform component3Parent = root.transform.Find("res_amount");
                    TMP_Text component3 = component3Parent.GetComponent<TMP_Text>();
                    component5 = GameObject.Instantiate(component3Parent.gameObject, root);// quality
                    component5.name = "res_quality";
                    component5.GetComponent<RectTransform>().localPosition = new Vector2(0, 0);
                    component5.GetComponent<TMP_Text>().text = "☆1";
                    component5.GetComponent<TMP_Text>().font = component3.font;

                }
                else
                    component5 = component5T.gameObject;

              
                if (reqh.m_resItem != null && __result && WMRecipeCust.QualityRecipeReq.ContainsKey(__instance.m_selectedRecipe.Key.name))
                {
                    //WMRecipeCust.WLog.LogInfo("Hello " + __instance.m_selectedRecipe.Key);
                    Dictionary<ItemDrop, int> testme = WMRecipeCust.QualityRecipeReq[__instance.m_selectedRecipe.Key.name];
                    if (!testme.ContainsKey(reqh.m_resItem))
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (__instance.m_recipeRequirementList[i].transform.Find("res_quality") && !__instance.m_recipeRequirementList[i].transform.Find("res_amount").gameObject.activeSelf)
                            {
                                component5 = __instance.m_recipeRequirementList[i].transform.Find("res_quality").gameObject;
                                component5.SetActive(false);
                            }
                        }
                        return;
                    }
                    var qual = testme[reqh.m_resItem];
                    if (qual == 1)
                    {
                        component5.SetActive(false);
                        return;
                    }
                    int num = 0;
                    int amount = reqh.GetAmount(qualhold);
                    if (amount <= 0)
                    {
                        InventoryGui.HideRequirement(root);
                        return;
                    }
                    component5.SetActive(true);
                    component5.GetComponent<TMP_Text>().text = "☆" + qual;
                    foreach (var slot in playerh.m_inventory.m_inventory)
                    {
                        if (slot.m_shared.m_name == reqh.m_resItem.m_itemData.m_shared.m_name && slot.m_quality == qual)
                        {
                            num++;
                        }
                    }
                    if (num < amount)
                    {
                        component5.GetComponent<TMP_Text>().color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
                    }
                    else
                    {
                        component5.GetComponent<TMP_Text>().color = Color.white;
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (__instance.m_recipeRequirementList[i].transform.Find("res_quality") && !__instance.m_recipeRequirementList[i].transform.Find("res_amount").gameObject.activeSelf)
                        {
                            component5 = __instance.m_recipeRequirementList[i].transform.Find("res_quality").gameObject;
                            component5.SetActive(false);
                        }
                    }

                }
            }
        }
    }



  [HarmonyPatch(typeof(Player), "ConsumeResources")]

    static class ConsumeAdjustmentLevel
    {
        private static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality = -1)
        {
            InventoryGui instance = InventoryGui.m_instance;
            if (instance.m_selectedRecipe.Key != null)
            {
                if (WMRecipeCust.QualityRecipeReq.ContainsKey(instance.m_selectedRecipe.Key.name))
                {
                    var found = false;
                    Dictionary<ItemDrop, int> searchme = WMRecipeCust.QualityRecipeReq[instance.m_selectedRecipe.Key.name];

                    foreach (Piece.Requirement requirement in requirements)
                    {
                        if (searchme.ContainsKey(requirement.m_resItem))
                        {
                            if (searchme[requirement.m_resItem] > 1)
                            {
                                found = true;
                            }

                        }
                    }

                    if (found)
                    {
                        foreach (Piece.Requirement requirement in requirements)
                        {
                            if (searchme.ContainsKey(requirement.m_resItem))
                            {
                                if (searchme[requirement.m_resItem] > 1)
                                {

                                    if ((bool)requirement.m_resItem)
                                    {
                                        int amount = requirement.GetAmount(searchme[requirement.m_resItem]);
                                        if (amount > 0)
                                        {
                                            __instance.m_inventory.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, amount, searchme[requirement.m_resItem]);
                                        }
                                    }
                                }
                                else
                                {
                                    if ((bool)requirement.m_resItem)
                                    {
                                        int amount = requirement.GetAmount(qualityLevel);
                                        if (amount > 0)
                                        {
                                            __instance.m_inventory.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, amount, itemQuality);
                                        }
                                    }
                                }

                            }
                        }
                        return false;
                    }
                }
            }
            return true;
        }
    }

   [HarmonyPatch(typeof(Player), "HaveRequirementItems")]

    static class HaveRecipeQuality
    {
        public static Recipe piecehold;
        public static bool discoverhold;
        public static int qualityLevelhold;
        private static bool Prefix(Recipe piece, bool discover, int qualityLevel)
        {
            piecehold = piece;
            discoverhold = discover;
            qualityLevelhold = qualityLevel;
            //WMRecipeCust.WLog.LogInfo("have item check " + piece.name + " " + piece.m_resources);
            return true;
        }
        private static void Postfix( Player __instance, ref bool __result)
        {
            // WMRecipeCust.WLog.LogInfo(__result);
            if (__result && WMRecipeCust.QualityRecipeReq.ContainsKey(piecehold.name) && discoverhold)
            {
                foreach (var rec in WMRecipeCust.QualityRecipeReq[piecehold.name])
                {
                    if (rec.Value == 1) // don't look for default quality
                        continue;

                    var found = false;
                    foreach (var slot in __instance.m_inventory.m_inventory)
                    {
                        if (slot.m_shared.m_name == rec.Key.m_itemData.m_shared.m_name && slot.m_quality == rec.Value)
                        {
                            found = true;
                            // found do nothing
                        }
                    }
                    if (!found)
                    {
                        __result = false;
                        return;
                    }

                } 
                return;
            }
            
            else if (__result && InventoryGui.m_instance.m_selectedRecipe.Key == piecehold && WMRecipeCust.QualityRecipeReq.ContainsKey(piecehold.name) && !discoverhold)
            {
                var foundall = false;
                foreach (var rec in WMRecipeCust.QualityRecipeReq[piecehold.name])
                {
                    if (rec.Value == 1) // don't look for default quality
                        continue;

                    var found = false;
                    foreach (var slot in __instance.m_inventory.m_inventory)
                    {
                        if (slot.m_shared.m_name == rec.Key.m_itemData.m_shared.m_name && slot.m_quality == rec.Value)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        __result = false;
                        return;
                    }
                }
            }                
            
        }
    }
    */
    

}
