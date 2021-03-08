using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;

namespace ValheimDiyagi
{
    /// <summary>
    ///     Hooks some Private Methods from ZNet.
    /// </summary>
    [HarmonyPatch(typeof(ZNet))]
    public class HookZNet
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZNet), "GetOtherPublicPlayers", typeof(List<ZNet.PlayerInfo>))]
        public static void GetOtherPublicPlayers(object instance, List<ZNet.PlayerInfo> playerList)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Saves the Map Data to a .map file in the world dir.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "SaveWorldThread")]
    public class SaveMinimapData
    {
        public static void Prefix(ref World ___m_world)
        {
            var savepath = ___m_world.GetDBPath().Replace(".db", ".map");
            var fileStream = File.Create(savepath);
            var binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Write(HookMinimap.GetMapData(Minimap.instance).Length);
            binaryWriter.Write(HookMinimap.GetMapData(Minimap.instance));
            binaryWriter.Flush();
            fileStream.Flush(true);
            fileStream.Close();
            fileStream.Dispose();
        }
    }

    /// <summary>
    ///     Always share players position.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "SetPublicReferencePosition")]
    public static class AlwaysSharePosition
    {
        private static void Postfix(ref bool ___m_publicReferencePosition)
        {
            ___m_publicReferencePosition = true;
        }
    }
}