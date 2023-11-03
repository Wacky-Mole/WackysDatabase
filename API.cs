using JetBrains.Annotations;
using System;
using System.Reflection;
using UnityEngine;
using wackydatabase;

namespace API;

[PublicAPI]
public class WackyDatabase_API
{

    private static readonly bool _IsInstalled;
    private static MethodInfo eAddBlacklistClone;
    private static MethodInfo eGetClonedMap;

    public static bool IsInstalled() => _IsInstalled;
    public static void AddBlacklistClone(string value)
    {
        eAddBlacklistClone?.Invoke(null, new object[] { value });
    }
    public static string GetClonedMap(string maybeclone)
    {
        string result ="";
        if (eGetClonedMap != null) result =  (string)eGetClonedMap.Invoke(null, new object[] { maybeclone });
        return result;
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
        eGetClonedMap = wackydatabaseAPI.GetMethod("GetClonedMap", BindingFlags.Public | BindingFlags.Static);
    }
}

// don't use
public static class WackyAPI
{
   public static void AddBlacklistClone(string value)
    {
        //WMRecipeCust.WLog.LogInfo("Added to blacklist "+ value);

        WMRecipeCust.AddBlacklistClone(value);
    }

    public static string GetClonedMap(string maybeclone)
    {
        if (WMRecipeCust.ClonedPrefabsMap.ContainsKey(maybeclone))
        {
            return WMRecipeCust.ClonedPrefabsMap[maybeclone];
        }
        else
        {
            return "";
        }
    }

}