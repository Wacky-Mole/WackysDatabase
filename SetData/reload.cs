using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using wackydatabase.Datas;
using wackydatabase.Util;
using System.IO;
using System.Collections;

using wackydatabase.Startup;

namespace wackydatabase.SetData
{
    public class Reload 
    {
        public void SyncEventDetected()
        {
            if (WMRecipeCust.Firstrun)
            {
                WMRecipeCust.GetAllMaterials();
                WMRecipeCust.Firstrun = false;
                DataHelpers.GetPieceStations();
                DataHelpers.GetPiecesatStart();
                //LoadinMultiplayerFirst = true; // this is going to require some rewrite
                if (!WMRecipeCust.isDebug.Value)
                    WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning($"Debug String is off, which suprisingly makes it hard to debug");
            }
            if (WMRecipeCust.NoMoreLoading)
            {
                //startupSync++;
                WMRecipeCust.recieveServerInfo = true;
                WMRecipeCust.NoMoreLoading = false;
                WMRecipeCust.Dbgl($" No More Loading was true");
                WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning("Warning any ServerFiles will see be On Your Local Games Until Restart! ");
            }
            else
            {
                WMRecipeCust.WackysRecipeCustomizationLogger.LogDebug("CustomSyncEventDetected was called ");
                WMRecipeCust.Dbgl($" You did reload SERVER Files");
                WMRecipeCust.Admin = WMRecipeCust.ConfigSync.IsAdmin;
                if (WMRecipeCust.Admin)
                {
                    WMRecipeCust.Dbgl($" You are an Admin");
                }
                else
                {
                    WMRecipeCust.Dbgl($" You are not an admin");
                }
                WMRecipeCust.recipeDatas.Clear();
                WMRecipeCust.ItemDatas.Clear();
                WMRecipeCust.PieceDatas.Clear();
                WMRecipeCust.armorDatas.Clear();
                WMRecipeCust.pieceWithLvl.Clear(); // ready for new
                ObjectDB Instant = ObjectDB.instance;
                string SyncedString = WMRecipeCust.skillConfigData.Value;
                if (SyncedString != null && SyncedString != "")
                {
                    WMRecipeCust.WackysRecipeCustomizationLogger.LogDebug("Synced String was  " + SyncedString);
                    string[] jsons = SyncedString.Split('@');
                    foreach (var word in jsons) // Should really do a first pass for clones?
                    {
                        if (word.Contains("m_weight")) //item
                        {
                            WItemData data2 = JsonUtility.FromJson<WItemData>(word);
                            WMRecipeCust.ItemDatas.Add(data2);
                            ArmorData data3 = JsonUtility.FromJson<ArmorData>(word);
                            WMRecipeCust.armorDatas.Add(data3);
                        }
                        else if (word.Contains("piecehammer")) // only piece
                        {
                            PieceData data = JsonUtility.FromJson<PieceData>(word);
                            WMRecipeCust.PieceDatas.Add(data);
                        }
                        else // has to be recipes
                        {
                            RecipeData data = JsonUtility.FromJson<RecipeData>(word);
                            WMRecipeCust.recipeDatas.Add(data);
                        }

                        //WackysRecipeCustomizationLogger.LogDebug(word);
                    }
                    if (WMRecipeCust.LoadinMultiplayerFirst)
                    {
                        WMRecipeCust.LoadinMultiplayerFirst = false; // Only for first Load in on Multiplayer, Keeps Mutliplayer loading last 
                        WMRecipeCust.Dbgl($" Delaying Server Reloading Until very end");
                        return;
                    }

                    // CLONE PASS FIRST - only for craftingStation

                    foreach (var data3 in WMRecipeCust.PieceDatas)
                    {
                        if (data3 != null && data3.clone)
                        {
                            try
                            {
                                CraftingStation checkifStation = null;
                                GameObject go = DataHelpers.FindPieceObjectName(data3.clonePrefabName);
                                string tempnam = null;
                                tempnam = go.GetComponent<CraftingStation>()?.m_name;
                                if (tempnam != null)
                                {
                                    checkifStation = DataHelpers.GetCraftingStation(tempnam); // for forge and other items that change names between item and CraftingStation
                                    if (checkifStation != null) // means the prefab being cloned is a craftingStation and needs to proceed
                                    {
                                        SetData.SetPieceRecipeData(data3, Instant);
                                    }
                                }
                            }
                            catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning($"SetPiece Clone PASS for {data3.name} failed"); }
                        }
                    }
                    // END CLONE PASS
                    // Real PASS NOW
                    foreach (var data2 in WMRecipeCust.ItemDatas)
                    {
                        if (data2 != null)
                        {
                            try
                            {
                                SetData.SetItemData(data2, Instant);
                            }
                            catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning($"SetItem Data for {data2.name} failed"); }
                        }
                    }
                    Instant.UpdateItemHashes();
                    foreach (var data3 in WMRecipeCust.PieceDatas)
                    {
                        if (data3 != null)
                        {
                            try
                            {
                                SetData.SetPieceRecipeData(data3, Instant);
                            }
                            catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning($"SetPiece Data for {data3.name} failed"); }

                        }
                    }
                    foreach (var data in WMRecipeCust.recipeDatas) // recipes last
                    {

                        if (data != null)
                        {
                            try
                            {

                                SetData.SetRecipeData(data, Instant);
                            }
                            catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning($"SetRecipe Data for {data.name} failed"); }

                        }
                    }

                    WMRecipeCust.WackysRecipeCustomizationLogger.LogDebug("done with customSyncEvent");
                }
                else
                {
                    WMRecipeCust.WackysRecipeCustomizationLogger.LogDebug("Synced String was blank " + SyncedString);
                }
            }
        }


        public void LoadAllRecipeData(bool reload)
        {
            if (reload)
            {
                ZNet Net = new ZNet();
                Startup.Startup.IsLocalInstance(Net);
            }
            if (reload && (WMRecipeCust.issettoSinglePlayer || WMRecipeCust.recieveServerInfo)) // single player only or recievedServerInfo
            {
                if (WMRecipeCust.recieveServerInfo && WMRecipeCust.issettoSinglePlayer)
                {
                    WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning($" You Loaded into Singleplayer local first and therefore will NOT be allowed to reload Server Configs");
                    return; // naughty boy no recipes for you
                }
                else
                {
                    ReadFiles.GetRecipeDataFromFiles();
                    ObjectDB Instant = ObjectDB.instance;
                    // CLONE PASS FIRST - only for craftingStation
                    foreach (var data3 in WMRecipeCust.PieceDatas)
                    {
                        if (data3 != null && data3.clone)
                        {
                            try
                            {
                                CraftingStation checkifStation = null;
                                GameObject go = DataHelpers.FindPieceObjectName(data3.clonePrefabName);
                                string tempnam = null;
                                tempnam = go.GetComponent<CraftingStation>()?.m_name;
                                if (tempnam != null)
                                {
                                    checkifStation = DataHelpers.GetCraftingStation(tempnam); // for forge and other items that change names between item and CraftingStation
                                    if (checkifStation != null) // means the prefab being cloned is a craftingStation and needs to proceed
                                    {
                                        SetData.SetPieceRecipeData(data3, Instant);
                                    }
                                }
                            }
                            catch { } // spams just catch any empty
                        }
                    }
                    // END CLONE PASS
                    // Real PASS NOW
                    foreach (var data in WMRecipeCust.ItemDatas) // call items first
                    {
                        try
                        {
                            SetData.SetItemData(data, Instant);
                        }
                        catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning($"SetItem Data for {data.name} failed"); }

                    }
                    Instant.UpdateItemHashes();
                    foreach (var data in WMRecipeCust.PieceDatas)
                    {
                        try
                        {
                            SetData.SetPieceRecipeData(data, Instant);
                        }
                        catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning($"SetPiece Data for {data.name} failed"); }
                    }
                    foreach (var data in WMRecipeCust.recipeDatas) // recipes last
                    {
                        try
                        {
                            SetData.SetRecipeData(data, Instant);
                        }
                        catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning($"SetRecipe Data for {data.name} failed"); }
                    }
                    WMRecipeCust.Dbgl($" You did reload LOCAL Files");
                }
                try
                {
                    ObjectDB.instance.UpdateItemHashes();
                }
                catch
                {
                    WMRecipeCust.Dbgl($"failed to update Hashes- probably due to too many calls");
                }
            }
            else
            {
                if (WMRecipeCust.issettoSinglePlayer)
                {
                    WMRecipeCust.Dbgl($" You did NOT reload LOCAL Files. You probably should have.");
                }
                if (WMRecipeCust.LoadinMultiplayerFirst)
                {
                    //WMRecipeCust goman = new WMRecipeCust();
                   // goman.CustomSyncEventDetected();
                }
            }
        }
    }
}
