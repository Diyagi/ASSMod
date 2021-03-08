using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace ValheimDiyagi {
    [HarmonyPatch(typeof(ZNet))]
    public class HookZNet {
        /// <summary>
        /// Hook base GetOtherPublicPlayer method
        /// </summary>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZNet), "GetOtherPublicPlayers", new Type[] { typeof(List<ZNet.PlayerInfo>) })]
        public static void GetOtherPublicPlayers(object instance, List<ZNet.PlayerInfo> playerList) => throw new NotImplementedException();
    }

    [HarmonyPatch(typeof(ZNet), "SaveWorldThread")]
    public class SaveMinimapData {
        public static void Prefix(ref World ___m_world) {
            string savepath = ___m_world.GetDBPath().Replace(".db", ".map");
            FileStream fileStream = File.Create(savepath);
            BinaryWriter binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Write(HookMinimap.GetMapData(Minimap.instance).Length);
            binaryWriter.Write(HookMinimap.GetMapData(Minimap.instance));
            binaryWriter.Flush();
            fileStream.Flush(true);
            fileStream.Close();
            fileStream.Dispose();
        }
    }

    /// <summary>
    /// Force player public reference position on
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "SetPublicReferencePosition")]
    public static class PreventPublicPositionToggle {
        private static void Postfix(ref bool pub, ref bool ___m_publicReferencePosition) {
            ___m_publicReferencePosition = true;
        }
    }
}
