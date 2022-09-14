using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using wackydatabase.Datas;

namespace wackydatabase.Startup
{
    internal class Closing 
    {

        internal static void DestoryClones()
        {
            GameObject go;
            ZNetScene znet = ZNetScene.instance;
            var delObj = ObjectDB.instance;
            GameObject piecehammer = null;

            foreach (var citem in WMRecipeCust.ClonedR) // just ignore ClonedR just index
            {
                try
                {
                    // since we are disableing recipes we should always have access
                    for (int i = ObjectDB.instance.m_recipes.Count - 1; i > 0; i--)
                    {
                        if (ObjectDB.instance.m_recipes[i].name == citem)
                        {
                            delObj.m_recipes.RemoveAt(i);
                        }
                    }

                }
                catch { WMRecipeCust.Dbgl($"Error Disabling recipe {citem}"); }
            }

            foreach (var citem in WMRecipeCust.ClonedI)
            {
                try
                {
                    go = DataHelpers.CheckforSpecialObjects(citem);// check for special cases
                    if (go == null)
                        go = delObj.GetItemPrefab(citem); // normal check
                    delObj.m_items.Remove(go);
                    var hash = go.name.GetStableHashCode();
                    znet.m_prefabs.Remove(go); // removing znets
                    znet.m_namedPrefabs.Remove(hash);
                    GameObject.Destroy(go);
                }
                catch { WMRecipeCust.Dbgl($"Error Destorying item {citem}"); }

            }
            foreach (var citem in WMRecipeCust.ClonedP)
            {
                piecehammer = null;
                WMRecipeCust.selectedPiecehammer = null;
                try
                {
                    //go = FindPieceObjectName(citem);
                    go = DataHelpers.GetModdedPieces(citem); // known modded Hammer search
                    if (go == null)
                    {
                        go = DataHelpers.CheckforSpecialObjects(citem); // check for special
                        piecehammer = ObjectDB.instance.GetItemPrefab("Hammer");
                    }

                    if (WMRecipeCust.selectedPiecehammer != null)
                        WMRecipeCust.selectedPiecehammer.m_pieces.Remove(go);
                    else piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go);

                    znet.m_prefabs.Remove(go);
                    var hash = go.name.GetStableHashCode();
                    znet.m_namedPrefabs.Remove(hash);
                    //GameObject.Destroy (go); craftingStations get Destoryed TAG

                }
                catch { WMRecipeCust.Dbgl($"Error Destorying piece {citem}"); }
            }
            WMRecipeCust.ClonedI.Clear();
            WMRecipeCust.ClonedR.Clear();
            WMRecipeCust.ClonedP.Clear();
            ObjectDB.instance.UpdateItemHashes();

            WMRecipeCust.Dbgl("All cloned Objects destoryed");
        }
    }
}
