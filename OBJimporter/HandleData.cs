using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using UnityEngine;

namespace wackydatabase.OBJimporter
{
    internal class HandleData
    {
        private const long RoutedRpcSoftPayloadLimitBytes = 5L * 1024L;
        private const string LargeTransferAssetPrefix = "WDB_ASSET_PAYLOAD|";

        private static long GetMaxSyncedAssetFileSizeBytes()
        {
            int maxMb = WMRecipeCust.maxAssetSyncFileSizeMB.Value;
            if (maxMb < 1)
            {
                maxMb = 1;
            }

            return maxMb * 1024L * 1024L;
        }

        // scheme is going to be filename:type;base64==filename:type;base64==
        internal static string bigDataR;
        internal static List<string> bigDataRChucks = new List<string>();
        internal static string bigDataS;
        internal static List<string> bigDataSChucks = new List<string>();   

        internal static HashSet<long> PendingSyncClients = new HashSet<long>();
        internal static long AdminPeerForSync = 0L;
        internal static bool AutoSyncRequestedAfterLoad = false;
        internal static int DownloadedAssetCountCurrentSync = 0;
        internal static bool AssetReloadInProgress = false;

        private static string BuildManifest()
        {
            StringBuilder sb = new StringBuilder();
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                void AddFiles(string folder, string type, string search)
                {
                    if (!Directory.Exists(folder)) return;
                    foreach (var file in Directory.GetFiles(folder, search, SearchOption.AllDirectories))
                    {
                        string filename = Path.GetFileNameWithoutExtension(file);
                        byte[] bytes = File.ReadAllBytes(file);
                        string hash = Convert.ToBase64String(md5.ComputeHash(bytes));
                        sb.Append($"{filename}:{type}:{hash}?");
                    }
                }

                AddFiles(WMRecipeCust.assetPathIcons, "icon", "*.png");
                AddFiles(WMRecipeCust.assetPathObjects, "obj", "*.obj");
                AddFiles(WMRecipeCust.assetPathObjects, "png", "*.png");
                AddFiles(WMRecipeCust.assetPathTextures, "tex", "*.png");
            }

            return sb.ToString();
        }

        public static void QueueAutoSyncToPeer(long peerId)
        {
            if (!WMRecipeCust.enableAssetSync.Value)
            {
                return;
            }

            if (!ZNet.instance.IsServer() || peerId == 0L || WMRecipeCust.issettoSinglePlayer)
            {
                return;
            }

            HandleData hd = new HandleData();
            WMRecipeCust.context.StartCoroutine(hd.SendDataToPeerWhenReady(peerId));
        }

        public static void ResetAutoSyncRequestState()
        {
            AutoSyncRequestedAfterLoad = false;
            DownloadedAssetCountCurrentSync = 0;
            AssetReloadInProgress = false;
        }

        public static void RequestAssetSyncAfterWorldLoad()
        {
            if (!WMRecipeCust.enableAssetSync.Value)
            {
                return;
            }

            if (ZNet.instance == null || ZRoutedRpc.instance == null)
            {
                return;
            }

            if (ZNet.instance.IsServer() || WMRecipeCust.issettoSinglePlayer)
            {
                return;
            }

            if (AutoSyncRequestedAfterLoad)
            {
                return;
            }

            long serverPeerId = ZRoutedRpc.instance.GetServerPeerID();
            if (serverPeerId == 0L)
            {
                return;
            }

            AutoSyncRequestedAfterLoad = true;
            HandleData hd = new HandleData();
            WMRecipeCust.context.StartCoroutine(hd.DelayedAssetSyncRequest(serverPeerId));
        }

        private IEnumerator DelayedAssetSyncRequest(long serverPeerId)
        {
            yield return new WaitForSeconds(10f);

            if (ZNet.instance == null || ZRoutedRpc.instance == null || ZNet.instance.IsServer() || WMRecipeCust.issettoSinglePlayer)
            {
                yield break;
            }

            var peers = ZNet.instance.GetPeers();
            if (peers == null || !peers.Any())
            {
                WMRecipeCust.WLog.LogWarning("WackyDB: Client disconnected before delayed asset sync request could be sent.");
                AutoSyncRequestedAfterLoad = false;
                yield break;
            }

            WMRecipeCust.WLog.LogInfo("WackyDB: Requesting server asset sync after world load (10s delay).");
            ZRoutedRpc.instance.InvokeRoutedRPC(serverPeerId, "WackyDB_RequestAssetSync");
        }

        public static void ReceiveAssetSyncRequest(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }

            WMRecipeCust.WLog.LogInfo($"WackyDB: Received post-load asset sync request from peer {sender}.");
            QueueAutoSyncToPeer(sender);
        }

        private IEnumerator SendDataToPeerWhenReady(long peerId)
        {
            const float timeout = 15f;
            float timer = 0f;

            while (timer < timeout)
            {
                if (ZNet.instance == null || !ZNet.instance.IsServer())
                    yield break;

                var peer = ZNet.instance.GetPeers()?.FirstOrDefault(p => p.m_uid == peerId);
                if (peer == null)
                {
                    WMRecipeCust.WLog.LogWarning($"WackyDB: Peer {peerId} disconnected before asset sync could begin.");
                    yield break;
                }

                if (peer.IsReady())
                {
                    SendDataToPeer(peerId);
                    yield break;
                }

                timer += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }

            WMRecipeCust.WLog.LogWarning($"WackyDB: Timed out waiting to auto-sync assets to peer {peerId}.");
        }

        public static void SendDataToPeer(long peerId)
        {
            if (!ZNet.instance.IsServer() || peerId == 0L)
            {
                return;
            }

            PendingSyncClients.Add(peerId);

            WMRecipeCust.WLog.LogInfo($"Starting automatic WackyDB asset sync for peer {peerId}");

            string manifest = BuildManifest();
            WMRecipeCust.WLog.LogInfo($"Sending Asset Manifest ({manifest.Length} chars) to peer {peerId}.");
            ZRoutedRpc.instance.InvokeRoutedRPC(peerId, "WackyDB_AssetManifest", manifest);
        }

        public static void RecievedData()
        {
            if (ZNet.instance == null || ZNet.instance.IsServer())
            {
                return;
            }

            string data = WMRecipeCust.largeTransfer.Value;
            if (string.IsNullOrEmpty(data) || !data.StartsWith(LargeTransferAssetPrefix))
            {
                return;
            }

            string payload = data.Substring(LargeTransferAssetPrefix.Length);
            string[] parts = payload.Split(new[] { '|' }, 4);
            if (parts.Length != 4)
            {
                WMRecipeCust.WLog.LogWarning("WackyDB: Received malformed largeTransfer asset payload.");
                return;
            }

            if (!long.TryParse(parts[0], out long targetPeer))
            {
                WMRecipeCust.WLog.LogWarning("WackyDB: Could not parse largeTransfer target peer id.");
                return;
            }

            long localSessionId = ZDOMan.GetSessionID();
            if (targetPeer != 0L && localSessionId != 0L && targetPeer != localSessionId)
            {
                return;
            }

            ReceivePayload(0L, parts[1], parts[2], parts[3]);
        }

        private static void SendPayloadViaLargeTransfer(long targetPeer, string type, string filename, string base64)
        {
            WMRecipeCust.WLog.LogInfo($"WackyDB: Sending '{filename}' to peer {targetPeer} via largeTransfer.");
            WMRecipeCust.largeTransfer.Value = $"{LargeTransferAssetPrefix}{targetPeer}|{type}|{filename}|{base64}";
        }

        public static void SendData(long peer, ZPackage go) // should probably be a console command because this will send it to everyone and be huge!
        {
            if (!WMRecipeCust.enableAssetSync.Value)
            {
                WMRecipeCust.WLog.LogInfo("WackyDB: Asset sync is disabled in config. Skipping manual send.");
                return;
            }

            if (!ZNet.instance.IsServer())
            {
                return;
            }
            if (WMRecipeCust.issettoSinglePlayer)
            {
                WMRecipeCust.WLog.LogInfo("Singleplayer mode detected. Skipping SendData because there are no clients.");
                return;
            }

            PendingSyncClients.Clear();
            AdminPeerForSync = peer;
            var peers = ZNet.instance.GetPeers();
            if (peers != null)
            {
                foreach (var p in peers)
                {
                    if (p.IsReady())
                        PendingSyncClients.Add(p.m_uid);
                }
            }

            WMRecipeCust.WLog.LogInfo("Starting Object, Icon, and Texture folder Manifest generation");

            string manifest = BuildManifest();
            
            WMRecipeCust.WLog.LogInfo($"Sending Asset Manifest ({manifest.Length} chars) to all clients.");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "WackyDB_AssetManifest", manifest);

            if (peer != 0L)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(peer, "WackyDB_AdminLogMsg", "WackyDB: Server is calculating hashes and sending Manifests to clients...");
            }
            
            if (PendingSyncClients.Count == 0)
            {
                CheckPendingSyncClients();
            }
        }

        private static void CheckPendingSyncClients()
        {
            if (!ZNet.instance.IsServer()) return;

            if (PendingSyncClients.Count == 0)
            {
                WMRecipeCust.WLog.LogInfo("WackyDB: All clients have successfully synced server assets.");
                if (AdminPeerForSync != 0L)
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(AdminPeerForSync, "WackyDB_AdminLogMsg", "WackyDB: All clients have successfully synced server assets.");
                    AdminPeerForSync = 0L;
                }
            }
        }

        public static void ReceiveManifest(long sender, string manifest)
        {
            if (ZNet.instance.IsServer()) return;
            
            WMRecipeCust.WLog.LogInfo("Received Asset Manifest from server, checking local files...");
            
            string[] checkfor = { "?" };
            var chunks = manifest.Split(checkfor, System.StringSplitOptions.RemoveEmptyEntries);
            
            List<string> neededFiles = new List<string>();
            
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                foreach (var chunk in chunks)
                {
                    var parts = chunk.Split(':');
                    if (parts.Length != 3) continue;
                    string filename = parts[0];
                    string type = parts[1];
                    string hash = parts[2];
                    
                    string path = "";
                    if (type == "icon") path = Path.Combine(WMRecipeCust.assetPathIcons, filename + ".png");
                    else if (type == "png") path = Path.Combine(WMRecipeCust.assetPathObjects, filename + ".png");
                    else if (type == "obj") path = Path.Combine(WMRecipeCust.assetPathObjects, filename + ".obj");
                    else if (type == "tex") path = Path.Combine(WMRecipeCust.assetPathTextures, filename + ".png");
                    
                    bool needsFile = true;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        byte[] bytes = File.ReadAllBytes(path);
                        string localHash = Convert.ToBase64String(md5.ComputeHash(bytes));
                        if (localHash == hash) needsFile = false;
                    }
                    
                    if (needsFile)
                    {
                        neededFiles.Add($"{filename}:{type}");
                    }
                }
            }
            
            if (neededFiles.Count > 0)
            {
                DownloadedAssetCountCurrentSync = 0;
                WMRecipeCust.WLog.LogInfo($"Client missing {neededFiles.Count} files. Requesting specifically from server...");
                string requestStr = string.Join("?", neededFiles);
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "WackyDB_AssetRequest", requestStr);
            }
            else
            {
                WMRecipeCust.WLog.LogInfo("Client already has all WackyDB server assets up to date. Nothing to download!");
                if (Player.m_localPlayer != null)
                {
                   // MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "WackyDB: All Assets already up to date!");
                }
                
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "WackyDB_AssetRequest", "none");
            }
        }

        public static void ReceiveRequest(long sender, string request)
        {
            if (!ZNet.instance.IsServer()) return;
            
            if (request == "none") {
                PendingSyncClients.Remove(sender);
                CheckPendingSyncClients();
                return;
            }

            string[] checkfor = { "?" };
            var needed = request.Split(checkfor, System.StringSplitOptions.RemoveEmptyEntries);
            
            WMRecipeCust.WLog.LogInfo($"Peer {sender} requested {needed.Length} missing asset files. Streaming to them...");
            HandleData hd = new HandleData();
            WMRecipeCust.context.StartCoroutine(hd.SendRequestedFiles(sender, needed));
        }

        private IEnumerator SendRequestedFiles(long targetPeer, string[] requestedFiles)
        {
            foreach (var req in requestedFiles)
            {
                var peer = ZNet.instance.GetPeers()?.FirstOrDefault(p => p.m_uid == targetPeer);
                if (peer == null)
                {
                    WMRecipeCust.WLog.LogWarning($"WackyDB: Peer {targetPeer} disconnected during asset sync.");
                    PendingSyncClients.Remove(targetPeer);
                    CheckPendingSyncClients();
                    yield break;
                }

                var parts = req.Split(':');
                if (parts.Length != 2) continue;
                string filename = parts[0];
                string type = parts[1];
                
                string folder = "";
                string ext = "";
                
                if (type == "icon") { folder = WMRecipeCust.assetPathIcons; ext = "*.png"; }
                else if (type == "png") { folder = WMRecipeCust.assetPathObjects; ext = "*.png"; }
                else if (type == "obj") { folder = WMRecipeCust.assetPathObjects; ext = "*.obj"; }
                else if (type == "tex") { folder = WMRecipeCust.assetPathTextures; ext = "*.png"; }
                
                if (string.IsNullOrEmpty(folder)) continue;
                
                var files = Directory.GetFiles(folder, filename + ext, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    string filePath = files[0];
                    long fileSize = new FileInfo(filePath).Length;
                    long maxSizeBytes = GetMaxSyncedAssetFileSizeBytes();
                    if (fileSize > maxSizeBytes)
                    {
                        float sizeMb = fileSize / (1024f * 1024f);
                        float maxMb = maxSizeBytes / (1024f * 1024f);
                        string warn = $"WackyDB: Asset '{Path.GetFileName(filePath)}' is {sizeMb:F1} MB which exceeds sync limit ({maxMb:F1} MB). Aborting asset sync for peer {targetPeer}.";
                        WMRecipeCust.WLog.LogWarning(warn);
                        ZRoutedRpc.instance.InvokeRoutedRPC(targetPeer, "WackyDB_ClientMSG", $"WackyDB: Asset sync failed safely. '{filename}' is too large ({sizeMb:F1} MB). Please install server assets manually.");
                        if (AdminPeerForSync != 0L)
                        {
                            ZRoutedRpc.instance.InvokeRoutedRPC(AdminPeerForSync, "WackyDB_AdminLogMsg", warn);
                        }

                        PendingSyncClients.Remove(targetPeer);
                        CheckPendingSyncClients();
                        yield break;
                    }

                    try
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        string base64 = Convert.ToBase64String(bytes);
                        if (fileSize > RoutedRpcSoftPayloadLimitBytes)
                        {
                            SendPayloadViaLargeTransfer(targetPeer, type, filename, base64);
                        }
                        else
                        {
                            ZRoutedRpc.instance.InvokeRoutedRPC(targetPeer, "WackyDB_AssetPayload", type, filename, base64);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        WMRecipeCust.WLog.LogWarning($"WackyDB: Failed syncing asset '{Path.GetFileName(filePath)}' to peer {targetPeer}. {ex.Message}");
                        ZRoutedRpc.instance.InvokeRoutedRPC(targetPeer, "WackyDB_ClientMSG", "WackyDB: Asset sync failed during transfer. Please reconnect or install server assets manually.");
                        PendingSyncClients.Remove(targetPeer);
                        CheckPendingSyncClients();
                        yield break;
                    }
                }
                
                // Yield to prevent overwhelming network buffer
                yield return new WaitForEndOfFrame();
            }
            
            WMRecipeCust.WLog.LogInfo($"Finished streaming {requestedFiles.Length} files to Peer {targetPeer}.");
            ZRoutedRpc.instance.InvokeRoutedRPC(targetPeer, "WackyDB_ClientMSG", "WackyDB: Finished downloading missing assets. Reloading now...");

            PendingSyncClients.Remove(targetPeer);
            CheckPendingSyncClients();
        }

        public static void ReceivePayload(long sender, string type, string filename, string base64)
        {
            if (ZNet.instance.IsServer()) return;
            
            try
            {
                byte[] decodedBytes = Convert.FromBase64String(base64);
                string path = "";
                
                if (type == "icon") path = Path.Combine(WMRecipeCust.assetPathIcons, filename + ".png");
                else if (type == "png") path = Path.Combine(WMRecipeCust.assetPathObjects, filename + ".png");
                else if (type == "obj") path = Path.Combine(WMRecipeCust.assetPathObjects, filename + ".obj");
                else if (type == "tex") path = Path.Combine(WMRecipeCust.assetPathTextures, filename + ".png");
                
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllBytes(path, decodedBytes);
                    DownloadedAssetCountCurrentSync++;
                    WMRecipeCust.WLog.LogInfo($"Downloaded and saved {filename} of type {type}");
                }
            }
            catch (System.Exception ex)
            {
                WMRecipeCust.WLog.LogError($"Error decoding/saving {filename}: {ex.Message}");
            }
        }

        public static void ClientMSG(long sender, string msg)
        {
            if (Player.m_localPlayer != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, msg);
            }

            if (msg.Contains("Finished downloading missing assets") && DownloadedAssetCountCurrentSync > 0 && !AssetReloadInProgress)
            {
                HandleData hd = new HandleData();
                WMRecipeCust.context.StartCoroutine(hd.ReloadAfterAssetSync());
            }

            WMRecipeCust.WLog.LogInfo("Server MSG: " + msg);
        }

        private IEnumerator ReloadAfterAssetSync()
        {
            AssetReloadInProgress = true;
            WMRecipeCust.WLog.LogInfo($"WackyDB: Reloading after downloading {DownloadedAssetCountCurrentSync} asset(s).");

            if (Player.m_localPlayer != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "WackyDB: Reloading downloaded assets...");
            }

            try
            {
                yield return WMRecipeCust.context.StartCoroutine(WMRecipeCust.CurrentReload.LoadAllRecipeData(true, true));

            }
            finally
            {
                DownloadedAssetCountCurrentSync = 0;
                AssetReloadInProgress = false;
            }
        }

        public static void AdminLogMsg(long sender, string msg)
        {
            WMRecipeCust.WLog.LogInfo(msg);
            if (Console.instance != null)
            {
                Console.instance.Print(msg);
            }
        }
    }
}
