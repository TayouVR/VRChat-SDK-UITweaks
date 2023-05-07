using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using AnimatorControllerParameter = UnityEngine.AnimatorControllerParameter;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using ExpressionParameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;
using HarmonyLib;

namespace Tayou.VRChat.SDKUITweaks.Editor {

    public partial class VRCSDKUIPatches {

        // VRC Expression Parameters Editor
        [HarmonyPatch]
        [HarmonyPriority(Priority.Low)]
        private class PatchExpressionParametersList {
            
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod() => AccessTools.Method(typeof(VRCExpressionParametersEditor), nameof(VRCExpressionParametersEditor.OnInspectorGUI));

            [HarmonyPrefix]
            // ReSharper disable once InconsistentNaming
            public static bool Prefix(VRCExpressionParametersEditor __instance) {
                if (__instance == null) return true;

                if (!ExpressionParametersLists.TryGetValue(__instance, out var data)) {
                    ExpressionParametersLists[__instance] = data = new ExpressionParametersListData();
                }
                
                __instance.serializedObject.Update();
                {
                    __instance.serializedObject.Update();
                    if (data.List == null || __instance.serializedObject != data.List?.serializedProperty?.serializedObject) {
                        data.InitilizeList(__instance.serializedObject);
                    } else {
                        data.List.DoLayoutList();
                    }
                    __instance.serializedObject.ApplyModifiedProperties();

                    //Cost
                    int cost = ((ExpressionParameters) __instance.target).CalcTotalCost();
                    if (cost <= ExpressionParameters.MAX_PARAMETER_COST)
                        EditorGUILayout.HelpBox($"Total Memory: {cost}/{ExpressionParameters.MAX_PARAMETER_COST}", MessageType.Info);
                    else
                        EditorGUILayout.HelpBox($"Total Memory: {cost}/{ExpressionParameters.MAX_PARAMETER_COST}\nParameters use too much memory.  Remove parameters or use bools which use less memory.", MessageType.Error);

                    //Info
                    EditorGUILayout.HelpBox("Only parameters defined here can be used by expression menus, sync between all playable layers and sync across the network to remote clients.", MessageType.Info);
                    EditorGUILayout.HelpBox("The parameter name and type should match a parameter defined on one or more of your animation controllers.", MessageType.Info);
                    EditorGUILayout.HelpBox("Parameters used by the default animation controllers (Optional)\nVRCEmote, Int\nVRCFaceBlendH, Float\nVRCFaceBlendV, Float", MessageType.Info);

                    //Clear
                    if (GUILayout.Button("Clear Parameters")) {
                        if (EditorUtility.DisplayDialogComplex("Warning", "Are you sure you want to clear all expression parameters?", "Clear", "Cancel", "") == 0) {
                            InvokeMethod(__instance, "InitExpressionParameters", false);
                        }
                    }
                    if (GUILayout.Button("Default Parameters")) {
                        if (EditorUtility.DisplayDialogComplex("Warning", "Are you sure you want to reset all expression parameters to default?", "Reset", "Cancel", "") == 0) {
                            InvokeMethod(__instance, "InitExpressionParameters", true);
                        }
                    }
                    // TODO add transfer to controller thing
                    //var expressionParameters = target as ExpressionParameters;
                }
                __instance.serializedObject.ApplyModifiedProperties();
                return false;
            }
            
            // add transfer to controller option
            [HarmonyPostfix]
            // ReSharper disable once InconsistentNaming
            public static void Postfix(VRCExpressionParametersEditor __instance) {
                if (!ExpressionParametersLists.TryGetValue(__instance, out var data)) {
                    ExpressionParametersLists[__instance] = data = new ExpressionParametersListData();
                }
                
                data.ControllerToTransfer = (AnimatorController)EditorGUILayout.ObjectField("Controller to transfer to", data.ControllerToTransfer, typeof(AnimatorController), true);
                GUI.enabled = (object)data.ControllerToTransfer != null; 
                if (GUILayout.Button("Transfer Parameters to Animator")) { 
                    if (EditorUtility.DisplayDialogComplex("Warning", $"Are you sure you want to add Parameters '{data.FindAnimatorParameterMatch(__instance.target as ExpressionParameters)}' to the Animator \"{data.ControllerToTransfer.name}\"?", "Transfer", "Cancel", "") == 0) { 
                        data.TransferToAnimatorController(__instance.target as ExpressionParameters); 
                    } 
                } 
            }
        }

        /// <summary>
        /// Remove data from dictionary when the VRCExpressionParametersEditor object gets destroyed 
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VRCExpressionParametersEditor), nameof(VRCExpressionParametersEditor.Destroy))]
        // ReSharper disable once InconsistentNaming
        private static void ExpressionParametersListDestroy(VRCExpressionParametersEditor __instance) {
            ExpressionParametersLists.Remove(__instance);
        }

        private static void InvokeMethod(object targetObject, string methodName, params object[] parameters) {
            targetObject.GetType()
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(targetObject, parameters);
        }

        public class ExpressionParametersListData {
            public AnimatorController ControllerToTransfer;
            public ReorderableList List;
            
            private const int ParamCountLabelWidth = 20;
            private const int TypeWidth = 60;
            private const int DefaultWidth = 50;
            private const int SavedWidth = 50;
            private const int SyncedWidth = 45;

            public enum Column {
                OverallParamCount,
                Type,
                ParameterName,
                Default,
                Saved,
                Synced
            }
            
            private static Rect GetColumnSection(Column column, Rect rect, bool isHeader = false, bool isToggle = false) {
                int paramCountLabelWidth = isHeader ? ParamCountLabelWidth : 0;

                Rect outRect = new Rect(rect);
                outRect.height = EditorGUIUtility.singleLineHeight;

                switch (column) {
                    case Column.OverallParamCount:
                        outRect.width = paramCountLabelWidth;
                        outRect.x = rect.x;
                        break;
                    case Column.Type:
                        outRect.width = TypeWidth;
                        outRect.x = rect.x + paramCountLabelWidth;
                        break;
                    case Column.ParameterName:
                        outRect.width = rect.width - (DefaultWidth + SavedWidth + SyncedWidth + TypeWidth);
                        outRect.x = rect.x + paramCountLabelWidth + TypeWidth;
                        break;
                    case Column.Default:
                        outRect.width = DefaultWidth;
                        outRect.x = rect.x + rect.width - (DefaultWidth + SyncedWidth + SavedWidth) + (isToggle ? outRect.width / 2 - 4 : 0);
                        if (isToggle) outRect.width = EditorGUIUtility.singleLineHeight;
                        break;
                    case Column.Saved:
                        outRect.width = SavedWidth;
                        outRect.x = rect.x + rect.width - (SyncedWidth + SavedWidth) + (isToggle ? outRect.width / 2 - 4 : 0);
                        if (isToggle) outRect.width = EditorGUIUtility.singleLineHeight;
                        break;
                    case Column.Synced:
                        outRect.width = SyncedWidth;
                        outRect.x = rect.x + rect.width - SyncedWidth + (isToggle ? outRect.width / 2 - 4 : 0);
                        if (isToggle) outRect.width = EditorGUIUtility.singleLineHeight;
                        break;
                    default:
                        return rect;
                }

                if (!isHeader) { // add some spacing between elements to make it look better
                    outRect.x += 1;
                    outRect.width -= 2;
                }
                
                return outRect;
            }
            
            public void InitilizeList(SerializedObject serializedObject) {
                // initialize ReorderableList
                List = new ReorderableList(serializedObject, serializedObject.FindProperty("parameters"),
                    true, true, true, true) {
                    drawElementCallback = OnDrawElement,
                    drawHeaderCallback = OnDrawHeader
                };
            }

            private void OnDrawHeader(Rect rect) {
                // the default size of the rect is bs, need to shift it to the left and make it wider to fit the entire space
                rect.x -= 5;
                rect.width += 5;

                var centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label")) {
                    alignment = TextAnchor.UpperCenter
                };

                EditorGUI.LabelField(GetColumnSection(Column.OverallParamCount, rect, true), $"{List.count}");
                EditorGUI.LabelField(GetColumnSection(Column.Type, rect, true), "Type", centeredStyle);
                EditorGUI.LabelField(GetColumnSection(Column.ParameterName, rect, true), "Name", centeredStyle);
                EditorGUI.LabelField(GetColumnSection(Column.Default, rect, true), "Default", centeredStyle);
                EditorGUI.LabelField(GetColumnSection(Column.Saved, rect, true), "Saved", centeredStyle);
                EditorGUI.LabelField(GetColumnSection(Column.Synced, rect, true), "Synced", centeredStyle);
            }
            
            private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused) {
                var element = List.serializedProperty.GetArrayElementAtIndex(index);

                EditorGUI.PropertyField(GetColumnSection(Column.Type, rect), element.FindPropertyRelative("valueType"), GUIContent.none);

                EditorGUI.PropertyField(GetColumnSection(Column.ParameterName, rect), element.FindPropertyRelative("name"), GUIContent.none);

                SerializedProperty defaultValue = element.FindPropertyRelative("defaultValue");
                var type = (ExpressionParameters.ValueType)element.FindPropertyRelative("valueType").intValue;
                switch (type) {
                    case ExpressionParameters.ValueType.Int:
                        defaultValue.floatValue = Mathf.Clamp(EditorGUI.IntField(GetColumnSection(Column.Default, rect), (int)defaultValue.floatValue), 0, 255);
                        break;
                    case ExpressionParameters.ValueType.Float:
                        defaultValue.floatValue = Mathf.Clamp(EditorGUI.FloatField(GetColumnSection(Column.Default, rect), defaultValue.floatValue), -1f, 1f);
                        break;
                    case ExpressionParameters.ValueType.Bool:
                        defaultValue.floatValue = EditorGUI.Toggle(GetColumnSection(Column.Default, rect, false, true), defaultValue.floatValue != 0) ? 1f : 0f;
                        break;
                }
                EditorGUI.PropertyField(GetColumnSection(Column.Saved, rect, false, true), element.FindPropertyRelative("saved"), GUIContent.none);

                EditorGUI.PropertyField(GetColumnSection(Column.Synced, rect, false, true), element.FindPropertyRelative("networkSynced"), GUIContent.none);
            }

            public string FindAnimatorParameterMatch(ExpressionParameters expressionParameters) {
                List<string> matchingParameters = new List<string>();

                foreach (var parameter in expressionParameters.parameters) {
                    bool parameterExists = ControllerToTransfer.parameters.Any(controllerParameter => controllerParameter.name == parameter.name);
                    //AnimatorControllerParameter foundControllerParameter = null;

                    if (!parameterExists) {
                        matchingParameters.Add(parameter.name);
                    }
                }

                return string.Join(", ", matchingParameters);
            }

            public void TransferToAnimatorController(ExpressionParameters expressionParameters) {
                List<AnimatorControllerParameter> controllerParamsList = new List<AnimatorControllerParameter>(ControllerToTransfer.parameters);

                foreach (var parameter in expressionParameters.parameters) {
                    bool parameterExists = false;
                    //AnimatorControllerParameter foundControllerParameter = null;
                    foreach (var controllerParameter in ControllerToTransfer.parameters) {
                        if (controllerParameter.name == parameter.name) {
                            parameterExists = true;
                            //foundControllerParameter = controllerParameter;
                            break;
                        }
                    }

                    if (!parameterExists) {
                        controllerParamsList.Add(new AnimatorControllerParameter() {
                            name = parameter.name,
                            defaultBool = parameter.defaultValue > 0.5,
                            defaultFloat = parameter.defaultValue,
                            defaultInt = (int)Math.Floor(parameter.defaultValue),
                            type = VRCType2UnityType(parameter.valueType)
                        });
                    }
                }

                ControllerToTransfer.parameters = controllerParamsList.ToArray();
            }
        }
        
        private static AnimatorControllerParameterType VRCType2UnityType(ExpressionParameters.ValueType type) {
            switch (type) {
                case ExpressionParameters.ValueType.Int:
                    return AnimatorControllerParameterType.Int;
                case ExpressionParameters.ValueType.Float:
                    return AnimatorControllerParameterType.Float;
                case ExpressionParameters.ValueType.Bool:
                    return AnimatorControllerParameterType.Bool;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

    }
}