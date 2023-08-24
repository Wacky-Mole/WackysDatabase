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
using static ItemSets;
using System.Security.Policy;
using static ItemDrop;

namespace wackydatabase.SetData
{
    public class Reload 
    {

        public void SyncEventDetected()
        {
            // WMRecipeCust.WLog.LogInfo($"Dedicated Sync Detected - remove before release");
            if (ZNet.instance.IsServer())
                return; // no need for a server to get this 

            bool firstsyncreload = false;
            if (WMRecipeCust.Firstrun)
            {
                WMRecipeCust.GetAllMaterials();
                WMRecipeCust.Firstrun = false;
                DataHelpers.GetPieceStations();
                DataHelpers.GetPiecesatStart();
                firstsyncreload = true;
                //LoadinMultiplayerFirst = true; // this is going to require some rewrite
                if (!WMRecipeCust.isDebug.Value)
                    WMRecipeCust.WLog.LogInfo($"Debug is off, which suprisingly, makes it hard to debug");
            }
            if (WMRecipeCust.NoMoreLoading)
            {
                //startupSync++;
                WMRecipeCust.recieveServerInfo = true;
                WMRecipeCust.NoMoreLoading = false;
               // WMRecipeCust.WLog.LogDebug($" No More Loading was true");
                WMRecipeCust.WLog.LogInfo("Warning any modifcations will still be On Your Local Games Until Restart! ");
            }
            else
            {
                WMRecipeCust.WLog.LogDebug("CustomSyncEventDetected was called ");
                WMRecipeCust.Dbgl($" You recieved SERVER Files, so reloading");
                WMRecipeCust.Admin = WMRecipeCust.ConfigSync.IsAdmin;
                WMRecipeCust.recieveServerInfo = true;
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

                WMRecipeCust.pieceWithLvl.Clear(); 
                WMRecipeCust.recipeDatasYml.Clear();
                WMRecipeCust.itemDatasYml.Clear();
                WMRecipeCust.pieceDatasYml.Clear();
                WMRecipeCust.effectDataYml.Clear();
                WMRecipeCust.cacheDataYML.Clear();
                WMRecipeCust.creatureDatasYml.Clear();

                string SyncedString = WMRecipeCust.skillConfigData.Value;
                if (SyncedString != null && SyncedString != "")
                {
                    if(WMRecipeCust.isDebugString.Value)
                        WMRecipeCust.WLog.LogInfo("Synced String was  " + SyncedString);

                    var deserializer = new DeserializerBuilder()
                        .WithTypeConverter(new ColorConverter())
                        .WithTypeConverter(new ValheimTimeConverter())
                        .IgnoreUnmatchedProperties() // future proofing
                        .Build(); // make sure to include all

                    string[] yml = SyncedString.Split(WMRecipeCust.StringSeparator);
                    foreach (var word in yml) 
                    {
                        if (word.Contains("m_weight")) //item
                        {
                            WMRecipeCust.itemDatasYml.Add(deserializer.Deserialize<WItemData>(word));
                        }
                        else if (word.Contains("piecehammer")) // only piece
                        {
                            WMRecipeCust.pieceDatasYml.Add(deserializer.Deserialize<PieceData>(word));
                        }
                        else if (word.Contains("reqs"))// only recipes
                        {
                            WMRecipeCust.recipeDatasYml.Add(deserializer.Deserialize<RecipeData>(word));
                        }else if (word.Contains("Status_m_name"))
                        {
                            WMRecipeCust.effectDataYml.Add(deserializer.Deserialize<StatusData>(word));
                        }
                        else if (word.Contains("mob_display_name"))
                        {
                        WMRecipeCust.creatureDatasYml.Add(deserializer.Deserialize<CreatureData>(word));
                        }

                }
                    if (WMRecipeCust.LoadinMultiplayerFirst)
                    {
                        WMRecipeCust.LoadinMultiplayerFirst = false; // Only for first Load in on Multiplayer, Keeps Mutliplayer loading last 
                        WMRecipeCust.Dbgl($" Delaying Server Reloading Until very end");
                        return;
                    }

                    WMRecipeCust.context.StartCoroutine(Startup.Startup.CleartoReload());

                    //if (firstsyncreload)
                      //  LoadClonesEarly(); // trying to load clones first pass

                    WMRecipeCust.WLog.LogDebug("done with customSyncEvent");
                }
                else
                {
                    WMRecipeCust.WLog.LogDebug("Synced String was blank " + SyncedString);
                }
            }
        }


        public void LoadClonedCachedItems() // cached items for main menu only // don't load znets obviously 
        {
            if (WMRecipeCust.IsServer && WMRecipeCust.isDedServer) return;
            ObjectDB Instant = ObjectDB.instance;

            //load material cache .mats here
            
            foreach (var mat in WMRecipeCust.cacheMaterials) // materials last
            {

            }

            foreach (var data in WMRecipeCust.cacheDataYML) // recipes last
            {
                bool alreadyexist = false;
                foreach (var citem in WMRecipeCust.ClonedI)
                {
                    if (citem == data.name)
                    {
                        alreadyexist = true;
                    }
                                              
                }
                if (!alreadyexist)
                {
                    try
                    {
                        SetData.SetClonedItemsDataCache(data, Instant);// has issues
                    }
                    catch { WMRecipeCust.WLog.LogInfo($"Wackydb cache item {data.name} failed"); }

                    if (data.customVisual != null)
                    {
                        try
                        {
                            VisualController.UpdatePrefab(data.name, data.customVisual);
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"[{WMRecipeCust.ModName}]: Failed to update visuals for {data.name}"); } // spams just catch any empty
                    }
                }
            }
            try
            {
                Instant.UpdateItemHashes();
            }
            catch { WMRecipeCust.WLog.LogWarning($"Wackydb Update ItemHashes on cloned items failed, this could cause problems"); }

        }

        internal void LoadClonedItemsOnlyEarly(ObjectDB Instance)
        {
            if (WMRecipeCust.AwakeHasRun && WMRecipeCust.Firstrun)
            {
                WMRecipeCust.CheckModFolder();
                WMRecipeCust.GetAllMaterials();
                DataHelpers.GetPieceStations();
                DataHelpers.GetPiecesatStart();
                //WMRecipeCust.Firstrun = false; run again for final pickups
            }

            WMRecipeCust.WLog.LogInfo($"Loading Cloned Items");
            foreach (var data3 in WMRecipeCust.itemDatasYml)
            {
                if (data3 != null && !string.IsNullOrEmpty(data3.clonePrefabName) && !WMRecipeCust.ClonedI.Contains(data3.name))
                {
                    try
                    {
                        SetData.SetItemData(data3, Instance, null, false);

                    }
                    catch { WMRecipeCust.WLog.LogWarning($"Set Item Data for {data3.name} failed, might get it on second pass"); } // spams just catch any empty

                    if (data3.customVisual != null)
                    {
                        try
                        {
                            VisualController.UpdatePrefab(data3.name, data3.customVisual); // load clones early
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"[{WMRecipeCust.ModName}]: Failed to update visuals for {data3.name}"); } // spams just catch any empty
                    }
                }
            }

        }

        internal void LoadClonesEarly() // not working
        {
            if (WMRecipeCust.AwakeHasRun && WMRecipeCust.Firstrun)
            {
                WMRecipeCust.CheckModFolder();
                WMRecipeCust.GetAllMaterials();
                DataHelpers.GetPieceStations();
                DataHelpers.GetPiecesatStart();
                //WMRecipeCust.Firstrun = false; run again for final pickups
            }

            ObjectDB Instant = ObjectDB.instance;
            
            WMRecipeCust.WLog.LogInfo($"Loading Cloned CraftingStation");

            foreach (var data1 in WMRecipeCust.pieceDatasYml)
            {
                if (data1 != null && !string.IsNullOrEmpty(data1.clonePrefabName))
                {
                    try
                    {
                        CraftingStation checkifStation = null;
                        GameObject go = DataHelpers.FindPieceObjectName(data1.clonePrefabName);
                        string tempnam = null;
                        tempnam = go.GetComponent<CraftingStation>()?.m_name;
                        if (tempnam != null)
                        {
                            checkifStation = DataHelpers.GetCraftingStation(tempnam); // for forge and other items that change names between item and CraftingStation
                            if (checkifStation != null) // means the prefab being cloned is a craftingStation and needs to proceed
                            {
                                SetData.SetPieceRecipeData(data1, Instant);
                            }
                        }
                    }
                    catch { WMRecipeCust.WLog.LogWarning($"SetPiece CraftingStation for {data1.name} failed, might get it on second pass"); } // spams just catch any empty
                }
            }
            WMRecipeCust.WLog.LogInfo($"Loading SEs");
            foreach (var data in WMRecipeCust.effectDataYml) // recipes last
            {
                try
                {
                    SetData.SetStatusData(data, Instant);// has issues
                }
                catch { WMRecipeCust.WLog.LogWarning($"SetEffect  {data.Name} failed"); }
            }
                        // Broken for now
             // This was a failed project to get cloned items in earlier so other mods could touch them easier, but failed
            // it failed because it couldn't init after item pickup by player. So spawn in, pickup, drop error on object init. No idea why, but works after teh .1f delay
         /*   
            WMRecipeCust.WLog.LogInfo($"Loading Cloned Items");
            foreach (var data3 in WMRecipeCust.itemDatasYml)
            {
                if (data3 != null && !string.IsNullOrEmpty(data3.clonePrefabName) && !WMRecipeCust.ClonedI.Contains(data3.name))
                {
                    try
                    {
                        SetData.SetItemData(data3, Instant, null, false);

                    }
                    catch { WMRecipeCust.WLog.LogWarning($"Set Item Data for {data3.name} failed, might get it on second pass"); } // spams just catch any empty

                    if (data3.customVisual != null)
                    {
                        try
                        {
                            VisualController.UpdatePrefab(data3.name, data3.customVisual); // load clones early
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"[{WMRecipeCust.ModName}]: Failed to update visuals for {data3.name}"); } // spams just catch any empty
                    }
                }
            }
            
            try
            {
                Instant.UpdateItemHashes();
            }
            catch
            {
                WMRecipeCust.WLog.LogWarning($"Wackydb Update ItemHashes on cloned items failed, this could cause problems");
            }


            


            
            WMRecipeCust.WLog.LogInfo($"Loading Cloned Pieces");
            foreach (var data2 in WMRecipeCust.pieceDatasYml)
            {
                if (data2 != null && !string.IsNullOrEmpty(data2.clonePrefabName) && !WMRecipeCust.ClonedP.Contains(data2.name))
                {
                    try
                    {
                      SetData.SetPieceRecipeData(data2, Instant);                                      
                    }
                    catch { WMRecipeCust.WLog.LogWarning($"SetPiece Data for {data2.name} failed, might get it on second pass"); } // spams just catch any empty
                }
            }
            
            /*
            WMRecipeCust.WLog.LogInfo($"Loading Cloned Recipes");
            foreach (var data4 in WMRecipeCust.recipeDatasYml)
            {
                if (data4 != null && !string.IsNullOrEmpty(data4.clonePrefabName) && !WMRecipeCust.ClonedR.Contains(data4.name))
                {
                    try
                    {
                        SetData.SetRecipeData(data4, Instant);
                    }
                    catch { WMRecipeCust.WLog.LogWarning($"SetPiece Data for {data4.name} failed, might get it on second pass"); } // spams just catch any empty
                }
            } */ // No reason to do recipes early
        }

        /*
        internal void reloadDropPrefab()
        {
            if (WMRecipeCust.WaitList.Count > 0)
            {
                WMRecipeCust.WLog.LogInfo("Reloading DropPrefabs for early clones");

                foreach (var obj in WMRecipeCust.WaitList)
                {
                    if (obj.Key != null)
                    {
                        if ( obj.Key.GetComponent<ItemDrop>() != null)
                        {
                            var wa = obj.Key;
                            var wac = wa.GetComponent<ItemDrop>();
                            wac = obj.Value;

                        }else
                        {
                            WMRecipeCust.WLog.LogWarning("itemdrop is null");
                        }
                    }
                    
                }
                WMRecipeCust.WaitList.Clear();
            }

        }
       
        internal void FinishZnetObjects()
        {
            ZNetScene znet = ZNetScene.instance;
            if (WMRecipeCust.WaitList.Count > 0)
            {
                WMRecipeCust.WLog.LogInfo("Adding Znets from ZnetWaitlists");

                foreach (var obj in WMRecipeCust.WaitList)
                {
                    string name = obj.name;
                    var hash = name.GetStableHashCode();

                    if (znet.m_namedPrefabs.ContainsKey(hash))
                        WMRecipeCust.WLog.LogWarning($"Prefab {name} already in ZNetScene");
                    else
                    {
                        if (obj.GetComponent<ZNetView>() != null)
                            znet.m_prefabs.Add(obj);
                        else
                            znet.m_nonNetViewPrefabs.Add(obj);

                        znet.m_namedPrefabs.Add(hash, obj);
                        WMRecipeCust.Dbgl($"Added prefab {name}");
                    }

                }
                WMRecipeCust.WaitList.Clear();
            }
        }
        */
        internal IEnumerator LoadAllRecipeData(bool reload, bool slowmode = false) // same as LoadAllRecipeData except broken into chunks// maybe replace?
        {

            if (reload)
            {
                ZNet Net = new ZNet();
                Startup.Startup.IsLocalInstance(Net);
            }

            

            if (WMRecipeCust.AwakeHasRun && WMRecipeCust.Firstrun)
            {
                WMRecipeCust.CheckModFolder();
                WMRecipeCust.GetAllMaterials();
                DataHelpers.GetPieceStations();
                DataHelpers.GetPiecesatStart();
                WMRecipeCust.Firstrun = false;
            }


            if (reload && (WMRecipeCust.issettoSinglePlayer || WMRecipeCust.recieveServerInfo || WMRecipeCust.LobbyRegistered)) // single player only or recievedServerInfo
            {
                if (WMRecipeCust.recieveServerInfo && WMRecipeCust.issettoSinglePlayer) // Might remove this, this check might interfere with my existing checks and is not needed
                {
                    WMRecipeCust.WLog.LogWarning($" You Loaded into Singleplayer local first and therefore will NOT be allowed to reload Server Configs");
                    yield break; // naughty boy no recipes for you
                }
                else
                {
                    if (!WMRecipeCust.ServerDedLoad.Value && ZNet.instance.IsServer() && ZNet.instance.IsDedicated())
                        yield break;
                    ObjectDB Instant = ObjectDB.instance;
                    GameObject[] AllObjects = Resources.FindObjectsOfTypeAll<GameObject>(); // this is going slow down things

                    if (slowmode)
                    {
                        while (WMRecipeCust.LockReload)
                        {
                            yield return new WaitForSeconds(1f);
                        }

                        WMRecipeCust.WLog.LogInfo($"Beginning SLOW Update");

                    }
                    else
                        WMRecipeCust.WLog.LogInfo($"Beginning Update");

                    int processcount = 0;
                    // effects first
                    foreach (var data in WMRecipeCust.effectDataYml) // recipes last
                    {

                        try
                        {
                            SetData.SetStatusData(data, Instant);// has issues
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"SetEffect  {data.Name} failed"); }

                        processcount++;
                        if (processcount > WMRecipeCust.ProcessWait && slowmode)
                        {
                            yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                            processcount = 0;
                        }
                    }

                    WMRecipeCust.WLog.LogInfo($" Set Effects Loaded");
                    // CLONE PASS FIRST - only for craftingStation
                    foreach (var data3 in WMRecipeCust.pieceDatasYml)
                    {
                        if (data3 != null && !string.IsNullOrEmpty(data3.clonePrefabName))
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
                        processcount++;
                        if (processcount > WMRecipeCust.ProcessWait && slowmode)
                        {
                            yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                            processcount = 0;
                        }
                    }
                    // END CLONE PASS
                    // Real PASS NOW
                    foreach (var data in WMRecipeCust.itemDatasYml) // call items first
                    {
                        try
                        {
                            SetData.SetItemData(data, Instant, AllObjects,true );
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"SetItem Data for {data.name} failed"); }

                        if (data.customVisual != null)
                        {
                            try
                            {
                                VisualController.UpdatePrefab(data.name, data.customVisual);
                            }
                            catch { WMRecipeCust.WLog.LogWarning($"[{WMRecipeCust.ModName}]: Failed to update visuals for {data.name}"); } // spams just catch any empty
                        }

                            processcount++;
                        if (processcount > WMRecipeCust.ProcessWait && slowmode)
                        {
                            yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                            processcount = 0;
                        }
                    }
                    Instant.UpdateItemHashes();
                    foreach (var data in WMRecipeCust.pieceDatasYml)
                    {
                        try
                        {
                            SetData.SetPieceRecipeData(data, Instant);
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"SetPiece Data for {data.name} failed"); }
                        processcount++;
                        if (processcount > WMRecipeCust.ProcessWait && slowmode )
                        {
                            yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                            processcount = 0;
                        }
                    }
                    foreach (var data in WMRecipeCust.recipeDatasYml) // recipes last
                    {
                        try
                        {
                            SetData.SetRecipeData(data, Instant);
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"SetRecipe Data for {data.name} failed"); }
                        processcount++;
                        if (processcount > WMRecipeCust.ProcessWait && slowmode)
                        {
                            yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                            processcount = 0;
                        }
                    }
                    WMRecipeCust.Dbgl($"Setting Creatures ");
                    
                    foreach ( var data in WMRecipeCust.creatureDatasYml)
                    {
                        try
                        {
                           // WMRecipeCust.WLog.LogWarning($"SetRecipe Data for {data.name} ");
                            SetData.SetCreature(data, AllObjects);
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"Set Creature for {data.name} failed"); }

                        processcount++;
                        if (processcount > WMRecipeCust.ProcessWait && slowmode)
                        {
                            yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                            processcount = 0;
                        }
                    }

                    //string currentplayer = Player.m_localPlayer.name;// save item cache
                    WMRecipeCust.Dbgl($"Building Cache for Player ");
                    var serializer = new SerializerBuilder()
                                .Build();
                    var rand = new System.Random();
                    foreach (var data in WMRecipeCust.itemDatasYml)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(data.clonePrefabName))
                            {
                                int hash = data.name.GetStableHashCode(); // hash for the name now
                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathCache, "_" + hash + ".zz"), serializer.Serialize(data));
                            }
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"Item Cache save for {data.name} failed"); }
                        processcount++;
                        if (processcount > WMRecipeCust.ProcessWait && slowmode )
                        {
                            yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                            processcount = 0;
                        }
                    }
                    // textures don't need to be cached because always on player comp
                    /* future rexabtye caching

                    foreach (var data in WMRecipeCust.assetPathMaterials) //materials
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(data.clonePrefabName))
                            {
                                var hash = data.GetHashCode(); // rand.Next(501032334)
                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathCache, "_" + hash + ".zz"), serializer.Serialize(data));
                            }
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"Item Cache save for {data.name} failed"); }
                        processcount++;
                        if (processcount > WMRecipeCust.ProcessWait && slowmode)
                        {
                            yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                            processcount = 0;
                        }
                    }
                    */

                    try
                    {
                        ObjectDB.instance.UpdateItemHashes();
                    }
                    catch
                    {
                        WMRecipeCust.Dbgl($"failed to update Hashes- probably error in files");
                    }

                    WMRecipeCust.Dbgl($" You finished wackydb reload");

                    OnAllReloaded?.Invoke();

                    //FinishZnetObjects();
                    //reloadDropPrefab();

                    if (OtherApi.Marketplace_API.IsInstalled())
                    {
                        OtherApi.Marketplace_API.ResetTraderItems();
                    }
                }

            }
            else
            {
                if (WMRecipeCust.issettoSinglePlayer)
                {
                    WMRecipeCust.Dbgl($" You did NOT reload Files. You probably should have.");
                }
            }
                      

        }

        

        public static event Action OnAllReloaded;


    }
}
