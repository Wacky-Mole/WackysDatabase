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



namespace wackydatabase.PatchClasses
{


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
        private static void Postfix( Recipe __instance, ref int __result)
        {


            if (__instance == null) return;
            if(__instance.m_item == null) return;
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

}
