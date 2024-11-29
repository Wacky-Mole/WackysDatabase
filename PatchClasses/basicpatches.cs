using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using BepInEx;
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
using System.Web;

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

    [HarmonyPatch(typeof(Player), nameof(Player.QueueReloadAction))] // IG didn't add a check for Stamina for reloadStamina
    internal static class AddStaminaReloadCheck
    {
        public static bool Prefix( Player __instance)
        {
           if(!__instance.IsReloadActionQueued() && __instance.IsPlayer())
            {
                ItemDrop.ItemData currentWeapon = __instance.GetCurrentWeapon();
                if (currentWeapon != null && currentWeapon.m_shared.m_attack.m_requiresReload)
                {
                    if (currentWeapon.m_shared.m_attack.m_reloadStaminaDrain > 0)
                    {
                        if (__instance.HaveStamina(currentWeapon.m_shared.m_attack.m_reloadStaminaDrain)) 
                        {
                            return true;
                        }else
                        {
                            Hud.instance.StaminaBarEmptyFlash();
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.ModifyDamage))]
    internal static class ModifyDamageWM
    {
        public static void Postfix(ref HitData hitData, Attack __instance )
        {
            if (__instance.m_character.IsPlayer() )
            {
               if ( __instance.m_character.m_rightItem == null && __instance.m_character.m_leftItem == null)
                    return;
                if (__instance.m_weapon?.m_dropPrefab?.name == null)
                    return;


                if (WMRecipeCust.SEWeaponChoice.TryGetValue(__instance.m_weapon.m_dropPrefab.name, out Tuple<string,float,string,float> userSE))
                {
                   // WMRecipeCust.WLog.LogWarning("Primary or Secondary effect check");
                    /*
                    bool projectile = false;
                    if (__instance.m_weapon.m_dropPrefab.TryGetComponent<Projectile>(out var paul))
                        projectile = true;

                    __instance.m_weapon.m_dropPrefab.TryGetComponent<ItemDrop.ItemData>(out var paul2);
                    if (paul2.m_shared.m_ammoType != "")
                    {
                        projectile = true;
                    } */

                    if (__instance.m_character.m_currentAttackIsSecondary)
                    {
                        if (userSE.Item3 != "" && userSE.Item4 != 0)
                        {
                            if(UnityEngine.Random.Range(0f, 1f) < userSE.Item4)
                            {
                                hitData.m_statusEffectHash = userSE.Item3.GetStableHashCode();
                            }
                        }
                    } else
                    {
                        if (userSE.Item1 != "" && userSE.Item2 != 0 )
                        {
                            if (UnityEngine.Random.Range(0f, 1f) < userSE.Item2)
                            {
                                hitData.m_statusEffectHash = userSE.Item1.GetStableHashCode();
                            }
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetAvailableRecipes))]
    internal static class Player_GetAvailableRecipes_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance , ref Dictionary<Assembly, Dictionary<Recipe, bool>>? __state)
        {
            __state ??= new Dictionary<Assembly, Dictionary<Recipe, bool>>();
            Dictionary<Recipe, bool> hidden = new Dictionary<Recipe, bool>();

            if (InventoryGui.instance.InCraftTab())
            {
               hidden = WMRecipeCust.RequiredUpgradeItemsString.ToDictionary(entry => entry.Key,
                                               entry => entry.Value); // Opposite of ItemManager
            }
            else if (InventoryGui.instance.InUpradeTab())
            {
                hidden = WMRecipeCust.RequiredCraftItemsString.ToDictionary(entry => entry.Key,
                                               entry => entry.Value); 
            }
            else
            {
                return;
            }

            foreach (Recipe recipe in hidden.Keys)
            {
                recipe.m_enabled = false;
            }
            
            __state[Assembly.GetExecutingAssembly()] = hidden;
        }

        [HarmonyFinalizer]
        public static void Finalizer(Dictionary<Assembly, Dictionary<Recipe, bool>> __state)
        {
            if (__state.TryGetValue(Assembly.GetExecutingAssembly(), out Dictionary<Recipe, bool> hidden))
            {
                foreach (KeyValuePair<Recipe, bool> kv in hidden)
                {
                    kv.Key.m_enabled = kv.Value;
                }
            }
        }

    }

    [HarmonyPatch(typeof(Piece.Requirement))]
    internal static class PieceUpgradeReq 
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Piece.Requirement.GetAmount))]

        internal static bool Patch_RequirementGetAmountWM(Piece.Requirement __instance, int qualityLevel, ref int __result)
        {
            if (WMRecipeCust.requirementQuality.TryGetValue(__instance, out RequirementQuality quality))
            {
                __result = quality.quality == qualityLevel ? __instance.m_amountPerLevel : 0;
                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(ItemDrop.ItemData))]
    internal static class ItemDataPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.VeryLow)]
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
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyPatch(nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
        public static void GetWeaponLoadingTimePostfixWM(ItemDrop.ItemData __instance, ref float __state)
        {
            if (__instance.m_shared.m_attack.m_requiresReload && __state != -1f)
            {
                __instance.m_shared.m_attack.m_reloadTime = __state;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "TryPlacePiece")]
    static class Player_MessageforWackyDBTry
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
                        tempvalue = new Vector3(0, 0, 0); 

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
    [HarmonyPriority(Priority.Last)]
    static class RecipeStationPatchWackydb
    {
        private static void Postfix( ref int __result, Recipe __instance)
        {
            if (__instance == null) return;
            if (__instance.m_item == null) return;

            if (WMRecipeCust.RecipeMaxStationLvl.TryGetValue(__instance.m_item.name, out int level))
            {
                
                if (level == -1)
                {

                }
                else
                {
                    __result = Math.Min(__result, level);

                }
                //WMRecipeCust.WLog.LogInfo("Current instance name is " + __instance.m_item.name + "With result " +__result);

            }
        }
    }

}
