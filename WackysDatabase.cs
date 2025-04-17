﻿// Thank you AzumattDev for the template. It is very good https://github.com/AzumattDev/ItemManagerModTemplate
// Thanks to the Odin Discord server, for being active and good for the valheim community.
// Taking from Azu OpenDatabase code and the old one now. https://www.nexusmods.com/valheim/mods/319?tab=description
// CustomArmor code from https://github.com/aedenthorn/ValheimMods/blob/master/CustomArmorStats/BepInExPlugin.cs
// Thank you to Rexabit for Visual Modifier - It really is an amazing mod.

using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using System.Linq;
using PieceManager;
using System.Text;
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
using UnityEngine.Rendering;
using API;
using wackydatabase.OBJimporter;
using RainbowTrollArmor;
using System.Runtime.CompilerServices;
using LocalizationManager;

namespace wackydatabase
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]

    public class WMRecipeCust : BaseUnityPlugin
    {
        internal const string ModName = "WackysDatabase";
        internal const string ModVersion = "2.4.53";
        internal const string Author = "WackyMole";
        internal const string ModGUID = Author + "." + ModName;
        internal static string ConfigFileName = ModGUID + ".cfg";
        internal static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static WMRecipeCust context;
        internal readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource WLog =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        internal static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, MinimumRequiredVersion = "2.4.53" }; // it is very picky on version number

        public static ConfigEntry<string> NexusModID;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> isautoreload;
        public static ConfigEntry<bool> isDebugString;
        public static ConfigEntry<string> WaterName;
        public static ConfigEntry<float> globalArmorDurabilityLossMult;
        public static ConfigEntry<float> globalArmorMovementModMult;
        public static ConfigEntry<string> waterModifierName;
        public static ConfigEntry<bool> ServerDedLoad;
        public static ConfigEntry<bool> extraSecurity;
        public static ConfigEntry<bool> enableYMLWatcher;
        public static ConfigEntry<bool> clonedcache;
        public static ConfigEntry<string> extraEffectList;
        internal static ConfigEntry<bool>? _serverConfigLocked;
        internal static readonly CustomSyncedValue<string> skillConfigData = new(ConfigSync, "skillConfig", ""); // doesn't show up in config
        internal static readonly CustomSyncedValue<string> largeTransfer = new(ConfigSync, "largeTransfer", ""); // Experimental
        public static readonly ConditionalWeakTable<Piece.Requirement, RequirementQuality> requirementQuality = new();

        internal static bool issettoSinglePlayer = false;
        internal static bool isSettoAutoReload = false;
        internal static bool recieveServerInfo = false;
        internal static bool NoMoreLoading = false; // for shutdown from Server
        internal static bool LoadinMultiplayerFirst = false; // forces multiplayer sync to wait for first time
        internal static string ConnectionError = "";
        internal static bool Firstrun = true;
        internal static bool AwakeHasRun = false;
        internal static bool FirstSessionRun = false;
        internal static bool FirstSS = true;
        internal static int spawnedinWorld = 0;
        internal static bool dedLoad = false;
        internal static bool ssLock = false;
        internal static int ssLockcount = 0;
        internal static bool waitingforFirstLoad = false;

        public static List<RecipeData_json> recipeDatas = new List<RecipeData_json>();
        public static List<WItemData_json> ItemDatas = new List<WItemData_json>();
        public static List<PieceData_json> PieceDatas = new List<PieceData_json>();
        public static List<ArmorData_json> armorDatas = new List<ArmorData_json>();

        public static List<RecipeData> recipeDatasYml = new List<RecipeData>();
        public static List<WItemData> itemDatasYml = new List<WItemData>();
        public static List<PieceData> pieceDatasYml = new List<PieceData>();
        public static List<ArmorData> armorDatasYml = new List<ArmorData>();
        public static List<StatusData> effectDataYml = new List<StatusData>();
        public static List<CreatureData> creatureDatasYml = new List<CreatureData>();
        public static List<PickableData> pickableDatasYml = new List<PickableData>();
        public static List<TreeBaseData> treebaseDatasYml = new List<TreeBaseData>();
        public static List<WItemData> cacheItemsYML = new List<WItemData>();// cacheonly
        public static List<MaterialInstance> cacheMaterials = new List<MaterialInstance>();// cacheonly
        public static List<StatusData> cacheStatusYML = new List<StatusData>();// cacheonly

        public static List<string> ClonedI = new List<string>(); // items
        public static List<string> ClonedINoZ = new List<string>(); // items No Znet // not used
        public static Dictionary<string, GameObject> MasterCloneList = new Dictionary<string, GameObject>();
        public static readonly Dictionary<string, string> ClonedPrefabsMap = new();
        public static List<string> ClonedP = new List<string>(); // pieces
        public static List<string> ClonedR = new List<string>(); // recipes
        public static List<string> ClonedE = new List<string>(); // effects
        public static List<string> ClonedPI = new List<string>(); // pickables
        public static List<string> ClonedPTB = new List<string>(); // picktreebase
        public static List<string> ClonedC = new List<string>(); // creatures
        public static Dictionary<string, GameObject> ClonedCC = new Dictionary<string, GameObject>(); //Cloned creatureDick
        public static List<string> ClonedCR = new List<string>(); // creaturesReplacer
        public static List<string> MockI = new List<string>(); // MockItems
        public static List<string> BlacklistClone = new List<string>();
        public static List<string> MultiplayerApproved = new List<string>();
        public static List<GameObject> SnapshotPiecestoDo = new List<GameObject>();
        // internal static Dictionary<string, Texture2D> mainTextures = new Dictionary<string, Texture2D>(); // cached Textures for fun

        internal static string assetPath;
        internal static string assetPathconfig;
        internal static string assetPathItems;
        internal static string assetPathRecipes;
        internal static string assetPathPieces;
        internal static string assetPathTextures;
        internal static string assetPathMaterials;
        internal static string assetPathObjects;
        internal static string assetPathCreatures;
        internal static string assetPathPickables;
        internal static string assetPathOldJsons;
        internal static string assetPathBulkYML;
        internal static string assetPathBulkYMLItems;
        internal static string assetPathBulkYMLPieces;
        internal static string assetPathBulkYMLEffects;
        internal static string assetPathBulkYMLRecipes;
        internal static string assetPathBulkYMLCreatures;
        internal static string assetPathBulkYMLPickables;
        internal static string assetPathIcons;
        internal static string assetPathEffects;
        internal static string assetPathCache;
        internal static string jsonstring;
        internal static string ymlstring;
        internal static char StringSeparator = 'Ⰴ'; // handcuffs  The fifth letter of the Glagolitic alphabet.

        internal static bool Admin = true; // for single player, sets to false for multiplayer on connect
        public static List<string> pieceWithLvl = new List<string>();

        internal static GameObject Root;
        internal static GameObject MockItemBase;
        public static PieceTable selectedPiecehammer;

        //private static List<string> piecemods = new List<string>();
        public static PieceTable[] MaybePieceStations;

        public static Dictionary<GameObject, GameObject> AdminPiecesOnly;
        public static List<string> RealPieceStations = new List<string>();
        public static List<CraftingStation> NewCraftingStations = new List<CraftingStation>();
        public static Dictionary<string, Material> originalMaterials;
        public static Dictionary<string, GameObject> originalVFX;
        public static Dictionary<string, GameObject> originalSFX;
        public static Dictionary<string, GameObject> originalFX;
        public static Dictionary<string, GameObject> extraEffects;
        public static Dictionary<string, int> RecipeMaxStationLvl = new Dictionary<string, int>();
        public static Dictionary<string, Dictionary<bool, float>> AttackSpeed = new Dictionary<string, Dictionary<bool, float>>();
        public static Dictionary<string, Tuple <string, float, string, float>> SEWeaponChoice = new Dictionary<string, Tuple<string, float, string, float>>();
        public static Dictionary<string, Recipe> hiddenRecipeUpgrade; // Reqs_Upgrade
        public static Dictionary<string, float> crossbowReloadingTime = new Dictionary<string, float>();
        public static Dictionary<Recipe, bool> RequiredUpgradeItemsString = new(); // holding
        public static Dictionary<Recipe, bool> RequiredCraftItemsString = new(); // holding

        internal static Startupserver startupserver = new Startupserver();
        public static ReadFiles readFiles = new ReadFiles();
        public static Reload CurrentReload = new Reload();
        public static List<string> NoNotTheseSEs = new List<string>() { "GoblinShaman_shield", "SE_Dvergr_heal", "SE_Greydwarf_shaman_heal" }; // problematic
                                                                                                                                          
        internal static int kickcount = 0;
        internal static bool jsonsFound = false;
        public static bool ForceLogout = false;
        internal static bool LobbyRegistered = false;
        internal static bool HasLobbied = false;
        internal static IEnumerable<string> jsonfiles;
        internal static bool ReloadingOkay = false;
        internal static int ProcessWait = 10;
        internal static int ProcessWaitforRead = 10;
        internal static float WaitTime = .3f;
        internal static bool LockReload = false;
        internal static bool Reloading = false;

        internal static bool IsDedServer => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        public void Awake() // start
        {
           // Localizer.Load();// Does not work because wackydb needs to key what to look for
            StartupConfig(); // startup varables
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wackysDatabase");
            assetPathconfig = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath + Path.DirectorySeparatorChar), "wackysDatabase");
            assetPathItems = Path.Combine(assetPathconfig, "Items");
            assetPathRecipes = Path.Combine(assetPathconfig, "Recipes");
            assetPathPieces = Path.Combine(assetPathconfig, "Pieces");
            assetPathMaterials = Path.Combine(assetPathconfig, "Materials");
            assetPathTextures = Path.Combine(assetPathconfig, "Textures");
            assetPathEffects = Path.Combine(assetPathconfig, "Effects");
            assetPathObjects = Path.Combine(assetPathconfig, "Objects");
            assetPathCreatures = Path.Combine(assetPathconfig, "Creatures");
            assetPathPickables = Path.Combine(assetPathconfig, "Pickables");
            assetPathOldJsons = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath + Path.DirectorySeparatorChar), "wackysDatabase-OldJsons");

            assetPathBulkYML = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath + Path.DirectorySeparatorChar), "wackyDatabase-BulkYML");
            assetPathBulkYMLItems = Path.Combine(assetPathBulkYML, "Items");
            assetPathBulkYMLPieces = Path.Combine(assetPathBulkYML, "Pieces");
            assetPathBulkYMLRecipes = Path.Combine(assetPathBulkYML, "Recipes");
            assetPathBulkYMLEffects = Path.Combine(assetPathBulkYML, "Effects");
            assetPathBulkYMLCreatures = Path.Combine(assetPathBulkYML, "Creatures");
            assetPathBulkYMLPickables = Path.Combine(assetPathBulkYML, "Pickables");

            assetPathIcons = Path.Combine(assetPathconfig, "Icons");
            assetPathCache = Path.Combine(assetPathconfig, "Cache");
            // testme(); // function for testing things

            // ending files

            AnimationSpeedManager.Add((character, speed) =>
            {
                if (character is not Player player || !player.InAttack() || player.m_currentAttack is null)
                {
                    return speed;
                }
                var skilltype = player.GetCurrentWeapon().m_shared.m_skillType;
                if (skilltype == Skills.SkillType.Unarmed) return speed; // no unarmed

                GameObject val = player.GetCurrentWeapon()?.m_dropPrefab;
                if (WMRecipeCust.AttackSpeed.TryGetValue(val.name, out Dictionary<bool, float> Paul))
                {
                    if (player.m_currentAttackIsSecondary)
                    {
                        return speed * Paul[true];
                    }
                    else
                    {
                        return speed * Paul[false];
                    }
                }
                return speed;
            });

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);

            jsonfiles = startupserver.CheckForJsons(); // read jsons for server
            if (jsonsFound)
            {
                WMRecipeCust.WLog.LogWarning("Jsons Found");
                //startupserver.BeginConvertingJsons(jsoncount);
            }
            WMRecipeCust.context.StartCoroutine(readFiles.GetDataFromFiles()); // YML get
            AwakeHasRun = true;

            readFiles.SetupWatcher();

            skillConfigData.ValueChanged += CustomSyncEventDetected; // custom sync watcher for yml file synced from server

            largeTransfer.ValueChanged += LargeTransferDetected;
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

            //PrefabContainer.transform.parent = Main.RootObject.transform; ? JVL onto something

            WMRecipeCust.Root = new GameObject("myroot");
            WMRecipeCust.Root.SetActive(false);
            WMRecipeCust.DontDestroyOnLoad(WMRecipeCust.Root); // clone magic

            // ^^ // starting files
            context = this;
            modEnabled = config<bool>("General", "Enabled", true, "Enable this mod");
            NexusModID = config<string>("General", "NexusModID", "1825", "NexusModID Number", false);
            isDebug = config<bool>("General", "IsDebug", true, "Enable debug logs", false);
            isDebugString = config<bool>("General", "StringisDebug", false, "Do You want to see the String Debug Log - extra logs");
            isautoreload = config<bool>("General", "IsAutoReload", false, new ConfigDescription("Enable auto reload after wackydb_save or wackydb_clone for singleplayer", null, new ConfigurationManagerAttributes { Browsable = false })); // not browseable and can only be set before launch
            WaterName = config<string>("Armor", "WaterName", "Water", "Water name for Armor Resistance", false);
            ServerDedLoad = config<bool>("General", "DedServer load Memory", true, "Dedicated Servers will load wackydb files as a client would");
            extraSecurity = config<bool>("General", "ExtraSecurity on Servers", true, "Makes sure a player can't load into a server after going into Singleplayer -resulting in Game Ver .0.0.1, - Recommended to keep this enabled");
            enableYMLWatcher = config<bool>("General", "FileWatcher for YMLs", true, "EnableYMLWatcher Servers/Singleplayer, YMLs will autoreload if Wackydatabase folder changes(created,renamed,edited) - disable for some servers that auto reload too much");
            // clonedcache = config<bool>("General", "Enabled Cloned Cache", true, "Turn on CloneCache so that Character items appear in the Start Menu");
            extraEffectList = config<string>("Effects", "List of Extra Effects", "lightningAOE", "Extra Effects to look for from base game or Mods - (Use_a_comma,No_spaces)");
            ConfigSync.CurrentVersion = ModVersion;

            WLog.LogDebug("Mod Version " + ConfigSync.CurrentVersion);
            if (isautoreload.Value) // only sets at start
                isSettoAutoReload = true;
            else isSettoAutoReload = false;
        }

        private void OnDestroy()
        {
            Config.Save();
            //WLog.LogInfo("Calling the Destroyer of Worlds -End Game"); past its time :(
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

        internal static void AddBlacklistClone(string prefab)
        {
            if (prefab == null) return;
            if (BlacklistClone.Contains(prefab)) return;
            BlacklistClone.Add(prefab);
        }

        private void CustomSyncEventDetected()
        {
            if (WMRecipeCust.NoMoreLoading)
                return;
           // WMRecipeCust.WLog.LogInfo("skillConfig data recieved " + skillConfigData.Value) ;
            CurrentReload.SyncEventDetected();
        }

        private void LargeTransferDetected()
        {
            HandleData.RecievedData();
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
                Directory.CreateDirectory(assetPathTextures);
                Directory.CreateDirectory(assetPathMaterials);
            }

            if (!Directory.Exists(assetPathItems))
            {
                Dbgl("Creating Items Folder");
                Directory.CreateDirectory(assetPathItems);
            }

            if (!Directory.Exists(assetPathPieces))
            {
                Dbgl("Creating Pieces Folder");
                Directory.CreateDirectory(assetPathPieces);
            }

            if (!Directory.Exists(assetPathRecipes))
            {
                Dbgl("Creating Recipes Folder");
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
            if (!Directory.Exists(assetPathCache))
            {
                Dbgl("Creating Cache folder");
                Directory.CreateDirectory(assetPathCache);
            }
            if (!Directory.Exists(assetPathObjects))
            {
                Dbgl("Creating Objects folder");
                Directory.CreateDirectory(assetPathObjects);
            }
            if (!Directory.Exists(assetPathTextures))
            {
                Dbgl("Creating Texture folder");
                Directory.CreateDirectory(assetPathTextures);
            }
            if (!Directory.Exists(assetPathMaterials))
            {
                Dbgl("Creating Materials folder");
                Directory.CreateDirectory(assetPathMaterials);
            }
            if (!Directory.Exists(assetPathCreatures))
            {
                Dbgl("Creating Creature folder");
                Directory.CreateDirectory(assetPathCreatures);
            }
            if (!Directory.Exists(assetPathPickables))
            {
                Dbgl("Creating Pickable folder");
                Directory.CreateDirectory(assetPathPickables);
            }

            /*
            var versionpath = Path.Combine(assetPathCache, $"Last_Cleared.txt");
            if (File.Exists(versionpath))
            {
                var filev = File.ReadAllText(versionpath);
                if (filev != ModVersion)
                {
                    WMRecipeCust.WLog.LogWarning("New Wackydb Version, deleteing Cache");
                    DeleteCache();
                }
            }
            else
            {
                File.WriteAllText(versionpath, ModVersion);
            }
            */
        }

        public static void DeleteCache()
        {
            var versionpath = Path.Combine(assetPathCache, $"Last_Cleared.txt");
            Directory.Delete(assetPathCache, true);
            Directory.CreateDirectory(assetPathCache);
            File.WriteAllText(versionpath, ModVersion);
        }

        public static void AdminReload(long peer, ZPackage go)
        {
            WMRecipeCust.WLog.LogInfo("Recieved Admin Request to Reload");

            ReadFiles readnow = new ReadFiles();
            WMRecipeCust.context.StartCoroutine(readnow.GetDataFromFiles());
            WMRecipeCust.readFiles = readnow;

            WMRecipeCust.skillConfigData.Value = ymlstring;// push to clients

            SetData.Reload josh = new SetData.Reload();
            WMRecipeCust.CurrentReload = josh;
            WMRecipeCust.WLog.LogInfo("Sent YML to clients");

            if (WMRecipeCust.ServerDedLoad.Value)
                WMRecipeCust.context.StartCoroutine(josh.LoadAllRecipeData(true, true)); // Admin Reload
        }

        public static void GetAllMaterials(bool skipMD = false) // Get all Materials, SFX, VFX, FX
        {
            Material[] array = Resources.FindObjectsOfTypeAll<Material>();
            GameObject[] array3 = Resources.FindObjectsOfTypeAll<GameObject>();

            originalMaterials = new Dictionary<string, Material>();
            originalVFX = new Dictionary<string, GameObject>();
            originalSFX = new Dictionary<string, GameObject>();
            originalFX = new Dictionary<string, GameObject>();
            extraEffects = new Dictionary<string, GameObject>();

            foreach (Material val in array)
            {   
                originalMaterials[val.name] = val;
            }
            foreach (GameObject val1 in array3)
            {
                if (val1.name.ToLower().StartsWith("vfx"))
                {
                    originalVFX[val1.name] = val1;
                }
                else if (val1.name.ToLower().StartsWith("sfx"))
                {
                    originalSFX[val1.name] = val1;
                }
                else if (val1.name.ToLower().StartsWith("fx_"))
                {
                    originalFX[val1.name] = val1;
                }
                else if (extraEffectList.Value.Split(',').ToList().Contains(val1.name))
                {
                    extraEffects[val1.name] = val1;
                }
            }
            if (!skipMD)
            {
                MaterialDataManager.Instance.LoadFiles();
            }
        }

        /*
        [HarmonyPatch(typeof(FejdStartup), "Start")]
        static class FejdstartupWackyBlackPatch // Add your name/modname to this class method - should be unique
        {
            private static void Prefix()
            {
                if (FejdStartup.m_firstStartup)
                {
                    if (WackyDatabase_API.IsInstalled())
                    {
                        // Add blacklist here
                       // WackyDatabase_API.AddBlacklistClone("Wood");
                    }
                }
            }
        } example code for modders
        */
    }
}