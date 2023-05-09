using JetBrains.Annotations;
using System;
using System.Reflection;
using UnityEngine;
using wackydatabase;

namespace API;


public class WackyDatabase_API
{

    private static readonly bool _IsInstalled;
    private static MethodInfo eAddBlacklistClone;


    public static bool IsInstalled() => _IsInstalled;
    public static void AddBlacklistClone(string value)
    {
        eAddBlacklistClone?.Invoke(null, new object[] { value });
    }

    static WackyDatabase_API()
    {
            if (Type.GetType("API.WackyAPI, WackysDatabase") == null)
            {
            _IsInstalled = false;
            return;
        }
        Type wackydatabaseAPI = Type.GetType("API.WackyAPI, WackysDatabase");
        _IsInstalled = true;
        eAddBlacklistClone = wackydatabaseAPI.GetMethod("AddBlacklistClone", BindingFlags.Public | BindingFlags.Static);
    }
}

// don't use
[PublicAPI]
public static class WackyAPI
{
   public static void AddBlacklistClone(string value)
    {
        WMRecipeCust.WLog.LogInfo("Added to blacklist "+ value);

        WMRecipeCust.AddBlacklistClone(value);
    }

}