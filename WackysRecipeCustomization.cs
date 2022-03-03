// Most of the credit goes to  aedenthorn  and all of his Many Mods! https://github.com/aedenthorn/ValheimMods
// Thank you AzumattDev for the template. It is very good https://github.com/AzumattDev/ItemManagerModTemplate
// Thanks to the Odin Discord server, for being active and good for the valheim community.
// Do whatever you want with this mod. // except sale it as per Aedenthorn Permissions https://www.nexusmods.com/valheim/mods/1245
// Goal for this mod is RecipeCustomization + Recipe LVL station Requirement + Server Sync
// Taking from Azu OpenDatabase code and the orginal now. https://www.nexusmods.com/valheim/mods/319?tab=description
//
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
using System.Security.Cryptography;
using System.Text;




namespace recipecustomization
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class WMRecipeCust : BaseUnityPlugin
    {
        internal const string ModName = "WackysRecipeCustomization";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "WackyMole";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> isSinglePlayer;
        private static bool issettoSinglePlayer;
        private static bool recieveServerInfo = false;
        internal static string ConnectionError = "";
        private static WMRecipeCust context;
        private static int kickcount = 0;
        //public static string consoleNamespace = "recipecustomization";
        //private static bool IsServer = false;
        //private static bool IsServer => (int)SystemInfo.get_graphicsDeviceType() == 4;
        //private static bool IsServer = SystemInfo.graphicsDeviceType();

        public static ConfigEntry<float> globalArmorDurabilityLossMult;
        public static ConfigEntry<float> globalArmorMovementModMult;

        public static ConfigEntry<string> waterModifierName;

        private static List<RecipeData> recipeDatas = new List<RecipeData>();
        private static List<WItemData> ItemDatas = new List<WItemData>();
        private static string assetPath;
        RecipeData paul = new RecipeData();
        private static string jsonstring;
        private static bool isaclient = false;
        public static bool Admin = false;
        private static List<string> pieceWithLvl = new List<string>();
        private static int startupSync = 0;
        bool admin = !ConfigSync.IsLocked;



        #region extra functions
        private enum NewDamageTypes
        {
            Water = 1024
        }


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(WMRecipeCust).Namespace + " " : "") + str);
        }

        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource WackysRecipeCustomizationLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };


        #endregion



        public void Awake() // start
        {
            StartupConfig(); // startup varables 
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(WMRecipeCust).Namespace);

            //testifpossible();


            // ending files
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
            GetRecipeDataFromFilesForServer();
            skillConfigData.ValueChanged += CustomSyncEventDetected; // custom watcher for json file synced from server
        }

        private void testifpossible() // delete
        {

            paul.minStationLevel = 1;
            paul.name = "ShieldWood";
            paul.craftingStation = "$piece_workbench";
            paul.amount = 1;
            paul.disabled = false;
            // paul.reqs.InsertRange(3, new string[] 
            // { "Wood:10:5:True", "Resin:4:2:True", "LeatherScraps:4:2:True"});
            //skillConfigData.Value = paul;


            //after files have been loaded
            // for loop for each loaded
            // masterSyncJson = config("Recipes", "RecipeN1", paul, "RecipeD1");
            //masterSyncJson = config("Recipes", "RecipeN2", paul,

        }

        #region ConfigReading

        private static ConfigEntry<bool>? _serverConfigLocked;
        //internal static ConfigEntry<RecipeData> masterSyncJson; // doesn't work
        private static readonly CustomSyncedValue<string> skillConfigData = new(ConfigSync, "skillConfig", ""); // doesn't show up in config


        private void StartupConfig()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);


            // ^^ // starting files
            context = this;
            modEnabled = config<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = config<bool>("General", "IsDebug", true, "Enable debug logs", false);
            isSinglePlayer = config<bool>("General", "IsSinglePlayerOnly", false, new ConfigDescription("Allow Single Player- Must be off for Multiplayer", null, new ConfigurationManagerAttributes { Browsable = false }), false); // doesn't allow you to connect if set to true
            if (isSinglePlayer.Value)
            {
                ConfigSync.CurrentVersion = "0.0.1"; // kicking player from server
                WackysRecipeCustomizationLogger.LogWarning("You Will be kicked from Multiplayer Servers! " + ConfigSync.CurrentVersion);
                issettoSinglePlayer = true;
            }
            else
            {
                issettoSinglePlayer = false;
            }


        }
        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                WackysRecipeCustomizationLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                WackysRecipeCustomizationLogger.LogError($"There was an issue loading your {ConfigFileName}");
                WackysRecipeCustomizationLogger.LogError("Please check your config entries for spelling and format!");
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

        //[Serializable]


        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        private static void LoadAllRecipeData(bool reload)
        {
            if (isaclient)
            { // log You are a client and not an admin
                WackysRecipeCustomizationLogger.LogError("You are a client and not the Server. Cannot reload recipes");
            }
            else
            {
                if (reload && (issettoSinglePlayer || recieveServerInfo)) // single player only or recievedServerInfo
                {
                    if ((recieveServerInfo && issettoSinglePlayer))
                    {
                        return; // naughty boy no recipes for you
                    }
                    else
                    {
                        GetRecipeDataFromFiles();
                        foreach (var data in ItemDatas) // call items first
                        {
                            SetItemData(data);
                        }
                        foreach (var data in recipeDatas)
                        {
                            SetRecipeData(data);
                        }

                    }
                }
            }
        }

        private static void CustomSyncEventDetected()
        {
            // load up the files from skillConfigData
            // seperate them
            //reload
            // need to skip first call
            if (startupSync > 1)
            {
                startupSync++;
                recieveServerInfo = true;
            }
            else
            {
                WackysRecipeCustomizationLogger.LogDebug("CustomSyncEventDetected was called ");
                recipeDatas.Clear();
                ItemDatas.Clear();
                string SyncedString = skillConfigData.Value;
                if (SyncedString != null && SyncedString != "")
                {
                    WackysRecipeCustomizationLogger.LogDebug("Synced String was  " + SyncedString);
                    string[] jsons = SyncedString.Split('@');
                    foreach (var word in jsons)
                    {
                        if (word.Contains("m_name"))
                        {
                            WItemData data2 = JsonUtility.FromJson<WItemData>(word);
                            ItemDatas.Add(data2);

                        }
                        else
                        {
                            RecipeData data = JsonUtility.FromJson<RecipeData>(word);
                            recipeDatas.Add(data);
                        }

                        //WackysRecipeCustomizationLogger.LogDebug(word);
                    }
                    pieceWithLvl.Clear(); // ready for new
                    foreach (var data2 in ItemDatas)
                    {
                        if (data2 != null)
                        {
                            SetItemData(data2);
                        }
                    }
                    foreach (var data in recipeDatas)
                    {
                        if (data != null)
                        {
                            SetRecipeData(data);
                        }
                    }
                    
                    WackysRecipeCustomizationLogger.LogDebug("done with customSyncEvent");
                }
                else
                {
                    WackysRecipeCustomizationLogger.LogDebug("Synced String was blank " + SyncedString);
                }
                // isaclient = true; // don't allow reload
            }

        }

        private static void GetRecipeDataFromFiles()
        {
            CheckModFolder();

            recipeDatas.Clear();
            ItemDatas.Clear();
            var amber = new System.Text.StringBuilder();
            //JsonSerializer.Serialize

            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                if (file.Contains("Item") || file.Contains("item"))
                {
                    WItemData data = JsonUtility.FromJson<WItemData>(File.ReadAllText(file));
                    amber.Append(File.ReadAllText(file));
                    amber.Append("@");
                    ItemDatas.Add(data);

                }
                else
                {
                    RecipeData data = JsonUtility.FromJson<RecipeData>(File.ReadAllText(file));
                    amber.Append(File.ReadAllText(file));
                    amber.Append("@");
                    recipeDatas.Add(data);

                }


            }

            jsonstring = amber.ToString();
            // skillConfigData.Value = jsonstring; Only for server 1st time
            WackysRecipeCustomizationLogger.LogDebug(jsonstring);
        }
        private static void GetRecipeDataFromFilesForServer()
        {
            CheckModFolder();
            var amber = new System.Text.StringBuilder();
            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                if (file.Contains("Item") || file.Contains("item"))
                {
                    WItemData data = JsonUtility.FromJson<WItemData>(File.ReadAllText(file));
                    amber.Append(File.ReadAllText(file));
                    amber.Append("@");
                    ItemDatas.Add(data);

                }
                else
                {
                    RecipeData data = JsonUtility.FromJson<RecipeData>(File.ReadAllText(file));
                    amber.Append(File.ReadAllText(file));
                    amber.Append("@");
                    recipeDatas.Add(data);

                }
            }
            jsonstring = amber.ToString();
            skillConfigData.Value = jsonstring;
            WackysRecipeCustomizationLogger.LogDebug("loaded first pass files");
        }

        private static void CheckModFolder()
        {
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }
        }

        #endregion
        #region Set Object

        private static Vector3 tempvalue;
        private static bool piecehaslvl;

        private static void SetRecipeData(RecipeData data)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(data.name);
            if (go == null)
            {
                SetPieceRecipeData(data);
                Dbgl("maybe null " + data.name);
                return;
            }
            if (go.GetComponent<ItemDrop>() == null)
            {
                Dbgl($"Item data for {data.name} not found!");
                return;
            } // it is a prefab and it is an item.
            //if (data.)

            for (int i = ObjectDB.instance.m_recipes.Count - 1; i > 0; i--)
            {
                if (ObjectDB.instance.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                {
                    if (data.disabled)
                    {
                        Dbgl($"Removing recipe for {data.name} from the game");
                        ObjectDB.instance.m_recipes.RemoveAt(i);
                        return;
                    }

                    ObjectDB.instance.m_recipes[i].m_amount = data.amount;
                    ObjectDB.instance.m_recipes[i].m_minStationLevel = data.minStationLevel;
                    ObjectDB.instance.m_recipes[i].m_craftingStation = GetCraftingStation(data.craftingStation);
                    List<Piece.Requirement> reqs = new List<Piece.Requirement>();
                    // Dbgl("Made it to RecipeData!");
                    foreach (string req in data.reqs)
                    {
                        string[] parts = req.Split(':');
                        reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
                    }
                    Dbgl("Amost done with RecipeData!");
                    ObjectDB.instance.m_recipes[i].m_resources = reqs.ToArray();
                    return;
                } // end check if actually an recipe
            }// end for loop search
        }

        private static void SetPieceRecipeData(RecipeData data)
        {
            GameObject go = GetPieces().Find(g => Utils.GetPrefabName(g) == data.name);
            if (go == null)
            {
                Dbgl($"Item {data.name} not found!");
                return;
            }
            if (go.GetComponent<Piece>() == null)
            {
                Dbgl($"Item data for {data.name} not found!");
                return;
            }

            if (data.disabled)
            {
                Dbgl($"Removing recipe for {data.name} from the game");

                ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
                if (hammer && hammer.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                {
                    hammer.m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go);
                    return;
                }
                ItemDrop hoe = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
                if (hoe && hoe.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
                {
                    hoe.m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go);
                    return;
                }

            }

            go.GetComponent<Piece>().m_craftingStation = GetCraftingStation(data.craftingStation);
            //List<string> helpme = new List<string>();

            if (data.minStationLevel > 1)
            {
                pieceWithLvl.Add(go.name + "." + data.minStationLevel);
            }
            List<Piece.Requirement> reqs = new List<Piece.Requirement>();
            // Dbgl("made it to setpiece");
            foreach (string req in data.reqs)
            {
                string[] parts = req.Split(':');
                reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
            }
            //Dbgl("Amost done with setpiece!");
            go.GetComponent<Piece>().m_resources = reqs.ToArray();
            Dbgl("done with setpiece!");
        }

        private static void SetItemData(WItemData data)
        {
            Dbgl("Loaded SetItemData!");
            string tempname = "";
            if (data.clone)
            {
                tempname = data.clonePrefabName;
                data.name = data.clonePrefabName;

            }
            GameObject go = ObjectDB.instance.GetItemPrefab(data.name);
            if (go == null)
            {
                Dbgl(" item in SetItemData null " + data.name);
                return;
            }
            if (go.GetComponent<ItemDrop>() == null)
            {
                Dbgl($"Item data in SetItemData for {data.name} not found!");
                return;
            } // it is a prefab and it is an item.

            for (int i = ObjectDB.instance.m_items.Count - 1; i > 0; i--)  // need to handle clones
            {
                if (ObjectDB.instance.m_items[i]?.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name) //if (ObjectDB.instance.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                {
                    ItemDrop.ItemData PrimaryItemData = ObjectDB.instance.m_items[i].GetComponent<ItemDrop>().m_itemData;
                    if (data.clone && tempname != "") // object is a clone do clonethings
                    {

                        Dbgl($"Item CLONE DATA in SetItemData for {data.name} ");
                        GameObject newItem = go;
                        ItemDrop NewItemComp = newItem.GetComponent<ItemDrop>();
                        newItem.name = tempname; // maybe
                        NewItemComp.m_itemData.m_shared.m_name = tempname; // ingame name
                        ObjectDB.instance.m_items.Add(newItem); // add new GameObject to item list
                        PrimaryItemData = ObjectDB.instance.GetItemPrefab(tempname).GetComponent<ItemDrop>().m_itemData; // nonsense but verifies?
                        data.m_name = tempname; // putting back name


                    }
                    Dbgl($"Item being Set in SetItemData for {data.name} ");
                    if (data.m_damages != null)
                    {
                        HitData.DamageTypes damages = default(HitData.DamageTypes);
                        damages.m_blunt = data.m_damages.m_blunt;
                        damages.m_chop = data.m_damages.m_chop;
                        damages.m_damage = data.m_damages.m_damage;
                        damages.m_fire = data.m_damages.m_fire;
                        damages.m_frost = data.m_damages.m_frost;
                        damages.m_lightning = data.m_damages.m_lightning;
                        damages.m_pickaxe = data.m_damages.m_pickaxe;
                        damages.m_pierce = data.m_damages.m_pierce;
                        damages.m_poison = data.m_damages.m_poison;
                        damages.m_slash = data.m_damages.m_slash;
                        damages.m_spirit = data.m_damages.m_spirit;
                        PrimaryItemData.m_shared.m_damages = damages;
                    }
                    if (data.m_damagesPerLevel != null)
                    {
                        HitData.DamageTypes damagesPerLevel = default(HitData.DamageTypes);
                        damagesPerLevel.m_blunt = data.m_damagesPerLevel.m_blunt;
                        damagesPerLevel.m_chop = data.m_damagesPerLevel.m_chop;
                        damagesPerLevel.m_damage = data.m_damagesPerLevel.m_damage;
                        damagesPerLevel.m_fire = data.m_damagesPerLevel.m_fire;
                        damagesPerLevel.m_frost = data.m_damagesPerLevel.m_frost;
                        damagesPerLevel.m_lightning = data.m_damagesPerLevel.m_lightning;
                        damagesPerLevel.m_pickaxe = data.m_damagesPerLevel.m_pickaxe;
                        damagesPerLevel.m_pierce = data.m_damagesPerLevel.m_pierce;
                        damagesPerLevel.m_poison = data.m_damagesPerLevel.m_poison;
                        damagesPerLevel.m_slash = data.m_damagesPerLevel.m_slash;
                        damagesPerLevel.m_spirit = data.m_damagesPerLevel.m_spirit;
                        PrimaryItemData.m_shared.m_damagesPerLevel = damagesPerLevel;
                    }
                    PrimaryItemData.m_shared.m_name = data.m_name;
                    PrimaryItemData.m_shared.m_description = data.m_description;
                    PrimaryItemData.m_shared.m_weight = data.m_weight;
                    PrimaryItemData.m_shared.m_maxStackSize = data.m_maxStackSize;
                    PrimaryItemData.m_shared.m_food = data.m_food;
                    PrimaryItemData.m_shared.m_foodStamina = data.m_foodStamina;
                    PrimaryItemData.m_shared.m_foodRegen = data.m_foodRegen;
                    PrimaryItemData.m_shared.m_foodBurnTime = data.m_foodBurnTime;
                    if (data.m_foodColor != null && data.m_foodColor != "" && data.m_foodColor.StartsWith("#"))
                    {
                        PrimaryItemData.m_shared.m_foodColor = ColorUtil.GetColorFromHex(data.m_foodColor);
                    }
                    PrimaryItemData.m_shared.m_armor = data.m_armor;
                    PrimaryItemData.m_shared.m_armorPerLevel = data.m_armorPerLevel;
                    PrimaryItemData.m_shared.m_blockPower = data.m_blockPower;
                    PrimaryItemData.m_shared.m_blockPowerPerLevel = data.m_blockPowerPerLevel;
                    PrimaryItemData.m_shared.m_canBeReparied = data.m_canBeReparied;
                    PrimaryItemData.m_shared.m_timedBlockBonus = data.m_timedBlockBonus;
                    PrimaryItemData.m_shared.m_deflectionForce = data.m_deflectionForce;
                    PrimaryItemData.m_shared.m_deflectionForcePerLevel = data.m_deflectionForcePerLevel;
                    PrimaryItemData.m_shared.m_destroyBroken = data.m_destroyBroken;
                    PrimaryItemData.m_shared.m_dodgeable = data.m_dodgeable;
                    PrimaryItemData.m_shared.m_maxDurability = data.m_maxDurability;
                    PrimaryItemData.m_shared.m_durabilityDrain = data.m_durabilityDrain;
                    PrimaryItemData.m_shared.m_durabilityPerLevel = data.m_durabilityPerLevel;
                    PrimaryItemData.m_shared.m_equipDuration = data.m_equipDuration;
                    PrimaryItemData.m_shared.m_holdDurationMin = data.m_holdDurationMin;
                    PrimaryItemData.m_shared.m_holdStaminaDrain = data.m_holdStaminaDrain;
                    PrimaryItemData.m_shared.m_maxQuality = data.m_maxQuality;
                    PrimaryItemData.m_shared.m_useDurability = data.m_useDurability;
                    PrimaryItemData.m_shared.m_useDurabilityDrain = data.m_useDurabilityDrain;
                    PrimaryItemData.m_shared.m_questItem = data.m_questItem;
                    PrimaryItemData.m_shared.m_teleportable = data.m_teleportable;
                    PrimaryItemData.m_shared.m_toolTier = data.m_toolTier;
                    PrimaryItemData.m_shared.m_value = data.m_value;

                }

            }
            Dbgl("Amost done with SetItemData!");

        }


        #endregion
        #region GetObject

        private static CraftingStation GetCraftingStation(string name)
        {
            if (name == "" || name == null)
                return null;

            Dbgl("Looking for crafting station " + name);

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe?.m_craftingStation?.m_name == name)
                {

                    Dbgl("got crafting station " + name);

                    return recipe.m_craftingStation;
                }
            }
            foreach (GameObject piece in GetPieces())
            {

                if (piece.GetComponent<Piece>()?.m_craftingStation?.m_name == name)
                {

                    Dbgl("got crafting station " + name);

                    return piece.GetComponent<Piece>().m_craftingStation;

                }
            }

            return null;
        }
        private static List<GameObject> GetPieces()
        {
            var pieces = new List<GameObject>();
            if (!ObjectDB.instance)
                return pieces;

            ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();

            if (hammer)
                pieces.AddRange(Traverse.Create(hammer.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());

            ItemDrop hoe = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
            if (hoe)
                pieces.AddRange(Traverse.Create(hoe.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());
            return pieces;

        }
        private static RecipeData GetRecipeDataByName(string name)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(name);
            if (go == null)
            {
                return GetPieceRecipeByName(name);
            }

            ItemDrop.ItemData item = go.GetComponent<ItemDrop>().m_itemData;
            if (item == null)
            {
                Dbgl("Item data not found!");
                return null;
            }
            Recipe recipe = ObjectDB.instance.GetRecipe(item);
            if (!recipe)
            {
                if (Chainloader.PluginInfos.ContainsKey("com.jotunn.jotunn"))
                {
                    object itemManager = Chainloader.PluginInfos["com.jotunn.jotunn"].Instance.GetType().Assembly.GetType("Jotunn.Managers.ItemManager").GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    object cr = AccessTools.Method(itemManager.GetType(), "GetRecipe").Invoke(itemManager, new[] { item.m_shared.m_name });
                    if (cr != null)
                    {
                        recipe = (Recipe)AccessTools.Property(cr.GetType(), "Recipe").GetValue(cr);
                        Dbgl($"Jotunn recipe: {item.m_shared.m_name} {recipe != null}");
                    }
                }

                if (!recipe)
                {
                    Dbgl($"Recipe not found for item {item.m_shared.m_name}!");
                    return null;
                }
            }

            var data = new RecipeData()
            {
                name = name,
                amount = recipe.m_amount,
                craftingStation = recipe.m_craftingStation?.m_name ?? "",
                minStationLevel = recipe.m_minStationLevel,
            };
            foreach (Piece.Requirement req in recipe.m_resources)
            {
                data.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }

            return data;
        }

        private static RecipeData GetPieceRecipeByName(string name)
        {
            GameObject go = GetPieces().Find(g => Utils.GetPrefabName(g) == name);
            if (go == null)
            {
                Dbgl($"Item {name} not found!");
                return null;
            }
            Piece piece = go.GetComponent<Piece>();
            if (piece == null)
            {
                Dbgl("Item data not found!");
                return null;
            }
            var data = new RecipeData()
            {
                name = name,
                amount = 1,
                craftingStation = piece.m_craftingStation?.m_name ?? "",
                minStationLevel = 1,
            };
            foreach (Piece.Requirement req in piece.m_resources)
            {
                data.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }

            return data;
        }

        private static WItemData GetItemDataByName(string name)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(name);
            if (go == null)
            {
                Dbgl("GetItemDataByName data not found!");
                return null;
            }

            ItemDrop.ItemData data = go.GetComponent<ItemDrop>().m_itemData;
            if (data == null)
            {
                Dbgl("Item GetItemDataByName not found! - componets");
                return null;
            }
            /*
            Recipe recipe = ObjectDB.instance.GetRecipe(item);
            if (!recipe)
            {
                if (Chainloader.PluginInfos.ContainsKey("com.jotunn.jotunn"))
                {
                    object itemManager = Chainloader.PluginInfos["com.jotunn.jotunn"].Instance.GetType().Assembly.GetType("Jotunn.Managers.ItemManager").GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    object cr = AccessTools.Method(itemManager.GetType(), "GetRecipe").Invoke(itemManager, new[] { item.m_shared.m_name });
                    if (cr != null)
                    {
                        recipe = (Recipe)AccessTools.Property(cr.GetType(), "Recipe").GetValue(cr);
                        Dbgl($"Jotunn recipe: {item.m_shared.m_name} {recipe != null}");
                    }
                }
            */ // not sure about this stuff for Jotunn mangers


            WDamages damages = null;
            if (data.m_shared.m_damages.m_blunt > 0f || data.m_shared.m_damages.m_chop > 0f || data.m_shared.m_damages.m_damage > 0f || data.m_shared.m_damages.m_fire > 0f || data.m_shared.m_damages.m_frost > 0f || data.m_shared.m_damages.m_lightning > 0f || data.m_shared.m_damages.m_pickaxe > 0f || data.m_shared.m_damages.m_pierce > 0f || data.m_shared.m_damages.m_poison > 0f || data.m_shared.m_damages.m_slash > 0f || data.m_shared.m_damages.m_spirit > 0f)
            {
                damages = new WDamages
                {
                    m_blunt = data.m_shared.m_damages.m_blunt,
                    m_chop = data.m_shared.m_damages.m_chop,
                    m_damage = data.m_shared.m_damages.m_damage,
                    m_fire = data.m_shared.m_damages.m_fire,
                    m_frost = data.m_shared.m_damages.m_frost,
                    m_lightning = data.m_shared.m_damages.m_lightning,
                    m_pickaxe = data.m_shared.m_damages.m_pickaxe,
                    m_pierce = data.m_shared.m_damages.m_pierce,
                    m_poison = data.m_shared.m_damages.m_poison,
                    m_slash = data.m_shared.m_damages.m_slash,
                    m_spirit = data.m_shared.m_damages.m_spirit
                };
            }
            WDamages damagesPerLevel = null;
            if (data.m_shared.m_damagesPerLevel.m_blunt > 0f || data.m_shared.m_damagesPerLevel.m_chop > 0f || data.m_shared.m_damagesPerLevel.m_damage > 0f || data.m_shared.m_damagesPerLevel.m_fire > 0f || data.m_shared.m_damagesPerLevel.m_frost > 0f || data.m_shared.m_damagesPerLevel.m_lightning > 0f || data.m_shared.m_damagesPerLevel.m_pickaxe > 0f || data.m_shared.m_damagesPerLevel.m_pierce > 0f || data.m_shared.m_damagesPerLevel.m_poison > 0f || data.m_shared.m_damagesPerLevel.m_slash > 0f || data.m_shared.m_damagesPerLevel.m_spirit > 0f)
            {
                damagesPerLevel = new WDamages
                {
                    m_blunt = data.m_shared.m_damagesPerLevel.m_blunt,
                    m_chop = data.m_shared.m_damagesPerLevel.m_chop,
                    m_damage = data.m_shared.m_damagesPerLevel.m_damage,
                    m_fire = data.m_shared.m_damagesPerLevel.m_fire,
                    m_frost = data.m_shared.m_damagesPerLevel.m_frost,
                    m_lightning = data.m_shared.m_damagesPerLevel.m_lightning,
                    m_pickaxe = data.m_shared.m_damagesPerLevel.m_pickaxe,
                    m_pierce = data.m_shared.m_damagesPerLevel.m_pierce,
                    m_poison = data.m_shared.m_damagesPerLevel.m_poison,
                    m_slash = data.m_shared.m_damagesPerLevel.m_slash,
                    m_spirit = data.m_shared.m_damagesPerLevel.m_spirit
                };
            }
            WItemData jItemData = new WItemData
            {

                name = name,
                m_armor = data.m_shared.m_armor,
                clone = false,
                m_armorPerLevel = data.m_shared.m_armorPerLevel,
                m_blockPower = data.m_shared.m_blockPower,
                m_blockPowerPerLevel = data.m_shared.m_blockPowerPerLevel,
                m_deflectionForce = data.m_shared.m_deflectionForce,
                m_deflectionForcePerLevel = data.m_shared.m_deflectionForcePerLevel,
                m_description = data.m_shared.m_description,
                m_durabilityDrain = data.m_shared.m_durabilityDrain,
                m_durabilityPerLevel = data.m_shared.m_durabilityPerLevel,
                m_equipDuration = data.m_shared.m_equipDuration,
                m_food = data.m_shared.m_food,
                m_foodColor = ColorUtil.GetHexFromColor(data.m_shared.m_foodColor),
                m_foodBurnTime = data.m_shared.m_foodBurnTime,
                m_foodRegen = data.m_shared.m_foodRegen,
                m_foodStamina = data.m_shared.m_foodStamina,
                m_holdDurationMin = data.m_shared.m_holdDurationMin,
                m_holdStaminaDrain = data.m_shared.m_holdStaminaDrain,
                m_maxDurability = data.m_shared.m_maxDurability,
                m_maxQuality = data.m_shared.m_maxQuality,
                m_maxStackSize = data.m_shared.m_maxStackSize,
                m_toolTier = data.m_shared.m_toolTier,
                m_useDurability = data.m_shared.m_useDurability,
                m_useDurabilityDrain = data.m_shared.m_useDurabilityDrain,
                m_value = data.m_shared.m_value,
                m_weight = data.m_shared.m_weight,
                m_destroyBroken = data.m_shared.m_destroyBroken,
                m_dodgeable = data.m_shared.m_dodgeable,
                m_canBeReparied = data.m_shared.m_canBeReparied,
                m_damages = damages,
                m_damagesPerLevel = damagesPerLevel,
                m_name = data.m_shared.m_name,
                m_questItem = data.m_shared.m_questItem,
                m_teleportable = data.m_shared.m_teleportable,
                m_timedBlockBonus = data.m_shared.m_timedBlockBonus
            };
            if (jItemData.m_food == 0f && jItemData.m_foodRegen == 0f && jItemData.m_foodStamina == 0f)
            {
                jItemData.m_foodColor = null;
            }
            return jItemData;

        }
        /*
        public static void CreateItemFiles()
        {
            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                ItemDrop component = item.GetComponent<ItemDrop>();
                if (component != null)
                {
                    new JItemDrop();
                    OpenDatabase.Logger.Log("Generated Item '" + component.name + "'", (LogLevel)16);
                    string s = Helper.GetItemDataFromItemDrop(component).ToJson();
                    s = JsonFormatter.Format(s, !global::OpenDatabase.OpenDatabase.showZerosInJSON.get_Value());
                    File.WriteAllText(global::OpenDatabase.OpenDatabase.itemsFolder + "/" + component.name + ".json", s);
                }
            }
        }

        */
        #endregion
        #region Patches


        [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
        public static class Console_Patch
        // need to come up with a syntax now
        /* WackyEditor
         * Wackydbeditor
         * wackyDB
         * wackydb save/reload/dump
         * wackydbsave
         * wackydbitemsave
         * wackydb save item <item>
         * wackydb save <recipe/piece>
         * wackydb_save  <item>
         * wackydb_reload
         * wackydb_dump
         * wackydb_reset
         * wackydb <listcommands>
         * wackydb_save_item <item>
         * 
         */
        {
            private static void Postfix()
            {
                WackysRecipeCustomizationLogger.LogDebug("Patching Updated Console Commands");

                if (!modEnabled.Value && issettoSinglePlayer)
                    return; ;

               Terminal.ConsoleCommand WackyShowcommands =
                    new("wackydb", "Display Help ",
                        args =>
                        {
                            string output = $"wackydb_reset \r\n"
                            + $"wackydb_reload\r\n"
                            + $"wackydb_dump item/recipe/piece <ItemName>\r\n"
                            + $"wackydb_save <ItemName>(recipe or piece output)\r\n"
                            + $"wackydb_save_item <ItemName>(Item Output)\r\n"
                            + $"wackydb_help\r\n"
                            + $"wackydb_clone (clone an recipe or piece)\r\n"
                          //  + $"wackydb_clone_item (clone an item)\r\n"
                            ;

                            args.Context?.AddString(output);
                        });

                Terminal.ConsoleCommand WackyShowcommandshelp =
                     new("wackydb_help", "Display Help ",
                         args =>
                         {
                                string output = $"wackydb_reset \r\n"
                            + $"wackydb_reload\r\n"
                            + $"wackydb_dump <ItemName>\r\n"
                            + $"wackydb_save <ItemName>(recipe or piece output)\r\n"
                            + $"wackydb_save_item <ItemName>(Item Output)\r\n"
                            + $"wackydb_help\r\n"
                            + $"wackydb_clone (clone an recipe or piece)\r\n"
                            //+ $"wackydb_clone_item (clone an item)\r\n"
                            ;

                            args.Context?.AddString(output);
                         });

                Terminal.ConsoleCommand WackyReset =
                     new("wackydb_reset", "reload the whole config files", // I should probably delete this one?
                         args =>
                         {
                             context.Config.Reload();
                             context.Config.Save();
                             args.Context?.AddString("Configs reloaded");
                         });

                Terminal.ConsoleCommand WackyReload =
                     new("wackydb_reload", "reload the whole config files", 
                         args =>
                         {
                            // GetRecipeDataFromFiles(); called in loadallrecipes
                             if (ObjectDB.instance)
                             {
                                 LoadAllRecipeData(true);
                                 args.Context?.AddString($"WackyDatabase reloaded recipes from files");
                             }
                             else
                             {
                                 args.Context?.AddString($"WackyDatabase reloaded recipes from files"); // maybe?
                             }
                         });

                Terminal.ConsoleCommand WackyDump =
                     new("wackydb_dump", "dump the item or recipe into the logs",
                         args =>
                         {
                             string recipe = args[1];
                             string comtype = args[2];
                             if (comtype == "item")
                             {
                                 WItemData recipeData = GetItemDataByName(recipe);
                                 if (recipeData == null)
                                     return;
                                 Dbgl(JsonUtility.ToJson(recipeData));

                             }
                             else
                             {
                                 RecipeData recipeData = GetRecipeDataByName(recipe);
                                 if (recipeData == null)
                                     return;
                                 Dbgl(JsonUtility.ToJson(recipeData));
                             }
                             args.Context?.AddString($"WackyDatabase dumped {recipe}");
                         });

                Terminal.ConsoleCommand WackyitemSave =
                    new("wackydb_save_item", "Save an Item ",
                        args =>
                        {
                            string file = args[1];
                            WItemData recipData = GetItemDataByName(file);
                            if (recipData == null)
                                return;
                            CheckModFolder();
                            File.WriteAllText(Path.Combine(assetPath, "Item_" + recipData.name + ".json"), JsonUtility.ToJson(recipData, true));
                            args.Context?.AddString($"saved item data to Item_{file}.json");

                        });
                Terminal.ConsoleCommand WackySave =
                    new("wackydb_save", "Save a piece or recipe ",
                        args =>
                        {
                            string file = args[1];
                            RecipeData recipData = GetRecipeDataByName(file);
                            if (recipData == null)
                                return;
                            CheckModFolder();
                            File.WriteAllText(Path.Combine(assetPath, recipData.name + ".json"), JsonUtility.ToJson(recipData, true));
                            args.Context?.AddString($"saved data to {file}.json");

                        });
                /* syntax for cloning
                 * wackydb_clone <item/recipe/piece> <prefab to clone> <nameofclone>(has to be unquie otherwise we would have to check) 
                 * 
                 */
                Terminal.ConsoleCommand WackyClone =
                    new("wackydb_clone", "Clone an item or piecce with different status, names, effects ect... ",
                        args =>
                        {
                            string commandtype = args[1];
                            string prefab = args[2];
                            string newname = args[3];
                    

                        });







            }
        }

        [HarmonyPatch(typeof(Terminal), "InputText")] // aedenthorn Json mod
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                string text = __instance.m_input.text;

                if (text.ToLower().Equals($"{typeof(WMRecipeCust).Namespace.ToLower()} admin") && !issettoSinglePlayer)// backdoor for funizes only availble when on multiplayer mode.. hahaaa
                {
                    if (kickcount == 0)
                        WackysRecipeCustomizationLogger.LogWarning("Congrats on finding the backdoor... You have 3 chances to guess the password or you will be called out that your a dirty cheater in chat and probably being kicked by Azu or an admin");
                    var t = text.Split(' ');
                    string file = t[t.Length - 1];
                    string hash = ComputeSha256Hash(file);
                    string secrethash = "kjdkjilsnid2jskjhd"; // put a real hash in here 
                    if (hash == secrethash)
                    {
                        WackysRecipeCustomizationLogger.LogWarning("Congrats you cheater, you get to reload the recipes to whatever you want now. Enjoy ");

                    }
                    else
                    {
                        kickcount++;
                        if (kickcount == 4)
                        {
                            WackysRecipeCustomizationLogger.LogWarning("Cheater Cheater, pants on fire ");
                        }

                    }

                }
                #region unused input
                /* Not needed now
                // comment out all this in favor of commands above
                if (!modEnabled.Value && issettoSinglePlayer)
                    return true;


                if (text.ToLower().Equals($"{typeof(WMRecipeCust).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} config reloaded" });
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(WMRecipeCust).Namespace.ToLower()} reload"))
                {
                    GetRecipeDataFromFiles();
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    if (ObjectDB.instance)
                    {
                        LoadAllRecipeData(true);
                        AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} reloaded recipes from files" });
                    }
                    else
                    {
                        AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} reloaded recipes from files" });
                    }
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(WMRecipeCust).Namespace.ToLower()} itemsave "))
                {
                    var t = text.Split(' ');
                    string file = t[t.Length - 1];
                    WItemData recipData = GetItemDataByName(file);
                    if (recipData == null)
                        return false;
                    CheckModFolder();
                    File.WriteAllText(Path.Combine(assetPath, "Item_" + recipData.name + ".json"), JsonUtility.ToJson(recipData, true));
                    __instance.AddString(text);
                    __instance.AddString($"saved item data to {file}.json");
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(WMRecipeCust).Namespace.ToLower()} save "))
                {
                    var t = text.Split(' ');
                    string file = t[t.Length - 1];
                    RecipeData recipData = GetRecipeDataByName(file);
                    if (recipData == null)
                        return false;
                    CheckModFolder();
                    File.WriteAllText(Path.Combine(assetPath, recipData.name + ".json"), JsonUtility.ToJson(recipData));
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} saved recipe data to {file}.json" });
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(WMRecipeCust).Namespace.ToLower()} dump "))
                {
                    var t = text.Split(' ');
                    string recipe = t[t.Length - 1];
                    RecipeData recipeData = GetRecipeDataByName(recipe);
                    if (recipeData == null)
                        return false;
                    Dbgl(JsonUtility.ToJson(recipeData));
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { $"{context.Info.Metadata.Name} dumped {recipe}" });
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(WMRecipeCust).Namespace.ToLower()}"))
                {
                    string output = $"recipecustomization reset\r\n"
                    + $"recipecustomization reload\r\n"
                    + $"recipecustomization dump <ItemName>\r\n"
                    + $"recipecustomization save <ItemName>";

                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { text });
                    AccessTools.Method(typeof(Terminal), "AddString").Invoke(__instance, new object[] { output });
                    return false;
                }*/
                #endregion
                return true;

            }
                
        }

        [HarmonyPatch(typeof(Player), "PlacePiece")]
        private static class Player_MessageforPortal_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ref Player __instance, ref Piece piece)

            {
                if (piece == null) return true;
                foreach (var item in pieceWithLvl)
                {
                    var stringwithnumber = item.Split('.');
                    var PiecetoLookFor = stringwithnumber[0];
                    int CraftingStationlvl = int.Parse(stringwithnumber[1]);

                    if (piece.name == PiecetoLookFor && !__instance.m_noPlacementCost) // portal
                    {
                        if (__instance.transform.position != null)
                            tempvalue = __instance.transform.position; // save position //must be assigned
                        else
                            tempvalue = new Vector3(0, 0, 0); // shouldn't ever be called 

                        var paulstation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, tempvalue);
                        var paullvl = paulstation.GetLevel();

                        if (paullvl + 1 > CraftingStationlvl) // just for testing
                        {
                            // piecehaslvl = true;
                        }
                        else
                        {
                            string worktablename = piece.m_craftingStation.name;
                            GameObject temp = GetPieces().Find(g => Utils.GetPrefabName(g) == worktablename);
                            var name = temp.GetComponent<Piece>().m_name;
                            __instance.Message(MessageHud.MessageType.Center, "Need a Level " + CraftingStationlvl + " " + name + " for placement");
                            //var josh = skillConfigData.Value;
                            // WackysRecipeCustomizationLogger.LogDebug("Synced String  " + josh);

                            //piecehaslvl = false;
                            return false;
                        }
                    }
                }
                return true;
            }

        }
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        [HarmonyPriority(Priority.Last)]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                context.StartCoroutine(DelayedLoadRecipes());// very importrant for last sec load
                //LoadAllRecipeData(true);
            }
        }
        public static IEnumerator DelayedLoadRecipes()
        {
            yield return new WaitForSeconds(0.1f);
            LoadAllRecipeData(true);
            yield break;
        }




        #endregion
        #region others

        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }

        }
        #endregion

    }
}