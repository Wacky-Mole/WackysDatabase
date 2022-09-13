﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace ServerSync
{
    public abstract class OwnConfigEntryBase
    {
        public object? LocalBaseValue;
        public abstract ConfigEntryBase BaseConfig { get; }

        public bool SynchronizedConfig = true;
    }

    [PublicAPI]
    public class SyncedConfigEntry<T> : OwnConfigEntryBase
    {
        public override ConfigEntryBase BaseConfig => SourceConfig;
        public readonly ConfigEntry<T> SourceConfig;

        public SyncedConfigEntry(ConfigEntry<T> sourceConfig)
        {
            SourceConfig = sourceConfig;
        }

        public T Value
        {
            get => SourceConfig.Value;
            set => SourceConfig.Value = value;
        }

        public void AssignLocalValue(T value)
        {
            if (LocalBaseValue == null)
            {
                Value = value;
            }
            else
            {
                LocalBaseValue = value;
            }
        }
    }

    public abstract class CustomSyncedValueBase
    {
        public event Action? ValueChanged;

        public object? LocalBaseValue;

        public readonly string Identifier;
        public readonly Type Type;

        private object? boxedValue;

        public object? BoxedValue
        {
            get => boxedValue;
            set
            {
                boxedValue = value;
                ValueChanged?.Invoke();
            }
        }

        protected bool localIsOwner;

        protected CustomSyncedValueBase(ConfigSync configSync, string identifier, Type type)
        {
            Identifier = identifier;
            Type = type;
            configSync.AddCustomValue(this);
            localIsOwner = configSync.IsSourceOfTruth;
            configSync.SourceOfTruthChanged += truth => localIsOwner = truth;
        }
    }

    [PublicAPI]
    public sealed class CustomSyncedValue<T> : CustomSyncedValueBase
    {
        public T Value
        {
            get => (T)BoxedValue!;
            set => BoxedValue = value;
        }

        public CustomSyncedValue(ConfigSync configSync, string identifier, T value = default!) : base(configSync, identifier, typeof(T))
        {
            Value = value;
        }

        public void AssignLocalValue(T value)
        {
            if (localIsOwner)
            {
                Value = value;
            }
            else
            {
                LocalBaseValue = value;
            }
        }
    }

    internal class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public bool? ReadOnly = false;
    }

    [PublicAPI]
    public class ConfigSync
    {
        public static bool ProcessingServerUpdate = false;

        public readonly string Name;
        public string? DisplayName;
        public string? CurrentVersion;
        public string? MinimumRequiredVersion;

        private bool? forceConfigLocking;

        public bool IsLocked
        {
            get => (forceConfigLocking ?? lockedConfig != null && ((IConvertible)lockedConfig.BaseConfig.BoxedValue).ToInt32(CultureInfo.InvariantCulture) != 0) && !lockExempt;
            set => forceConfigLocking = value;
        }

        public bool IsAdmin => lockExempt;

        private bool isSourceOfTruth = true;

        public bool IsSourceOfTruth
        {
            get => isSourceOfTruth;
            private set
            {
                if (value != isSourceOfTruth)
                {
                    isSourceOfTruth = value;
                    SourceOfTruthChanged?.Invoke(value);
                }
            }
        }

        public event Action<bool>? SourceOfTruthChanged;

        private static readonly HashSet<ConfigSync> configSyncs = new();

        private readonly HashSet<OwnConfigEntryBase> allConfigs = new();
        private readonly HashSet<CustomSyncedValueBase> allCustomValues = new();

        private static bool isServer;
        private static string? connectionError;

        private static bool lockExempt = false;

        private OwnConfigEntryBase? lockedConfig = null;
        private event Action? lockedConfigChanged;

        static ConfigSync()
        {
            BepInEx.ThreadingHelper.Instance.StartSyncInvoke(() =>
            {
                if (PatchProcessor.GetPatchInfo(AccessTools.DeclaredMethod(typeof(ZNet), "Awake"))?.Postfixes.Count(p => p.PatchMethod.DeclaringType == typeof(RegisterRPCPatch)) > 0)
                {
                    return;
                }

                Harmony harmony = new("org.bepinex.helpers.ServerSync");
                foreach (Type type in typeof(ConfigSync).GetNestedTypes(BindingFlags.NonPublic).Where(t => t.IsClass))
                {
                    harmony.PatchAll(type);
                }
            });
        }

        public ConfigSync(string name)
        {
            Name = name;
            configSyncs.Add(this);
        }

        public SyncedConfigEntry<T> AddConfigEntry<T>(ConfigEntry<T> configEntry)
        {
            if (configData(configEntry) is not SyncedConfigEntry<T> syncedEntry)
            {
                syncedEntry = new SyncedConfigEntry<T>(configEntry);
                AccessTools.DeclaredField(typeof(ConfigDescription), "<Tags>k__BackingField").SetValue(configEntry.Description, new object[] { new ConfigurationManagerAttributes() }.Concat(configEntry.Description.Tags ?? Array.Empty<object>()).Concat(new[] { syncedEntry }).ToArray());
                configEntry.SettingChanged += (_, _) =>
                {
                    if (!ProcessingServerUpdate && syncedEntry.SynchronizedConfig)
                    {
                        Broadcast(ZRoutedRpc.Everybody, configEntry);
                    }
                };
                allConfigs.Add(syncedEntry);
            }

            return syncedEntry;
        }

        public SyncedConfigEntry<T> AddLockingConfigEntry<T>(ConfigEntry<T> lockingConfig) where T : IConvertible
        {
            if (lockedConfig != null)
            {
                throw new Exception("Cannot initialize locking ConfigEntry twice");
            }

            lockedConfig = AddConfigEntry(lockingConfig);
            lockingConfig.SettingChanged += (_, _) => lockedConfigChanged?.Invoke();

            return (SyncedConfigEntry<T>)lockedConfig;
        }

        internal void AddCustomValue(CustomSyncedValueBase customValue)
        {
            if (allCustomValues.Select(v => v.Identifier).Concat(new[] { "serverversion", "requiredversion" }).Contains(customValue.Identifier))
            {
                throw new Exception("Cannot have multiple settings with the same name or with a reserved name (serverversion or requiredversion)");
            }

            allCustomValues.Add(customValue);
            customValue.ValueChanged += () =>
            {
                if (!ProcessingServerUpdate)
                {
                    Broadcast(ZRoutedRpc.Everybody, customValue);
                }
            };
        }

        [HarmonyPatch(typeof(ZRpc), "HandlePackage")]
        private static class SnatchCurrentlyHandlingRPC
        {
            public static ZRpc? currentRpc;

            private static void Prefix(ZRpc __instance) => currentRpc = __instance;
        }

        [HarmonyPatch(typeof(ZNet), "Awake")]
        private static class RegisterRPCPatch
        {
            private static void Postfix(ZNet __instance)
            {
                connectionError = null;

                isServer = __instance.IsServer();
                foreach (ConfigSync configSync in configSyncs)
                {
                    configSync.IsSourceOfTruth = __instance.IsDedicated() || __instance.IsServer();
                    ZRoutedRpc.instance.Register<ZPackage>(configSync.Name + " ConfigSync", configSync.RPC_ConfigSync);
                    if (isServer)
                    {
                        Debug.Log($"Registered '{configSync.Name} ConfigSync' RPC - waiting for incoming connections");
                    }
                }

                IEnumerator WatchAdminListChanges()
                {
                    SyncedList adminList = (SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance);
                    List<string> CurrentList = new(adminList.GetList());
                    for (; ; )
                    {
                        yield return new WaitForSeconds(30);
                        if (!adminList.GetList().SequenceEqual(CurrentList))
                        {
                            CurrentList = new List<string>(adminList.GetList());

                            void SendAdmin(List<ZNetPeer> peers, bool isAdmin)
                            {
                                ZPackage package = ConfigsToPackage(packageEntries: new[]
                                {
                                new PackageEntry { section = "Internal", key = "lockexempt", type = typeof(bool), value = isAdmin }
                            });

                                if (configSyncs.First() is { } configSync)
                                {
                                    ZNet.instance.StartCoroutine(configSync.sendZPackage(peers, package));
                                }
                            }

                            List<ZNetPeer> adminPeer = ZNet.instance.GetPeers().Where(p => adminList.Contains(p.m_rpc.GetSocket().GetHostName())).ToList();
                            List<ZNetPeer> nonAdminPeer = ZNet.instance.GetPeers().Except(adminPeer).ToList();
                            SendAdmin(nonAdminPeer, false);
                            SendAdmin(adminPeer, true);
                        }
                    }
                    // ReSharper disable once IteratorNeverReturns
                }

                if (isServer)
                {
                    __instance.StartCoroutine(WatchAdminListChanges());
                }
            }
        }

        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        private static class RegisterClientRPCPatch
        {
            private static void Postfix(ZNet __instance, ZNetPeer peer)
            {
                if (!__instance.IsServer())
                {
                    foreach (ConfigSync configSync in configSyncs)
                    {
                        peer.m_rpc.Register<ZPackage>(configSync.Name + " ConfigSync", configSync.RPC_InitialConfigSync);
                    }
                }
            }
        }

        private const byte PARTIAL_CONFIGS = 1;
        private const byte FRAGMENTED_CONFIG = 2;
        private const byte COMPRESSED_CONFIG = 4;

        private readonly Dictionary<string, SortedDictionary<int, byte[]>> configValueCache = new();
        private readonly List<KeyValuePair<long, string>> cacheExpirations = new(); // avoid leaking memory

        private void RPC_InitialConfigSync(ZRpc rpc, ZPackage package) => RPC_ConfigSync(0, package);

        private void RPC_ConfigSync(long sender, ZPackage package)
        {
            try
            {
                if (isServer && IsLocked)
                {
                    bool? exempt = ((SyncedList?)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance))?.Contains(SnatchCurrentlyHandlingRPC.currentRpc?.GetSocket()?.GetHostName());
                    if (exempt == false)
                    {
                        return;
                    }
                }

                cacheExpirations.RemoveAll(kv =>
                {
                    if (kv.Key < DateTimeOffset.Now.Ticks)
                    {
                        configValueCache.Remove(kv.Value);
                        return true;
                    }

                    return false;
                });

                byte packageFlags = package.ReadByte();

                if ((packageFlags & FRAGMENTED_CONFIG) != 0)
                {
                    long uniqueIdentifier = package.ReadLong();
                    string cacheKey = sender.ToString() + uniqueIdentifier;
                    if (!configValueCache.TryGetValue(cacheKey, out SortedDictionary<int, byte[]> dataFragments))
                    {
                        dataFragments = new SortedDictionary<int, byte[]>();
                        configValueCache[cacheKey] = dataFragments;
                        cacheExpirations.Add(new KeyValuePair<long, string>(DateTimeOffset.Now.AddSeconds(60).Ticks, cacheKey));
                    }

                    int fragment = package.ReadInt();
                    int fragments = package.ReadInt();

                    dataFragments.Add(fragment, package.ReadByteArray());

                    if (dataFragments.Count < fragments)
                    {
                        return;
                    }

                    configValueCache.Remove(cacheKey);

                    package = new ZPackage(dataFragments.Values.SelectMany(a => a).ToArray());
                    packageFlags = package.ReadByte();
                }

                ProcessingServerUpdate = true;

                if ((packageFlags & COMPRESSED_CONFIG) != 0)
                {
                    byte[] data = package.ReadByteArray();

                    MemoryStream input = new(data);
                    MemoryStream output = new();
                    using (DeflateStream deflateStream = new(input, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(output);
                    }

                    package = new ZPackage(output.ToArray());
                    packageFlags = package.ReadByte();
                }

                if ((packageFlags & PARTIAL_CONFIGS) == 0)
                {
                    resetConfigsFromServer();
                }

                if (!isServer)
                {
                    if (IsSourceOfTruth)
                    {
                        lockedConfigChanged += serverLockedSettingChanged;
                    }
                    IsSourceOfTruth = false;
                }

                ParsedConfigs configs = ReadConfigsFromPackage(package);

                foreach (KeyValuePair<OwnConfigEntryBase, object?> configKv in configs.configValues)
                {
                    if (!isServer && configKv.Key.LocalBaseValue == null)
                    {
                        configKv.Key.LocalBaseValue = configKv.Key.BaseConfig.BoxedValue;
                    }

                    configKv.Key.BaseConfig.BoxedValue = configKv.Value;
                }

                foreach (KeyValuePair<CustomSyncedValueBase, object?> configKv in configs.customValues)
                {
                    if (!isServer)
                    {
                        configKv.Key.LocalBaseValue ??= configKv.Key.BoxedValue;
                    }

                    configKv.Key.BoxedValue = configKv.Value;
                }

                if (!isServer)
                {
                    Debug.Log($"Received {configs.configValues.Count} configs and {configs.customValues.Count} custom values from the server for mod {DisplayName ?? Name}");

                    serverLockedSettingChanged(); // Re-evaluate for intial locking
                }
            }
            finally
            {
                ProcessingServerUpdate = false;
            }
        }

        private class ParsedConfigs
        {
            public readonly Dictionary<OwnConfigEntryBase, object?> configValues = new();
            public readonly Dictionary<CustomSyncedValueBase, object?> customValues = new();
        }

        private ParsedConfigs ReadConfigsFromPackage(ZPackage package)
        {
            ParsedConfigs configs = new();
            Dictionary<string, OwnConfigEntryBase> configMap = allConfigs.Where(c => c.SynchronizedConfig).ToDictionary(c => c.BaseConfig.Definition.Section + "_" + c.BaseConfig.Definition.Key, c => c);

            Dictionary<string, CustomSyncedValueBase> customValueMap = allCustomValues.ToDictionary(c => c.Identifier, c => c);

            int valueCount = package.ReadInt();
            for (int i = 0; i < valueCount; ++i)
            {
                string groupName = package.ReadString();
                string configName = package.ReadString();
                string typeName = package.ReadString();

                Type? type = Type.GetType(typeName);
                if (typeName == "" || type != null)
                {
                    object? value;
                    try
                    {
                        value = typeName == "" ? null : ReadValueWithTypeFromZPackage(package, type!);
                    }
                    catch (InvalidDeserializationTypeException e)
                    {
                        Debug.LogWarning($"Got unexpected struct internal type {e.received} for field {e.field} struct {typeName} for {configName} in section {groupName} for mod {DisplayName ?? Name}, expecting {e.expected}");
                        continue;
                    }
                    if (groupName == "Internal")
                    {
                        if (configName == "serverversion")
                        {
                            if (value?.ToString() != CurrentVersion)
                            {
                                Debug.LogWarning($"Received server version is not equal: server version = {value?.ToString() ?? "null"}; local version = {CurrentVersion ?? "unknown"}");
                            }
                        }
                        else if (configName == "requiredversion")
                        {
                            // ReSharper disable RedundantNameQualifier
                            if (CurrentVersion == null || new System.Version(value?.ToString() ?? "0.0.0") > new System.Version(CurrentVersion))
                            {
                                // ReSharper restore RedundantNameQualifier
                                Debug.LogError($"Received minimum version is higher than required version: minimum required version = {value?.ToString() ?? "0.0.0"}; local version = {CurrentVersion ?? "unknown"}");
                                Game.instance.Logout();
                                AccessTools.DeclaredField(typeof(ZNet), "m_connectionStatus").SetValue(null, ZNet.ConnectionStatus.ErrorVersion);
                                if (CurrentVersion == "0.0.1")
                                {
                                    connectionError = $"{DisplayName ?? Name}- You started a Local Game before Multiplayer. That is Not allowed. -Restart Game";
                                }
                                else
                                    connectionError = $"Mod {DisplayName ?? Name} requires minimum {value}. Installed is version {CurrentVersion}.";
                            }
                        }
                        else if (configName == "lockexempt")
                        {
                            if (value is bool exempt)
                            {
                                lockExempt = exempt;
                            }
                        }
                        else if (customValueMap.TryGetValue(configName, out CustomSyncedValueBase config))
                        {
                            if ((typeName == "" && (!config.Type.IsValueType || Nullable.GetUnderlyingType(config.Type) != null)) || GetZPackageTypeString(config.Type) == typeName)
                            {
                                configs.customValues[config] = value;
                            }
                            else
                            {
                                Debug.LogWarning($"Got unexpected type {typeName} for internal value {configName} for mod {DisplayName ?? Name}, expecting {config.Type.AssemblyQualifiedName}");
                            }
                        }
                    }
                    else if (configMap.TryGetValue(groupName + "_" + configName, out OwnConfigEntryBase config))
                    {
                        Type expectedType = configType(config.BaseConfig);
                        if ((typeName == "" && (!expectedType.IsValueType || Nullable.GetUnderlyingType(expectedType) != null)) || GetZPackageTypeString(expectedType) == typeName)
                        {
                            configs.configValues[config] = value;
                        }
                        else
                        {
                            Debug.LogWarning($"Got unexpected type {typeName} for {configName} in section {groupName} for mod {DisplayName ?? Name}, expecting {expectedType.AssemblyQualifiedName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Received unknown config entry {configName} in section {groupName} for mod {DisplayName ?? Name}. This may happen if client and server versions of the mod do not match.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Got invalid type {typeName}, abort reading of received configs");
                    return new ParsedConfigs();
                }
            }

            return configs;
        }

        [HarmonyPatch(typeof(FejdStartup), "ShowConnectError")]
        private class ShowConnectionError
        {
            private static void Postfix(FejdStartup __instance)
            {
                if (__instance.m_connectionFailedPanel.activeSelf && connectionError != null)
                {
                    __instance.m_connectionFailedError.text += "\n" + connectionError;
                }
            }
        }

        [HarmonyPatch(typeof(ZNet), "Shutdown")]
        private class ResetConfigsOnShutdown
        {
            private static void Postfix()
            {
                ProcessingServerUpdate = true;
                foreach (ConfigSync serverSync in configSyncs)
                {
                    serverSync.resetConfigsFromServer();
                }
                ProcessingServerUpdate = false;
            }
        }

        private static bool isWritableConfig(OwnConfigEntryBase config)
        {
            if (configSyncs.FirstOrDefault(cs => cs.allConfigs.Contains(config)) is not { } configSync)
            {
                return true;
            }

            return configSync.IsSourceOfTruth || !config.SynchronizedConfig || config.LocalBaseValue == null || (!configSync.IsLocked && (config != configSync.lockedConfig || lockExempt));
        }

        private void serverLockedSettingChanged()
        {
            foreach (OwnConfigEntryBase configEntryBase in allConfigs)
            {
                configAttribute<ConfigurationManagerAttributes>(configEntryBase.BaseConfig).ReadOnly = !isWritableConfig(configEntryBase);
            }
        }

        private void resetConfigsFromServer()
        {
            foreach (OwnConfigEntryBase config in allConfigs.Where(config => config.LocalBaseValue != null))
            {
                config.BaseConfig.BoxedValue = config.LocalBaseValue;
                config.LocalBaseValue = null;
            }

            foreach (CustomSyncedValueBase config in allCustomValues.Where(config => config.LocalBaseValue != null))
            {
                config.BoxedValue = config.LocalBaseValue;
                config.LocalBaseValue = null;
            }

            lockedConfigChanged -= serverLockedSettingChanged;
            IsSourceOfTruth = true;
            serverLockedSettingChanged();
        }

        private static long packageCounter = 0;

        private IEnumerator<bool> distributeConfigToPeers(ZNetPeer peer, ZPackage package)
        {
            if (ZRoutedRpc.instance is not { } rpc)
            {
                yield break;
            }

            const int packageSliceSize = 250000;
            const int maximumSendQueueSize = 20000;

            IEnumerable<bool> waitForQueue()
            {
                float timeout = Time.time + 30;
                while (peer.m_socket.GetSendQueueSize() > maximumSendQueueSize)
                {
                    if (Time.time > timeout)
                    {
                        Debug.Log($"Disconnecting {peer.m_uid} after 30 seconds config sending timeout");
                        peer.m_rpc.Invoke("Error", ZNet.ConnectionStatus.ErrorConnectFailed);
                        ZNet.instance.Disconnect(peer);
                        yield break;
                    }

                    yield return false;
                }
            }

            void SendPackage(ZPackage pkg)
            {
                string method = Name + " ConfigSync";
                if (isServer)
                {
                    peer.m_rpc.Invoke(method, pkg);
                }
                else
                {
                    rpc.InvokeRoutedRPC(peer.m_server ? 0 : peer.m_uid, method, pkg);
                }
            }

            if (package.GetArray() is { LongLength: > packageSliceSize } data)
            {
                int fragments = (int)(1 + (data.LongLength - 1) / packageSliceSize);
                long packageIdentifier = ++packageCounter;
                for (int fragment = 0; fragment < fragments; ++fragment)
                {
                    foreach (bool wait in waitForQueue())
                    {
                        yield return wait;
                    }

                    if (!peer.m_socket.IsConnected())
                    {
                        yield break;
                    }

                    ZPackage fragmentedPackage = new();
                    fragmentedPackage.Write(FRAGMENTED_CONFIG);
                    fragmentedPackage.Write(packageIdentifier);
                    fragmentedPackage.Write(fragment);
                    fragmentedPackage.Write(fragments);
                    fragmentedPackage.Write(data.Skip(packageSliceSize * fragment).Take(packageSliceSize).ToArray());
                    SendPackage(fragmentedPackage);

                    if (fragment != fragments - 1)
                    {
                        yield return true;
                    }
                }
            }
            else
            {
                foreach (bool wait in waitForQueue())
                {
                    yield return wait;
                }

                SendPackage(package);
            }
        }

        private IEnumerator sendZPackage(long target, ZPackage package)
        {
            if (!ZNet.instance)
            {
                return Enumerable.Empty<object>().GetEnumerator();
            }

            List<ZNetPeer> peers = (List<ZNetPeer>)AccessTools.DeclaredField(typeof(ZRoutedRpc), "m_peers").GetValue(ZRoutedRpc.instance);
            if (target != ZRoutedRpc.Everybody)
            {
                peers = peers.Where(p => p.m_uid == target).ToList();
            }

            return sendZPackage(peers, package);
        }

        private IEnumerator sendZPackage(List<ZNetPeer> peers, ZPackage package)
        {
            if (!ZNet.instance)
            {
                yield break;
            }

            const int compressMinSize = 10000;

            if (package.GetArray() is { LongLength: > compressMinSize } rawData)
            {
                ZPackage compressedPackage = new();
                compressedPackage.Write(COMPRESSED_CONFIG);
                MemoryStream output = new();
                using (DeflateStream deflateStream = new(output, System.IO.Compression.CompressionLevel.Optimal))
                {
                    deflateStream.Write(rawData, 0, rawData.Length);
                }
                compressedPackage.Write(output.ToArray());
                package = compressedPackage;
            }

            List<IEnumerator<bool>> writers = peers.Where(peer => peer.IsReady()).Select(p => distributeConfigToPeers(p, package)).ToList();
            writers.RemoveAll(writer => !writer.MoveNext());
            while (writers.Count > 0)
            {
                yield return null;
                writers.RemoveAll(writer => !writer.MoveNext());
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
        private class SendConfigsAfterLogin
        {
            private class BufferingSocket : ISocket
            {
                public volatile bool finished = false;
                public readonly List<ZPackage> Package = new();
                public readonly ISocket Original;

                public BufferingSocket(ISocket original)
                {
                    Original = original;
                }

                public bool IsConnected() => Original.IsConnected();
                public ZPackage Recv() => Original.Recv();
                public int GetSendQueueSize() => Original.GetSendQueueSize();
                public int GetCurrentSendRate() => Original.GetCurrentSendRate();
                public bool IsHost() => Original.IsHost();
                public void Dispose() => Original.Dispose();
                public bool GotNewData() => Original.GotNewData();
                public void Close() => Original.Close();
                public string GetEndPointString() => Original.GetEndPointString();
                public void GetAndResetStats(out int totalSent, out int totalRecv) => Original.GetAndResetStats(out totalSent, out totalRecv);
                public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec) => Original.GetConnectionQuality(out localQuality, out remoteQuality, out ping, out outByteSec, out inByteSec);
                public ISocket Accept() => Original.Accept();
                public int GetHostPort() => Original.GetHostPort();
                public bool Flush() => Original.Flush();
                public string GetHostName() => Original.GetHostName();

                public void Send(ZPackage pkg)
                {
                    pkg.SetPos(0);
                    int methodHash = pkg.ReadInt();
                    if ((methodHash == "PeerInfo".GetStableHashCode() || methodHash == "RoutedRPC".GetStableHashCode() || methodHash == "ZDOData".GetStableHashCode()) && !finished)
                    {
                        Package.Add(new ZPackage(pkg.GetArray())); // the original ZPackage gets reused, create a new one
                    }
                    else
                    {
                        Original.Send(pkg);
                    }
                }
            }

            [HarmonyPriority(Priority.First)]
            private static void Prefix(ref Dictionary<Assembly, BufferingSocket>? __state, ZNet __instance, ZRpc rpc)
            {
                if (__instance.IsServer())
                {
                    BufferingSocket bufferingSocket = new(rpc.GetSocket());
                    AccessTools.DeclaredField(typeof(ZRpc), "m_socket").SetValue(rpc, bufferingSocket);

                    __state ??= new Dictionary<Assembly, BufferingSocket>();
                    __state[Assembly.GetExecutingAssembly()] = bufferingSocket;
                }
            }

            private static void Postfix(Dictionary<Assembly, BufferingSocket> __state, ZNet __instance, ZRpc rpc)
            {
                if (!__instance.IsServer())
                {
                    return;
                }

                void SendBufferedData()
                {
                    if (rpc.GetSocket() is BufferingSocket bufferingSocket)
                    {
                        AccessTools.DeclaredField(typeof(ZRpc), "m_socket").SetValue(rpc, bufferingSocket.Original);
                    }

                    bufferingSocket = __state[Assembly.GetExecutingAssembly()];
                    bufferingSocket.finished = true;

                    foreach (ZPackage package in bufferingSocket.Package)
                    {
                        bufferingSocket.Original.Send(package);
                    }
                }

                if (AccessTools.DeclaredMethod(typeof(ZNet), "GetPeer", new[] { typeof(ZRpc) }).Invoke(__instance, new object[] { rpc }) is not ZNetPeer peer)
                {
                    SendBufferedData();
                    return;
                }

                IEnumerator sendAsync()
                {
                    foreach (ConfigSync configSync in configSyncs)
                    {
                        List<PackageEntry> entries = new();
                        if (configSync.CurrentVersion != null)
                        {
                            entries.Add(new PackageEntry { section = "Internal", key = "serverversion", type = typeof(string), value = configSync.CurrentVersion });
                        }

                        if (configSync.MinimumRequiredVersion != null)
                        {
                            entries.Add(new PackageEntry { section = "Internal", key = "requiredversion", type = typeof(string), value = configSync.MinimumRequiredVersion });
                        }

                        entries.Add(new PackageEntry { section = "Internal", key = "lockexempt", type = typeof(bool), value = ((SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance)).Contains(rpc.GetSocket().GetHostName()) });

                        ZPackage package = ConfigsToPackage(configSync.allConfigs.Select(c => c.BaseConfig), configSync.allCustomValues, entries, false);

                        yield return __instance.StartCoroutine(configSync.sendZPackage(new List<ZNetPeer> { peer }, package));

                    }

                    SendBufferedData();
                }

                __instance.StartCoroutine(sendAsync());
            }
        }

        private class PackageEntry
        {
            public string section = null!;
            public string key = null!;
            public Type type = null!;
            public object? value;
        }

        private void Broadcast(long target, params ConfigEntryBase[] configs)
        {
            if (!IsLocked || isServer)
            {
                ZPackage package = ConfigsToPackage(configs);
                ZNet.instance?.StartCoroutine(sendZPackage(target, package));
            }
        }

        private void Broadcast(long target, params CustomSyncedValueBase[] customValues)
        {
            if (!IsLocked || isServer)
            {
                ZPackage package = ConfigsToPackage(customValues: customValues);
                ZNet.instance?.StartCoroutine(sendZPackage(target, package));
            }
        }

        private static OwnConfigEntryBase? configData(ConfigEntryBase config)
        {
            return config.Description.Tags?.OfType<OwnConfigEntryBase>().SingleOrDefault();
        }

        public static SyncedConfigEntry<T>? ConfigData<T>(ConfigEntry<T> config)
        {
            return config.Description.Tags?.OfType<SyncedConfigEntry<T>>().SingleOrDefault();
        }

        private static T configAttribute<T>(ConfigEntryBase config)
        {
            return config.Description.Tags.OfType<T>().First();
        }

        private static Type configType(ConfigEntryBase config) => configType(config.SettingType);

        private static Type configType(Type type) => type.IsEnum ? Enum.GetUnderlyingType(type) : type;

        [HarmonyPatch(typeof(ConfigEntryBase), nameof(ConfigEntryBase.GetSerializedValue))]
        private static class PreventSavingServerInfo
        {
            private static bool Prefix(ConfigEntryBase __instance, ref string __result)
            {
                if (configData(__instance) is not { } data || isWritableConfig(data))
                {
                    return true;
                }

                __result = TomlTypeConverter.ConvertToString(data.LocalBaseValue, __instance.SettingType);
                return false;
            }
        }

        [HarmonyPatch(typeof(ConfigEntryBase), nameof(ConfigEntryBase.SetSerializedValue))]
        private static class PreventConfigRereadChangingValues
        {
            private static bool Prefix(ConfigEntryBase __instance, string value)
            {
                if (configData(__instance) is not { } data || data.LocalBaseValue == null)
                {
                    return true;
                }

                try
                {
                    data.LocalBaseValue = TomlTypeConverter.ConvertToValue(value, __instance.SettingType);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Config value of setting \"{__instance.Definition}\" could not be parsed and will be ignored. Reason: {e.Message}; Value: {value}");
                }
                return false;
            }
        }

        private static ZPackage ConfigsToPackage(IEnumerable<ConfigEntryBase>? configs = null, IEnumerable<CustomSyncedValueBase>? customValues = null, IEnumerable<PackageEntry>? packageEntries = null, bool partial = true)
        {
            List<ConfigEntryBase> configList = configs?.Where(config => configData(config)!.SynchronizedConfig).ToList() ?? new List<ConfigEntryBase>();
            List<CustomSyncedValueBase> customValueList = customValues?.ToList() ?? new List<CustomSyncedValueBase>();
            ZPackage package = new();
            package.Write(partial ? PARTIAL_CONFIGS : (byte)0);
            package.Write(configList.Count + customValueList.Count + (packageEntries?.Count() ?? 0));
            foreach (PackageEntry packageEntry in packageEntries ?? Array.Empty<PackageEntry>())
            {
                AddEntryToPackage(package, packageEntry);
            }
            foreach (CustomSyncedValueBase customValue in customValueList)
            {
                AddEntryToPackage(package, new PackageEntry { section = "Internal", key = customValue.Identifier, type = customValue.Type, value = customValue.BoxedValue });
            }
            foreach (ConfigEntryBase config in configList)
            {
                AddEntryToPackage(package, new PackageEntry { section = config.Definition.Section, key = config.Definition.Key, type = configType(config), value = config.BoxedValue });
            }

            return package;
        }

        private static void AddEntryToPackage(ZPackage package, PackageEntry entry)
        {
            package.Write(entry.section);
            package.Write(entry.key);
            package.Write(entry.value == null ? "" : GetZPackageTypeString(entry.type));
            AddValueToZPackage(package, entry.value);
        }

        private static string GetZPackageTypeString(Type type) => type.AssemblyQualifiedName!;

        private static void AddValueToZPackage(ZPackage package, object? value)
        {
            Type? type = value?.GetType();
            if (value is Enum)
            {
                value = ((IConvertible)value).ToType(Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture);
            }
            else if (value is ICollection collection)
            {
                package.Write(collection.Count);
                foreach (object item in collection)
                {
                    AddValueToZPackage(package, item);
                }
                return;
            }
            else if (type is { IsValueType: true, IsPrimitive: false })
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                package.Write(fields.Length);
                foreach (FieldInfo field in fields)
                {
                    package.Write(GetZPackageTypeString(field.FieldType));
                    AddValueToZPackage(package, field.GetValue(value));
                }
                return;
            }

            ZRpc.Serialize(new[] { value }, ref package);
        }

        private static object ReadValueWithTypeFromZPackage(ZPackage package, Type type)
        {
            if (type is { IsValueType: true, IsPrimitive: false, IsEnum: false })
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                int fieldCount = package.ReadInt();
                if (fieldCount != fields.Length)
                {
                    throw new InvalidDeserializationTypeException { received = $"(field count: {fieldCount})", expected = $"(field count: {fields.Length})" };
                }

                object value = FormatterServices.GetUninitializedObject(type);
                foreach (FieldInfo field in fields)
                {
                    string typeName = package.ReadString();
                    if (typeName != GetZPackageTypeString(field.FieldType))
                    {
                        throw new InvalidDeserializationTypeException { received = typeName, expected = GetZPackageTypeString(field.FieldType), field = field.Name };
                    }
                    field.SetValue(value, ReadValueWithTypeFromZPackage(package, field.FieldType));
                }
                return value;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                int entriesCount = package.ReadInt();
                IDictionary dict = (IDictionary)Activator.CreateInstance(type);
                Type kvType = typeof(KeyValuePair<,>).MakeGenericType(type.GenericTypeArguments);
                FieldInfo keyField = kvType.GetField("key", BindingFlags.NonPublic | BindingFlags.Instance)!;
                FieldInfo valueField = kvType.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance)!;
                for (int i = 0; i < entriesCount; ++i)
                {
                    object kv = ReadValueWithTypeFromZPackage(package, kvType);
                    dict.Add(keyField.GetValue(kv), valueField.GetValue(kv));
                }
                return dict;
            }
            if (type != typeof(List<string>) && type.IsGenericType && typeof(ICollection<>).MakeGenericType(type.GenericTypeArguments[0]) is { } collectionType && collectionType.IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                int entriesCount = package.ReadInt();
                object list = Activator.CreateInstance(type);
                MethodInfo adder = collectionType.GetMethod("Add")!;
                for (int i = 0; i < entriesCount; ++i)
                {
                    adder.Invoke(list, new[] { ReadValueWithTypeFromZPackage(package, type.GenericTypeArguments[0]) });
                }
                return list;
            }

            ParameterInfo param = (ParameterInfo)FormatterServices.GetUninitializedObject(typeof(ParameterInfo));
            AccessTools.DeclaredField(typeof(ParameterInfo), "ClassImpl").SetValue(param, type);
            List<object> data = new();
            ZRpc.Deserialize(new[] { null, param }, package, ref data);
            return data.First();
        }

        private class InvalidDeserializationTypeException : Exception
        {
            public string expected = null!;
            public string received = null!;
            public string field = "";
        }
    }
}