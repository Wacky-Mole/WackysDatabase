using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wackydatabase.Startup
{
    internal class Startup : WMRecipeCust
    {
        public static bool SinglePlayerchecker
        {
            get { return issettoSinglePlayer; }
            set
            {
                issettoSinglePlayer = true;
                return;
            }
        }
        public static bool IsLocalInstance(ZNet znet)
        {
            if (znet.IsServer() && !znet.IsDedicated())
            {
                issettoSinglePlayer = true;
                ConfigSync.CurrentVersion = "0.0.1"; // kicking player from server
                WackysRecipeCustomizationLogger.LogWarning("You Will be kicked from Multiplayer Servers! " + ConfigSync.CurrentVersion);
            }
            return issettoSinglePlayer;
        }


    }
}
