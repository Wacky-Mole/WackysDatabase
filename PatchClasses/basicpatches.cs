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


}
