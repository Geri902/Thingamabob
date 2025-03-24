using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Class storing VehicleComponent state.
    /// </summary>
    [Serializable]
    public partial class StateDefinition
    {
        /// <summary>
        /// Full type name of the VehicleComponent.
        /// </summary>
        public string fullName = null;

        /// <summary>
        /// When VehicleController is enabled, should this component be enabled too?
        /// </summary>
        public bool isEnabled = false;

        /// <summary>
        /// Has the component been initialized?
        /// </summary>
        public bool initialized = false;

        /// <summary>
        /// Active LOD. -1 for disabled LODs.
        /// </summary>
        public int lodIndex = -1;


        public StateDefinition()
        {
        }

        public StateDefinition(string fullName, bool isEnabled, int lod)
        {
            this.fullName = fullName;
            this.isEnabled = isEnabled;
            lodIndex = lod;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Custom property drawer for StateDefinition.
    /// </summary>
    [CustomPropertyDrawer(typeof(StateDefinition))]
    public partial class StateDefinitionDrawer : NVP_NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            drawer.BeginProperty(position, property, label);

            // Draw label
            string fullName = drawer.FindProperty("fullName").stringValue.Replace("NWH.VehiclePhysics2.", "");
            string shortName = fullName.Split('.').Last();

            GUIStyle miniStyle = EditorStyles.centeredGreyMiniLabel;
            miniStyle.alignment = TextAnchor.MiddleLeft;

            Rect labelRect = drawer.positionRect;
            labelRect.x += 5f;

            Rect miniRect = drawer.positionRect;
            miniRect.x += 200f;

            EditorGUI.LabelField(labelRect, shortName, EditorStyles.boldLabel);
            EditorGUI.LabelField(miniRect, fullName, miniStyle);
            drawer.Space(NUISettings.fieldHeight);

            StateSettings stateSettings =
                SerializedPropertyHelper.GetTargetObjectWithProperty(property) as StateSettings;
            if (stateSettings == null)
            {
                drawer.EndProperty();
                return false;
            }
            bool isEnabled = property.FindPropertyRelative("isEnabled").boolValue;
            int lodIndex = property.FindPropertyRelative("lodIndex").intValue;

            bool wasEnabled = isEnabled;
            int prevLodIndex = lodIndex;

            ComponentNUIPropertyDrawer.DrawStateSettingsBar(
                null,
                position,
                stateSettings.LODs.Count,
                ref isEnabled,
                ref lodIndex);

            property.FindPropertyRelative("isEnabled").boolValue = isEnabled;
            property.FindPropertyRelative("lodIndex").intValue = lodIndex;

            if (wasEnabled != isEnabled || prevLodIndex != lodIndex)
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
