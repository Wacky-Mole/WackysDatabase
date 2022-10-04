using System.Reflection;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;

namespace VisualsModifier
{
    [BepInPlugin(PluginID, PluginName, PluginVersion)]
    [BepInDependency("WackyMole.WackysDatabase")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginID   = "org.bepinex.visualsmodifier";
        public const string PluginName = "Visuals Modifier";
        public const string PluginVersion = "0.0.1";

        private Harmony _harmony;

        [UsedImplicitly]
        public void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginID);
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
