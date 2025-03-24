#if UNITY_EDITOR
using NWH.NUI;
using NWH.VehiclePhysics2.Modules;
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Used for drawing VehicleComponent properties.
    ///     Adds state header functionality to the NUIPropertyDrawer.
    /// </summary>
    public partial class ComponentNUIPropertyDrawer : NVP_NUIPropertyDrawer
    {
        public static bool DrawStateSettingsBar(VehicleComponent component, Rect position, int lodCount,
            ref bool isEnabled, ref int lodIndex, float topOffset = 4f)
        {
            Color initialColor = GUI.backgroundColor;

            // Button style
            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
            buttonStyle.fixedHeight = 15;
            buttonStyle.fontSize = 8;
            buttonStyle.padding = new RectOffset(0, 0, buttonStyle.padding.top, buttonStyle.padding.bottom);
            buttonStyle.alignment = TextAnchor.MiddleCenter;


            // DRAW isOn BUTTON
            bool guiWasEnabled = GUI.enabled;

            // DRAW LOD BUTTONS
            {
                // Draw LOD menu
                if (lodCount > 0)
                {
                    bool lodActive = lodIndex >= 0;
                    float rightOffset = -53;
                    float lodButtonWidth = 18f;
                    float lodLabelWidth = 35f;
                    float lodWidth = lodCount * lodButtonWidth;

                    /// Draw label
                    Rect lodLabelRect = new Rect(
                        position.x + position.width - lodWidth - lodLabelWidth + rightOffset,
                        position.y + topOffset, lodLabelWidth, 15f);
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = false;

                    GUIStyle lodLabelStyle = new GUIStyle(EditorStyles.miniButtonLeft);
                    lodLabelStyle.fixedHeight = 15;
                    lodLabelStyle.fontSize = 10;

                    GUI.Button(lodLabelRect, "LOD", lodLabelStyle);

                    GUI.enabled = wasEnabled;

                    // Draw lod buttons
                    if (lodIndex >= 0)
                    {
                        GUI.backgroundColor = NUISettings.enabledColor;
                    }

                    GUIStyle lodButtonStyle;
                    GUIStyle middleLODButtonStyle = new GUIStyle(EditorStyles.miniButtonMid);
                    GUIStyle lastLODButtonStyle = new GUIStyle(EditorStyles.miniButtonRight);

                    middleLODButtonStyle.fixedHeight = lastLODButtonStyle.fixedHeight = 15;
                    middleLODButtonStyle.fontSize = lastLODButtonStyle.fontSize = 8;
                    middleLODButtonStyle.alignment = lastLODButtonStyle.alignment = TextAnchor.MiddleCenter;

                    for (int i = 0; i < lodCount; i++)
                    {
                        Rect lodButtonRect = new Rect(
                            position.x + position.width - lodWidth + i * lodButtonWidth +
                            rightOffset,
                            position.y + topOffset, lodButtonWidth, 15f);

                        string buttonText = i.ToString();
                        lodButtonStyle = i == lodCount - 1 ? lastLODButtonStyle : middleLODButtonStyle;

                        if (GUI.Button(lodButtonRect, buttonText, lodButtonStyle))
                        {
                            if (i == lodIndex)
                            {
                                lodIndex = -1;
                            }
                            else
                            {
                                lodIndex = i;
                            }

                            if (component != null && Application.isPlaying)
                            {
                                component.UpdateLOD();
                            }
                        }

                        if (i == lodIndex)
                        {
                            GUI.backgroundColor = NUISettings.disabledColor;
                        }
                    }

                    GUI.backgroundColor = initialColor;
                }
            }


            // Draw Enabled button
            {
                if (lodIndex < 0)
                {
                    GUI.enabled = guiWasEnabled;
                }
                else
                {
                    GUI.enabled = false;
                }

                GUI.backgroundColor = isEnabled ? NUISettings.enabledColor : NUISettings.disabledColor;
                string text = isEnabled ? "ENABLED" : "DISABLED";
                Rect buttonRect = new Rect(position.x + position.width - 50f, position.y + topOffset, 45f, 17f);
                if (GUI.Button(buttonRect, text, buttonStyle))
                {
                    if (Application.isPlaying && component != null)
                    {
                        component.ToggleState();
                    }
                    else
                    {
                        isEnabled = !isEnabled;
                    }
                }

                GUI.backgroundColor = initialColor;
            }

            GUI.enabled = guiWasEnabled;

            return true;
        }


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool isExpanded = base.OnNUI(position, property, label);

            if (property == null)
            {
                return isExpanded;
            }

            VehicleComponent component = SerializedPropertyHelper.GetTargetObjectOfProperty(drawer.serializedProperty) as VehicleComponent;
            var targetObject = property.serializedObject.targetObject;
            VehicleController vehicleController = null;
            if (targetObject.GetType() == typeof(VehicleController))
            {
                vehicleController = property.serializedObject.targetObject as VehicleController;
            }
            else
            {
                ModuleWrapper moduleWrapper = property.serializedObject.targetObject as ModuleWrapper;
                if (moduleWrapper != null)
                {
                    vehicleController = moduleWrapper.GetComponent<VehicleController>();
                }
            }

            if (vehicleController == null || vehicleController.stateSettings == null)
            {
                return isExpanded;
            }

            if (property.serializedObject.targetObjects.Length > 1)
            {
                return isExpanded;
            }


            // Draw state settings
            if (Application.isPlaying && vehicleController.IsInitialized)
            {
                bool guiWasEnabled = GUI.enabled;
                GUI.enabled = vehicleController.enabled;
                DrawStateSettingsBar(
                    component,
                    position,
                    vehicleController.stateSettings.LODs.Count,
                    ref component.state.isEnabled,
                    ref component.state.lodIndex);
                GUI.enabled = guiWasEnabled;
            }
            else
            {
                string fullName = SerializedPropertyHelper.GetTargetObjectOfProperty(property)?.GetType()?.FullName;
                if (fullName == null)
                {
                    return isExpanded;
                }

                int definitionIndex = vehicleController.stateSettings.definitions.FindIndex(d => d.fullName == fullName);
                if (definitionIndex < 0)
                {
                    //Debug.LogWarning($"State definition for {fullName} not found.");
                    return isExpanded;
                }

                StateDefinition definition = vehicleController.stateSettings.definitions[definitionIndex];

                bool wasEnabled = definition.isEnabled;
                int prevIndex = definition.lodIndex;

                DrawStateSettingsBar(
                    component,
                    position,
                    vehicleController.stateSettings.LODs.Count,
                    ref definition.isEnabled,
                    ref definition.lodIndex);

                vehicleController.stateSettings.definitions[definitionIndex] = definition;

                if (definition.isEnabled != wasEnabled ||
                    definition.lodIndex != prevIndex)
                {
                    EditorUtility.SetDirty(vehicleController);
                    EditorUtility.SetDirty(vehicleController.stateSettings);
                    drawer.serializedObject.ApplyModifiedProperties();
                }
            }

            return isExpanded;
        }
    }
}

#endif
