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
using System.CodeDom.Compiler;

namespace wackydatabase.SetData
{
    public class Reload 
    {
        internal static ItemDrop lastItemSet = null;
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
                if (WMRecipeCust.ssLock)
                {
                    WMRecipeCust.Dbgl($" You recieved SERVER files again before finishing current ones");
                    return;
                }
                WMRecipeCust.WLog.LogDebug("CustomSyncEventDetected was called ");
                WMRecipeCust.Dbgl($" You recieved SERVER Files, so reloading");
                WMRecipeCust.Admin = WMRecipeCust.ConfigSync.IsAdmin;
                WMRecipeCust.recieveServerInfo = true;
                WMRecipeCust.ssLock = true;

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
                WMRecipeCust.cacheItemsYML.Clear();
                WMRecipeCust.creatureDatasYml.Clear();

                WMRecipeCust.MultiplayerApproved.Clear();

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

                    if (WMRecipeCust.FirstSS)
                        WMRecipeCust.waitingforFirstLoad = true; // WMRecipeCust.context.StartCoroutine(Startup.Startup.CleartoReloadWait());
                    else WMRecipeCust.context.StartCoroutine(WMRecipeCust.CurrentReload.LoadAllRecipeData(true, true)); // slow mode

                    //if (firstsyncreload)
                    //  LoadClonesEarly(); // trying to load clones first pass

                    WMRecipeCust.WLog.LogDebug("done with customSyncEvent");
                }
                else
                {
                    WMRecipeCust.WLog.LogWarning("Synced String was blank " + SyncedString);
                }
            }
        }

        public void LoadZDOsForClones()
        {
            ZNetScene znet = ZNetScene.instance;
            if (znet) {

                WMRecipeCust.WLog.LogInfo($"Setting Cloned ZDO Data");
                foreach (var item in WMRecipeCust.MasterCloneList)
                {
                    string name = item.Key;
                    int hash = name.GetStableHashCode();
                    if (znet.m_namedPrefabs.ContainsKey(hash))
                        WMRecipeCust.WLog.LogWarning($"Prefab {name} already in ZNetScene");
                    else
                    {
                        if (item.Value.GetComponent<ZNetView>() != null)
                            znet.m_prefabs.Add(item.Value);
                        else
                            znet.m_nonNetViewPrefabs.Add(item.Value);

                        znet.m_namedPrefabs.Add(hash, item.Value);
                        WMRecipeCust.WLog.LogDebug($"Added prefab {name}");
                    }
                    znet.m_namedPrefabs[hash].gameObject.SetActive(false);
                    
                }         
            }
        }

        public void AddClonedItemstoObjectDB()
        {

            ObjectDB Instant = ObjectDB.instance;
            foreach (var citem in WMRecipeCust.ClonedI)
            {
                if (WMRecipeCust.MasterCloneList.ContainsKey(citem)){
                    if (!Instant.m_itemByHash.TryGetValue(citem.GetStableHashCode(), out var ign))
                    {
                        WMRecipeCust.Dbgl($"Adding {citem} to ObjectDB");
                        Instant.m_items.Add(WMRecipeCust.MasterCloneList[citem]);
                        Instant.m_itemByHash.Add(citem.GetStableHashCode(), WMRecipeCust.MasterCloneList[citem]);
                        WMRecipeCust.MasterCloneList[citem].SetActive(true);

                    }

                }
            }
            foreach (var citem in WMRecipeCust.MockI)
            {
                if (WMRecipeCust.MasterCloneList.ContainsKey(citem))
                 {
                    if (!Instant.m_itemByHash.TryGetValue(citem.GetStableHashCode(), out var ign))
                    {
                        WMRecipeCust.Dbgl($"Adding {citem} to ObjectDB");
                        Instant.m_items.Add(WMRecipeCust.MasterCloneList[citem]);
                        Instant.m_itemByHash.Add(citem.GetStableHashCode(), WMRecipeCust.MasterCloneList[citem]);
                        WMRecipeCust.MasterCloneList[citem].SetActive(true);
                    }
                }
            }

        }

        public void LoadClonedCachedItems(bool WithZdo =false) // cached items and item mocks for main menu
        {
             
            ObjectDB Instant = ObjectDB.instance;
            UPdateItemHashesWacky(Instant);

            //load material cache .mats here

            if (WMRecipeCust.AwakeHasRun && WMRecipeCust.Firstrun)
            {
                WMRecipeCust.CheckModFolder();
                WMRecipeCust.GetAllMaterials();

            }
       
            foreach (var data in WMRecipeCust.cacheItemsYML) 
            {
                bool alreadyexist = false;
                var copy = WMRecipeCust.ClonedI;

                foreach (var citem in copy)
                {
                    if (citem == data.name)
                    {
                        alreadyexist = true;
                        WMRecipeCust.WLog.LogInfo($"Another item named {data.name} has all ready loaded for mainmenu");                       
                    }                                             
                }
                foreach (var citem in WMRecipeCust.MockI)
                {
                    if (citem == data.name)
                    {
                        var testmock = Instant.GetItemPrefab(data.name);
                        if (testmock != null)
                        {
                            alreadyexist = true;
                            WMRecipeCust.WLog.LogInfo($"Another Mock named {data.name} has all ready loaded for mainmenu");
                        }                     
                    }
                }

                if (!alreadyexist)
                {
                    try
                    {
                        GameObject thing = SetData.SetClonedItemsDataCache(data, Instant, false);
                        if (thing != null)
                        {
                            if (WMRecipeCust.MasterCloneList.ContainsKey(data.name))
                                WMRecipeCust.MasterCloneList[data.name] = thing;
                            else
                                WMRecipeCust.MasterCloneList.Add(data.name, thing);
                        }else
                        {
                            WMRecipeCust.WLog.LogInfo($"Wackydb cache item {data.name} was null, so removing from List");
                            WMRecipeCust.MockI.Remove(data.name);

                        }
                    }
                    catch { WMRecipeCust.WLog.LogInfo($"Wackydb cache item {data.name} failed"); }

                    if (data.customVisual != null )
                    {
                        try
                        {
                            VisualController.UpdatePrefab(data.name, data.customVisual);
                        }
                        catch { WMRecipeCust.WLog.LogWarning($"[{WMRecipeCust.ModName}]: Failed to update visuals for {data.name}"); } // spams just catch any empty
                    }
                }
            }

            UPdateItemHashesWacky(Instant);

        }
        internal void UPdateItemHashesWacky(ObjectDB Instant, bool notclones = false)
        {
            GameObject problem = null;
            try
            {
                Instant.m_itemByHash.Clear();
                
                foreach (GameObject item in Instant.m_items)
                {
                    problem = item;
                    Instant.m_itemByHash.Add(item.name.GetStableHashCode(), item);
                }
            }
            catch {
                Instant.m_items.Remove(problem);
                WMRecipeCust.WLog.LogWarning($"Wackydb {problem.name} failed hashes, Please fix yaml or Bug, removing from ObjectDB, rerunning");
                UPdateItemHashesWacky(Instant);
            }
        }

        internal void removeLocalData()
        {
            if (!ZNet.instance.IsServer())// for everyone not the server
            {
                if (WMRecipeCust.extraSecurity.Value)
                {
                    WMRecipeCust.WLog.LogInfo("Removing SinglePlayer Clones not in Multiplayer Server");
                    foreach (var item in WMRecipeCust.MasterCloneList)
                    {
                        if (!WMRecipeCust.MultiplayerApproved.Contains(item.Key))
                        {

                            item.Value.SetActive(false);
                            ObjectDB Instant = ObjectDB.instance;
                            ZNetScene znet = ZNetScene.instance;
                            Instant.m_items.Remove(item.Value);
                            //Instant.m_items.Find(x => x.name == item.Key).SetActive(false);
                            var hash = item.Key.GetStableHashCode();
                            znet.m_prefabs.Remove(item.Value); // removing znets
                            znet.m_namedPrefabs.Remove(hash);
                            //znet.m_prefabs[item.Value].SetActive(false);
                            //
                            //WMRecipeCust.MasterCloneList.Remove(item.Key);
                        }
                    }
                }             
            }
        }

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
            

            if (!WMRecipeCust.ServerDedLoad.Value && WMRecipeCust.IsDedServer)
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
                if (data3 != null && !string.IsNullOrEmpty(data3.clonePrefabName) && data3.craftingStationData != null)
                {
                    try
                    {
                        CraftingStation checkifStation = null;
                        GameObject go = DataHelpers.FindPieceObjectName(data3.clonePrefabName);
                        
                         
                        if (go.TryGetComponent<CraftingStation>(out var tempnam))
                        {
                            checkifStation = DataHelpers.GetCraftingStation(tempnam.m_name); // for forge and other items that change names between item and CraftingStation
                            if (checkifStation != null) // means the prefab being cloned is a craftingStation and needs to proceed
                            {
                                SetData.SetPieceRecipeData(data3, Instant, AllObjects);
                            }
                        }
                    }
                    catch { WMRecipeCust.WLog.LogWarning($"Setting Early Cloned CraftingStation for {data3.name} didn't complete"); } // spams just catch any empty
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

                if(!string.IsNullOrEmpty(data.clonePrefabName) || !string.IsNullOrEmpty(data.mockName))
                {
                    WMRecipeCust.MultiplayerApproved.Add(data.name);
                }

                if (data.customVisual != null)
                {
                    try
                    {
                        VisualController.UpdatePrefab(data.name, data.customVisual);

                        if (DataHelpers.ECheck(data.customIcon))
                        {
                            Functions.SnapshotItem(lastItemSet); // snapshot go
                        }
   
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
            UPdateItemHashesWacky(Instant);
            //Instant.UpdateItemHashes();

            foreach (var data in WMRecipeCust.pieceDatasYml) // clones only first
            {
                if (string.IsNullOrEmpty(data.clonePrefabName))
                    continue;
                try
                {
                    SetData.SetPieceRecipeData(data, Instant, AllObjects);
                }
                catch { WMRecipeCust.WLog.LogWarning($"SetPiece Clone for {data.name} failed"); }
                processcount++;
                if (processcount > WMRecipeCust.ProcessWait && slowmode)
                {
                    yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                    processcount = 0;
                }
            }
            WMRecipeCust.SnapshotPiecestoDo.Clear(); // only add to snapshot once
            foreach (var data in WMRecipeCust.pieceDatasYml)
            {
                try
                {
                    SetData.SetPieceRecipeData(data, Instant, AllObjects);
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
            WMRecipeCust.Dbgl($"Building Cloned Cache for Player Items/Mock");
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

                    if (!string.IsNullOrEmpty(data.mockName))
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

            if (!WMRecipeCust.dedLoad) 
             removeLocalData();

            UPdateItemHashesWacky(ObjectDB.instance);


            WMRecipeCust.WLog.LogInfo($" You finished wackydb reload");

            OnAllReloaded?.Invoke();


            if (OtherApi.Marketplace_API.IsInstalled() && !WMRecipeCust.dedLoad)
            {
                OtherApi.Marketplace_API.ResetTraderItems();
            }
            WMRecipeCust.ssLock = false;


        }      
        public static event Action OnAllReloaded;

    }
}
