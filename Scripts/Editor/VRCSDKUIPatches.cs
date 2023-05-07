using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;
using VRC.SDKBase;
using VRC.SDK3.Avatars.ScriptableObjects;
using HarmonyLib;
using UnityEditor;

namespace Tayou.VRChat.SDKUITweaks.Editor {

    [InitializeOnLoad]
    public partial class VRCSDKUIPatches {
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
        
        private static readonly HarmonyLib.Harmony HarmonyInstance = new HarmonyLib.Harmony("Tayou.VRChat.SDKUITweaks");

        public static VRCSDKUIPatchesPreferences Prefs = new VRCSDKUIPatchesPreferences();

        static VRCSDKUIPatches() {
            // Register our patch delegate
            EditorApplication.update -= DoPatches;
            EditorApplication.update += DoPatches;

            ExpressionParametersLists = new Dictionary<VRCExpressionParametersEditor, ExpressionParametersListData>();
        }
        
        static void DoPatches() {
            // Wait a couple cycles to patch to let static initializers run
            wait++;
            if(wait > 2) {
                // Unregister our delegate so it doesn't run again
                EditorApplication.update -= DoPatches;

                try {
                    HarmonyInstance.PatchAll();
                    DebugLog("Patches Applied!");
                } catch (Exception e) {
                    DebugLog("Harmony Patching Failed with exception, unpatching!\n" + e.Message, DebugLogSeverity.Error);
                    HarmonyInstance.UnpatchAll();
                }
            }
        }

        public static void DebugLog(string message, DebugLogSeverity severity = DebugLogSeverity.Message) {
            switch (severity) {
                case DebugLogSeverity.Message:
                    Debug.Log($"[VRCSDKUIPatches v{Version}] " + message);
                    break;
                case DebugLogSeverity.Warning:
                    Debug.LogWarning($"[VRCSDKUIPatches v{Version}] " + message);
                    break;
                case DebugLogSeverity.Error:
                    Debug.LogError($"[VRCSDKUIPatches v{Version}] " + message);
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
    }

    public class VRCSDKUIPatchesPreferences {
    }
}