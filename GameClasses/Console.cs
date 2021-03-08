using System;
using HarmonyLib;
using UnityEngine.UI;

namespace ValheimDiyagi
{
    /// <summary>
    ///     Hooks some Private Methods from Console.
    /// </summary>
    [HarmonyPatch(typeof(Console))]
    public static class HookConsole
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Console), "AddString", typeof(string))]
        public static void AddString(object instance, string text)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Listens to command to Sync the Map Data to the callers client.
    /// </summary>
    [HarmonyPatch(typeof(Console), "InputText")]
    public static class InputText
    {
        private static bool Prefix(ref Console __instance, ref InputField ___m_input)
        {
            var text = ___m_input.text;
            if (text != "setmapdata") return true;
            HookConsole.AddString(__instance, text);

            ServerSideMinimap.SendMapData();

            return false;

        }
    }
}