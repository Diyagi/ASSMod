using HarmonyLib;
using UnityEngine;

namespace ValheimDiyagi
{
    /// <summary>
    ///     Modify method RPC_DiscoverClosestLocation so the RPC is sent to everyone (including server).
    /// </summary>
    [HarmonyPatch(typeof(Game), "RPC_DiscoverClosestLocation")]
    public static class RPC_DiscoverClosestLocation
    {
        private static bool Prefix(long sender, string name, Vector3 point, string pinName, int pinType)
        {
            ZoneSystem.LocationInstance locationInstance;
            if (ZoneSystem.instance.FindClosestLocation(name, point, out locationInstance))
            {
                ZLog.Log("Found location of type " + name);
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "DiscoverLocationRespons", pinName, pinType,
                    locationInstance.m_position);
                return false;
            }

            ZLog.LogWarning("Failed to find location of type " + name);
            return false;
        }
    }
}