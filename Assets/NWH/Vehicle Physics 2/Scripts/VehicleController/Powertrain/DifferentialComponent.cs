using System;
using System.Collections.Generic;
using UnityEngine;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class DifferentialComponent : PowertrainComponent
    {
        /// <summary>
        ///     Torque bias between left (A) and right (B) output in [0,1] range.
        /// </summary>
        [SerializeField]
        [Range(0, 1)]
        [Tooltip("    Torque bias between left (A) and right (B) output in [0,1] range.")]
        [ShowInTelemetry]
        [ShowInSettings("Bias A/B", 0f, 1f, 0.1f)]
        public float biasAB = 0.5f;

        /// <summary>
        /// Stiffness of the differential under acceleration. N per rad/s of angular velocity difference between outputs.
        /// </summary>
        [SerializeField]
        [Range(0, 1)]
        [ShowInTelemetry]
        [ShowInSettings("Power Ramp", 0f, 1f, 0.1f)]
        [UnityEngine.Tooltip("Stiffness of the differential under acceleration.")]
        public float powerStiffness = 1f;

        /// <summary>
        /// Stiffness of the differential under braking. N per rad/s of angular velocity difference between outputs.
        /// </summary>
        [Range(0, 1)]
        [ShowInTelemetry]
        [ShowInSettings("Coast Ramp", 0f, 1f, 0.1f)]
        [UnityEngine.Tooltip("Stiffness of the differential under braking.")]
        public float coastStiffness = 0.5f;

        /// <summary>
        /// Slip torque of a differential. Typically in the range of 100-500 Nm for a sports car. Use >1000 for a locking differential.
        /// </summary>
        [Tooltip("Slip torque of a differential. Typically in the range of 100-500 Nm for a sports car. Use >1000 for a locking differential.")]
        [ShowInTelemetry]
        [ShowInSettings("LSD Slip Tq", 0f, 2000f, 100f)]
        public float slipTorque = 150f;

        /// <summary>
        /// If true, the torque will be sent to left/right based on the steering input.
        /// </summary>
        [Tooltip("If true, the torque will be sent to left/right based on the steering input.")]
        public bool differentialSteering = false;


        /// <summary>
        ///     Second output of differential.
        /// </summary>
        public PowertrainComponent OutputB
        {
            get { return _outputB; }
            set
            {
                if (value == this)
                {
                    Debug.LogWarning($"{name}: PowertrainComponent Output can not be self.");
                    outputBNameHash = 0;
                    _output = null;
                }
                else
                {
                    if (_outputB != null)
                    {
                        _outputB.inputNameHash = 0;
                        _outputB.Input = null;
                    }

                    _outputB = value;

                    if (_outputB != null)
                    {
                        outputBNameHash = _outputB.name.GetHashCode();
                        _outputB.Input = this;
                    }
                    else
                    {
                        outputBNameHash = 0;
                    }
                }
            }
        }

        [NonSerialized]
        protected PowertrainComponent _outputB;
        public int outputBNameHash;




        protected override void VC_Initialize()
        {
            LoadComponentFromHash(vehicleController, ref _outputB, outputBNameHash);
            base.VC_Initialize();
        }


        public override void VC_Validate(VehicleController vc)
        {
            base.VC_Validate(vc);

            if (outputBNameHash == 0)
            {
                Debug.Log(outputBNameHash);
                PC_LogWarning(vc, "PowertrainComponent output not set. This might be a result of the 10.20f update, in which case the " +
                    $"powertrain outputs need to be re-assigned.");
            }


            if (Application.isPlaying && Input == null)
            {
                PC_LogWarning(vc, "Differential has no input. Differential that are in no way connected to the engine" +
                                  " will not be updated and should be removed or they might cause the wheels attached to them" +
                                  " to spin up slower than usual due to the inertia of a dangling/dead differential.");
            }
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            name = "Differential";
            inertia = 0.02f;
        }


        public override float QueryAngularVelocity(float angularVelocity, float dt)
        {
            inputAngularVelocity = angularVelocity;

            if (outputNameHash == 0 || outputBNameHash == 0)
            {
                return angularVelocity;
            }

            outputAngularVelocity = inputAngularVelocity;
            float Wa = _output.QueryAngularVelocity(outputAngularVelocity, dt);
            float Wb = _outputB.QueryAngularVelocity(outputAngularVelocity, dt);
            return (Wa + Wb) * 0.5f;
        }


        public override float QueryInertia()
        {
            if (outputNameHash == 0 || outputBNameHash == 0)
            {
                return inertia;
            }

            float Ia = _output.QueryInertia();
            float Ib = _outputB.QueryInertia();
            float I = inertia + (Ia + Ib);
            return I;
        }


        public override float ForwardStep(float torque, float inertiaSum, float dt)
        {
            inputTorque = torque;
            inputInertia = inertiaSum;

            if (outputNameHash == 0 || outputBNameHash == 0)
            {
                return torque;
            }

            float Ta, Tb;

            float Wa = _output.QueryAngularVelocity(outputAngularVelocity, dt);
            float Wb = _outputB.QueryAngularVelocity(outputAngularVelocity, dt);

            if (differentialSteering)
            {
                float steeringInput = vehicleController.input.Steering;
                Ta = torque * (1.0f + steeringInput) / 2.0f;
                Tb = torque * (1.0f - steeringInput) / 2.0f;
            }
            else
            {
                // Calculate stiffness based on power or coasting
                // Stiffness is clamped to 0, 1 in the inspector to prevent having over-stiff differentials
                // which causes powertrain to be unstable
                float totalStiffness = torque > 0 ? powerStiffness * 1000f : coastStiffness * 1000f;

                // Angular velocity difference
                float Wdiff = Wa - Wb;

                // Correction torque
                float Tcorrective = Mathf.Clamp(totalStiffness * Wdiff, -slipTorque, slipTorque);

                // Output torque
                Ta = torque - Tcorrective;
                Tb = torque + Tcorrective;

                // Apply bias
                Ta *= (1f - biasAB);
                Tb *= biasAB;
            }

            float outAInertia = inertiaSum * 0.5f;
            float outBInertia = inertiaSum * 0.5f;

            outputTorque = Ta + Tb;
            outputInertia = outAInertia + outBInertia;

            return _output.ForwardStep(Ta, outAInertia, dt) + _outputB.ForwardStep(Tb, outBInertia, dt);
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(DifferentialComponent))]
    public partial class DifferentialComponentDrawer : PowertrainComponentDrawer
    {
        private int selectionA;
        private int selectionB;

        public override void DrawPowertrainOutputSection(ref Rect rect, VehicleController vc, PowertrainComponent pc)
        {
            // Cast the PowertrainComponent to DifferentialComponent
            DifferentialComponent dc = pc as DifferentialComponent;

            // Remember initial values of selections to know if the change happened later
            int initialSelectionA = selectionA;
            int initialSelectionB = selectionB;

            // Find the index of the current output A and output B
            selectionA = componentNames.FindIndex(n => n.GetHashCode() == dc.outputNameHash);
            selectionB = componentNames.FindIndex(n => n.GetHashCode() == dc.outputBNameHash);

            // Create the output A dropdown with the list of component names
            selectionA = EditorGUI.Popup(drawer.positionRect, "OutputA",
                selectionA < 0 ? 0 : selectionA, componentNames.ToArray());
            drawer.Space(22);

            // Create the output B dropdown with the list of component names
            selectionB = EditorGUI.Popup(drawer.positionRect, "OutputB",
                selectionB < 0 ? 0 : selectionB, componentNames.ToArray());

            drawer.Space(22);

            // Check if either output A or output B dropdown selection has changed
            if (selectionA != initialSelectionA || selectionB != initialSelectionB)
            {
                // Set the new output A and output B and mark the VehicleController as dirty to save changes
                dc.Output = components[selectionA];
                dc.OutputB = components[selectionB];
                EditorUtility.SetDirty(vc);
            }
        }


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            DrawCommonProperties();

            drawer.BeginSubsection("Differential Settings");

            if (property.FindPropertyRelative("differentialSteering").boolValue == false)
            {
                drawer.Info("Differential type was replaced in v12 with a single differential that can be used as open, limited slip or locked, based on settings\r\n" +
                    "- Open: Stiffness = 0\r\n" +
                    "- LSD: Stiffness > 0, ~200 slip torque\r\n" +
                    "- Locked: Stiffnes >= 1, >1000 slip torque");
                drawer.Field("biasAB");
                drawer.Field("slipTorque");
                drawer.Field("powerStiffness");
                drawer.Field("coastStiffness");

                drawer.Label("Presets:", true);
                if (drawer.Button("Open"))
                {
                    property.FindPropertyRelative("biasAB").floatValue = 0.5f;
                    property.FindPropertyRelative("powerStiffness").floatValue = 0f;
                    property.FindPropertyRelative("coastStiffness").floatValue = 0f;
                    property.FindPropertyRelative("slipTorque").floatValue = 0f;
                }
                else if (drawer.Button("LSD"))
                {
                    property.FindPropertyRelative("biasAB").floatValue = 0.5f;
                    property.FindPropertyRelative("powerStiffness").floatValue = 1f;
                    property.FindPropertyRelative("coastStiffness").floatValue = 1f;
                    property.FindPropertyRelative("slipTorque").floatValue = 200f;
                }
                else if (drawer.Button("Locked"))
                {
                    property.FindPropertyRelative("biasAB").floatValue = 0.5f;
                    property.FindPropertyRelative("powerStiffness").floatValue = 1f;
                    property.FindPropertyRelative("coastStiffness").floatValue = 1f;
                    property.FindPropertyRelative("slipTorque").floatValue = 4000f;
                }
            }

            drawer.Field("differentialSteering");
            drawer.Info("When using differential steering the torque will be sent to left/right based on the steering input. Other settings " +
                "have no effect.");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
