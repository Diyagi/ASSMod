using System;
using HarmonyLib;

namespace ValheimDiyagi
{
    /// <summary>
    ///     Hooks some Private Methods from ZRoutedRpc.
    /// </summary>
    [HarmonyPatch(typeof(ZRoutedRpc))]
    public class HookZRoutedRpc
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZRoutedRpc), "GetServerPeerID", new Type[] { })]
        public static long GetServerPeerID(object instance)
        {
            throw new NotImplementedException();
        }
    }
}