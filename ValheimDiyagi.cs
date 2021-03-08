using BepInEx;
using HarmonyLib;

namespace ValheimDiyagi
{
    [BepInPlugin("diyagi.ServerSideMap", "DSSMap", version)]
    public class ValheimDiyagi : BaseUnityPlugin
    {
        public const string version = "0.0.1";

        private void Awake()
        {
            var harmony = new Harmony("diyagi.ServerSideMap");
            harmony.PatchAll();

            Logger.LogInfo("Server Side Map Loaded Successfully - Version(" + version + ")");
        }
    }
}