using System;
using NWH.Common.Utility;
using UnityEngine;
using UnityEngine.Serialization;
using NWH.Common.Vehicles;
using NWH.Common;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class ClutchComponent : PowertrainComponent
    {
        /// <summary>
        ///     RPM at which automatic clutch will try to engage.
        /// </summary>
        [Tooltip("    RPM at which automatic clutch will try to engage.")]
        [FormerlySerializedAs("baseEngagementRPM")]
        [ShowInTelemetry]
        [ShowInSettings("Engagement RPM", 900, 2000, 100)]
        public float engagementRPM = 1200f;


        public float throttleEngagementOffsetRPM = 400f;


        /// <summary>
        ///     Clutch engagement in range [0,1] where 1 is fully engaged clutch.
        ///     Affected by Slip Torque field as the clutch can transfer [clutchEngagement * slipTorque] Nm
        ///     meaning that higher value of slipTorque will result in more sensitive clutch.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Clutch engagement in range [0,1] where 1 is fully engaged clutch.\r\nAffected by Slip Torque field as the clutch can transfer [clutchEngagement * slipTorque] Nm\r\nmeaning that higher value of slipTorque will result in more sensitive clutch.")]
        [ShowInTelemetry]
        public float clutchInput;


        /// <summary>
        /// Curve representing pedal travel vs. clutch engagement. Should start at 0,0 and end at 1,1.
        /// </summary>
        [UnityEngine.Tooltip("Curve representing pedal travel vs. clutch engagement. Should start at 0,0 and end at 1,1.")]
        [FormerlySerializedAs("clutchEngagementCurve")]
        public AnimationCurve engagementCurve = new AnimationCurve();


        public enum ClutchControlType
        {
            Automatic,
            UserInput,
            Manual
        }


        public ClutchControlType controlType = ClutchControlType.Automatic;


        /// <summary>
        /// The RPM range in which the clutch will go from disengaged to engaged and vice versa. 
        /// E.g. if set to 400 and engagementRPM is 1000, 1000 will mean clutch is fully disengaged and
        /// 1400 fully engaged. Setting it too low might cause clutch to hunt/oscillate.
        /// </summary>
        [ShowInSettings("Engagement Range", 200f, 1000f, 100f)]
        [UnityEngine.Tooltip("The RPM range in which the clutch will go from disengaged to engaged and vice versa. \r\nE.g. if set to 400 and engagementRPM is 1000, 1000 will mean clutch is fully disengaged and\r\n1400 fully engaged. Setting it too low might cause clutch to hunt/oscillate.")]
        public float engagementRange = 400f;


        /// <summary>
        ///     Torque at which the clutch will slip / maximum torque that the clutch can transfer.
        ///     This value also affects clutch engagement as higher slip value will result in clutch
        ///     that grabs higher up / sooner. Too high slip torque value combined with low inertia of
        ///     powertrain components might cause instability in powertrain solver.
        /// </summary>
        [SerializeField]
        [Tooltip(
            "Torque at which the clutch will slip / maximum torque that the clutch can transfer.\r\nThis value also affects clutch engagement as higher slip value will result in clutch\r\nthat grabs higher up / sooner. Too high slip torque value combined with low inertia of\r\npowertrain components might cause instability in powertrain solver.")]
        [ShowInSettings("Slip Torque", 10f, 5000f, 100f)]
        public float slipTorque = 500f;


        /// <summary>
        /// Amount of torque that will be passed through clutch even when completely disengaged
        /// to emulate torque converter creep on automatic transmissions.
        /// Should be higher than rolling resistance of the wheels to get the vehicle rolling.
        /// </summary>
        [Tooltip("Amount of torque that will be passed through clutch even when completely disengaged to emulate torque converter creep on automatic transmissions." +
                 "Should be higher than rolling resistance of the wheels to get the vehicle rolling.")]
        [ShowInSettings("Creep Torque", 0f, 100f, 10f)]
        public float creepTorque = 0;

        public float creepSpeedLimit = 1f;


        /// <summary>
        /// Clutch engagement based on clutchInput and the clutchEngagementCurve
        /// </summary>
        public float Engagement
        {
            get { return _clutchEngagement; }
        }

        [NonSerialized]
        private float _clutchEngagement;



        public override void VC_Validate(VehicleController vc)
        {
            base.VC_Validate(vc);

            // Warnings
            if (vc.powertrain.engine.engineType == EngineComponent.EngineType.ICE
             && engagementRPM <= vc.powertrain.engine.idleRPM)
            {
                PC_LogWarning(vc, $"Clutch engagement RPM is too low on vehicle {vc.name}. Clutch might stay engaged while in idle. Increase clutch" +
                                 " engagement RPM to be larger than engine idle RPM.");
            }

            if (engagementCurve == null || engagementCurve.keys.Length < 2)
            {
                PC_LogWarning(vc, $"Clutch engagement curve is not set. A simple [0,0] to [1,1] curve can be used.");
            }
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            inertia = 0.02f;
            slipTorque = vehicleController.powertrain.engine.EstimatedPeakTorque * 1.5f;
            SetDefaultClutchEngagementCurve();
            Output = vehicleController.powertrain.transmission;
        }

        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                clutchInput = 0f;
                _clutchEngagement = 0f;
                return true;
            }
            return false;
        }


        private void SetDefaultClutchEngagementCurve()
        {
            // Create a linear clutch engagement curve with two keyframes
            engagementCurve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(1, 1)
            );
        }



        public override float QueryAngularVelocity(float angularVelocity, float dt)
        {
            inputAngularVelocity = angularVelocity;

            // Return input angular velocity if inputNameHash or outputNameHash is 0
            if (inputNameHash == 0 || outputNameHash == 0)
            {
                return inputAngularVelocity;
            }

            // Adjust clutch engagement based on conditions
            if (controlType == ClutchControlType.Automatic)
            {
                EngineComponent engine = vehicleController.powertrain.engine;

                // Engine is at risk of stalling, disconnect the clutch
                if (vehicleController.powertrain.engine.OutputRPM < engine.idleRPM)
                {
                    clutchInput = 0f;
                }
                // Override engagement when shifting to smoothly engage and disengage gears
                else if (vehicleController.powertrain.transmission.isShifting)
                {
                    float shiftProgress = vehicleController.powertrain.transmission.shiftProgress;
                    clutchInput = Mathf.Abs(Mathf.Cos(Mathf.PI * shiftProgress));
                }
                // Clutch engagement calculation for automatic clutch
                else
                {
                    // Calculate engagement
                    // Engage the clutch if the input spinning faster than the output, but also if vice versa.
                    float throttleInput = vehicleController.input.InputSwappedThrottle;
                    float finalEngagementRPM = engagementRPM + throttleEngagementOffsetRPM * (throttleInput * throttleInput);
                    float referenceRPM = Mathf.Max(InputRPM, OutputRPM);
                    float targetInputValue = (referenceRPM - finalEngagementRPM) / engagementRange;

                    // Smoothly engage the clutch in case the RPM jumped over the engagement range in one frame
                    if (targetInputValue > clutchInput)
                    {
                        clutchInput = Mathf.SmoothStep(clutchInput, targetInputValue, dt * 15.0f);
                    }
                    // Quickly disengage the clutch to prevent stalling
                    else
                    {
                        clutchInput = targetInputValue;
                    }
                    clutchInput = Mathf.Clamp(clutchInput, 0f, 1f);

                    // Avoid disconnecting clutch at high speed
                    if (engine.OutputRPM > engine.idleRPM * 1.1f && vehicleController.Speed > 3f)
                    {
                        clutchInput = 1f;
                    }
                }
            }
            else if (controlType == ClutchControlType.UserInput)
            {
                // Manual clutch engagement through user input
                clutchInput = vehicleController.input.Clutch;
            }

            outputAngularVelocity = inputAngularVelocity * _clutchEngagement;
            float Wout = Output.QueryAngularVelocity(outputAngularVelocity, dt) * _clutchEngagement;
            float Win = angularVelocity * (1f - _clutchEngagement);
            return Wout + Win;
        }


        public override float QueryInertia()
        {
            if (outputNameHash == 0)
            {
                return inertia;
            }

            float I = inertia + Output.QueryInertia() * _clutchEngagement;
            return I;
        }


        public override float ForwardStep(float torque, float inertiaSum, float dt)
        {
            inputTorque = torque;
            inputInertia = inertiaSum;

            if (outputNameHash == 0)
            {
                return torque;
            }

            // Get the clutch engagement point from the input value
            // Do not use the clutchEnagement directly for any calculations!
            _clutchEngagement = engagementCurve.Evaluate(clutchInput);
            _clutchEngagement = Mathf.Clamp01(_clutchEngagement);

            // Calculate output inertia and torque based on the clutch engagement
            // Assume half of the inertia is on the input plate and the other half is on the output clutch plate.
            float halfClutchInertia = inertia * 0.5f;
            outputInertia = (inertiaSum + halfClutchInertia) * _clutchEngagement + halfClutchInertia;

            // Allow the torque output to be only up to the slip torque value
            float outputTorqueClamp = slipTorque * _clutchEngagement;
            outputTorque = inputTorque;
            MathUtility.ClampWithRemainder(ref outputTorque, outputTorqueClamp, out float slipOverflowTorque);

            // Apply the creep torque commonly caused by torque converter drag in automatic transmissions
            ApplyCreepTorque(ref outputTorque, creepTorque);

            // Send the torque downstream
            float returnTorque = _output.ForwardStep(outputTorque, outputInertia, dt) * _clutchEngagement;

            // Clamp the return torque to the slip torque of the clutch once again
            returnTorque = Mathf.Clamp(returnTorque, -slipTorque, slipTorque);

            // Torque returned to the input is a combination of torque returned by the powertrain and the torque that 
            // was possibly never sent downstream
            return returnTorque + slipOverflowTorque;
        }


        private void ApplyCreepTorque(ref float torque, float creepTorque)
        {
            // Apply creep torque to forward torque
            if (creepTorque != 0 && vehicleController.powertrain.engine.IsRunning && vehicleController.Speed < creepSpeedLimit)
            {
                bool torqueWithinCreepRange = torque < creepTorque && torque > -creepTorque;

                if (torqueWithinCreepRange)
                {
                    torque = creepTorque;
                }
            }

        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(ClutchComponent))]
    public partial class ClutchComponentDrawer : PowertrainComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            DrawCommonProperties();

            ClutchComponent cc = SerializedPropertyHelper.GetTargetObjectOfProperty(property) as ClutchComponent;

            drawer.BeginSubsection("General");
            drawer.Field("controlType");

            drawer.BeginSubsection("Engagement");
            if (cc.controlType == ClutchComponent.ClutchControlType.Automatic)
            {
                drawer.Field("engagementRPM");
                drawer.Field("throttleEngagementOffsetRPM");
                drawer.Field("engagementRange");

                drawer.Info("Final Engagement RPM = EngagementRPM + ThrottleEngagementOffsetRPM + EngagementRange");
            }
            drawer.Field("engagementCurve");
            drawer.Label($"Current engagement:\t{cc.Engagement}");
            drawer.EndSubsection();

            drawer.Field("clutchInput", cc.controlType == ClutchComponent.ClutchControlType.Manual);

            if (cc.controlType == ClutchComponent.ClutchControlType.Automatic)
            {
                drawer.Info("Clutch input is being set automatically based on engine RPM.");
            }
            else if (cc.controlType == ClutchComponent.ClutchControlType.UserInput)
            {
                drawer.Info("Clutch input is being set through user input. Check input settings for 'Clutch' axis.");
            }
            else
            {
                drawer.Info("Clutch input is not being set due to the controlType being 'Manual'. Set it manually.");
            }

            drawer.Field("slipTorque", true, "Nm");

            drawer.BeginSubsection("Creep");
            drawer.Field("creepTorque", true, "Nm");
            drawer.Field("creepSpeedLimit", true, "m/s");
            drawer.EndSubsection();
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
