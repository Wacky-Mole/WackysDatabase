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

namespace wackydatabase.Startup
{
    public class ReadFiles 
    {
        public void SetupWatcher()
        {
            WMRecipeCust.CheckModFolder();
            FileSystemWatcher watcher = new(WMRecipeCust.assetPathconfig); // jsons in config
            watcher.Changed += ReadJsonValues;
            watcher.Created += ReadJsonValues;
            watcher.Renamed += ReadJsonValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadJsonValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(WMRecipeCust.ConfigFileFullPath)) return;
            try
            {
                if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated() || WMRecipeCust.issettoSinglePlayer && WMRecipeCust.isSettoAutoReload)
                {  // should only load for the server now
                    WMRecipeCust.Dbgl("Jsons files have changed and access is either on a dedicated server or singleplayer with autoreload on therefore reloading everything");
                    GetRecipeDataFromFiles(); // load stuff in mem
                    WMRecipeCust.skillConfigData.Value = WMRecipeCust.jsonstring; //Sync Event // Single player forces client to reload as well. 
                }
            }
            catch
            {
                //WackysRecipeCustomizationLogger.LogError($"There was an issue loading your Sync ");
                if (WMRecipeCust.issettoSinglePlayer)
                    WMRecipeCust.WackysRecipeCustomizationLogger.LogError("Please check your JSON entries for spelling and format!");
                else
                {
                    WMRecipeCust.WackysRecipeCustomizationLogger.LogDebug("Not checking Json Files because either in Main Screen or ....");
                }
            }
        }

        internal static void GetRecipeDataFromFiles()
        {
            if (WMRecipeCust.Firstrun)
            {
                WMRecipeCust.CheckModFolder();
                WMRecipeCust.GetAllMaterials();
                DataHelpers.GetPieceStations();
                DataHelpers.GetPiecesatStart();
                WMRecipeCust.Firstrun = false;
            }
            WMRecipeCust.recipeDatas.Clear();
            WMRecipeCust.ItemDatas.Clear();
            WMRecipeCust.PieceDatas.Clear();
            WMRecipeCust.armorDatas.Clear();
            WMRecipeCust.pieceWithLvl.Clear(); // ready for new
            var amber = new System.Text.StringBuilder();
            foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathconfig, "*.json", SearchOption.AllDirectories))
            {
                if (file.Contains("Item") || file.Contains("item")) // items are being rather mean with the damage classes
                {
                    try
                    {
                        WItemData data = JsonUtility.FromJson<WItemData>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append("@");
                        WMRecipeCust.ItemDatas.Add(data);
                        ArmorData data3 = JsonUtility.FromJson<ArmorData>(File.ReadAllText(file));
                        WMRecipeCust.armorDatas.Add(data3);
                    }
                    catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning("Something went wrong in file " + file); }

                }
                else if (file.Contains("Piece") || file.Contains("piece"))
                {
                    try
                    {
                        PieceData data = JsonUtility.FromJson<PieceData>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append("@");
                        WMRecipeCust.PieceDatas.Add(data);
                    }
                    catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning("Something went wrong in file " + file); }
                }
                else // recipes
                {
                    try
                    {
                        RecipeData data = JsonUtility.FromJson<RecipeData>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append("@");
                        WMRecipeCust.recipeDatas.Add(data);
                    }
                    catch { WMRecipeCust.WackysRecipeCustomizationLogger.LogWarning("Something went wrong in file " + file); }

                }
            }
        }
    }
}
