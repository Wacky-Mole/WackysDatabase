using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace wackydatabase.Startup
{
    internal class Startup 
    {
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


    }
}
