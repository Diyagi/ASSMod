using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ValheimDiyagi
{
    /// <summary>
    ///     Hooks Minimap methods
    /// </summary>
    [HarmonyPatch(typeof(Minimap))]
    public class HookMinimap
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "Explore", typeof(Vector3), typeof(float))]
        public static void call_Explore(object instance, Vector3 p, float radius)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "ClearPins", new Type[] { })]
        public static void ClearPins(object instance)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "SetMapData", typeof(byte[]))]
        public static void SetMapData(object instance, byte[] data)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "GetMapData", new Type[] { })]
        public static byte[] GetMapData(object instance)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "ScreenToWorldPoint", typeof(Vector3))]
        public static Vector3 ScreenToWorldPoint(object instance, Vector3 mousePos)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "ShowPinNameInput", typeof(Minimap.PinData))]
        public static void ShowPinNameInput(object instance, Minimap.PinData pin)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "GetClosestPin", typeof(Vector3), typeof(float))]
        public static Minimap.PinData GetClosestPin(object instance, Vector3 pos, float radius)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "RemovePin", typeof(Vector3), typeof(float))]
        public static bool RemovePin(object instance, Vector3 pos, float radius)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "HaveSimilarPin", typeof(Vector3), typeof(Minimap.PinType), typeof(string),
            typeof(bool))]
        public static bool HaveSimilarPin(object instance, Vector3 pos, Minimap.PinType type, string name, bool save)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Update exploration from others
    /// </summary>
    [HarmonyPatch(typeof(Minimap), "UpdateExplore")]
    public static class ChangeMapBehavior
    {
        private static void Prefix(ref Minimap __instance, ref float ___m_exploreTimer, ref float ___m_exploreInterval,
            ref List<ZNet.PlayerInfo> ___m_tempPlayerInfo, ref float ___m_exploreRadius)
        {
            var explorerTime = ___m_exploreTimer;
            explorerTime += Time.deltaTime;
            if (explorerTime > ___m_exploreInterval)
            {
                ___m_tempPlayerInfo.Clear();
                HookZNet.GetOtherPublicPlayers(ZNet.instance, ___m_tempPlayerInfo);

                if (___m_tempPlayerInfo.Any())
                    foreach (var m_Player in ___m_tempPlayerInfo)
                        HookMinimap.call_Explore(__instance, m_Player.m_position, ___m_exploreRadius);
            }
        }
    }

    [HarmonyPatch(typeof(Minimap))]
    /// <summary>
    /// Server Side Minimap Data
    /// </summary>
    internal class ServerSideMinimap
    {
        /// <summary>
        ///     (RPC_AddPin) Adds new pin on map.
        /// </summary>
        private static void RPC_AddPin(long sender, Vector3 pos, int type, string name)
        {
            if (sender == ZDOMan.instance.GetMyID()) return;

            ZLog.Log("RPC_AddPin Called");
            Minimap.instance.AddPin(pos, (Minimap.PinType) type, name, true, false);
        }

        /// <summary>
        ///     (RPC_RenamePin) Renames the specified pin on map.
        /// </summary>
        private static void RPC_RenamePin(long sender, Vector3 pos, string name)
        {
            if (sender == ZDOMan.instance.GetMyID()) return;

            ZLog.Log("RPC_RenamePin Called");
            var closestPin = HookMinimap.GetClosestPin(Minimap.instance, pos, 2f);
            if (closestPin != null) closestPin.m_name = name;
        }

        /// <summary>
        ///     (RPC_CheckPin) Cross Checks the specified Pin on Map.
        /// </summary>
        private static void RPC_CheckPin(long sender, Vector3 pos, float radius, bool check)
        {
            if (sender == ZDOMan.instance.GetMyID()) return;

            ZLog.Log("RPC_CheckPin Called");
            var closestPin = HookMinimap.GetClosestPin(Minimap.instance, pos, radius);
            if (closestPin != null) closestPin.m_checked = !check;
        }

        /// <summary>
        ///     (RPC_RemovePin) Removes the especified Pin from map.
        /// </summary>
        private static void RPC_RemovePin(long sender, Vector3 pos, float radius)
        {
            if (sender == ZDOMan.instance.GetMyID()) return;

            ZLog.Log("RPC_RemovePin Called");
            var closestPin = HookMinimap.GetClosestPin(Minimap.instance, pos, radius);
            if (closestPin != null) HookMinimap.RemovePin(Minimap.instance, closestPin.m_pos, radius);
        }

        /// <summary>
        ///     (Server) Response to requests for Map Data.
        /// </summary>
        private static void RPC_RequestMapData(long sender)
        {
            ZLog.Log("Map Data Requested by " + sender);

            var data = HookMinimap.GetMapData(Minimap.instance);
            var pkg = Utils.CompressWithMD5(data);

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_ResponseMapData", pkg);
        }

        /// <summary>
        ///     (Client) Send the client Map Data to the server.
        /// </summary>
        public static void SendMapData()
        {
            var data = HookMinimap.GetMapData(Minimap.instance);

            var pkg = Utils.CompressWithMD5(data);

            ZRoutedRpc.instance.InvokeRoutedRPC("RPC_SetMapData", pkg);
        }

        /// <summary>
        ///     (Client) Receives the Map Data, decompacts, verify (MD5 Hashes) and applies them.
        /// </summary>
        private static void RPC_ResponseMapData(long sender, ZPackage pkg)
        {
            ZLog.Log("Map Data Received by " + sender + " (Expected: " +
                     HookZRoutedRpc.GetServerPeerID(ZRoutedRpc.instance) + ")");
            if (sender != HookZRoutedRpc.GetServerPeerID(ZRoutedRpc.instance)) return;

            var data = Utils.DecompressAndVerify(pkg);

            if (data == null) return;
            ZLog.Log("Map Data Applied");
            HookMinimap.SetMapData(Minimap.instance, data);
        }

        /// <summary>
        ///     (Server) Receives the Map Data from client and Applies it on the server.
        /// </summary>
        private static void RPC_SetMapData(long sender, ZPackage pkg)
        {
            var data = Utils.DecompressAndVerify(pkg);

            if (data != null)
            {
                ZLog.Log("Map Data Applied");
                HookMinimap.SetMapData(Minimap.instance, data);
            }
        }

        /// <summary>
        ///     Register RPC Calls.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "Start")]
        public static class Start
        {
            private static void Prefix(ref Minimap __instance)
            {
                __instance.m_exploreInterval =
                    2f; ///Default value should be 2, but during runtime (don't know why) this value is set to 0.2.
                ZRoutedRpc.instance.Register("RPC_AddPin", new Action<long, Vector3, int, string>(RPC_AddPin));
                ZRoutedRpc.instance.Register("RPC_RenamePin", new Action<long, Vector3, string>(RPC_RenamePin));
                ZRoutedRpc.instance.Register("RPC_CheckPin", new Action<long, Vector3, float, bool>(RPC_CheckPin));
                ZRoutedRpc.instance.Register("RPC_RemovePin", new Action<long, Vector3, float>(RPC_RemovePin));
                ZRoutedRpc.instance.Register("RPC_ResponseMapData", new Action<long, ZPackage>(RPC_ResponseMapData));
                if (ZNet.instance.IsServer())
                {
                    ZRoutedRpc.instance.Register("RPC_RequestMapData", RPC_RequestMapData);
                    ZRoutedRpc.instance.Register("RPC_SetMapData", new Action<long, ZPackage>(RPC_SetMapData));
                }
            }
        }

        /// <summary>
        ///     Modify the OnMapDblClick method to call RPC_AddPin.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "OnMapDblClick")]
        public static class OnMapDblClick
        {
            private static bool Prefix(ref Minimap __instance, ref Minimap.PinType ___m_selectedType)
            {
                var pos = HookMinimap.ScreenToWorldPoint(__instance, Input.mousePosition);
                var pin = Minimap.instance.AddPin(pos, ___m_selectedType, "", true, false);
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_AddPin", pin.m_pos, (int) pin.m_type,
                    pin.m_name);
                HookMinimap.ShowPinNameInput(__instance, pin);

                return false;
            }
        }

        /// <summary>
        ///     Modify the UpdateNameInput method to call RPC_RenamePin.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "UpdateNameInput")]
        public static class UpdateNameInput
        {
            private static bool Prefix(ref Minimap __instance, ref Minimap.PinData ___m_namePin,
                ref bool ___m_wasFocused, ref MapMode ___m_mode, ref InputField ___m_nameInput)
            {
                if (___m_namePin == null) ___m_wasFocused = false;
                if (___m_namePin != null && ___m_mode == MapMode.Large)
                {
                    ___m_nameInput.gameObject.SetActive(true);
                    if (!___m_nameInput.isFocused) EventSystem.current.SetSelectedGameObject(___m_nameInput.gameObject);
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        var text = ___m_nameInput.text;
                        text = text.Replace('$', ' ');
                        text = text.Replace('<', ' ');
                        text = text.Replace('>', ' ');
                        ___m_namePin.m_name = text;
                        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_RenamePin", ___m_namePin.m_pos,
                            ___m_namePin.m_name);
                        ___m_namePin = null;
                    }

                    ___m_wasFocused = true;
                    return false;
                }

                ___m_nameInput.gameObject.SetActive(false);
                return false;
            }

            private enum MapMode
            {
                // Token: 0x040010FD RID: 4349
                None,

                // Token: 0x040010FE RID: 4350
                Small,

                // Token: 0x040010FF RID: 4351
                Large
            }
        }

        /// <summary>
        ///     Modify the OnMapLeftClick method to call RPC_CheckPin.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
        public static class OnMapLeftClick
        {
            private static bool Prefix(ref Minimap __instance, ref float ___m_removeRadius, ref float ___m_largeZoom)
            {
                ZLog.Log("Left click");
                var pos = HookMinimap.ScreenToWorldPoint(__instance, Input.mousePosition);
                var radius = ___m_removeRadius * (___m_largeZoom * 2f);
                var closestPin = HookMinimap.GetClosestPin(__instance, pos, radius);
                if (closestPin != null)
                {
                    closestPin.m_checked = !closestPin.m_checked;
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_CheckPin", closestPin.m_pos, radius,
                        closestPin.m_checked);
                }

                return false;
            }
        }

        /// <summary>
        ///     Modify the OnMapRightClick method to call RPC_RemovePin.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "OnMapRightClick")]
        public static class OnMapRightClick
        {
            private static bool Prefix(ref Minimap __instance, ref float ___m_removeRadius, ref float ___m_largeZoom,
                ref Minimap.PinData ___m_namePin)
            {
                ZLog.Log("Right click");
                var pos = HookMinimap.ScreenToWorldPoint(__instance, Input.mousePosition);
                var radius = ___m_removeRadius * (___m_largeZoom * 2f);
                var closestPin = HookMinimap.GetClosestPin(__instance, pos, radius);
                if (closestPin != null)
                {
                    HookMinimap.RemovePin(__instance, pos, radius);
                    ___m_namePin = null;

                    var pkg = new ZPackage();
                    pkg.Write((double) radius);
                    pkg.Write(closestPin.m_pos);
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_RemovePin", closestPin.m_pos,
                        radius);
                }

                return false;
            }
        }

        /// <summary>
        ///     Add Map Data File Load and Others Map Explore to Minimap.Update method.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "Update")]
        public static class Update
        {
            private static void Prefix(ref Minimap __instance, ref bool ___m_hasGenerated, ref float ___m_exploreTimer,
                ref float ___m_exploreInterval, ref List<ZNet.PlayerInfo> ___m_tempPlayerInfo,
                ref float ___m_exploreRadius)
            {
                if (ZNet.instance.IsServer() || ZNet.instance.IsDedicated())
                {
                    if (!___m_hasGenerated)
                    {
                        if (WorldGenerator.instance == null) return;
                        var savepath = global::Utils.GetSaveDataPath() + "/worlds/" + ZNet.instance.GetWorldName() +
                                       ".map";
                        FileStream fileStream;
                        try
                        {
                            fileStream = File.OpenRead(savepath);
                        }
                        catch
                        {
                            ZLog.Log("  failed to load " + savepath);
                            ___m_hasGenerated = true;
                            return;
                        }

                        byte[] data;

                        try
                        {
                            var binaryReader = new BinaryReader(fileStream);
                            var bytes = binaryReader.ReadInt32();
                            data = binaryReader.ReadBytes(bytes);
                        }
                        catch
                        {
                            ZLog.LogError("  error loading map data");
                            fileStream.Dispose();
                            return;
                        }

                        fileStream.Dispose();
                        HookMinimap.SetMapData(Minimap.instance, data);
                        ___m_hasGenerated = true;
                    }

                    ___m_exploreTimer += Time.deltaTime;
                    if (!(___m_exploreTimer > ___m_exploreInterval)) return;
                    ___m_exploreTimer = 0f;
                    ___m_tempPlayerInfo.Clear();
                    HookZNet.GetOtherPublicPlayers(ZNet.instance, ___m_tempPlayerInfo);

                    if (!___m_tempPlayerInfo.Any()) return;
                    foreach (var m_Player in ___m_tempPlayerInfo)
                        HookMinimap.call_Explore(__instance, m_Player.m_position, ___m_exploreRadius);
                }
            }
        }

        /// <summary>
        ///     Modify method LoadMapData to Request Map Data from server.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "LoadMapData")]
        public static class LoadMapData
        {
            private static bool Prefix(ref Minimap __instance)
            {
                var playerProfile = Game.instance.GetPlayerProfile();
                if (playerProfile.GetMapData() != null) HookMinimap.SetMapData(__instance, playerProfile.GetMapData());

                ZRoutedRpc.instance.InvokeRoutedRPC("RPC_RequestMapData");

                return false;
            }
        }

        /// <summary>
        ///     Modify method DiscoverLocation so it can run on Server Side.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "DiscoverLocation")]
        public static class DiscoverLocation
        {
            private static bool Prefix(ref Minimap __instance, ref bool __result, Vector3 pos, Minimap.PinType type,
                string name)
            {
                if (!ZNet.instance.IsServer()) return true;
                if (HookMinimap.HaveSimilarPin(__instance, pos, type, name, true))
                {
                    __result = false;
                    return false;
                }

                __instance.AddPin(pos, type, name, true, false);
                __result = true;

                return false;
            }
        }
    }
}