using HarmonyLib;
using ServerSync;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using wackydatabase.SetData;
using wackydatabase.Datas;
using wackydatabase.Read;
using wackydatabase.OBJimporter;
using System.Security.Policy;
using System.Reflection;
using static CharacterAnimEvent;
using wackydatabase.Util;

namespace wackydatabase.Startup
{

    internal class Startup 
    {

        [HarmonyPatch(typeof(FejdStartup), "SetupObjectDB")]
        static class FejdStartupObjectDBSetup
        {
            static void Postfix()
            {
                if (WMRecipeCust.IsDedServer) return; // dedicated servers don't have to load clones // can load  later

                //if (WMRecipeCust.clonedcache.Value)
                
                WMRecipeCust.Dbgl("Checking Cache Folder and Loading Any Item/Mock Clones");
                ReadFiles clones = new ReadFiles();
                clones.GetCacheClonesOnly();
                SetData.Reload Startup = new SetData.Reload();
                Startup.LoadCacheStatus();
                Startup.LoadClonedCachedItems();


                if (WMRecipeCust.FirstSessionRun) {
 
                    SetData.Reload Startup2 = new SetData.Reload();
                    Startup2.AddClonedItemstoObjectDB();

                }

                if (!WMRecipeCust.FirstSessionRun)
                {
                    WMRecipeCust.FirstSessionRun = true;
                }
                          
            }

        }
        [HarmonyPriority(Priority.HigherThanNormal)]
        [HarmonyPatch(typeof(ZoneSystem), "Start")]
        static class ZoneSystemStart
        {
            static void Prefix()
            {

                if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated()) 
                {
                    WMRecipeCust.WLog.LogInfo("Dedicated with loaded memory");
                    SetData.Reload temp = new SetData.Reload();
                    WMRecipeCust.CurrentReload = temp;

                    if (WMRecipeCust.ServerDedLoad.Value)// Ded with Load
                    {
                        WMRecipeCust.dedLoad = true;
                        WMRecipeCust.context.StartCoroutine(temp.LoadAllRecipeData(true));
                    }
                    else //  DedServer load Memory = false. 
                    {

                        WMRecipeCust.CheckModFolder();
                        WMRecipeCust.Firstrun = false;
                        WMRecipeCust.WLog.LogInfo("Dedicated with no load memory");
                        WMRecipeCust.skillConfigData.Value = WMRecipeCust.ymlstring;
                            
                        
                    }
                }

                if (ZNet.instance.IsServer() && !(ZNet.instance.IsServer() && ZNet.instance.IsDedicated()) ) // COOP and SOLO
                {
                    SetData.Reload temp = new SetData.Reload();
                    WMRecipeCust.CurrentReload = temp;
                    WMRecipeCust.context.StartCoroutine(temp.LoadAllRecipeData(true));
                }
               
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        [HarmonyPriority(Priority.VeryLow)]
        static class ObjectAwake
        {
            static void Postfix()
            {
                if (!WMRecipeCust.modEnabled.Value)
                    return;

                //WMRecipeCust.WLog.LogWarning("objectDB Awake");

                if (WMRecipeCust.dedLoad) // only for dedicatedservers
                    PrepareLoadData();

                if (!WMRecipeCust.FirstSessionRun)
                {
                    return;
                }

                WMRecipeCust.Dbgl("ObjectDB 2nd Awake Load"); // Only adds Gameobjects to ObjectDB

                if (ZNet.instance != null) // for loading from a game to back to main menu
                {
                    WMRecipeCust.Dbgl("ZnetActive in ObjectDB Awake ");
                    if (WMRecipeCust.IsDedServer) { } // dedicated servers don't have to load clones // can load later
                    else
                    {
                        SetData.Reload Startup2 = new SetData.Reload();
                        Startup2.AddClonedItemstoObjectDB();
                       // WMRecipeCust.spawnedinWorld = 1;
                    }

                    if (ZNet.instance.IsServer())
                    {
                        // Only Load if Singleplayer or COOP Server -otherwise need to wait for client
                        PrepareLoadData();

                    }

                    if (!ZNet.instance.IsServer() && WMRecipeCust.HasLobbied    ) // is client now
                    {
                            WMRecipeCust.ForceLogout = true;                       
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        //[HarmonyPriority(Priority.Last)]
        //[HarmonyPriority(Priority.VeryLow)] // might work
        static class ZNetScene_Awake_Patch_LastWackysDatabase
        {
            static void Postfix()
            {
                if (!WMRecipeCust.modEnabled.Value)
                    return;

                //WMRecipeCust.Dbgl("Znet Awake");
                WMRecipeCust.CurrentReload.LoadZDOsForClones();
                

                if (WMRecipeCust.ServerDedLoad.Value && WMRecipeCust.IsDedServer)
                {
                    WMRecipeCust.dedLoad = true;
                    //WMRecipeCust.WLog.LogWarning("This is a dedicated Server wtih ServerDedLoad active");
                }
                    

            }

        }


        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
        public static class FejdStartupPatch
        {
            static void Postfix(FejdStartup __instance)
            {

                if (!WMRecipeCust.IsDedServer) // everyone except ded
                {
                    WMRecipeCust.WLog.LogInfo("extra YML read");
                    WMRecipeCust.context.StartCoroutine(WMRecipeCust.readFiles.GetDataFromFiles(false, true));
                }


            }
        }
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        public static class ZNetAwakeSecurityPatch
        {
            static void Postfix(ZNet __instance)
            {
                WMRecipeCust.ConnectionError = "";
                WMRecipeCust.waitingforFirstLoad = false;
                WMRecipeCust.NoMoreLoading = false;
                IsLocalInstance(__instance);
            }
        }

        [HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
        public static class ZNetPeerInfoSecurityPatch
        {
            static bool Prefix(ZNet __instance, bool __runOriginal)
            {
                if (!__runOriginal)
                {
                    return false;
                }

                if (__instance != null && !__instance.IsServer())
                {
                    return CheckSecurity();
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OpenServer))]
        public static class OpenServerWacky
        {
            static void Postfix(ZNet __instance)
            {
                if (ZNet.m_isServer) {
                    try
                    {

                        if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
                        {
                            gamepassServer();
                           //  WMRecipeCust.WLog.LogInfo("Zplay Lobby");

                        }
                        else if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
                        {
                            //WMRecipeCust.WLog.LogInfo("Steam Lobby ");
                            steamServer();


                        }
                        else
                        {
                            WMRecipeCust.WLog.LogWarning("Server Lobby is active");
                            WMRecipeCust.LobbyRegistered = true;
                            WMRecipeCust.skillConfigData.Value = WMRecipeCust.ymlstring;
                        }
                    } catch { WMRecipeCust.WLog.LogWarning("Steam or Gamepass wasn't found"); }

                    WMRecipeCust.issettoSinglePlayer = false;
                    WMRecipeCust.ConfigSync.CurrentVersion = WMRecipeCust.ModVersion;
                    if (__instance != null && !__instance.IsDedicated() && !WMRecipeCust.IsDedServer)
                    {
                        WMRecipeCust.HasLobbied = true;
                        WMRecipeCust.ForceLogout = true;
                    }
                }

            }

            private static void steamServer()
            {
                WMRecipeCust.WLog.LogWarning("Steam Lobby is active");
                WMRecipeCust.LobbyRegistered = true;
                WMRecipeCust.skillConfigData.Value = WMRecipeCust.ymlstring;
            }

            private static void gamepassServer()
            {
                WMRecipeCust.WLog.LogWarning("Zplay Lobby is active");
                WMRecipeCust.LobbyRegistered = true;
                WMRecipeCust.skillConfigData.Value = WMRecipeCust.ymlstring;
            }

        }

        public static void DestroyStartupItems()
        {
            var delObj = ObjectDB.instance;
            foreach (var citem in WMRecipeCust.ClonedI)
            {
                try
                {
                    var go = DataHelpers.CheckforSpecialObjects(citem);// check for special cases
                    if (go == null)
                        go = delObj.GetItemPrefab(citem); // normal check
                    delObj.m_items.Remove(go);
                    GameObject.Destroy(go);
                }
                catch { WMRecipeCust.Dbgl($"Error Destorying item {citem}"); }

            }
            WMRecipeCust.Dbgl($"Destroyed cloned items used in startup ");
            WMRecipeCust.ClonedI.Clear();
            WMRecipeCust.CurrentReload.UPdateItemHashesWacky(delObj);
            //delObj.UpdateItemHashes();
        }

        public static bool SinglePlayerchecker
        {
            get { return WMRecipeCust.issettoSinglePlayer; }
            set
            {
                WMRecipeCust.issettoSinglePlayer = true;
                return;
            }
        }

        public static bool CheckSecurity()
        {
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                return true;
            }

            if (WMRecipeCust.IsExtraSecurityEnabled && WMRecipeCust.ForceLogout)
            {
                WMRecipeCust.WLog.LogError("You ARE kicked from Multiplayer Servers! " +" 0.0.1");
                WMRecipeCust.ConnectionError =$"{WMRecipeCust.ModName} You started a Local Game before Multiplayer. That is Not allowed. -Restart Game";
                WMRecipeCust.WLog.LogError(WMRecipeCust.ConnectionError);
                if (Game.instance != null)
                {
                    Game.instance.Logout();
                }
                AccessTools.DeclaredField(typeof(ZNet), "m_connectionStatus").SetValue(null, ZNet.ConnectionStatus.ErrorVersion);
                return false;
            }

            return true;
        }

        public static bool IsLocalInstance(ZNet znet)
        {
            if (znet == null)
            {
                return WMRecipeCust.issettoSinglePlayer;
            }

            bool isDedicated = znet.IsDedicated() || WMRecipeCust.IsDedServer;
            bool isOpenServer = ZNet.IsOpenServer() || WMRecipeCust.LobbyRegistered;

            if (znet.IsServer() && !isDedicated)
            {
                if (isOpenServer)
                {
                    WMRecipeCust.issettoSinglePlayer = false;
                    WMRecipeCust.ConfigSync.CurrentVersion = WMRecipeCust.ModVersion; // Just in case goes from singleplayer to hosting.
                    WMRecipeCust.HasLobbied = true;
                    WMRecipeCust.ForceLogout = true;
                }
                else
                {
                    WMRecipeCust.issettoSinglePlayer = true;
                    WMRecipeCust.ForceLogout = true;
                }
            }
            else
            {
                WMRecipeCust.issettoSinglePlayer = false;
                if (!znet.IsServer() && WMRecipeCust.HasLobbied)
                {
                    WMRecipeCust.ForceLogout = true;
                }
            }

            return WMRecipeCust.issettoSinglePlayer;
        }

        [HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(Game), nameof(Game._RequestRespawn))]
        public static class MainReloadStart
        {
            static bool Prefix(Game __instance, bool __runOriginal)
            {
                if (!__runOriginal)
                {
                    return false;
                }

                if (ZNet.instance != null && !ZNet.instance.IsServer() && !CheckSecurity())
                {
                    WMRecipeCust.waitingforFirstLoad = false;
                    return false;
                }

                if (WMRecipeCust.waitingforFirstLoad)
                {
                    WMRecipeCust.waitingforFirstLoad = false;

                    if (ZNet.instance != null && !ZNet.instance.IsServer())
                    {
                        WMRecipeCust.Dbgl($" Now loading SERVER Files");
                        WMRecipeCust.context.StartCoroutine(WMRecipeCust.CurrentReload.LoadAllRecipeData(true)); //  Sync Reload 
                        WMRecipeCust.FirstSS = false; // reset in a destory patch
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
        public static class SpawnPost
        {
            static void Postfix(Game __instance)
            {
                foreach (var item in WMRecipeCust.SnapshotPiecestoDo.ToList())
                {
                    if (Functions.SnapshotPiece(item))
                    {
                        WMRecipeCust.SnapshotPiecestoDo.Remove(item);
                    }
                }

                HandleData.RequestAssetSyncAfterWorldLoad();
            }
        }



        public static void PrepareLoadData()
        {

            WMRecipeCust.ReloadingOkay = true;
            WMRecipeCust.waitingforFirstLoad = true;
        }

    }
}
