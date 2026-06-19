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
        // scheme is going to be filename:type;base64==filename:type;base64==
        internal static string bigDataR;
        internal static List<string> bigDataRChucks = new List<string>();
        internal static string bigDataS;
        internal static List<string> bigDataSChucks = new List<string>();   

        internal static HashSet<long> PendingSyncClients = new HashSet<long>();
        internal static long AdminPeerForSync = 0L;

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
            if (!ZNet.instance.IsServer() || peerId == 0L || WMRecipeCust.issettoSinglePlayer)
            {
                return;
            }

            HandleData hd = new HandleData();
            WMRecipeCust.context.StartCoroutine(hd.SendDataToPeerWhenReady(peerId));
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
                    yield break;

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
            // Legacy function kept for backward compatibility with `largeTransfer` 
            // All actual syncing is now handled by ZRoutedRpc through ReceiveManifest, ReceiveRequest, and ReceivePayload.
            return;
        }

        public static void SendData(long peer, ZPackage go) // should probably be a console command because this will send it to everyone and be huge!
        {
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
                WMRecipeCust.WLog.LogInfo($"Client missing {neededFiles.Count} files. Requesting specifically from server...");
                string requestStr = string.Join("?", neededFiles);
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "WackyDB_AssetRequest", requestStr);
            }
            else
            {
                WMRecipeCust.WLog.LogInfo("Client already has all WackyDB server assets up to date. Nothing to download!");
                if (Player.m_localPlayer != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "WackyDB: All Assets already up to date!");
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
                    byte[] bytes = File.ReadAllBytes(files[0]);
                    string base64 = Convert.ToBase64String(bytes);
                    
                    ZRoutedRpc.instance.InvokeRoutedRPC(targetPeer, "WackyDB_AssetPayload", type, filename, base64);
                }
                
                // Yield to prevent overwhelming network buffer
                yield return new WaitForEndOfFrame();
            }
            
            WMRecipeCust.WLog.LogInfo($"Finished streaming {requestedFiles.Length} files to Peer {targetPeer}.");
            ZRoutedRpc.instance.InvokeRoutedRPC(targetPeer, "WackyDB_ClientMSG", "WackyDB: Finished downloading missing assets. Restart game to apply!");

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
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, msg);
            }
            WMRecipeCust.WLog.LogInfo("Server MSG: " + msg);
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
