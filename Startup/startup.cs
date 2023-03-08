using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using wackydatabase.SetData;


namespace wackydatabase.Startup
{


    internal class Startup 
    {

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        [HarmonyPriority(Priority.Last)]
        class ZNetScene_Awake_Patch_WackysDatabase
        {
            static void Postfix()
            {
                if (!WMRecipeCust.modEnabled.Value)
                    return;
                WMRecipeCust.context.StartCoroutine(DelayedLoadRecipes());// very importrant for last sec load
                                                                          //LoadAllRecipeData(true);
                //public Reload CurrentReload = new Reload();
                // Reload.DelayedLoadRecipes();

            }
        }


        public static bool SinglePlayerchecker
        {
            get { return WMRecipeCust.issettoSinglePlayer; }
            set
            {
                WMRecipeCust.issettoSinglePlayer = true;
                return;
            }
        }
        public static bool IsLocalInstance(ZNet znet)
        {
            if (znet.IsServer() && !znet.IsDedicated())
            {
                WMRecipeCust.issettoSinglePlayer = true;
                WMRecipeCust.ConfigSync.CurrentVersion = "0.0.1"; // kicking player from server
                WMRecipeCust.WLog.LogWarning("You Will be kicked from Multiplayer Servers! " + WMRecipeCust.ConfigSync.CurrentVersion);
            }
            return WMRecipeCust.issettoSinglePlayer;
        }

        public static IEnumerator DelayedLoadRecipes()
        {
            yield return new WaitForSeconds(0.1f);
            
            SetData.Reload temp = new SetData.Reload();
            WMRecipeCust.CurrentReload = temp;
            //ReadFiles readnow = new ReadFiles(); // should already be read
            //readnow.GetDataFromFiles(); Don't need to reload files on first run, only on reload otherwise might override skillConfigData.Value

            temp.LoadAllRecipeData(true);
            yield break;
        }


    }
}
