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
using YamlDotNet.Serialization;

namespace wackydatabase.Startup
{
    public class ReadFiles 
    {
        public void SetupWatcher()
        {
            WMRecipeCust.CheckModFolder();
            FileSystemWatcher watcher = new(WMRecipeCust.assetPathconfig); // jsons in config
            watcher.Changed += ReadYMLValues;
            watcher.Created += ReadYMLValues;
            watcher.Renamed += ReadYMLValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadYMLValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(WMRecipeCust.ConfigFileFullPath)) return;
            try
            {
                if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated() || ZNet.instance.IsServer() && WMRecipeCust.isSettoAutoReload)
                {  // should only load for the server now
                    WMRecipeCust.Dbgl("YML files have changed. Computer is some type of Server and Autoreload is on");
                    GetDataFromFiles(); // load stuff in mem
                    WMRecipeCust.skillConfigData.Value = WMRecipeCust.ymlstring; //Sync Event // Single player forces client to reload as well. 
                }
            }
            catch
            {
                //WLog.LogError($"There was an issue loading your Sync ");
                if (WMRecipeCust.issettoSinglePlayer)
                    WMRecipeCust.WLog.LogError("Please check your YML entries for spelling and format!");
                else
                {
                    WMRecipeCust.WLog.LogDebug("Not checking YML Files because either in Main Screen or ....");
                }
            }
        }
        public void GetDataFromFiles()
        {
            wackydatabase.WMRecipeCust.WLog.LogWarning("Running Get DataFromFiles");
            if (WMRecipeCust.AwakeHasRun && WMRecipeCust.Firstrun) 
            {
                WMRecipeCust.CheckModFolder();
                WMRecipeCust.GetAllMaterials();
                DataHelpers.GetPieceStations();
                DataHelpers.GetPiecesatStart();
                //WMRecipeCust.Firstrun = false;
            }
            WMRecipeCust.recipeDatas.Clear();
            WMRecipeCust.ItemDatas.Clear();
            WMRecipeCust.PieceDatas.Clear();
            WMRecipeCust.armorDatas.Clear();
            WMRecipeCust.pieceWithLvl.Clear(); // ready for new

            WMRecipeCust.CheckModFolder();
            var amber = new System.Text.StringBuilder();
            var deserializer = new DeserializerBuilder()
                  .Build();

            foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathconfig, "*.yml", SearchOption.AllDirectories))
            {
                if (file.Contains("Item") || file.Contains("item")) // items are being rather mean with the damage classes
                {
                    try
                    {
                        var yml = File.ReadAllText(file);
                        amber.Append(yml);
                        amber.Append(WMRecipeCust.StringSeparator);
                        WMRecipeCust.itemDatasYml.Add(deserializer.Deserialize<WItemData>(yml));

                        /* // I think this is no longer needed?
                        ArmorData_json data3 = JsonUtility.FromJson<ArmorData_json>(File.ReadAllText(file));
                        WMRecipeCust.armorDatasYml.Add(data3);
                        */
                    }
                    catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }

                }
                else if (file.Contains("Piece") || file.Contains("piece"))
                {
                    try
                    {
                        var yml = File.ReadAllText(file);
                        amber.Append(yml);
                        amber.Append(WMRecipeCust.StringSeparator);
                        WMRecipeCust.pieceDatasYml.Add(deserializer.Deserialize<PieceData>(yml));
                    }
                    catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }
                }
                else // recipes
                {
                    try
                    {
                        var yml = File.ReadAllText(file);
                        amber.Append(yml);
                        amber.Append(WMRecipeCust.StringSeparator);
                        WMRecipeCust.recipeDatasYml.Add(deserializer.Deserialize<RecipeData>(yml));
                    }
                    catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }

                }
            }
            WMRecipeCust.ymlstring = amber.ToString();

            WMRecipeCust.WLog.LogDebug("Loaded YML files");
            if (WMRecipeCust.isSetStringisDebug)
                WMRecipeCust.Dbgl(WMRecipeCust.ymlstring);
        }

       
    }
}
