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
    public static void AddBlacklistClone(GameObject value)
    {
        eAddBlacklistClone?.Invoke(null, new object[] { value });
    }



    static WackyDatabase_API()
    {
        if (Type.GetType("API.WackydbAPI, wackydb") is not { } wackydatabaseAPI)
        {
            _IsInstalled = false;
            return;
        }

        _IsInstalled = true;
        eAddBlacklistClone = wackydatabaseAPI.GetMethod("AddAddBlacklistClone", BindingFlags.Public | BindingFlags.Static);
    }

}

// don't use
public static class WackydbAPI
{
   public static void AddBlacklistClone(GameObject value)
    {
        WMRecipeCust.AddBlacklistClone(value);
    }

}