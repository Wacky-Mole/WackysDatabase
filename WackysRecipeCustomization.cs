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
using UnityEngine.SceneManagement;
using System;
using System.Globalization;

namespace recipecustomization
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class WMRecipeCust : BaseUnityPlugin
    {
        internal const string ModName = "WackysDatabase";
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
        private static List<string> Cloned = new List<string>();
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
                Debug.Log((pref ? ModName + " " : "") + str);
        }

        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource WackysRecipeCustomizationLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName,  MinimumRequiredVersion = "1.0.0" };


        #endregion


        public void Awake() // start
        {
            StartupConfig(); // startup varables 
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wackysDatabase");
            testme();

            // ending files
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            //SetupWatcher(); You can game it by using false-true(load)-false
            GetRecipeDataFromFilesForServer();
            skillConfigData.ValueChanged += CustomSyncEventDetected; // custom watcher for json file synced from server
        }


        #region ConfigReading

        private static ConfigEntry<bool>? _serverConfigLocked;
        //internal static ConfigEntry<RecipeData> masterSyncJson; // doesn't work
        private static readonly CustomSyncedValue<string> skillConfigData = new(ConfigSync, "skillConfig", ""); // doesn't show up in config

        public static void testme()
        {
            /*
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cubeRenderer = cube.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_Color", Color.red); // now red cube
            
            var cubeCollider = cube.GetComponent<MeshRenderer>();
            //cubeCollider.materials.SetValue(inn)
            //var cubemodel = cube.GetComponent<>

                Object originalPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
            GameObject objSource = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;
            GameObject prefabVariant = PrefabUtility.SaveAsPrefabAsset(objSource, localPath);
            */
            //UnityEngine.Material.FindObjectOfType
        }

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
                ConfigSync.CurrentVersion = ModVersion;
                issettoSinglePlayer = false;
            }
            WackysRecipeCustomizationLogger.LogWarning("Mod Version " + ConfigSync.CurrentVersion);

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
                    //ObjectDB.instance.UpdateItemHashes(); // move to the end of updating all componets
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
            if (originalMaterials.Count <= 0) GetAllMaterials();// call materials // don't forget to do this for server
            //JsonSerializer.Serialize

            foreach (string file in Directory.GetFiles(assetPath, "*.json", SearchOption.AllDirectories))
            {
                if (file.Contains("Item") || file.Contains("item")) // items are being rather mean with the damage classes
                {
                    //Items = new List<WDamages>();//string content = File.ReadAllText(file);
                    // string[] divide = content.Split('}');
                    // WItemData data = JsonUtility.FromJson<WItemData>(content);// WItemData data = JsonUtility.FromJson<WItemData>(divide[0]);
                    try
                    {
                        WItemData data = JsonUtility.FromJson<WItemData>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append("@");
                        ItemDatas.Add(data);
                    }
                    catch { WackysRecipeCustomizationLogger.LogWarning("Something went wrong in file " + file); }

                }
                else
                {
                    try
                    {
                        RecipeData data = JsonUtility.FromJson<RecipeData>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append("@");
                        recipeDatas.Add(data);
                    }
                    catch { WackysRecipeCustomizationLogger.LogWarning("Something went wrong in file " + file); }

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
            foreach (string file in Directory.GetFiles(assetPath, "*.json", SearchOption.AllDirectories))
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
            bool skip = false;
            foreach (var citem in Cloned)
            {
                if (citem == data.name)
                    skip = true;   
            }
            string tempname = data.name;
            if (data.clone && !skip)
            {
                data.name = data.clonePrefabName;
            }
            GameObject go = ObjectDB.instance.GetItemPrefab(data.name);
            if (go == null)
            {
                if (data.clone)
                {
                    data.name = tempname; // change back 
                }
                SetPieceRecipeData(data);
               // Dbgl("maybe null " + data.name);
                return;
            }

            if (go.GetComponent<ItemDrop>() == null)
            {
                Dbgl($"Item recipe data for {data.name} not found!");
                return;
            } // it is a prefab and it is an item.
            if (data.clone && !skip)
            {
                if (!data.disabled)
                {
                    Dbgl("Setting CLONED Recipe for " + tempname);
                    Recipe clonerecipe = ScriptableObject.CreateInstance<Recipe>();
                    Cloned.Add(tempname); // add to list

                    //int hash = tempname.GetStableHashCode();
                    //Dictionary<int, GameObject> namedPrefabs = ZNetScene.instance.m_namedPrefabs;
                   // bool check = namedPrefabs.TryGetValue(hash, out var value); // check if hash is already in the system

                    clonerecipe.m_item = go.GetComponent<ItemDrop>();
                    clonerecipe.m_craftingStation = GetCraftingStation(data.craftingStation);
                    clonerecipe.m_minStationLevel = data.minStationLevel;
                    clonerecipe.m_amount = data.amount;
                    clonerecipe.name = tempname; //maybe

                    List<Piece.Requirement> reqs = new List<Piece.Requirement>();

                    // Dbgl("Made it to RecipeData!");
                    foreach (string req in data.reqs)
                    {
                        if (!string.IsNullOrEmpty(req))
                        {
                            string[] array = req.Split(':'); // safer vewrsion
                            string itemname = array[0];
                            if (ObjectDB.instance.GetItemPrefab(itemname))
                            {
                                int amount = ((array.Length < 2) ? 1 : int.Parse(array[1]));
                                int amountPerLevel = ((array.Length < 3) ? 1 : int.Parse(array[2]));
                                bool recover = array.Length != 4 || bool.Parse(array[3]);
                                Piece.Requirement item = new Piece.Requirement
                                {
                                    m_amount = amount,
                                    m_recover = recover,
                                    m_resItem = ObjectDB.instance.GetItemPrefab(itemname).GetComponent<ItemDrop>(),
                                    m_amountPerLevel = amountPerLevel
                                };
                                reqs.Add(item);
                            }
                        }
                    }// foreach
                    int index = 0;
                    clonerecipe.m_resources = reqs.ToArray();
                    for (int i = ObjectDB.instance.m_recipes.Count - 1; i > 0; i--)
                    {
                        if (ObjectDB.instance.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                        {
                            index = i++; // some extra resourses, but I think it's worth it
                            break;
                        }
                    }
                            ObjectDB.instance.m_recipes.Insert(index,clonerecipe);
                }
                else
                {
                    Dbgl("Cloned Recipe is disabled for " + data.clonePrefabName + " Will not unload if already loaded");
                    return;
                }
            }
            else
            {

                for (int i = ObjectDB.instance.m_recipes.Count - 1; i > 0; i--)
                {
                    if (ObjectDB.instance.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                    {
                        // if not clone normal edit
                        Dbgl("Setting Recipe for " + data.name);
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
                        // Dbgl("Amost done with RecipeData!");
                        ObjectDB.instance.m_recipes[i].m_resources = reqs.ToArray();
                        return;
                    } // end normal
                } // checking recipes
            }
        }

        private static void SetPieceRecipeData(RecipeData data)
        {
            bool skip = false;
            foreach (var citem in Cloned)
            {
                if (citem == data.name)
                    skip = true;
            }
            string tempname = data.name;
            if (data.clone && !skip)
            {
                data.name = data.clonePrefabName;
            }
            GameObject go = GetPieces().Find(g => Utils.GetPrefabName(g) == data.name);
            if (go == null)
            {
                Dbgl($"Item {data.name} not found in SetPiece!");
                return;
            }
            if (go.GetComponent<Piece>() == null)
            {
                Dbgl($"Item data for {data.name} not found!");
                return;
            }
            if (data.clone && !skip) // object is a clone do clonethings
                {
                Dbgl($"Item CLONE DATA in SetPiece for {tempname} ");
                Piece NewItemComp = Instantiate(go.GetComponent<Piece>());
                //Dbgl($"Item CLONE DATA Part-1 {tempname} ");
                GameObject newItem = NewItemComp.gameObject;
                Cloned.Add(tempname); // check against
               // Dbgl($"Item CLONE DATA Part0 {tempname} ");
                newItem.name = tempname; // resets the orginal name- needs to be unquie
                NewItemComp.name = tempname; // ingame name
                 
                //Dbgl($"Item CLONE DATA Part1 {tempname} ");
                var hash = newItem.name.GetStableHashCode();   
                ZNetScene znet = ZNetScene.instance;
                if (znet)
                {
                    string name = newItem.name;
                    //int hash2 = name.GetStableHashCode();

                    if (znet.m_namedPrefabs.ContainsKey(hash))
                    {
                        Dbgl($"Prefab {name} already in ZNetScene");
                    }
                    else
                    {
                        if (newItem.GetComponent<ZNetView>() != null)
                        {
                            znet.m_prefabs.Add(newItem);
                        }
                        else
                        {
                            znet.m_nonNetViewPrefabs.Add(newItem);
                        }
                        znet.m_namedPrefabs.Add(hash, newItem);
                        Dbgl($"Added prefab {name}");
                    }
                }
                //ObjectDB.instance.m_items.Add(newItem);
                CraftingStation craft = GetCraftingStation(data.craftingStation);
                newItem.GetComponent<Piece>().m_craftingStation = craft; // sets crafing item place

                //Dbgl($"Item CLONE DATA Part2 {tempname} ");
                GameObject piecehammer = ObjectDB.instance.GetItemPrefab(data.piecehammer);
                //if (piecehammer != null)
               // {
                    Dbgl($"piecehammer named {data.piecehammer} will not be used. I am lazy. Pieces will only be added to Hammer");
                    piecehammer = ObjectDB.instance.GetItemPrefab("Hammer");
                //}
                 piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(newItem); // added to Piecehammer or morelikely Hammer  - Just don't mess with right now
                //Dbgl($"Item CLONE DATA Part3 {tempname} ");


                data.name = tempname; // putting back name
                
                //Dbgl($"Item CLONE DATA Part4 {tempname} ");
                go = GetPieces().Find(g => Utils.GetPrefabName(g) == data.name); // just verifying
                if (go == null)
                {
                    Dbgl($"Item {data.name} not found in SetPiece! after clone");
                    return;
                }
                if (go.GetComponent<Piece>() == null)
                {
                    Dbgl($"Item data for {data.name} not found! after clone");
                    return;
                }
                go.GetComponent<Piece>().m_name = tempname; // set pieces name
                //ObjectDB.instance.UpdateItemHashes(); // end of clone
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
            Dbgl("Setting Piece data for " + data.name);
            go.GetComponent<Piece>().m_craftingStation = GetCraftingStation(data.craftingStation);
            if (data.minStationLevel > 1)
            {
                pieceWithLvl.Add(go.name + "." + data.minStationLevel);
            }
            List<Piece.Requirement> reqs = new List<Piece.Requirement>();
            foreach (string req in data.reqs)
            {
                string[] parts = req.Split(':');
                reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
            }
            //Dbgl("Amost done with setpiece!");
            go.GetComponent<Piece>().m_resources = reqs.ToArray();
            ObjectDB.instance.UpdateItemHashes();
           // Dbgl("done with setpiece!");
        }

        private static void SetItemData(WItemData data)
        {
            // Dbgl("Loaded SetItemData!");
            bool skip = false;
            foreach (var citem in Cloned)
            {
                if (citem == data.name)
                    skip = true;
            }
            string tempname = data.name;
            if (data.clone && !skip)
            {
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
            if (string.IsNullOrEmpty(tempname) && data.clone)
            {
                Dbgl($"Item cloned name is empty!");
                return;
            }
            for (int i = ObjectDB.instance.m_items.Count - 1; i > 0; i--)  // need to handle clones
            {
                if (ObjectDB.instance.m_items[i]?.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name) //if (ObjectDB.instance.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                {
                    ItemDrop.ItemData PrimaryItemData = ObjectDB.instance.m_items[i].GetComponent<ItemDrop>().m_itemData;
                    if (data.clone && !skip) // object is a clone do clonethings
                    {
                        Dbgl($"Item CLONE DATA in SetItemData for {tempname} ");
                        ItemDrop NewItemComp = Instantiate(go.GetComponent<ItemDrop>());
                        GameObject newItem = NewItemComp.gameObject;
                        Cloned.Add(tempname);

                        newItem.name = tempname; // resets the orginal name- needs to be unquie
                        NewItemComp.m_itemData.m_shared.m_name = tempname; // ingame name
                        //ZNetScene.instance.m_prefabs.Add(newItem); // znetscene
                        //ObjectDB.instance.m_items.Add(newItem); // add new GameObject to item list
                        var hash = newItem.name.GetStableHashCode();
                        ObjectDB.instance.m_items.Add(newItem);
                        ZNetScene znet = ZNetScene.instance;
                        if (znet)
                        {
                            string name = newItem.name;
                            //int hash2 = name.GetStableHashCode();

                            if (znet.m_namedPrefabs.ContainsKey(hash))
                            {
                                Dbgl($"Prefab {name} already in ZNetScene");
                            }
                            else
                            {
                                if (newItem.GetComponent<ZNetView>() != null)
                                {
                                    znet.m_prefabs.Add(newItem);
                                }
                                else
                                {
                                    znet.m_nonNetViewPrefabs.Add(newItem);
                                }
                                znet.m_namedPrefabs.Add(hash, newItem);
                                Dbgl($"Added prefab {name}");
                            }
                        }
                        Material fireme = originalMaterials["weapons1_fire"];
                       // go.GetComponentInChildren<Renderer>().materials.AddItem(fireme);
                           // Append(originalMaterials["weapons1_fire"]); // only for cloned

                         PrimaryItemData = ObjectDB.instance.GetItemPrefab(tempname).GetComponent<ItemDrop>().m_itemData; // get ready to set stuff
                         data.name = tempname; // putting back name
                         ObjectDB.instance.UpdateItemHashes();
                    }
                    
                    Dbgl($"Item being Set in SetItemData for {data.name} ");
                    /*Dbgl($"Itemdamage for SetItemData for {data.m_damages.m_slash} ");
                    foreach (PropertyInfo prop in typeof(WDamages).GetProperties())
                    {
                        Dbgl($"Itemdamage for SetItemData for {data.name} is " + prop.GetValue(data.m_damages, null));
                    }
                    */

                    if (data.m_damages != null && data.m_damages != "")
                    {
                        Dbgl($"   {data.name} Item has damage values ");
                        // has to be in order, should be
                        char[] delims = new[] { ',' };
                        string[] divideme = data.m_damages.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                        //Dbgl($"Item damge for 0 {divideme[0]} " + $" Item damge for 10 {divideme[10]} ");
                        HitData.DamageTypes damages = default(HitData.DamageTypes);
                        damages.m_blunt = stringtoFloat(divideme[0]);
                        damages.m_chop = stringtoFloat(divideme[1]);
                        damages.m_damage = stringtoFloat(divideme[2]);
                        damages.m_fire = stringtoFloat(divideme[3]);
                        damages.m_frost = stringtoFloat(divideme[4]);
                        damages.m_lightning = stringtoFloat(divideme[5]);
                        damages.m_pickaxe = stringtoFloat(divideme[6]);
                        damages.m_pierce = stringtoFloat(divideme[7]);
                        damages.m_poison = stringtoFloat(divideme[8]);
                        damages.m_slash = stringtoFloat(divideme[9]);
                        damages.m_spirit = stringtoFloat(divideme[10]);
                        PrimaryItemData.m_shared.m_damages = damages;


                        /*
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
                        */
                    }
                    if (data.m_damagesPerLevel != null && data.m_damagesPerLevel != "")
                    {
                        char[] delims = new[] { ',' };
                        string[] divideme = data.m_damagesPerLevel.Split(delims, StringSplitOptions.RemoveEmptyEntries);

                        HitData.DamageTypes damagesPerLevel = default(HitData.DamageTypes);
                        damagesPerLevel.m_blunt = stringtoFloat(divideme[0]);
                        damagesPerLevel.m_chop = stringtoFloat(divideme[1]);
                        damagesPerLevel.m_damage = stringtoFloat(divideme[2]);
                        damagesPerLevel.m_fire = stringtoFloat(divideme[3]);
                        damagesPerLevel.m_frost = stringtoFloat(divideme[4]);
                        damagesPerLevel.m_lightning = stringtoFloat(divideme[5]);
                        damagesPerLevel.m_pickaxe = stringtoFloat(divideme[6]);
                        damagesPerLevel.m_pierce = stringtoFloat(divideme[7]);
                        damagesPerLevel.m_poison = stringtoFloat(divideme[8]);
                        damagesPerLevel.m_slash = stringtoFloat(divideme[9]);
                        damagesPerLevel.m_spirit = stringtoFloat(divideme[10]);
                        PrimaryItemData.m_shared.m_damagesPerLevel = damagesPerLevel;
                    }
                    PrimaryItemData.m_shared.m_name = data.m_name;
                    PrimaryItemData.m_shared.m_description = data.m_description;
                    PrimaryItemData.m_shared.m_weight = data.m_weight;
                    PrimaryItemData.m_shared.m_maxStackSize = data.m_maxStackSize;
                    PrimaryItemData.m_shared.m_food = data.m_foodHealth;
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
                    PrimaryItemData.m_shared.m_movementModifier = data.m_movementModifier;
                    PrimaryItemData.m_shared.m_attack.m_attackStamina = data.m_attackStamina;

                    
                }
                // Dbgl("Amost done with SetItemData!");
            }

        }


        #endregion
        #region GetObject

        private static CraftingStation GetCraftingStation(string name)
        {
            if (name == "" || name == null)
                return null;
                
            //Dbgl("Looking for crafting station " + name);

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe?.m_craftingStation?.m_name == name)
                {

                  //  Dbgl("got crafting station " + name);

                    return recipe.m_craftingStation;
                }
            }
            foreach (GameObject piece in GetPieces())
            {

                if (piece.GetComponent<Piece>()?.m_craftingStation?.m_name == name)
                {

                   // Dbgl("got crafting station " + name);

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
            string piecehammer = "Hammer";
            ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
            if (hammer && hammer.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
            {
                piecehammer = "Hammer"; 
                
            }
            ItemDrop hoe = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();

            if (hoe && hoe.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(go))
            {
                piecehammer = "Hoe";
                
            }
            piecehammer = "unused right now";


            var data = new RecipeData()
            {
                name = name,
                amount = 1,
                craftingStation = piece.m_craftingStation?.m_name ?? "",
                minStationLevel = 1,
                piecehammer = piecehammer,
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
            string damagestring ="";
           // Dbgl("Item "+ name + " data.m_shared.m_damages.mslash" + data.m_shared.m_damages.m_slash);
            if (data.m_shared.m_damages.m_blunt > 0f || data.m_shared.m_damages.m_chop > 0f || data.m_shared.m_damages.m_damage > 0f || data.m_shared.m_damages.m_fire > 0f || data.m_shared.m_damages.m_frost > 0f || data.m_shared.m_damages.m_lightning > 0f || data.m_shared.m_damages.m_pickaxe > 0f || data.m_shared.m_damages.m_pierce > 0f || data.m_shared.m_damages.m_poison > 0f || data.m_shared.m_damages.m_slash > 0f || data.m_shared.m_damages.m_spirit > 0f)
            {
                Dbgl("Item " + name + " damage on ");

                damages = new WDamages // not used
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
                 damagestring = $"m_blunt:{data.m_shared.m_damages.m_blunt},"
                + $"m_chop:{data.m_shared.m_damages.m_chop},"
                + $"m_damage:{data.m_shared.m_damages.m_damage},"
                + $"m_fire:{data.m_shared.m_damages.m_fire},"
                + $"m_frost:{data.m_shared.m_damages.m_frost},"
                + $"m_lightning:{data.m_shared.m_damages.m_lightning},"
                + $"m_pickaxe:{data.m_shared.m_damages.m_pickaxe},"
                + $"m_pierce:{data.m_shared.m_damages.m_pierce},"
                + $"m_poison:{data.m_shared.m_damages.m_poison},"
                + $"m_slash:{data.m_shared.m_damages.m_slash},"
                + $"m_spirit:{data.m_shared.m_damages.m_spirit},"

                ;
                damagestring = damagestring.Replace(",", ", " );
            }
            WDamages damagesPerLevel = null;
            string damgelvlstring = "";
            if (data.m_shared.m_damagesPerLevel.m_blunt > 0f || data.m_shared.m_damagesPerLevel.m_chop > 0f || data.m_shared.m_damagesPerLevel.m_damage > 0f || data.m_shared.m_damagesPerLevel.m_fire > 0f || data.m_shared.m_damagesPerLevel.m_frost > 0f || data.m_shared.m_damagesPerLevel.m_lightning > 0f || data.m_shared.m_damagesPerLevel.m_pickaxe > 0f || data.m_shared.m_damagesPerLevel.m_pierce > 0f || data.m_shared.m_damagesPerLevel.m_poison > 0f || data.m_shared.m_damagesPerLevel.m_slash > 0f || data.m_shared.m_damagesPerLevel.m_spirit > 0f)
            {
                damagesPerLevel = new WDamages // not used
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
                 damgelvlstring = $"m_blunt:{data.m_shared.m_damagesPerLevel.m_blunt},"
                + $"m_chop:{data.m_shared.m_damagesPerLevel.m_chop},"
                + $"m_damage:{data.m_shared.m_damagesPerLevel.m_damage},"
                + $"m_fire:{data.m_shared.m_damagesPerLevel.m_fire},"
                + $"m_frost:{data.m_shared.m_damagesPerLevel.m_frost},"
                + $"m_lightning:{data.m_shared.m_damagesPerLevel.m_lightning},"
                + $"m_pickaxe:{data.m_shared.m_damagesPerLevel.m_pickaxe},"
                + $"m_pierce:{data.m_shared.m_damagesPerLevel.m_pierce},"
                + $"m_poison:{data.m_shared.m_damagesPerLevel.m_poison},"
                + $"m_slash:{data.m_shared.m_damagesPerLevel.m_slash},"
                + $"m_spirit:{data.m_shared.m_damagesPerLevel.m_spirit},"

                ;
                damgelvlstring = damgelvlstring.Replace(",", ", " );
            }
            /*
             * f
            foreach (Piece.Requirement req in piece.m_resources) // maybe use in future
            {
                data.reqs.Add($"{Utils.GetPrefabName(req.m_resItem.gameObject)}:{req.m_amount}:{req.m_amountPerLevel}:{req.m_recover}");
            }*/

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
                m_foodHealth = data.m_shared.m_food,
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
                m_damages = damagestring,
                m_damagesPerLevel = damgelvlstring,
                m_name = data.m_shared.m_name,
                m_questItem = data.m_shared.m_questItem,
                m_teleportable = data.m_shared.m_teleportable,
                m_timedBlockBonus = data.m_shared.m_timedBlockBonus,
                m_movementModifier = data.m_shared.m_movementModifier,
                m_attackStamina = data.m_shared.m_attack.m_attackStamina,
            };
           // Dbgl("Item " + name + " damages " + damages.m_slash); // I think damages is being overwritten?
            if (jItemData.m_foodHealth == 0f && jItemData.m_foodRegen == 0f && jItemData.m_foodStamina == 0f)
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
                    return;
                if (SceneManager.GetActiveScene().name != "main") return; // can't do anything from main

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
                            + $"wackydb_dump <item/recipe> <ItemName>\r\n"
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
                                 args.Context?.AddString($"WackyDatabase reloaded recipes/items from files");
                                 Dbgl("WackyDatabase reloaded recipes/items from files");
                             }
                             else
                             {
                                 args.Context?.AddString($"WackyDatabase did NOT reload recipes/items from files"); // maybe?
                                 Dbgl("WackyDatabase did NOT reload recipes/items from files");
                             }
                             
                         });

                Terminal.ConsoleCommand WackyDump =
                     new("wackydb_dump", "dump the item or recipe into the logs",
                         args =>
                         {
                             if (args.Length - 1 < 2) 
                             {
                                 args.Context?.AddString("Not enough arguments");

                             }
                             else
                             {
                                 string recipe = args[1];
                                 string comtype = args[2];
                                 if (recipe == "item")
                                 {
                                     WItemData recipeData = GetItemDataByName(comtype);
                                     if (recipeData == null)
                                         return;
                                     Dbgl(JsonUtility.ToJson(recipeData));

                                 }
                                 else
                                 {
                                     RecipeData recipeData = GetRecipeDataByName(comtype);
                                     if (recipeData == null)
                                         return;
                                     Dbgl(JsonUtility.ToJson(recipeData));
                                 }
                                 args.Context?.AddString($"WackyDatabase dumped {comtype}");
                             }
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
                            if (args.Length - 1 < 3)
                            {
                                args.Context?.AddString("<color=lime>Not enough arguments</color>");

                            }
                            else
                            {
                                string commandtype = args[1];
                                string prefab = args[2];
                                string newname = args[3];
                                string file = args[3];

                                if (commandtype == "recipe" || commandtype == "Recipe")
                                {

                                    RecipeData clone = GetRecipeDataByName(prefab);
                                    if (clone == null)
                                        return;
                                    clone.name = newname;
                                    clone.clone = true;
                                    clone.clonePrefabName = prefab;


                                    if (clone == null)
                                        return;
                                    CheckModFolder();
                                    File.WriteAllText(Path.Combine(assetPath, clone.name + ".json"), JsonUtility.ToJson(clone, true));

                                }
                                if (commandtype == "item" || commandtype == "Item")
                                {
                                    WItemData clone = GetItemDataByName(prefab);
                                    if (clone == null)
                                        return;
                                    clone.name = newname;
                                    clone.clone = true;
                                    clone.clonePrefabName = prefab;


                                    if (clone == null)
                                        return;
                                    CheckModFolder();
                                    File.WriteAllText(Path.Combine(assetPath, "Item_" + clone.name + ".json"), JsonUtility.ToJson(clone, true));
                                    file = "Item_" + clone.name;



                                }
                                if (commandtype == "piece" || commandtype == "Piece")
                                {
                                    RecipeData clone = GetPieceRecipeByName(prefab);
                                    if (clone == null)
                                        return;
                                    clone.name = newname;
                                    clone.clone = true;
                                    clone.clonePrefabName= prefab;
                                    


                                    if (clone == null)
                                        return;
                                    CheckModFolder();
                                    File.WriteAllText(Path.Combine(assetPath, clone.name + ".json"), JsonUtility.ToJson(clone, true));

                                }
                                args.Context?.AddString($"saved cloned data to {file}.json");
                            }
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
                    string secrethash = "f289b4717485d90d9dee6ce2a9992e4fcfa4317a9439c148053d52c637b0691b"; // real hash is entered
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
        public static float stringtoFloat(string data)
        {
            data = data.Split(':').Last();
            float value = float.Parse(data, CultureInfo.InvariantCulture.NumberFormat);
            return value;
        }
        private static Dictionary<string, Material> originalMaterials;
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


        #endregion

    }
}
/*



Material replacer


			MaterialReplacer.GetAllMaterials();
			prefabRoot = new GameObject("CartPrefabs");
			prefabRoot.SetActive(false);
			Object.DontDestroyOnLoad((Object)(object)prefabRoot);
			GameObject prefab = Cache.GetPrefab<GameObject>("Cart");
			GameObject val = Object.Instantiate<GameObject>(prefab, prefabRoot.get_transform());
			Object.DestroyImmediate((Object)(object)((Component)val.get_transform().Find("AttachPoint")).get_gameObject());
			Object.DestroyImmediate((Object)(object)((Component)val.get_transform().Find("Wheel1").GetChild(0)).get_gameObject());
			Object.DestroyImmediate((Object)(object)((Component)val.get_transform().Find("Wheel2").GetChild(0)).get_gameObject());
			Object.DestroyImmediate((Object)(object)((Component)val.get_transform().Find("Container")).get_gameObject());
			Object.DestroyImmediate((Object)(object)((Component)val.get_transform().Find("Vagon")).get_gameObject());
			Object.DestroyImmediate((Object)(object)((Component)val.get_transform().Find("load")).get_gameObject());
			Object.DestroyImmediate((Object)(object)((Component)val.get_transform().Find("LineAttach0")).get_gameObject());
			Object.DestroyImmediate((Object)(object)((Component)val.get_transform().Find("LineAttach1")).get_gameObject());
			Object.DestroyImmediate((Object)(object)((Component)val.get_transform().Find("cart_Destruction")).get_gameObject());
			prefab.GetComponent<Rigidbody>().set_mass(50f);
			((Object)val).set_name("CraftyCarts.WorkbenchCart");
			GameObject val2 = Object.Instantiate<GameObject>(val, prefabRoot.get_transform());
			((Object)val2).set_name("CraftyCarts.StoneCart");
			GameObject val3 = Object.Instantiate<GameObject>(val, prefabRoot.get_transform());
			((Object)val3).set_name("CraftyCarts.ForgeCart");
			AssetBundle val4 = AssetBundle.LoadFromMemory(ResourceUtils.GetResource(Assembly.GetExecutingAssembly(), "CraftyCarts.Resources.carts"));
			GameObject model = val4.LoadAsset<GameObject>("workbench_cart");
			Sprite icon = val4.LoadAsset<Sprite>("workbenchcarticon");
			GameObject model2 = val4.LoadAsset<GameObject>("forge_cart");
			Sprite icon2 = val4.LoadAsset<Sprite>("forgecarticon");
			GameObject model3 = val4.LoadAsset<GameObject>("stone_cart");
			Sprite icon3 = val4.LoadAsset<Sprite>("stonecarticon");
			val4.Unload(false);
			Debug.Log((object)"add crafting station");
			CraftingStation component = Cache.GetPrefab<GameObject>("piece_workbench").GetComponent<CraftingStation>();
			CraftingStation component2 = Cache.GetPrefab<GameObject>("piece_stonecutter").GetComponent<CraftingStation>();
			CraftingStation component3 = Cache.GetPrefab<GameObject>("forge").GetComponent<CraftingStation>();
			Piece piece = SetupCraftingStation(model, component, val);
			Piece piece2 = SetupCraftingStation(model2, component3, val3);

 public static class MaterialReplacer
    {
        static MaterialReplacer()
        {
            originalMaterials = new Dictionary<string, Material>();
            ObjectToSwap = new Dictionary<GameObject, bool>();
            Harmony harmony = new("org.bepinex.helpers.PieceManager");
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ZoneSystem), nameof(ZoneSystem.Start)),
                postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MaterialReplacer),
                    nameof(GetAllMaterials))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ZoneSystem), nameof(ZoneSystem.Start)),
                postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MaterialReplacer),
                    nameof(ReplaceAllMaterialsWithOriginal))));
        }

        private static Dictionary<GameObject, bool> ObjectToSwap;
        internal static Dictionary<string, Material> originalMaterials;

        public static void RegisterGameObjectForMatSwap(GameObject go, bool isJotunnMock = false)
        {
            ObjectToSwap.Add(go, isJotunnMock);
        }
        
        [HarmonyPriority(Priority.VeryHigh)]
        private static void GetAllMaterials()
        {
            var allmats = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var item in allmats)
            {
                originalMaterials[item.name] = item;
            }
        }
        
        [HarmonyPriority(Priority.VeryHigh)]
        private static void ReplaceAllMaterialsWithOriginal()
        {
            if(originalMaterials.Count <= 0) GetAllMaterials();
            foreach (var renderer in ObjectToSwap.SelectMany(gameObject => gameObject.Key.GetComponentsInChildren<Renderer>(true)))
            {
                ObjectToSwap.TryGetValue(renderer.gameObject, out bool jotunnPrefabFlag);
                foreach (var t in renderer.materials)
                {
                    if (jotunnPrefabFlag)
                    {
                        if (!t.name.StartsWith("JVLmock_")) continue;
                        var matName = renderer.material.name.Replace(" (Instance)", string.Empty).Replace("_REPLACE_", "");

                        if (originalMaterials!.ContainsKey(matName))
                        {
                            renderer.material = originalMaterials[matName];
                        }
                        else
                        {
                            Debug.LogWarning("No suitable material found to replace: " + matName);
                            // Skip over this material in future
                            originalMaterials[matName] = renderer.material;
                        }
                    }
                    else
                    {
                        if (!t.name.StartsWith("_REPLACE_")) continue;
                        var matName = renderer.material.name.Replace(" (Instance)", string.Empty).Replace("_REPLACE_", "");

                        if (originalMaterials!.ContainsKey(matName))
                        {
                            renderer.material = originalMaterials[matName];
                        }
                        else
                        {
                            Debug.LogWarning("No suitable material found to replace: " + matName);
                            // Skip over this material in future
                            originalMaterials[matName] = renderer.material;
                        }   
                    }
                    
                }
            }
        }
    }
*/