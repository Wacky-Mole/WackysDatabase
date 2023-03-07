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
using YamlDotNet.Serialization;
using VisualsModifier;

namespace wackydatabase.SetData
{
    public class Reload 
    {
        public void SyncEventDetected()
        {
            WMRecipeCust.WLog.LogWarning($"DSync Detected - remove before release");

            if (WMRecipeCust.Firstrun)
            {
                WMRecipeCust.GetAllMaterials();
                WMRecipeCust.Firstrun = false;
                DataHelpers.GetPieceStations();
                DataHelpers.GetPiecesatStart();
                //LoadinMultiplayerFirst = true; // this is going to require some rewrite
                if (!WMRecipeCust.isDebug.Value)
                    WMRecipeCust.WLog.LogWarning($"Debug String is off, which suprisingly makes it hard to debug");
            }
            if (WMRecipeCust.NoMoreLoading)
            {
                //startupSync++;
                WMRecipeCust.recieveServerInfo = true;
                WMRecipeCust.NoMoreLoading = false;
                WMRecipeCust.Dbgl($" No More Loading was true");
                WMRecipeCust.WLog.LogWarning("Warning any ServerFiles will see be On Your Local Games Until Restart! ");
            }
            else
            {
                WMRecipeCust.WLog.LogDebug("CustomSyncEventDetected was called ");
                WMRecipeCust.Dbgl($" You recieved SERVER Files, so reloading");
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
                    var deserializer = new DeserializerBuilder()
                    .Build();
                    WMRecipeCust.WLog.LogDebug("Synced String was  " + SyncedString);
                    string[] yml = SyncedString.Split(WMRecipeCust.StringSeparator);
                    foreach (var word in yml) // Should really do a first pass for clones?
                    {
                        if (word.Contains("m_weight")) //item
                        {
                            WMRecipeCust.itemDatasYml.Add(deserializer.Deserialize<WItemData>(word));
                            //ArmorData_json data3 = JsonUtility.FromJson<ArmorData_json>(word);
                            //WMRecipeCust.armorDatas.Add(data3);
                        }
                        else if (word.Contains("piecehammer")) // only piece
                        {
                            WMRecipeCust.recipeDatasYml.Add(deserializer.Deserialize<RecipeData>(word));
                        }
                        else // has to be recipes
                        {
                            WMRecipeCust.recipeDatasYml.Add(deserializer.Deserialize<RecipeData>(word));
                        }

                        //WLog.LogDebug(word);
                    }
                    if (WMRecipeCust.LoadinMultiplayerFirst)
                    {
                        WMRecipeCust.LoadinMultiplayerFirst = false; // Only for first Load in on Multiplayer, Keeps Mutliplayer loading last 
                        WMRecipeCust.Dbgl($" Delaying Server Reloading Until very end");
                        return;
                    }
                    LoadAllRecipeData(true); // true magic

                    WMRecipeCust.WLog.LogDebug("done with customSyncEvent");
                }
                else
                {
                    WMRecipeCust.WLog.LogDebug("Synced String was blank " + SyncedString);
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
                    WMRecipeCust.WLog.LogWarning($" You Loaded into Singleplayer local first and therefore will NOT be allowed to reload Server Configs");
                    return; // naughty boy no recipes for you
                }
                else
                {
                    WMRecipeCust.WLog.LogWarning($" Reloading - remove before final");
                    ObjectDB Instant = ObjectDB.instance;
                    // CLONE PASS FIRST - only for craftingStation
                    foreach (var data3 in WMRecipeCust.pieceDatasYml)
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
                    foreach (var data in WMRecipeCust.itemDatasYml) // call items first
                    {
                        try
                        {
                            SetData.SetItemData(data, Instant);
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"SetItem Data for {data.name} failed"); }

                    }
                    Instant.UpdateItemHashes();
                    foreach (var data in WMRecipeCust.pieceDatasYml)
                    {
                        try
                        {
                            SetData.SetPieceRecipeData(data, Instant);
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"SetPiece Data for {data.name} failed"); }
                    }
                    foreach (var data in WMRecipeCust.recipeDatasYml) // recipes last
                    {
                        try
                        {
                            SetData.SetRecipeData(data, Instant);
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"SetRecipe Data for {data.name} failed"); }
                    }
                    
                    // Ignore visual data here for now, this is all JSON related

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
