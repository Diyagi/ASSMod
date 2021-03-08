using BepInEx;
using HarmonyLib;
using System;

namespace ValheimDiyagi {

    [BepInPlugin("diyagi.ValheimMod", "Valheim Mod", version)]
    public class ValheimDiyagi : BaseUnityPlugin {

        public const string version = "0.0.1";

        void Awake() {

            Harmony harmony = new Harmony("diyagi.ValheimMod");
            harmony.PatchAll();

            Logger.LogInfo("Valheim Diyagi Mod Loaded Successfully - Version(" + version + ")");
        }
    }
}
