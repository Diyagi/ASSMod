using HarmonyLib;
using System;

namespace ValheimDiyagi {

    [HarmonyPatch(typeof(ZRoutedRpc))]
    public class HookZRoutedRpc {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZRoutedRpc), "GetServerPeerID", new Type[] { })]
        public static long GetServerPeerID(object instance) => throw new NotImplementedException();
    }
}
