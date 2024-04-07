using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionControl = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditorInternal;

namespace Tayou.VRChat.SDKUITweaks.Editor {

    public partial class Patches {

        // VRC Expression Parameters Editor
        [HarmonyPriority(Priority.Low)]
        private class PatchExpressionMenu : PatchBase {
            
	        public override string GetPatchDisplayName() {
		        return "Expression Menu ReorderableList";
	        }
            
            protected override IEnumerable<MethodBase> GetPatches() {
	            yield return AccessTools.Method(typeof(VRCExpressionsMenuEditor), nameof(VRCExpressionsMenuEditor.OnInspectorGUI));
            }

            [HarmonyPrefix]
            // ReSharper disable once InconsistentNaming
            public static bool Prefix(VRCExpressionsMenuEditor __instance) {
                if (__instance == null) return true;

                if (!ExpressionMenuLists.TryGetValue(__instance, out var data)) {
                    ExpressionMenuLists[__instance] = data = new ExpressionMenuData(__instance.serializedObject, (VRCExpressionsMenu) __instance.target);
                }
                
                data.OnInspectorGUI();
                
                return false;
            }
        }

        /// <summary>
        /// Remove data from dictionary when the VRCExpressionParametersEditor object gets destroyed 
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VRCExpressionsMenuEditor), nameof(VRCExpressionsMenuEditor.Destroy))]
        // ReSharper disable once InconsistentNaming
        private static void ExpressionMenuDestroy(VRCExpressionsMenuEditor __instance) {
            ExpressionMenuLists.Remove(__instance);
        }

        public class ExpressionMenuData {
	        static string[] ToggleStyles = { "Pip-Slot", "Animation" };

			List<UnityEngine.Object> foldoutList = new List<UnityEngine.Object>();

			private ReorderableList list;
			
			public ReorderableList List {
				get {
					if (list == null || serializedObject != list?.serializedProperty?.serializedObject) {
						list = new ReorderableList(serializedObject, controls);
						list.drawElementCallback += DrawElementCallback;
						list.drawHeaderCallback = DrawHeaderCallback;
						list.onAddCallback = ONAddCallback;
						list.onRemoveCallback = ONRemoveCallback;
						list.elementHeightCallback = ElementHeightCallback;
					}
					
					list.displayAdd = controls.arraySize < ExpressionsMenu.MAX_CONTROLS;
					return list;
				}
			}

			private SerializedProperty controls;
			private SerializedObject serializedObject;
			private VRCExpressionsMenu target;

			public ExpressionMenuData(SerializedObject serializedObject, VRCExpressionsMenu target) {
				this.serializedObject = serializedObject;
				this.target = target;
				controls = serializedObject.FindProperty("controls");
			}

			private float ElementHeightCallback(int index) {
				var entity = controls.GetArrayElementAtIndex(index);
				
				
				var name = entity.FindPropertyRelative("name");
				var icon = entity.FindPropertyRelative("icon");
				var type = entity.FindPropertyRelative("type");
				var parameter = entity.FindPropertyRelative("parameter");
				var value = entity.FindPropertyRelative("value");
				var subMenu = entity.FindPropertyRelative("subMenu");

				var subParameters = entity.FindPropertyRelative("subParameters");
				var labels = entity.FindPropertyRelative("labels");

				float height = EditorGUIUtility.singleLineHeight * 1.25f; // Foldout & Name
					
				if (entity.isExpanded) {
					height += EditorGUIUtility.singleLineHeight * 1.25f; // Icon
					height += EditorGUIUtility.singleLineHeight * 1.25f; // Type
					height += EditorGUIUtility.singleLineHeight * 3f; // Type Help box
					height += EditorGUIUtility.singleLineHeight * 1.25f; // Parameter

					string paramName = parameter.FindPropertyRelative("name").stringValue;
					if (!string.IsNullOrEmpty(paramName)) {
						var paramDef = FindExpressionParameterDef(paramName);
						if (paramDef != null) {
							if (paramDef.valueType == ExpressionParameters.ValueType.Int) {
								height += EditorGUIUtility.singleLineHeight * 1.25f; // Value
							} else if (paramDef.valueType == ExpressionParameters.ValueType.Float) {
								height += EditorGUIUtility.singleLineHeight * 1.25f; // Value
							}
						} else {
							height += EditorGUIUtility.singleLineHeight * 1.25f; // Value
							height += EditorGUIUtility.singleLineHeight * 2.5f; // Warning Box
						}
					}

					var controlType = (ExpressionControl.ControlType)type.intValue;
					switch (controlType) {
						case VRCExpressionsMenu.Control.ControlType.Button:
							break;
						case VRCExpressionsMenu.Control.ControlType.Toggle:
							break;
						case VRCExpressionsMenu.Control.ControlType.SubMenu:
							height += EditorGUIUtility.singleLineHeight * 1.25f; // Seperator Slider

							height += EditorGUIUtility.singleLineHeight * 1.25f; // Sub Menu Object Field
							break;
						case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet:
							height += EditorGUIUtility.singleLineHeight * 1.25f; // Seperator Slider

							height += EditorGUIUtility.singleLineHeight * (1.25f) * 2; // Parameters
							height += EditorGUIUtility.singleLineHeight * (1.25f * 3) * 4; // Labels
							break;
						case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet:
							height += EditorGUIUtility.singleLineHeight * 1.25f; // Seperator Slider

							height += EditorGUIUtility.singleLineHeight * (1.25f) * 4; // Parameters
							height += EditorGUIUtility.singleLineHeight * (1.25f * 3) * 4; // Labels
							break;
						case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
							height += EditorGUIUtility.singleLineHeight * 1.25f; // Seperator Slider

							height += EditorGUIUtility.singleLineHeight * (1.25f); // Parameters
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				return height;
			}

			private void ONRemoveCallback(ReorderableList reorderableList) {
				controls.DeleteArrayElementAtIndex(reorderableList.index);
				if (controls.arraySize < ExpressionsMenu.MAX_CONTROLS && controls.arraySize > 0) {
					List.displayAdd = true;
				}
			}

			private void ONAddCallback(ReorderableList reorderableList) {
				var menu = serializedObject.targetObject as ExpressionsMenu;

				var control = new ExpressionControl();
				control.name = "New Control";
				menu.controls.Add(control);
				if (controls.arraySize >= ExpressionsMenu.MAX_CONTROLS -1) {
					List.displayAdd = false;
				}
			}

			private void DrawHeaderCallback(Rect rect) {
				EditorGUI.LabelField(rect, $"Controls ({controls.arraySize})");
			}

			private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused) {
				var control = controls.GetArrayElementAtIndex(index);
				DrawControl(rect, controls, control as SerializedProperty, index);
			}

			public void OnDisable()
			{
				SelectAvatarDescriptor(null);
			}
			public void OnInspectorGUI()
			{
				serializedObject.Update();

				SelectAvatarDescriptor();

				if(activeDescriptor == null)
				{
					EditorGUILayout.HelpBox("No active avatar descriptor found in scene.", MessageType.Error);
				}
				EditorGUILayout.Space();

				//Controls
				EditorGUI.BeginDisabledGroup(activeDescriptor == null);
				List.DoLayoutList();
				EditorGUI.EndDisabledGroup();

				serializedObject.ApplyModifiedProperties();
			}
			void DrawControl(Rect rect, SerializedProperty control, SerializedProperty entity, int index)
			{
				var name = entity.FindPropertyRelative("name");
				var icon = entity.FindPropertyRelative("icon");
				var type = entity.FindPropertyRelative("type");
				var parameter = entity.FindPropertyRelative("parameter");
				var value = entity.FindPropertyRelative("value");
				var subMenu = entity.FindPropertyRelative("subMenu");

				var subParameters = entity.FindPropertyRelative("subParameters");
				var labels = entity.FindPropertyRelative("labels");

				//Foldout
				EditorGUI.BeginChangeCheck();
					
				rect.y += 2;
				Rect _rect = new Rect(rect.x + 10, rect.y, rect.width/2 - 30, EditorGUIUtility.singleLineHeight);

				entity.isExpanded = EditorGUI.Foldout(_rect, entity.isExpanded, "", true);

				_rect = new Rect(rect.x + 10, rect.y, rect.width - 10, EditorGUIUtility.singleLineHeight);
				EditorGUI.PropertyField(_rect, name);
				rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

				if (!entity.isExpanded)
					return;

				{

					//Generic params
					{

						_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
						EditorGUI.PropertyField(_rect, icon);
						rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

						_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
						EditorGUI.PropertyField(_rect, type);
						rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

						_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight*2.5f);
						//Type Info
						var controlType = (ExpressionControl.ControlType)type.intValue;
						switch (controlType)
						{
							case VRCExpressionsMenu.Control.ControlType.Button:
								EditorGUI.HelpBox(_rect, "Click or hold to activate. The button remains active for a minimum 0.2s.\nWhile active the (Parameter) is set to (Value).\nWhen inactive the (Parameter) is reset to zero.", MessageType.Info);
								break;
							case VRCExpressionsMenu.Control.ControlType.Toggle:
								EditorGUI.HelpBox(_rect, "Click to toggle on or off.\nWhen turned on the (Parameter) is set to (Value).\nWhen turned off the (Parameter) is reset to zero.", MessageType.Info);
								break;
							case VRCExpressionsMenu.Control.ControlType.SubMenu:
								EditorGUI.HelpBox(_rect, "Opens another expression menu.\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.", MessageType.Info);
								break;
							case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet:
								EditorGUI.HelpBox(_rect, "Puppet menu that maps the joystick to two parameters (-1 to +1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.", MessageType.Info);
								break;
							case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet:
								EditorGUI.HelpBox(_rect, "Puppet menu that maps the joystick to four parameters (0 to 1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.", MessageType.Info);
								break;
							case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
								EditorGUI.HelpBox(_rect, "Puppet menu that sets a value based on joystick rotation. (0 to 1)\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.", MessageType.Info);
								break;
						}
						rect.y += EditorGUIUtility.singleLineHeight * 2.75f;

						_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
						DrawParameterDropDown(_rect, parameter, "Parameter");
						rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

						{
							float rect_y = rect.y;
							_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
							DrawParameterValue(_rect, parameter, value, ref rect_y);
							rect.y = rect_y;
						}

						//Style
						/*if (controlType == ExpressionsControl.ControlType.Toggle)
						{
							style.intValue = EditorGUILayout.Popup("Visual Style", style.intValue, ToggleStyles);
						}*/

						//Puppet Parameter Set
						switch (controlType)
						{
							case ExpressionControl.ControlType.TwoAxisPuppet:
								// Separator Slider
								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								EditorGUI.LabelField(_rect, "", GUI.skin.horizontalSlider);
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

								subParameters.arraySize = 2;
								labels.arraySize = 4;


								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawParameterDropDown(_rect, subParameters.GetArrayElementAtIndex(0), "Parameter Horizontal", false);
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawParameterDropDown(_rect, subParameters.GetArrayElementAtIndex(1), "Parameter Vertical", false);
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawLabel(_rect, labels.GetArrayElementAtIndex(0), "Label Up");
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f * 3;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawLabel(_rect, labels.GetArrayElementAtIndex(1), "Label Right");
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f * 3;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawLabel(_rect, labels.GetArrayElementAtIndex(2), "Label Down");
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f * 3;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawLabel(_rect, labels.GetArrayElementAtIndex(3), "Label Left");
								break;
							case ExpressionControl.ControlType.FourAxisPuppet:
								// Separator Slider
								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								EditorGUI.LabelField(_rect, "", GUI.skin.horizontalSlider);
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

								subParameters.arraySize = 4;
								labels.arraySize = 4;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawParameterDropDown(_rect, subParameters.GetArrayElementAtIndex(0), "Parameter Up", false);
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawParameterDropDown(_rect, subParameters.GetArrayElementAtIndex(1), "Parameter Right", false);

								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;
								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawParameterDropDown(_rect, subParameters.GetArrayElementAtIndex(2), "Parameter Down", false);

								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;
								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawParameterDropDown(_rect, subParameters.GetArrayElementAtIndex(3), "Parameter Left", false);
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;


								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawLabel(_rect, labels.GetArrayElementAtIndex(0), "Label Up");
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f * 3;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawLabel(_rect, labels.GetArrayElementAtIndex(1), "Label Right");
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f * 3;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawLabel(_rect, labels.GetArrayElementAtIndex(2), "Label Down");
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f * 3;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawLabel(_rect, labels.GetArrayElementAtIndex(3), "Label Left");
								break;
							case ExpressionControl.ControlType.RadialPuppet:
								// Separator Slider
								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								EditorGUI.LabelField(_rect, "", GUI.skin.horizontalSlider);
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

								subParameters.arraySize = 1;
								labels.arraySize = 0;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								DrawParameterDropDown(_rect, subParameters.GetArrayElementAtIndex(0), "Paramater Rotation", false);
								break;
							case VRCExpressionsMenu.Control.ControlType.SubMenu:
								// Separator Slider
								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								EditorGUI.LabelField(_rect, "", GUI.skin.horizontalSlider);
								rect.y += EditorGUIUtility.singleLineHeight * 1.25f;

								_rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
								EditorGUI.PropertyField(_rect, subMenu);
								break;
							default:
								subParameters.arraySize = 0;
								labels.arraySize = 0;
								break;
						}
					}
				}
			}
			void DrawLabel(Rect rect, SerializedProperty subControl, string name)
			{
				var nameProp = subControl.FindPropertyRelative("name");
				var icon = subControl.FindPropertyRelative("icon");

				EditorGUI.LabelField(rect, name);
				EditorGUI.indentLevel += 2;
				rect.y += EditorGUIUtility.singleLineHeight * 1.25f;
				EditorGUI.PropertyField(new Rect(rect), nameProp); 
				rect.y += EditorGUIUtility.singleLineHeight * 1.25f;
				EditorGUI.PropertyField(new Rect(rect), icon);
				EditorGUI.indentLevel -= 2;
			}

			VRC.SDK3.Avatars.Components.VRCAvatarDescriptor activeDescriptor = null;
			string[] parameterNames;
			void SelectAvatarDescriptor()
			{
				var descriptors = GameObject.FindObjectsOfType<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
				if (descriptors.Length > 0)
				{
					//Compile List of names
					string[] names = new string[descriptors.Length];
					for(int i=0; i<descriptors.Length; i++)
						names[i] = descriptors[i].gameObject.name;

					//Select
					var currentIndex = System.Array.IndexOf(descriptors, activeDescriptor);
					var nextIndex = EditorGUILayout.Popup("Active Avatar", currentIndex, names);
					if(nextIndex < 0)
						nextIndex = 0;
					if (nextIndex != currentIndex)
						SelectAvatarDescriptor(descriptors[nextIndex]);
				}
				else
					SelectAvatarDescriptor(null);
			}
			void SelectAvatarDescriptor(VRC.SDK3.Avatars.Components.VRCAvatarDescriptor desc)
			{
				if (desc == activeDescriptor)
					return;

				activeDescriptor = desc;
				if(activeDescriptor != null)
				{
					//Init stage parameters
					int paramCount = desc.GetExpressionParameterCount();
					parameterNames = new string[paramCount + 1];
					parameterNames[0] = "[None]";
					for (int i = 0; i < paramCount; i++)
					{
						var param = desc.GetExpressionParameter(i);
						string name = "[None]";
						if (param != null && !string.IsNullOrEmpty(param.name))
							name = string.Format("{0}, {1}", param.name, param.valueType.ToString(), i + 1);
						parameterNames[i + 1] = name;
					}
				}
				else
				{
					parameterNames = null;
				}
			}
			int GetExpressionParametersCount()
			{
				if (activeDescriptor != null && activeDescriptor.expressionParameters != null && activeDescriptor.expressionParameters.parameters != null)
					return activeDescriptor.expressionParameters.parameters.Length;
				return 0;
			}
			ExpressionParameters.Parameter GetExpressionParameter(int i)
			{
				if (activeDescriptor != null)
					return activeDescriptor.GetExpressionParameter(i);
				return null;
			}
			void DrawParameterDropDown(Rect rect, SerializedProperty parameter, string name, bool allowBool=true)
			{
				var parameterName = parameter.FindPropertyRelative("name");
				VRCExpressionParameters.Parameter param = null;
				string value = parameterName.stringValue;

				bool parameterFound = false;
				{
					if(activeDescriptor != null)
					{
						//Dropdown
						int currentIndex;
						if (string.IsNullOrEmpty(value))
						{
							currentIndex = -1;
							parameterFound = true;
						}
						else
						{
							currentIndex = -2;
							for (int i = 0; i < GetExpressionParametersCount(); i++)
							{
								var item = activeDescriptor.GetExpressionParameter(i);
								if (item.name == value)
								{
									param = item;
									parameterFound = true;
									currentIndex = i;
									break;
								}
							}
						}

						//Dropdown
						EditorGUI.BeginChangeCheck();
						currentIndex = EditorGUI.Popup(new Rect(rect.x, rect.y, rect.width - rect.width * 0.25f, rect.height), name, currentIndex + 1, parameterNames);
						if (EditorGUI.EndChangeCheck())
						{
							if (currentIndex == 0)
								parameterName.stringValue = "";
							else
								parameterName.stringValue = GetExpressionParameter(currentIndex - 1).name;
						}
					}
					else
					{
						EditorGUI.BeginDisabledGroup(true);
						EditorGUI.Popup(new Rect(rect.x, rect.y, rect.width - rect.width * 0.25f, rect.height), 0, new string[0]);
						EditorGUI.EndDisabledGroup();
					}

					//Text field
					parameterName.stringValue = EditorGUI.TextField(new Rect(rect.width - rect.width * 0.25f + 40, rect.y, rect.width * 0.25f, rect.height), parameterName.stringValue);
				}

				if (!parameterFound)
				{
					EditorGUI.HelpBox(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight * 1.25f * 2), rect.width, EditorGUIUtility.singleLineHeight * 2), "Parameter not found on the active avatar descriptor.", MessageType.Warning);
				}

				if(!allowBool && param != null && param.valueType == ExpressionParameters.ValueType.Bool)
				{
					EditorGUI.HelpBox(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight * 1.25f * 2), rect.width, EditorGUIUtility.singleLineHeight * 2), "Bool parameters not valid for this choice.", MessageType.Error);
				}
			}
			void DrawParameterValue(Rect rect, SerializedProperty parameter, SerializedProperty value, ref float rect_y)
			{
				string paramName = parameter.FindPropertyRelative("name").stringValue;
				if (!string.IsNullOrEmpty(paramName))
				{ 
					var paramDef = FindExpressionParameterDef(paramName);
					if (paramDef != null)
					{
						if (paramDef.valueType == ExpressionParameters.ValueType.Int)
						{
							value.floatValue = EditorGUI.IntField(rect, "Value", Mathf.Clamp((int)value.floatValue, 0, 255));
							rect_y += EditorGUIUtility.singleLineHeight * 1.25f;
						}
						else if (paramDef.valueType == ExpressionParameters.ValueType.Float)
						{
							value.floatValue = EditorGUI.FloatField(rect, "Value", Mathf.Clamp(value.floatValue, -1f, 1f));
							rect_y += EditorGUIUtility.singleLineHeight * 1.25f;
						}
						else if(paramDef.valueType == ExpressionParameters.ValueType.Bool)
						{
							value.floatValue = 1f;
						}
					}
					else
					{
						EditorGUI.BeginDisabledGroup(true);
						value.floatValue = EditorGUI.FloatField(rect, "Value", value.floatValue);
						EditorGUI.EndDisabledGroup();
					}
				}
			}

			ExpressionParameters.Parameter FindExpressionParameterDef(string name)
			{
				if (activeDescriptor == null || string.IsNullOrEmpty(name))
					return null;

				//Find
				int length = GetExpressionParametersCount();
				for(int i=0; i<length; i++)
				{
					var item = GetExpressionParameter(i);
					if (item != null && item.name == name)
						return item;
				}
				return null;
			}
        }
    }
}