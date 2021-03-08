using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace ValheimDiyagi {

    [HarmonyPatch(typeof(Console))]
    public static class HookConsole {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Console), "AddString", new Type[] { typeof(string)})]
        public static void AddString(object instance, string text) => throw new NotImplementedException();
    }
    
    [HarmonyPatch(typeof(Console), "InputText")]
    public static class InputText {
        private static bool Prefix(ref Console __instance, ref InputField ___m_input) {
            string text = ___m_input.text;
            if (text == "setmapdata") {
                HookConsole.AddString(__instance, text);

                ServerSideMinimap.SendMapData();
                
                return false;
            }
            return true;
        }
    }
}
