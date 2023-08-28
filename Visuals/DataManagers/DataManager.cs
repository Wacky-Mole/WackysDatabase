/***
 * This file manages configuration files of any type.
 * Includes a file system watcher that triggers events providing the objects parsed.
 */

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using ServerSync;
using BepInEx;
using System;
using wackydatabase.Datas;
using System.Security.Policy;

namespace wackydatabase
{
    public abstract class DataManager<T>
    {
        protected static readonly ColorConverter cc = new ColorConverter();
        protected static readonly ValheimTimeConverter vtc = new ValheimTimeConverter();

        protected FileSystemWatcher fileSystemWatcher;

        public static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(cc)
            .WithTypeConverter(vtc)
            .Build();

        public static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(cc)
            .WithTypeConverter(vtc)
            .Build();

        protected string Storage { get; set; }
        protected string Name { get; set; }

        public CustomSyncedValue<Dictionary<string, string>> YamlData { get; set; }

        public event EventHandler<EventArgs> OnSync;
        public event EventHandler<DataEventArgs<T>> OnDataChange;

        public DataManager(string path, string name)
        {
            Storage = path;
            Name = name;

            YamlData = new(WMRecipeCust.ConfigSync, $"{name} YAML", new());
            YamlData.AssignLocalValue(Reload());
            YamlData.ValueChanged += SyncDetected;

            fileSystemWatcher = new FileSystemWatcher(Storage, "*.yml");

            fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcher.Created += FileSystemWatcher_Changed;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Changed;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public abstract void Cache(T item);
       // public abstract void WackyForce(T item);

        #region Event Handlers
        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs evt)
        {
            Debug.Log($"[{WMRecipeCust.ModName}]: {Name} Change: {evt.Name}");

            try
            {
                if (!WMRecipeCust.ConfigSync.IsSourceOfTruth) return;

                T data = LoadFile(evt.FullPath);

                OnDataChange?.Invoke(null, new DataEventArgs<T>(data));

                if (WMRecipeCust.ConfigSync.IsSourceOfTruth)
                {
                    YamlData.AssignLocalValue(Reload());
                } else
                {
                    YamlData.Value = Reload();
                }
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Detected {Name} file change, but importing failed with an error.\n{e.Message + (e.InnerException != null ? ": " + e.InnerException.Message : "")}");
            }
        }

        private void SyncDetected()
        {
            Debug.Log($"[{WMRecipeCust.ModName}]: {Name} sync detected");

            try
            {
                LoadSyncedValue(YamlData.Value);

                OnSync?.Invoke(this, new EventArgs());

                // Update the local files if we received the changes from the server
                // Changes from Server shouldn't be saved to Client Folder, instead saved to cache\
                
                if (!WMRecipeCust.ConfigSync.IsSourceOfTruth)
                {
                    // Save(YamlData.Value);
                    SaveCache(YamlData.Value);
                }

            } catch (Exception e)
            {
                Debug.LogError($"Detected {Name} sync, but importing failed with an error.\n{e.Message + (e.InnerException != null ? ": " + e.InnerException.Message : "")}");
            }
        }
        #endregion

        /// <summary>
        /// Reads all the configuration files from storage and attempts to load them
        /// </summary>
        public void LoadFiles()
        {
            Debug.Log($"[{WMRecipeCust.ModName}]: Searching for files: {Storage}");

            foreach (string file in Directory.GetFiles(Storage, "*.yml", SearchOption.AllDirectories))
            {
                Debug.Log($"[{WMRecipeCust.ModName}]: Loading {Name}: {file}");

                LoadFile(file);
            }
        }

        /// <summary>
        /// Loads a file at the specified path, returns the configuration object
        /// </summary>
        /// <param name="path">The path to the configuration file</param>
        /// <returns></returns>
        public virtual T LoadFile(string path)
        {
            try
            {
                string yaml = File.ReadAllText(path);

                return LoadYaml(yaml);
            } catch (Exception e)
            {
                Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to load data from {path} - {e.Message}");
                return default;
            }
        }
        
        public void Save(Dictionary<string, string> data)
        {
            foreach (KeyValuePair<string, string> d in data)
            {
                File.WriteAllText(Path.Combine(Storage, d.Key), d.Value);
            }
        }

        public void SaveCache(Dictionary<string, string> data) //Materials Only
        {
            WMRecipeCust.WLog.LogInfo("Mat SaveCache");
            foreach (KeyValuePair<string, string> d in data)
            {
                int hash = d.Key.GetStableHashCode();
                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathCache, "_" + hash + ".mat"), d.Value);
            }
        }

        private void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void Export(T data, string name)
        {
            string contents = Serializer.Serialize(data);

            EnsureFolder(Storage);

            File.WriteAllText(Path.Combine(Storage, name + ".yml"), contents);
        }

        /// <summary>
        /// Processes all of the configuration files
        /// </summary>
        /// <param name="data"></param>
        public void LoadSyncedValue(Dictionary<string, string> data)
        {
            foreach (KeyValuePair<string, string> pair in data)
            {
                LoadYaml(pair.Value);
            }
        }

        /// <summary>
        /// Reads all files for the configuration type and attempts to load them
        /// </summary>
        /// <returns>A dictionary of filename and content</returns>
        protected Dictionary<string, string> Reload()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            WMRecipeCust.WLog.LogInfo("Mat Cache Save");

            foreach (string file in Directory.GetFiles(Storage, "*.yml", SearchOption.AllDirectories))  
            {
                try
                {
                    string key = Path.GetFileName(Storage);

                    if (!map.ContainsKey(key))
                    {
                        map.Add(key, File.ReadAllText(file));
                    } else
                    {
                        map[key] = File.ReadAllText(file);
                    }
                    
                    int hash = map[key].GetStableHashCode(); // write cache
                    File.WriteAllText(Path.Combine(WMRecipeCust.assetPathCache, "_" + hash + ".mat"), map[key]);                                    
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[{WMRecipeCust.ModName}]: Failed to load data from {file} - {e.Message}");
                }
            }

            return map;
        }

        /// <summary>
        /// Processes a configuration file from YAML into its proper object
        /// </summary>
        /// <param name="yaml">The yaml data</param>
        /// <returns>The configuration object</returns>
        public T LoadYaml(string yaml)
        {
            try
            {
                T data = Deserializer.Deserialize<T>(yaml);

                WMRecipeCust.WLog.LogInfo("hello from beyond");
                Cache(data);



                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Found {WMRecipeCust.ModName} config error in yaml.\n{e.Message + (e.InnerException != null ? ": " + e.InnerException.Message : "")}");

                return default;
            }
        }
    }

    public class DataEventArgs<T> : EventArgs
    {
        public T Data { get; private set; }

        public DataEventArgs(T data)
        {
            Data = data;
        }
    }
}
