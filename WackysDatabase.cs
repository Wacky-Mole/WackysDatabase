// A Lot of the credit goes to  aedenthorn  and all of his Many Mods! https://github.com/aedenthorn/ValheimMods
// Thank you AzumattDev for the template. It is very good https://github.com/AzumattDev/ItemManagerModTemplate
// Thanks to the Odin Discord server, for being active and good for the valheim community.
// Do whatever you want with this mod. // except sale it as per Aedenthorn Permissions https://www.nexusmods.com/valheim/mods/1245
// Goal for this mod is RecipeCustomization + Recipe LVL station Requirement + Server Sync
// Taking from Azu OpenDatabase code and the orginal now. https://www.nexusmods.com/valheim/mods/319?tab=description
// CustomArmor code from https://github.com/aedenthorn/ValheimMods/blob/master/CustomArmorStats/BepInExPlugin.cs
// Thx Aedenthorn again


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
using wackydatabase.SetData;
using wackydatabase.Read;


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
        internal static WMRecipeCust context;
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource WLog =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        internal static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, MinimumRequiredVersion = "2.0.0" }; // it is very picky on version number

        public static ConfigEntry<string> NexusModID;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> isautoreload;
        public static ConfigEntry<bool> isDebugString;
        public static ConfigEntry<string> WaterName;
        public static ConfigEntry<float> globalArmorDurabilityLossMult;
        public static ConfigEntry<float> globalArmorMovementModMult;
        public static ConfigEntry<string> waterModifierName;
        internal static ConfigEntry<bool>? _serverConfigLocked;
        internal static readonly CustomSyncedValue<string> skillConfigData = new(ConfigSync, "skillConfig", ""); // doesn't show up in config

        internal static bool issettoSinglePlayer = false;
        internal static bool isSettoAutoReload = false;
        internal static bool isSetStringisDebug = false;
        internal static bool recieveServerInfo = false;
        internal static bool isDedServer = false;
        internal static bool NoMoreLoading = false; // for shutdown from Server
        internal static bool LoadinMultiplayerFirst = false; // forces multiplayer sync to wait for first time
        internal static string ConnectionError = "";
        internal static bool Firstrun = true;
        internal static bool AwakeHasRun = false;


        public static List<RecipeData_json> recipeDatas = new List<RecipeData_json>();
        public static List<WItemData_json> ItemDatas = new List<WItemData_json>();
        public static List<PieceData_json> PieceDatas = new List<PieceData_json>();
        public static List<ArmorData_json> armorDatas = new List<ArmorData_json>();

        public static List<RecipeData> recipeDatasYml = new List<RecipeData>();
        public static List<WItemData> itemDatasYml = new List<WItemData>();
        public static List<PieceData> pieceDatasYml = new List<PieceData>();
        public static List<ArmorData> armorDatasYml = new List<ArmorData>();
        public static List<VisualData> visualDatasYml = new List<VisualData>();
        public static List<StatusData> effectDataYml = new List<StatusData>();

        public static List<string> ClonedI = new List<string>();
        public static List<string> ClonedP = new List<string>();
        public static List<string> ClonedR = new List<string>();

        internal static string assetPath;
        internal static string assetPathconfig;
        internal static string assetPathItems;
        internal static string assetPathRecipes;
        internal static string assetPathPieces;
        internal static string assetPathVisuals;
        internal static string assetPathOldJsons;
        internal static string assetPathBulkYML;
        internal static string assetPathIcons;
        internal static string assetPathEffects;
        internal static string jsonstring;
        internal static string ymlstring;
        internal static char StringSeparator = 'Ⰴ'; // handcuffs  The fifth letter of the Glagolitic alphabet.

        internal static bool Admin = true; // for single player, sets to false for multiplayer on connect
        public static List<string> pieceWithLvl = new List<string>();

        //bool admin = !ConfigSync.IsLocked;
        // bool admin2 = ConfigSync.IsAdmin;

        internal static GameObject Root;
        public static PieceTable selectedPiecehammer;
        //private static List<string> piecemods = new List<string>();
        public static PieceTable[] MaybePieceStations;
        public static Dictionary<GameObject, GameObject> AdminPiecesOnly;
        public static List<string> RealPieceStations = new List<string>();
        public static List<CraftingStation> NewCraftingStations = new List<CraftingStation>();
        public static Dictionary<string, Material> originalMaterials;
        public static Dictionary<string, GameObject> originalVFX;

        Startupserver startupserver = new Startupserver();
        public static ReadFiles readFiles = new ReadFiles();
        public static Reload CurrentReload = new Reload();
        public static List<string> NoNotTheseSEs= new List<string>() { };

        internal static int kickcount = 0;
        internal static bool jsonsFound = false;



        public void Awake() // start
        {
            StartupConfig(); // startup varables 
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wackysDatabase");
            assetPathconfig = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath + Path.DirectorySeparatorChar), "wackysDatabase");
            assetPathItems = Path.Combine(assetPathconfig, "Items");
            assetPathRecipes = Path.Combine(assetPathconfig, "Recipes");
            assetPathPieces = Path.Combine(assetPathconfig, "Pieces");
            assetPathVisuals = Path.Combine(assetPathconfig, "Visuals");
            assetPathEffects = Path.Combine(assetPathconfig, "Effects");
            assetPathOldJsons = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath + Path.DirectorySeparatorChar), "wackysDatabase-OldJsons");
            assetPathBulkYML = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath + Path.DirectorySeparatorChar), "wackyDatabase-BulkYML");
            assetPathIcons = Path.Combine(assetPathconfig, "Icons");
            // testme(); // function for testing things

            // ending files
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);

            var jsoncount = startupserver.CheckForJsons(); // read jsons for server
            if (jsonsFound)
            {
                WMRecipeCust.WLog.LogWarning("Jsons Found");
                startupserver.BeginConvertingJsons(jsoncount);
            }
            readFiles.GetDataFromFiles(); // YML get
            AwakeHasRun = true;
            skillConfigData.Value = ymlstring; // Shouldn't matter - maybe...

            readFiles.SetupWatcher(); 

            skillConfigData.ValueChanged += CustomSyncEventDetected; // custom sync watcher for yml file synced from server



        }
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? ModName + " " : "") + str);
        }


        private void StartupConfig()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            Root = new GameObject("myroot");
            Root.SetActive(false);
            DontDestroyOnLoad(Root); // clone magic

            // ^^ // starting files
            context = this;
            modEnabled = config<bool>("General", "Enabled", true, "Enable this mod");
            NexusModID = config<string>("General", "NexusModID", "1825", "NexusModID Number", false);
            isDebug = config<bool>("General", "IsDebug", true, "Enable debug logs", false);
            isDebugString = config<bool>("General", "StringisDebug", false, "Do You want to see the String Debug Log - extra logs");
            isautoreload = config<bool>("General", "IsAutoReload", false, new ConfigDescription("Enable auto reload after wackydb_save or wackydb_clone for singleplayer", null, new ConfigurationManagerAttributes { Browsable = false }), false); // not browseable and can only be set before launch
            WaterName = config<string>("Armor", "WaterName", "Water", "Water name for Armor Resistance", false);
            ConfigSync.CurrentVersion = ModVersion;
            if (isDebugString.Value)
                isSetStringisDebug = true;

            WLog.LogDebug("Mod Version " + ConfigSync.CurrentVersion);
            if (isautoreload.Value)
                isSettoAutoReload = true;
            else isSettoAutoReload = false;

        }
        private void OnDestroy()
        {
            Config.Save();
            WLog.LogInfo("Calling the Destroyer of Worlds -End Game");
            //need to unload cloned objects
        }


        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Config.Reload();
            }
            catch { WLog.LogError($"There was an issue loading Config File "); }

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



        private void CustomSyncEventDetected()
        {
            if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated())
            {
                isDedServer = true;
            }
            //  else
            {
                CurrentReload.SyncEventDetected();
              
            }// end is not the server
        }


        internal static void CheckModFolder()
        {
            /*
            if (Directory.Exists(assetPath) && !Directory.Exists(assetPathconfig)) Too OLD NOW, you should have upgraded
            {
                WLog.LogWarning("Creating Config Mod folder and Moving Old WackysDatafolder to configs");
                try { Directory.Move(assetPath, assetPathconfig); } catch { WLog.LogWarning("Error caught,but should have moved wackyDatabase folder correctly though"); }
            } */
            if (!Directory.Exists(assetPathconfig))
            {
                Dbgl("Creating Config Mod folder");
                Directory.CreateDirectory(assetPathconfig);
                Directory.CreateDirectory(assetPathItems);
                Directory.CreateDirectory(assetPathPieces);
                Directory.CreateDirectory(assetPathRecipes);
            }

            if (!Directory.Exists(assetPathIcons))
            {
                Dbgl("Creating Icons Folder");
                Directory.CreateDirectory(assetPathIcons);
            }
            if (!Directory.Exists(assetPathEffects))
            {
                Dbgl("Creating Effects folder");
                Directory.CreateDirectory(assetPathEffects);
            }
        }

        public static void GetAllMaterials()
        {
            Material[] array = Resources.FindObjectsOfTypeAll<Material>();
            originalMaterials = new Dictionary<string, Material>();
            Material[] array2 = array;
            foreach (Material val in array2)
            {
                // Dbgl($"Material {val.name}" );
                originalMaterials[val.name] = val;
            }
        }
    }
}