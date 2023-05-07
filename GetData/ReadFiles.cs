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
using wackydatabase.GetData;
using System.Runtime.InteropServices.ComTypes;
using System.Collections;
using wackydatabase.OBJimporter;



namespace wackydatabase.Read
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
                if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated() && WMRecipeCust.enableYMLWatcher.Value || ZNet.instance.IsServer() && WMRecipeCust.isSettoAutoReload && WMRecipeCust.enableYMLWatcher.Value)
                {  // should only load for the server now
                    if (WMRecipeCust.Reloading)
                        return;
                    WMRecipeCust.Dbgl("YML files have changed. Server or Singleplayer and Autoreload is on");
                    WMRecipeCust.context.StartCoroutine(GetDataFromFiles()); // load stuff in mem
                    WMRecipeCust.context.StartCoroutine(StartReloadingTimer());
                    WMRecipeCust.skillConfigData.Value = WMRecipeCust.ymlstring; //Sync Event // Single player forces client to reload as well. 
                    WMRecipeCust.Reloading = true;
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
        public void GetCacheClonesOnly()
        {
            WMRecipeCust.cacheDataYML.Clear();
            YamlLoader cache = new YamlLoader(); // cache Only

            foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathCache, "*.zz", SearchOption.AllDirectories))
            {
                cache.Load<WItemData>(file, WMRecipeCust.cacheDataYML);
            }
        }
        internal IEnumerator StartReloadingTimer()
        {

             yield return new WaitForSeconds(10); // wait 10 seconds before another reload can be called
            WMRecipeCust.Reloading = false;
            

        }
            internal IEnumerator GetDataFromFiles(bool slowmode = false)
        {
            //wackydatabase.WMRecipeCust.WLog.LogWarning("Running Get DataFromFiles");

            WMRecipeCust.recipeDatas.Clear();
            WMRecipeCust.ItemDatas.Clear();
            WMRecipeCust.PieceDatas.Clear();
            WMRecipeCust.recipeDatasYml.Clear();
            WMRecipeCust.itemDatasYml.Clear();
            WMRecipeCust.pieceDatasYml.Clear();
            WMRecipeCust.armorDatas.Clear();
            WMRecipeCust.pieceWithLvl.Clear(); // ready for new
            WMRecipeCust.visualDatasYml.Clear();
            WMRecipeCust.effectDataYml.Clear();
            WMRecipeCust.ymlstring = ""; //clear

            WMRecipeCust.CheckModFolder();

            YamlLoader yaml = new YamlLoader();
            int processcount = 0;

            if (WMRecipeCust.Firstrun)
            {
                ObjModelLoader.LoadObjs(); // This means will never get sync data, but that's okay?
            }

            if (slowmode)
            {
                WMRecipeCust.WLog.LogInfo($"Beginning SLOW Reading");
                WMRecipeCust.LockReload = true;
            }

                foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathconfig, "?tem_*.yml", SearchOption.AllDirectories))
            {
                yaml.Load<WItemData>(file, WMRecipeCust.itemDatasYml);

                processcount++;
                if (processcount > WMRecipeCust.ProcessWaitforRead && slowmode)
                {
                    yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                    processcount = 0;
                }
            }

            foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathconfig, "?iece_*.yml", SearchOption.AllDirectories))
            {
                yaml.Load<PieceData>(file, WMRecipeCust.pieceDatasYml);

                processcount++;
                if (processcount > WMRecipeCust.ProcessWaitforRead && slowmode)
                {
                    yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                    processcount = 0;
                }
            }

            foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathconfig, "?ecipe_*.yml", SearchOption.AllDirectories))
            {
                yaml.Load<RecipeData>(file, WMRecipeCust.recipeDatasYml);

                processcount++;
                if (processcount > WMRecipeCust.ProcessWaitforRead && slowmode)
                {
                    yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                    processcount = 0;
                }
            }

            foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathconfig, "?isual_*.yml", SearchOption.AllDirectories))
            {
                yaml.Load<VisualData>(file, WMRecipeCust.visualDatasYml);

                processcount++;
                if (processcount > WMRecipeCust.ProcessWaitforRead && slowmode)
                {
                    yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                    processcount = 0;
                }
            }

            foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathconfig, "SE_*.yml", SearchOption.AllDirectories))
            {
                yaml.Load<StatusData>(file, WMRecipeCust.effectDataYml);

                processcount++;
                if (processcount > WMRecipeCust.ProcessWaitforRead && slowmode)
                {
                    yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                    processcount = 0;
                }
            }

            WMRecipeCust.ymlstring = yaml.ToString();//(WMRecipeCust.itemDatasYml.ToString() + WMRecipeCust.pieceDatasYml.ToString() + WMRecipeCust.recipeDatasYml + WMRecipeCust.visualDatasYml + WMRecipeCust.effectDataYml).ToString();


            YamlLoader cache = new YamlLoader(); // cache Only

            foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathCache, "*.zz", SearchOption.AllDirectories))
            {
                cache.Load<WItemData>(file, WMRecipeCust.cacheDataYML);

                processcount++;
                if (processcount > WMRecipeCust.ProcessWaitforRead && slowmode)
                {
                    yield return new WaitForSeconds(WMRecipeCust.WaitTime);
                    processcount = 0;
                }
            }
            WMRecipeCust.LockReload = false;
            if (slowmode)
            {
                WMRecipeCust.WLog.LogInfo($"Finished SLOW Reading");
            }
            WMRecipeCust.WLog.LogDebug("Loaded YML files in ReadFiles");
            if (WMRecipeCust.isDebugString.Value)
            {
                WMRecipeCust.WLog.LogInfo(WMRecipeCust.ymlstring);
            }
        }
    }
}
