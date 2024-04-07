using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Tayou.VRChat.SDKUITweaks.Editor {

    public partial class Patches {

        // ******************
        // This doesn't work in SDK versions 3.3.0 and higher, I have no idea how to fix right now.
        // ******************

        [HarmonyPriority(Priority.Low)]
        private class PatchControlPanelErrorsOrIssues : PatchBase {
            
            public override string GetPatchDisplayName() {
                return "Disable Control Panel Error Blocking 1";
            }

            protected override IEnumerable<MethodBase> GetPatches() {
                yield return AccessTools.Method(typeof(VRCSdkControlPanel), nameof(VRCSdkControlPanel.NoGuiErrorsOrIssues));
            }
            
            // ReSharper disable once InconsistentNaming
            public static bool Prefix(VRCExpressionParametersEditor __instance, ref bool __result) {
                __result = true;
                //DebugLog("The patch is working 1");
                return false;
            }
        }
        
        [HarmonyPriority(Priority.Low)]
        private class PatchControlPanelErrors : PatchBase {
            
            public override string GetPatchDisplayName() {
                return "Disable Control Panel Error Blocking 2";
            }

            protected override IEnumerable<MethodBase> GetPatches() {
                yield return AccessTools.Method(typeof(VRCSdkControlPanel), nameof(VRCSdkControlPanel.NoGuiErrors));
            }

            // ReSharper disable once InconsistentNaming
            public static bool Prefix(VRCExpressionParametersEditor __instance, ref bool __result) {
                __result = true;
                //DebugLog("The patch is working 2");
                return false;
            }
        }

    }

}