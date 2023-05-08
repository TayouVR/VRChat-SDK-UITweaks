using System.Reflection;
using HarmonyLib;

namespace Tayou.VRChat.SDKUITweaks.Editor {

    public partial class Patches {

        [HarmonyPatch]
        [HarmonyPriority(Priority.Low)]
        private class PatchControlPanel1 {
            
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod() => AccessTools.Method(typeof(VRCSdkControlPanel), nameof(VRCSdkControlPanel.NoGuiErrorsOrIssues));

            [HarmonyPrefix]
            // ReSharper disable once InconsistentNaming
            public static bool Prefix(VRCExpressionParametersEditor __instance, ref bool __result) {
                __result = true;
                //DebugLog("The patch is working 1");
                return false;
            }
        }
        
        [HarmonyPatch]
        [HarmonyPriority(Priority.Low)]
        private class PatchControlPanel2 {
            
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod() => AccessTools.Method(typeof(VRCSdkControlPanel), nameof(VRCSdkControlPanel.NoGuiErrors));

            [HarmonyPrefix]
            // ReSharper disable once InconsistentNaming
            public static bool Prefix(VRCExpressionParametersEditor __instance, ref bool __result) {
                __result = true;
                //DebugLog("The patch is working 2");
                return false;
            }
        }

    }

}