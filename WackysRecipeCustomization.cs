// A Lot of the credit goes to  aedenthorn  and all of his Many Mods! https://github.com/aedenthorn/ValheimMods
// Thank you AzumattDev for the template. It is very good https://github.com/AzumattDev/ItemManagerModTemplate
// Thanks to the Odin Discord server, for being active and good for the valheim community.
// Do whatever you want with this mod. // except sale it as per Aedenthorn Permissions https://www.nexusmods.com/valheim/mods/1245
// Goal for this mod is RecipeCustomization + Recipe LVL station Requirement + Server Sync
// Taking from Azu OpenDatabase code and the orginal now. https://www.nexusmods.com/valheim/mods/319?tab=description
// CustomArmor code from https://github.com/aedenthorn/ValheimMods/blob/master/CustomArmorStats/BepInExPlugin.cs
// Thx Aedenthorn again
/*
 * i wouldn't say there is a single bottleneck that you can remove and everything is fine.
but lots of small stuff that can be improved.
you iterate the entire object db for each item, just to find an item with a matching name.
you instantiate every item.
you call update item hashes for each item.
so, it's mostly suffering, because you reload everything for each update. */

using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ItemManager;
using ServerSync;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using System.Linq;
using PieceManager;
//using System.Security.Cryptography;
using System.Text;
//using UnityEngine.SceneManagement;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection.Emit;
using YamlDotNet;
using wackydatabase.Datas;
using Object = UnityEngine.Object;
using wackydatabase.Startup;
using wackydatabase.Util;


namespace wackydatabase
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class WMRecipeCust : BaseUnityPlugin
    {
        internal const string ModName = "WackysDatabase";
        internal const string ModVersion = "2.0.0";
        internal const string Author = "WackyMole";
        internal const string ModGUID = Author + "." + ModName;
        internal static string ConfigFileName = ModGUID + ".cfg";
        internal static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        public static ConfigEntry<string> NexusModID;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> isautoreload;
        public static ConfigEntry<bool> isDebugString;
        public static ConfigEntry<string> WaterName;
        internal static bool issettoSinglePlayer = false;
        internal static bool isSettoAutoReload;
        internal static bool isSetStringisDebug = false;
        internal static bool recieveServerInfo = false;
        internal static bool isDedServer = false;
        internal static bool NoMoreLoading = false; // for shutdown from Server
        internal static bool LoadinMultiplayerFirst = false; // forces multiplayer sync to wait for first time
        internal static string ConnectionError = "";
        internal static WMRecipeCust context;
        //private static int kickcount = 0;

        public static ConfigEntry<float> globalArmorDurabilityLossMult;
        public static ConfigEntry<float> globalArmorMovementModMult;
        public static ConfigEntry<string> waterModifierName;

        public static List<RecipeData> recipeDatas = new List<RecipeData>();
        public static List<WItemData> ItemDatas = new List<WItemData>();
        public static List<PieceData> PieceDatas = new List<PieceData>();
        public static List<ArmorData> armorDatas = new List<ArmorData>();// load with others
        public static List<string> ClonedI = new List<string>();
        public static List<string> ClonedP = new List<string>();
        public static List<string> ClonedR = new List<string>();
        internal static string assetPath;
        internal static string assetPathconfig;
        internal static string assetPathItems;
        internal static string assetPathRecipes;
        internal static string assetPathPieces;
        internal static string jsonstring;
        internal static bool Admin = true; // for single player, sets to false for multiplayer on connect
        internal static List<string> pieceWithLvl = new List<string>();
        //bool admin = !ConfigSync.IsLocked;
        // bool admin2 = ConfigSync.IsAdmin;
        internal static GameObject Root;
        internal static bool Firstrun = true;
        internal static PieceTable selectedPiecehammer;
        //private static List<string> piecemods = new List<string>();
        internal static PieceTable[] MaybePieceStations;
        internal static Dictionary<GameObject, GameObject> AdminPiecesOnly;
        public static List<string> RealPieceStations = new List<string>();
        public static List<CraftingStation> NewCraftingStations = new List<CraftingStation>();

        private enum NewDamageTypes
        {
            Water = 1024
        }



        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource WackysRecipeCustomizationLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        internal static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, MinimumRequiredVersion = "2.0.0" }; // it is very picky on version number



        public void Awake() // start
        {
            StartupConfig(); // startup varables 
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wackysDatabase");
            assetPathconfig = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath + Path.DirectorySeparatorChar), "wackysDatabase");
            assetPathItems = Path.Combine(assetPathconfig, "Items");
            assetPathRecipes = Path.Combine(assetPathconfig, "Recipes");
            assetPathPieces = Path.Combine(assetPathconfig, "Pieces");
            // testme(); // function for testing things

            // ending files
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher(); // so if files change after startup it reloads recipes/ but doesn't input them.
            startupserver startupserver = new startupserver();
            startupserver.GetRecipeDataFromFilesForServer();
            skillConfigData.ValueChanged += CustomSyncEventDetected; // custom watcher for json file synced from server



        }


        internal static ConfigEntry<bool>? _serverConfigLocked;
        internal static readonly CustomSyncedValue<string> skillConfigData = new(ConfigSync, "skillConfig", ""); // doesn't show up in config

        private void StartupConfig()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            Root = new GameObject("myroot");
            Root.SetActive(false);
            DontDestroyOnLoad(Root);

            // ^^ // starting files
            context = this;
            modEnabled = config<bool>("General", "Enabled", true, "Enable this mod");
            NexusModID = config<string>("General", "NexusModID", "1825", "NexusModID Number", false);
            isDebug = config<bool>("General", "IsDebug", true, "Enable debug logs", false);
            isDebugString = config<bool>("General", "StringisDebug", false, "Do You want to see the String Debug Log - extra logs");
            isautoreload = config<bool>("General", "IsAutoReload", false, new ConfigDescription("Enable auto reload after wackydb_save or wackydb_clone for singleplayer", null, new ConfigurationManagerAttributes { Browsable = false }), false); // not browseable and can only be set before launch
            //isSinglePlayer = config<bool>("General", "IsSinglePlayerOnly", false, new ConfigDescription("Allow Single Player- Must be off for Multiplayer", null, new ConfigurationManagerAttributes { Browsable = false }), false); // doesn't allow you to connect if set to true
            WaterName = config<string>("Armor", "WaterName", "Water", "Water name for Armor Resistance", false);
            ConfigSync.CurrentVersion = ModVersion;
            if (isDebugString.Value)
                isSetStringisDebug = true;

            WackysRecipeCustomizationLogger.LogDebug("Mod Version " + ConfigSync.CurrentVersion);
            if (isautoreload.Value)
                isSettoAutoReload = true;
            else isSettoAutoReload = false;

        }
        private void OnDestroy()
        {
            Config.Save();
            WackysRecipeCustomizationLogger.LogWarning("Calling the Destoryer of Worlds -End Game");
            //need to unload cloned objects
        }

        private void SetupWatcher()
        {
            CheckModFolder();
            FileSystemWatcher watcher = new(assetPathconfig); // jsons in config
            watcher.Changed += ReadJsonValues;
            watcher.Created += ReadJsonValues;
            watcher.Renamed += ReadJsonValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Config.Reload();
            }
            catch { WackysRecipeCustomizationLogger.LogError($"There was an issue loading Config File "); }

        }
        private void ReadJsonValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated() || issettoSinglePlayer && isSettoAutoReload)
                {  // should only load for the server now
                    Dbgl("Jsons files have changed and access is either on a dedicated server or singleplayer with autoreload on therefore reloading everything");
                    GetRecipeDataFromFiles(); // load stuff in mem
                    skillConfigData.Value = jsonstring; //Sync Event // Single player forces client to reload as well. 
                }
            }
            catch
            {
                //WackysRecipeCustomizationLogger.LogError($"There was an issue loading your Sync ");
                if (issettoSinglePlayer)
                    WackysRecipeCustomizationLogger.LogError("Please check your JSON entries for spelling and format!");
                else
                {
                    WackysRecipeCustomizationLogger.LogDebug("Not checking Json Files because either in Main Screen or ....");
                }
            }
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }


        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        private static Dictionary<string, Material> originalMaterials;
        private static Dictionary<string, GameObject> originalVFX;
        private void CustomSyncEventDetected()
        {
            if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated())
            {
                isDedServer = true;
            }
            //  else
            {

                if (Firstrun)
                {
                    GetAllMaterials();
                    Firstrun = false;
                    GetPieceStations();
                    GetPiecesatStart();
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
                        if(LoadinMultiplayerFirst)
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
                                    SetItemData(data2, Instant);
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
                                    SetPieceRecipeData(data3, Instant);
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

                                    SetRecipeData(data, Instant);
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
            }// end is not the server
        }


        internal static void CheckModFolder()
        {
            if (Directory.Exists(assetPath) && !Directory.Exists(assetPathconfig))
            {
                WackysRecipeCustomizationLogger.LogWarning("Creating Config Mod folder and Moving Old WackysDatafolder to configs");
                try { Directory.Move(assetPath, assetPathconfig); } catch { WackysRecipeCustomizationLogger.LogWarning("Error caught,but should have moved wackyDatabase folder correctly though"); }
            }
            if (!Directory.Exists(assetPathconfig))
            {
                Dbgl("Creating Config Mod folder");
                Directory.CreateDirectory(assetPathconfig);
                Directory.CreateDirectory(assetPathItems);
                Directory.CreateDirectory(assetPathPieces);
                Directory.CreateDirectory(assetPathRecipes);
            }
        }
    }
}