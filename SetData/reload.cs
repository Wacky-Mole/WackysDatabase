using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wackydatabase.SetData
{
    internal class Reload : WMRecipeCust
    {

        internal static void LoadAllRecipeData(bool reload)
        {
            if (reload)
            {
                ZNet Net = new ZNet();
                IsLocalInstance(Net);
            }
            if (reload && (issettoSinglePlayer || recieveServerInfo)) // single player only or recievedServerInfo
            {
                if (recieveServerInfo && issettoSinglePlayer)
                {
                    WackysRecipeCustomizationLogger.LogWarning($" You Loaded into Singleplayer local first and therefore will NOT be allowed to reload Server Configs");
                    return; // naughty boy no recipes for you
                }
                else
                {
                    GetRecipeDataFromFiles();
                    ObjectDB Instant = ObjectDB.instance;
                    // CLONE PASS FIRST - only for craftingStation
                    foreach (var data3 in PieceDatas)
                    {
                        if (data3 != null && data3.clone)
                        {
                            try
                            {
                                CraftingStation checkifStation = null;
                                GameObject go = FindPieceObjectName(data3.clonePrefabName);
                                string tempnam = null;
                                tempnam = go.GetComponent<CraftingStation>()?.m_name;
                                if (tempnam != null)
                                {
                                    checkifStation = GetCraftingStation(tempnam); // for forge and other items that change names between item and CraftingStation
                                    if (checkifStation != null) // means the prefab being cloned is a craftingStation and needs to proceed
                                    {
                                        SetPieceRecipeData(data3, Instant);
                                    }
                                }
                            }
                            catch { } // spams just catch any empty
                        }
                    }
                    // END CLONE PASS
                    // Real PASS NOW
                    foreach (var data in ItemDatas) // call items first
                    {
                        try
                        {
                            SetItemData(data, Instant);
                        }
                        catch { WackysRecipeCustomizationLogger.LogWarning($"SetItem Data for {data.name} failed"); }

                    }
                    Instant.UpdateItemHashes();
                    foreach (var data in PieceDatas)
                    {
                        try
                        {
                            SetPieceRecipeData(data, Instant);
                        }
                        catch { WackysRecipeCustomizationLogger.LogWarning($"SetPiece Data for {data.name} failed"); }
                    }
                    foreach (var data in recipeDatas) // recipes last
                    {
                        try
                        {
                            SetRecipeData(data, Instant);
                        }
                        catch { WackysRecipeCustomizationLogger.LogWarning($"SetRecipe Data for {data.name} failed"); }
                    }
                    Dbgl($" You did reload LOCAL Files");
                }
                try
                {
                    ObjectDB.instance.UpdateItemHashes();
                }
                catch
                {
                    Dbgl($"failed to update Hashes- probably due to too many calls");
                }
            }
            else
            {
                if (issettoSinglePlayer)
                {
                    Dbgl($" You did NOT reload LOCAL Files. You probably should have.");
                }
                if (LoadinMultiplayerFirst)
                {
                    WMRecipeCust goman = new WMRecipeCust();
                    goman.CustomSyncEventDetected();
                }
            }
        }
    }
}
