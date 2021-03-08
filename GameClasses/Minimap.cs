using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

namespace ValheimDiyagi {

    /// <summary>
    /// Hooks Minimap methods
    /// </summary>
    [HarmonyPatch(typeof(Minimap))]
    public class HookMinimap {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "Explore", new Type[] { typeof(Vector3), typeof(float) })]
        public static void call_Explore(object instance, Vector3 p, float radius) => throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "ClearPins", new Type[] { })]
        public static void ClearPins(object instance) => throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "GetSprite", new Type[] { typeof(Minimap.PinType) })]
        public static Sprite GetSprite(object instance, Minimap.PinType type) => throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "SetMapData", new Type[] { typeof(byte[]) })]
        public static void SetMapData(object instance, byte[] data) => throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "GetMapData", new Type[] { })]
        public static byte[] GetMapData(object instance) => throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "ScreenToWorldPoint", new Type[] { typeof(Vector3) })]
        public static Vector3 ScreenToWorldPoint(object instance, Vector3 mousePos) => throw new NotImplementedException();
        
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "ShowPinNameInput", new Type[] { typeof(Minimap.PinData) })]
        public static void ShowPinNameInput(object instance, Minimap.PinData pin) => throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "GetClosestPin", new Type[] { typeof(Vector3), typeof(float) })]
        public static Minimap.PinData GetClosestPin(object instance, Vector3 pos, float radius) => throw new NotImplementedException();

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "RemovePin", new Type[] { typeof(Vector3), typeof(float) })]
        public static bool RemovePin(object instance, Vector3 pos, float radius) => throw new NotImplementedException();
        
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Minimap), "HaveSimilarPin", new Type[] { typeof(Vector3), typeof(Minimap.PinType), typeof(string), typeof(bool) })]
        public static bool HaveSimilarPin(object instance, Vector3 pos, Minimap.PinType type, string name, bool save) => throw new NotImplementedException();
    }

    /// <sumary>
    /// Update exploration from others
    /// <sumary>
    [HarmonyPatch(typeof(Minimap), "UpdateExplore")]
    public static class ChangeMapBehavior {
        private static void Prefix(ref Minimap __instance, ref float ___m_exploreTimer, ref float ___m_exploreInterval, ref List<ZNet.PlayerInfo> ___m_tempPlayerInfo, ref float ___m_exploreRadius) {

            float explorerTime = ___m_exploreTimer;
            explorerTime += Time.deltaTime;
            if (explorerTime > ___m_exploreInterval) {
                ___m_tempPlayerInfo.Clear();
                HookZNet.GetOtherPublicPlayers(ZNet.instance, ___m_tempPlayerInfo);

                if (___m_tempPlayerInfo.Count() > 0) {
                    foreach (ZNet.PlayerInfo m_Player in ___m_tempPlayerInfo) {
                        HookMinimap.call_Explore(__instance, m_Player.m_position, ___m_exploreRadius);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Minimap))]

    /// <sumary>
    /// Server Side Minimap Data
    /// <sumary>
    class ServerSideMinimap {

        /// <sumary>
        /// Register RPC Calls
        /// <sumary>
        [HarmonyPatch(typeof(Minimap), "Start")]
        public static class Start {
            private static void Prefix(ref Minimap __instance, ref float ___m_exploreRadius) {
                __instance.m_exploreInterval = 2f;
                ZRoutedRpc.instance.Register("RPC_AddPin", new Action<long, Vector3, int, string>(RPC_AddPin));
                ZRoutedRpc.instance.Register("RPC_RenamePin", new Action<long, Vector3, string>(RPC_RenamePin));
                ZRoutedRpc.instance.Register("RPC_CheckPin", new Action<long, Vector3, float, bool>(RPC_CheckPin));
                ZRoutedRpc.instance.Register("RPC_RemovePin", new Action<long, Vector3, float>(RPC_RemovePin));
                ZRoutedRpc.instance.Register("RPC_ResponseMapData", new Action<long, ZPackage>(RPC_ResponseMapData));
                if (ZNet.instance.IsServer()) {
                    ZRoutedRpc.instance.Register("RPC_RequestMapData", new Action<long>(RPC_RequestMapData));
                    ZRoutedRpc.instance.Register("RPC_SetMapData", new Action<long, ZPackage>(RPC_SetMapData));
                }
            }
        }

        /// <sumary>
        /// RPC_AddPin Method
        /// <sumary>
        private static void RPC_AddPin(long sender, Vector3 pos, int type, string name) {
            if (sender == ZDOMan.instance.GetMyID()) return;

            ZLog.Log("RPC_AddPin Called");
            Minimap.instance.AddPin(pos, (Minimap.PinType)type, name, true, false);
        }
        
        /// <sumary>
        /// RPC_AddPin Method
        /// <sumary>
        private static void RPC_RenamePin(long sender, Vector3 pos, string name) {
            if (sender == ZDOMan.instance.GetMyID()) return;

            ZLog.Log("RPC_AddPin Called");
            Minimap.PinData closestPin = HookMinimap.GetClosestPin(Minimap.instance, pos, 2f);
            if (closestPin != null) {
                closestPin.m_name = name;
            }
        }

        /// <sumary>
        /// RPC_CheckPin Method
        /// <sumary>
        private static void RPC_CheckPin(long sender, Vector3 pos, float radius, bool check) {
            if (sender == ZDOMan.instance.GetMyID()) return;

            ZLog.Log("RPC_CheckPin Called");
            Minimap.PinData closestPin = HookMinimap.GetClosestPin(Minimap.instance, pos, radius);
            if (closestPin != null) {
                closestPin.m_checked = !check;
            }
        }

        /// <sumary>
        /// RPC_RemovePin Method
        /// <sumary>
        private static void RPC_RemovePin(long sender, Vector3 pos, float radius) {
            if (sender == ZDOMan.instance.GetMyID()) return;

            ZLog.Log("RPC_RemovePin Called");
            Minimap.PinData closestPin = HookMinimap.GetClosestPin(Minimap.instance, pos, radius);
            if (closestPin != null) {
                HookMinimap.RemovePin(Minimap.instance, closestPin.m_pos, radius);
            }
        }

        private static void RPC_RequestMapData(long sender) {
            ZLog.Log("Map Data Requested by " + sender);

            byte[] data = HookMinimap.GetMapData(Minimap.instance);
            byte[] cdata = Utils.Compress(data);

            byte[] cdatamd5 = Utils.GenerateMD5(cdata);
            byte[] datamd5 = Utils.GenerateMD5(data);

            ZPackage pkg = new ZPackage();
            pkg.Write(cdatamd5);
            pkg.Write(datamd5);
            pkg.Write(cdata);

            ZLog.Log("Map Packet Size: " + pkg.Size() + "Bs of " + data.Length + "Bs");

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_ResponseMapData", new object[] { pkg });
        }

        private static void RPC_ResponseMapData(long sender, ZPackage pkg) {
            ZLog.Log("Map Data Recived by " + sender + " (Expected: " + HookZRoutedRpc.GetServerPeerID(ZRoutedRpc.instance) + ")");
            if (sender == HookZRoutedRpc.GetServerPeerID(ZRoutedRpc.instance)) {
                byte[] rcdatamd5 = pkg.ReadByteArray();
                byte[] rdatamd5 = pkg.ReadByteArray();
                byte[] cdata = pkg.ReadByteArray();

                byte[] cdatamd5 = Utils.GenerateMD5(cdata);

                ZLog.Log("Compressed Data MD5: " + Utils.ByteArrayToString(cdatamd5) + " | Expected: " + Utils.ByteArrayToString(rcdatamd5));
                if (cdatamd5.SequenceEqual(rcdatamd5)) {
                    byte[] data = Utils.Decompress(cdata);
                    byte[] datamd5 = Utils.GenerateMD5(data);

                    ZLog.Log("Uncompressed Data MD5: " + Utils.ByteArrayToString(datamd5) + " | Expected: " + Utils.ByteArrayToString(rdatamd5));

                    ZLog.Log("Map Packet Size: " + pkg.Size() + "Bs of " + data.Length + "Bs");
                    if (datamd5.SequenceEqual(rdatamd5)) {
                        ZLog.Log("Map Data Applied");
                        HookMinimap.SetMapData(Minimap.instance, data);
                    }
                }
            }
        }

        private static void RPC_SetMapData(long sender, ZPackage pkg) {
            byte[] rcdatamd5 = pkg.ReadByteArray();
            byte[] rdatamd5 = pkg.ReadByteArray();
            byte[] cdata = pkg.ReadByteArray();

            byte[] cdatamd5 = Utils.GenerateMD5(cdata);

            ZLog.Log("Compressed Data MD5: " + Utils.ByteArrayToString(cdatamd5) + " | Expected: " + Utils.ByteArrayToString(rcdatamd5));
            if (cdatamd5.SequenceEqual(rcdatamd5)) {
                byte[] data = Utils.Decompress(cdata);
                byte[] datamd5 = Utils.GenerateMD5(data);

                ZLog.Log("Uncompressed Data MD5: " + Utils.ByteArrayToString(datamd5) + " | Expected: " + Utils.ByteArrayToString(rdatamd5));
                if (datamd5.SequenceEqual(rdatamd5)) {
                    ZLog.Log("Map Data Applied");
                    HookMinimap.SetMapData(Minimap.instance, data);
                }
            }
        }

        public static void SendMapData() {

            byte[] data = HookMinimap.GetMapData(Minimap.instance);
            byte[] cdata = Utils.Compress(data);

            byte[] cdatamd5 = Utils.GenerateMD5(cdata);
            byte[] datamd5 = Utils.GenerateMD5(data);

            ZPackage pkg = new ZPackage();
            pkg.Write(cdatamd5);
            pkg.Write(datamd5);
            pkg.Write(cdata);

            ZRoutedRpc.instance.InvokeRoutedRPC("RPC_SetMapData", new object[] { pkg });
        }

        /// <sumary>
        /// Call RPC AddPin on On Map Double Click
        /// <sumary>
        [HarmonyPatch(typeof(Minimap), "OnMapDblClick")]
        public static class OnMapDblClick {
            private static bool Prefix(ref Minimap __instance, ref Minimap.PinType ___m_selectedType) {

                Vector3 pos = HookMinimap.ScreenToWorldPoint(__instance, Input.mousePosition);
                Minimap.PinData pin = Minimap.instance.AddPin(pos, ___m_selectedType, "", true, false);
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_AddPin", new object[] { pin.m_pos, (int)pin.m_type, pin.m_name });
                HookMinimap.ShowPinNameInput(__instance, pin);

                return false;

            }
        }

        /// <sumary>
        /// Call RPC RenamePin on NameInputUpdate
        /// <sumary>
        [HarmonyPatch(typeof(Minimap), "UpdateNameInput")]
        public static class UpdateNameInput {
            private enum MapMode {
                // Token: 0x040010FD RID: 4349
                None,
                // Token: 0x040010FE RID: 4350
                Small,
                // Token: 0x040010FF RID: 4351
                Large
            }
            private static bool Prefix(ref Minimap __instance, ref Minimap.PinData ___m_namePin, ref bool ___m_wasFocused, ref MapMode ___m_mode, ref InputField ___m_nameInput) {
                if (___m_namePin == null) {
                    ___m_wasFocused = false;
                }
                if (___m_namePin != null && ___m_mode == MapMode.Large) {
                    ___m_nameInput.gameObject.SetActive(true);
                    if (!___m_nameInput.isFocused) {
                        EventSystem.current.SetSelectedGameObject(___m_nameInput.gameObject);
                    }
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) {
                        string text = ___m_nameInput.text;
                        text = text.Replace('$', ' ');
                        text = text.Replace('<', ' ');
                        text = text.Replace('>', ' ');
                        ___m_namePin.m_name = text;
                        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_RenamePin", new object[] { ___m_namePin.m_pos, ___m_namePin.m_name });
                        ___m_namePin = null;
                    }
                    ___m_wasFocused = true;
                    return false;
                }
                ___m_nameInput.gameObject.SetActive(false);
                return false;
            }
        }

        /// <sumary>
        /// Call RPC CheckPin on Map Left Click
        /// <sumary>
        [HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
        public static class OnMapLeftClick {
            private static bool Prefix(ref Minimap __instance, ref float ___m_removeRadius, ref float ___m_largeZoom) {
                ZLog.Log("Left click");
                Vector3 pos = HookMinimap.ScreenToWorldPoint(__instance, Input.mousePosition);
                float radius = ___m_removeRadius * (___m_largeZoom * 2f);
                Minimap.PinData closestPin = HookMinimap.GetClosestPin(__instance, pos, radius);
                if (closestPin != null) {

                    closestPin.m_checked = !closestPin.m_checked;
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_CheckPin", new object[] { closestPin.m_pos, radius, closestPin.m_checked });
                }

                return false;
            }
        }

        /// <sumary>
        /// Call RPC RemovePin on Map Right Click
        /// <sumary>
        [HarmonyPatch(typeof(Minimap), "OnMapRightClick")]
        public static class OnMapRightClick {
            private static bool Prefix(ref Minimap __instance, ref float ___m_removeRadius, ref float ___m_largeZoom, ref Minimap.PinData ___m_namePin) {
                ZLog.Log("Right click");
                Vector3 pos = HookMinimap.ScreenToWorldPoint(__instance, Input.mousePosition);
                float radius = ___m_removeRadius * (___m_largeZoom * 2f);
                Minimap.PinData closestPin = HookMinimap.GetClosestPin(__instance, pos, radius);
                if (closestPin != null) {

                    HookMinimap.RemovePin(__instance, pos, radius);
                    ___m_namePin = null;

                    ZPackage pkg = new ZPackage();
                    pkg.Write((double)radius);
                    pkg.Write(closestPin.m_pos);
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_RemovePin", new object[] { closestPin.m_pos, radius });
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(Minimap), "Update")]
        public static class Update {
            private static void Prefix(ref Minimap __instance, ref bool ___m_hasGenerated, ref float ___m_exploreTimer, ref float ___m_exploreInterval, ref List<ZNet.PlayerInfo> ___m_tempPlayerInfo, ref float ___m_exploreRadius) {
                if (ZNet.instance.IsServer() || ZNet.instance.IsDedicated()) {

                    if (!___m_hasGenerated) {
                        if (WorldGenerator.instance == null) {
                            return;
                        }
                        string savepath = global::Utils.GetSaveDataPath() + "/worlds/" + ZNet.instance.GetWorldName() + ".map";
                        FileStream fileStream;
                        try {
                            fileStream = File.OpenRead(savepath);
                        }
                        catch {
                            ZLog.Log("  failed to load " + savepath);
                            return;
                        }

                        byte[] data;

                        try {
                            BinaryReader binaryReader = new BinaryReader(fileStream);
                            var bytes = binaryReader.ReadInt32();
                            data = binaryReader.ReadBytes(bytes);
                        }
                        catch {
                            ZLog.LogError("  error loading player.dat");
                            fileStream.Dispose();
                            return;
                        }
                        fileStream.Dispose();
                        HookMinimap.SetMapData(Minimap.instance, data);
                        ___m_hasGenerated = true;
                    }
                    ___m_exploreTimer += Time.deltaTime;
                    if (___m_exploreTimer > ___m_exploreInterval) {
                        ___m_exploreTimer = 0f;
                        ___m_tempPlayerInfo.Clear();
                        HookZNet.GetOtherPublicPlayers(ZNet.instance, ___m_tempPlayerInfo);

                        if (___m_tempPlayerInfo.Count() > 0) {
                            foreach (ZNet.PlayerInfo m_Player in ___m_tempPlayerInfo) {
                                HookMinimap.call_Explore(__instance, m_Player.m_position, ___m_exploreRadius);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Minimap), "LoadMapData")]
        public static class LoadMapData {
            private static bool Prefix(ref Minimap __instance) {

                PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                if (playerProfile.GetMapData() != null) {
                    HookMinimap.SetMapData(__instance, playerProfile.GetMapData());
                }

                ZRoutedRpc.instance.InvokeRoutedRPC("RPC_RequestMapData", new object[] { });

                return false;
            }
        }

        [HarmonyPatch(typeof(Minimap), "DiscoverLocation")]
        public static class DiscoverLocation {
            private static bool Prefix(ref Minimap __instance, ref bool __result, Vector3 pos, Minimap.PinType type, string name) {
                if (!ZNet.instance.IsServer()) return true;
                if (HookMinimap.HaveSimilarPin(__instance, pos, type, name, true)) {
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