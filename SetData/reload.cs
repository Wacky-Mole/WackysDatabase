﻿using System;
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
    public class Reload : WMRecipeCust
    {
        public void SyncEventDetected()
        {
            if (Firstrun)
            {
                GetAllMaterials();
                Firstrun = false;
                DataHelpers.GetPieceStations();
                DataHelpers.GetPiecesatStart();
                //LoadinMultiplayerFirst = true; // this is going to require some rewrite
                if (!isDebug.Value)
                    WackysRecipeCustomizationLogger.LogWarning($"Debug String is off, which suprisingly makes it hard to debug");
            }
            if (NoMoreLoading)
            {
                //startupSync++;
                recieveServerInfo = true;
                NoMoreLoading = false;
                Dbgl($" No More Loading was true");
                WackysRecipeCustomizationLogger.LogWarning("Warning any ServerFiles will see be On Your Local Games Until Restart! ");
            }
            else
            {
                WackysRecipeCustomizationLogger.LogDebug("CustomSyncEventDetected was called ");
                Dbgl($" You did reload SERVER Files");
                Admin = ConfigSync.IsAdmin;
                if (Admin)
                {
                    Dbgl($" You are an Admin");
                }
                else
                {
                    Dbgl($" You are not an admin");
                }
                recipeDatas.Clear();
                ItemDatas.Clear();
                PieceDatas.Clear();
                armorDatas.Clear();
                pieceWithLvl.Clear(); // ready for new
                ObjectDB Instant = ObjectDB.instance;
                string SyncedString = skillConfigData.Value;
                if (SyncedString != null && SyncedString != "")
                {
                    WackysRecipeCustomizationLogger.LogDebug("Synced String was  " + SyncedString);
                    string[] jsons = SyncedString.Split('@');
                    foreach (var word in jsons) // Should really do a first pass for clones?
                    {
                        if (word.Contains("m_weight")) //item
                        {
                            WItemData data2 = JsonUtility.FromJson<WItemData>(word);
                            ItemDatas.Add(data2);
                            ArmorData data3 = JsonUtility.FromJson<ArmorData>(word);
                            armorDatas.Add(data3);
                        }
                        else if (word.Contains("piecehammer")) // only piece
                        {
                            PieceData data = JsonUtility.FromJson<PieceData>(word);
                            PieceDatas.Add(data);
                        }
                        else // has to be recipes
                        {
                            RecipeData data = JsonUtility.FromJson<RecipeData>(word);
                            recipeDatas.Add(data);
                        }

                        //WackysRecipeCustomizationLogger.LogDebug(word);
                    }
                    if (LoadinMultiplayerFirst)
                    {
                        LoadinMultiplayerFirst = false; // Only for first Load in on Multiplayer, Keeps Mutliplayer loading last 
                        Dbgl($" Delaying Server Reloading Until very end");
                        return;
                    }

                    // CLONE PASS FIRST - only for craftingStation

                    foreach (var data3 in PieceDatas)
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
                            catch { WackysRecipeCustomizationLogger.LogWarning($"SetPiece Clone PASS for {data3.name} failed"); }
                        }
                    }
                    // END CLONE PASS
                    // Real PASS NOW
                    foreach (var data2 in ItemDatas)
                    {
                        if (data2 != null)
                        {
                            try
                            {
                                SetData.SetItemData(data2, Instant);
                            }
                            catch { WackysRecipeCustomizationLogger.LogWarning($"SetItem Data for {data2.name} failed"); }
                        }
                    }
                    Instant.UpdateItemHashes();
                    foreach (var data3 in PieceDatas)
                    {
                        if (data3 != null)
                        {
                            try
                            {
                                SetData.SetPieceRecipeData(data3, Instant);
                            }
                            catch { WackysRecipeCustomizationLogger.LogWarning($"SetPiece Data for {data3.name} failed"); }

                        }
                    }
                    foreach (var data in recipeDatas) // recipes last
                    {

                        if (data != null)
                        {
                            try
                            {

                                SetData.SetRecipeData(data, Instant);
                            }
                            catch { WackysRecipeCustomizationLogger.LogWarning($"SetRecipe Data for {data.name} failed"); }

                        }
                    }

                    WackysRecipeCustomizationLogger.LogDebug("done with customSyncEvent");
                }
                else
                {
                    WackysRecipeCustomizationLogger.LogDebug("Synced String was blank " + SyncedString);
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
            if (reload && (issettoSinglePlayer || recieveServerInfo)) // single player only or recievedServerInfo
            {
                if (recieveServerInfo && issettoSinglePlayer)
                {
                    WackysRecipeCustomizationLogger.LogWarning($" You Loaded into Singleplayer local first and therefore will NOT be allowed to reload Server Configs");
                    return; // naughty boy no recipes for you
                }
                else
                {
                    ReadFiles.GetRecipeDataFromFiles();
                    ObjectDB Instant = ObjectDB.instance;
                    // CLONE PASS FIRST - only for craftingStation
                    foreach (var data3 in PieceDatas)
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
                    foreach (var data in ItemDatas) // call items first
                    {
                        try
                        {
                            SetData.SetItemData(data, Instant);
                        }
                        catch { WackysRecipeCustomizationLogger.LogWarning($"SetItem Data for {data.name} failed"); }

                    }
                    Instant.UpdateItemHashes();
                    foreach (var data in PieceDatas)
                    {
                        try
                        {
                            SetData.SetPieceRecipeData(data, Instant);
                        }
                        catch { WackysRecipeCustomizationLogger.LogWarning($"SetPiece Data for {data.name} failed"); }
                    }
                    foreach (var data in recipeDatas) // recipes last
                    {
                        try
                        {
                            SetData.SetRecipeData(data, Instant);
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
                    //WMRecipeCust goman = new WMRecipeCust();
                   // goman.CustomSyncEventDetected();
                }
            }
        }
    }
}