// Most of the credit goes to  aedenthorn  and all of his Many Mods! https://github.com/aedenthorn/ValheimMods
// Thank you AzumattDev for the template. It is very good https://github.com/AzumattDev/ItemManagerModTemplate
// Thanks to the Odin Discord server, for being active and good for the valheim community.
// Do whatever you want with this mod. // except sale it as per Aedenthorn Permissions https://www.nexusmods.com/valheim/mods/1245
// Goal for this mod is RecipeCustomization + Recipe LVL station Requirement + Server Sync
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
//using ItemManager;
using ServerSync;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Bootstrap;
//using System;


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
        internal static string ConnectionError = "";
        private static WMRecipeCust context;
        //public static string consoleNamespace = "recipecustomization";

        public static ConfigEntry<float> globalArmorDurabilityLossMult;
        public static ConfigEntry<float> globalArmorMovementModMult;

        public static ConfigEntry<string> waterModifierName;

        private static List<RecipeData> recipeDatas = new List<RecipeData>();
        private static string assetPath;
        RecipeData paul = new RecipeData();
        private static string jsonstring;
        private static bool isaclient = false;
        public static bool Admin = false;
        private static List<string> pieceWithLvl = new List<string>();
        private static bool startupSync = true;

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




        public void Awake() // start
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            // ^^ // starting files
            context = this;
            modEnabled = config<bool>("General", "Enabled", true, "Enable this mod");
            isDebug= config<bool>("General", "IsDebug", true, "Enable debug logs", false);

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(WMRecipeCust).Namespace);

            //testifpossible();
            //WackysRecipeCustomizationLogger.LogDebug("testing logger");

            // ending files
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
            GetRecipeDataFromFilesForServer();
           skillConfigData.ValueChanged += CustomSyncEventDetected; // custom watcher for json file synced from server
        }

        private void testifpossible()
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


        #region AzumattDev Or ConfigOptions

        private static ConfigEntry<bool>? _serverConfigLocked;
        //internal static ConfigEntry<RecipeData> masterSyncJson; // doesn't work
        private static readonly CustomSyncedValue<string> skillConfigData = new(ConfigSync, "skillConfig", ""); // doesn't show up in config


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
        [HarmonyPatch(typeof(Terminal), "InputText")] // aedenthorn Json mod
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;

                string text = __instance.m_input.text;
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
                }
                return true;
            }
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

        #endregion
        #region Mostly AedenthornMod

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        [HarmonyPriority(Priority.Last)]
        static class ZNetScene_Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                context.StartCoroutine(DelayedLoadRecipes());
                //LoadAllRecipeData(true);
            }
        }
        public static IEnumerator DelayedLoadRecipes()
        {
            yield return new WaitForSeconds(0.1f);
            LoadAllRecipeData(true);
            yield break;
        }

        private static void LoadAllRecipeData(bool reload)
        {
            if (isaclient)
            { // log You are a client and not an admin
                WackysRecipeCustomizationLogger.LogError("You are a client and not the Server. Cannot reload recipes");
            }
            else { 
                if (reload)
                    GetRecipeDataFromFiles();
                foreach (var data in recipeDatas)
                {
                    SetRecipeData(data);
                }
            }
        }

        private void CustomSyncEventDetected()
        {
            // load up the files from skillConfigData
            // seperate them
            //reload
            // need to skip first call
            if (startupSync)
            {
                startupSync = false;
            }
            else
            {
                WackysRecipeCustomizationLogger.LogDebug("CustomSyncEventDetected was called ");
                recipeDatas.Clear();
                string SyncedString = skillConfigData.Value;
                if (SyncedString != null && SyncedString != "")
                {
                    string[] jsons = SyncedString.Split('.');
                    foreach (var word in jsons)
                    {
                        RecipeData data = JsonUtility.FromJson<RecipeData>(word);
                        recipeDatas.Add(data);
                        WackysRecipeCustomizationLogger.LogDebug(word);
                    }
                    foreach (var data in recipeDatas)
                    {
                        SetRecipeData(data);
                    }
                }else
                {
                    WackysRecipeCustomizationLogger.LogDebug("Synced String was blank " + SyncedString);
                }
                //isaclient = true; // don't allow reload
            }

        }

        private static void GetRecipeDataFromFiles()
        {
            CheckModFolder();

            recipeDatas.Clear();
            var amber = new System.Text.StringBuilder();
            //JsonSerializer.Serialize

            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                RecipeData data = JsonUtility.FromJson<RecipeData>(File.ReadAllText(file));
                amber.Append(File.ReadAllText(file));
                amber.Append(".");
                recipeDatas.Add(data);
                
            }

            jsonstring = amber.ToString();
            skillConfigData.Value = jsonstring;
            //WackysRecipeCustomizationLogger.LogDebug(jsonstring);
        }
        private static void GetRecipeDataFromFilesForServer()
        {
            CheckModFolder();
            var amber = new System.Text.StringBuilder();
            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                amber.Append(File.ReadAllText(file));
                amber.Append(".");
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
        private static Vector3 tempvalue;
        private static bool piecehaslvl;

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
                            var josh = skillConfigData.Value;
                            WackysRecipeCustomizationLogger.LogDebug("Synced String  " + josh);

                            //piecehaslvl = false;
                            return false;
                        }
                    }
                }
                return true;
            }

        }

        private static void SetRecipeData(RecipeData data)
        {
            GameObject go = ObjectDB.instance.GetItemPrefab(data.name);
            if (go == null)
            {
                SetPieceRecipeData(data);
                return;
            }
            if (go.GetComponent<ItemDrop>() == null)
            {
                Dbgl($"Item data for {data.name} not found!");
                return;
            }

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
                    foreach (string req in data.reqs)
                    {
                        string[] parts = req.Split(':');
                        reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
                    }
                    ObjectDB.instance.m_recipes[i].m_resources = reqs.ToArray();
                    return;
                }
            }
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
            if(data.minStationLevel > 1 )
            {
               pieceWithLvl.Add(go.name + "." + data.minStationLevel);
           }
            List<Piece.Requirement> reqs = new List<Piece.Requirement>();
            
            foreach (string req in data.reqs)
            {
                string[] parts = req.Split(':');
                reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
            }
            go.GetComponent<Piece>().m_resources = reqs.ToArray();

        }

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


        #endregion
    }
}