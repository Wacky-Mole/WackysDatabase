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
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Core;
using System.Security.Policy;
using HarmonyLib;
using wackydatabase.GetData;
using static Interpolate;
using System.Reflection;
using wackydatabase.OBJimporter;

namespace wackydatabase.Startup
{

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZrouteMethodsAdminReloadPushOnServer
    {
        internal static void Prefix()
        {
            if (!ZNet.instance.IsServer()) return; // for servers only

            WMRecipeCust.WLog.LogInfo("Server Ready to receive AdminReload");
           // ZRoutedRpc.instance.Register($"{WMRecipeCust.ModName} AdminReload",new Action<long, bool>(WMRecipeCust.AdminReload));

            ZRoutedRpc.instance.Register("WackyDBAdminReload", new Action<long, ZPackage>(WMRecipeCust.AdminReload));
            ZRoutedRpc.instance.Register("WackyDBAdminBigData", new Action<long, ZPackage>(HandleData.SendData));

            if (WMRecipeCust.Firstrun)
            {
                ObjModelLoader.LoadObjs(); // This means will never get sync data, but that's okay?
                if (ZNet.instance.IsServer() & ZNet.instance.IsDedicated() && !WMRecipeCust.ServerDedLoad.Value)
                    WMRecipeCust.Firstrun = false;
            }

        }
    }

}
