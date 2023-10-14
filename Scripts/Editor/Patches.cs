using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using UnityEditor;

namespace Tayou.VRChat.SDKUITweaks.Editor {

    [InitializeOnLoad]
    public partial class Patches {
        private const string PackageJsonGuid = "c41bc17027c993147aa4f25b8cdf3c45";
        private static string version;
        public static string Version {
            get {
                if(String.IsNullOrEmpty(version)) {
                    string assetPath = AssetDatabase.GUIDToAssetPath(PackageJsonGuid);
                    if(String.IsNullOrEmpty(assetPath))
                        version = "0.0.0";
                    else
                        version = JsonUtility.FromJson<PackageManifestData>(File.ReadAllText(Path.GetFullPath(assetPath))).version;
                }

                return version;
            }
        }
        public class PackageManifestData {
            public string version;
        }
        
        private static int wait = 0;
        
        private static readonly Harmony HarmonyInstance = new Harmony("Tayou.VRChat.SDKUITweaks");

        public static VRCSDKUIPatchesPreferences Prefs = new VRCSDKUIPatchesPreferences();

        static Patches() {
            // Register our patch delegate
            EditorApplication.update -= DoPatches;
            EditorApplication.update += DoPatches;

            ExpressionParametersLists = new Dictionary<VRCExpressionParametersEditor, ExpressionParametersListData>();
            ParameterDriverLists = new Dictionary<AvatarParameterDriverEditor, ParameterDriverData>();
            ExpressionMenuLists = new Dictionary<VRCExpressionsMenuEditor, ExpressionMenuData>();
        }
        
        static void DoPatches() {
            // Wait a couple cycles to patch to let static initializers run
            wait++;
            if(wait > 2) {
                // Unregister our delegate so it doesn't run again
                EditorApplication.update -= DoPatches;

                ApplyPatches();
            }
        }
        private static List<IPatch> patches;

        internal static void ApplyPatches()
        {
            if (patches == null) {
                patches = new List<IPatch>();
                var col = TypeCache.GetTypesDerivedFrom(typeof(IPatch));
                foreach (var t in col) {
                    if (t.IsAbstract) continue;
                    var inst = Activator.CreateInstance(t) as IPatch;
                    patches.Add(inst);
                }
				
            }

            foreach (var p in patches) {
                try {
                    p.Apply(HarmonyInstance);
                } catch (Exception e) {
                    DebugLog($"Harmony Patching for Patch \"{p.GetType()}\" Failed with exception, unpatching!\n{e.Message}", DebugLogSeverity.Error);
                    p.Remove(HarmonyInstance);
                    return;
                }
            }
            DebugLog("Patches Applied!");
        }

        internal static void RemovePatches()
        {
            if (HarmonyInstance == null) return;
			
            foreach (var p in patches)
                p.Remove(HarmonyInstance);
        }

        public static void DebugLog(string message, DebugLogSeverity severity = DebugLogSeverity.Message) {
            switch (severity) {
                case DebugLogSeverity.Message:
                    Debug.Log($"[VSUT v{Version}] {message}");
                    break;
                case DebugLogSeverity.Warning:
                    Debug.LogWarning($"[VSUT v{Version}] {message}");
                    break;
                case DebugLogSeverity.Error:
                    Debug.LogError($"[VSUT v{Version}] {message}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }

        public enum DebugLogSeverity {
            Message = 0,
            Warning = 1,
            Error = 2
        }

        public static Dictionary<VRCExpressionParametersEditor, ExpressionParametersListData> ExpressionParametersLists;
        public static Dictionary<AvatarParameterDriverEditor, ParameterDriverData> ParameterDriverLists;
        public static Dictionary<VRCExpressionsMenuEditor, ExpressionMenuData> ExpressionMenuLists;

        private static object InvokeMethod(object targetObject, string methodName, params object[] parameters) {
            return targetObject.GetType()
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(targetObject, parameters);
        }
    }

    public class VRCSDKUIPatchesPreferences {
        // Stub -- I will add settings here eventually... maybe... like selectively enabling and disabling patches. We'll see..
    }
}