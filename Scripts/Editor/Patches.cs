using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static VSUTPreferences Prefs = new();

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
                foreach (var type in col) {
                    if (type.IsAbstract) continue;
                    var inst = Activator.CreateInstance(type) as IPatch;
                    patches.Add(inst);
                    if (!Prefs.PatchProperties.Any(pp => pp.typeName == type.ToString())) {
                        Prefs.PatchProperties.Add(new PatchProperties {
                            typeName = type.ToString(), 
                            displayName = inst.GetPatchDisplayName(),
                            isEnabled = true
                        });
                    }
                }
            }

            foreach (var p in patches) {
                if (Prefs.PatchProperties.All(pp => pp.typeName != p.GetType().ToString())) {
                    return;
                }
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

    [Serializable]
    public class PatchProperties {
        public string typeName;
        public string displayName;
        public bool isEnabled;
    }

    [Serializable]
    public class VSUTPreferences {
        private const string SettingsKey = "VSUT-settings";
        [SerializeField] public List<PatchProperties> PatchProperties;

        public VSUTPreferences() {
            var settingsJsonObject = (VSUTPreferences)JsonUtility.FromJson(EditorPrefs.GetString(SettingsKey), typeof(VSUTPreferences));
            PatchProperties = settingsJsonObject?.PatchProperties == null ? new() : settingsJsonObject.PatchProperties;
        }

        public void Save() {
            EditorPrefs.SetString(SettingsKey, JsonUtility.ToJson(this));
        }

        public bool IsPatchEnabled(string patchName) {
            return PatchProperties.First(patch => patch.typeName == patchName).isEnabled;
        }
        public void SetPatchEnabled(string patchName, bool isEnabled) {
            var patchProperty = PatchProperties.First(patch => patch.typeName == patchName);
            if (patchProperty != null) {
                patchProperty.isEnabled = isEnabled;
            }
        }
    }
}