using System;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Modules.ArcadeModule
{
    /// <summary>
    ///     Arcade assists for NVP2.
    /// </summary>
    [Serializable]
    public partial class ArcadeModule : VehicleComponent
    {
        // Steer assist

        /// <summary>
        /// Torque that will be applied to the Rigidbody to try and reach the steering angle,
        /// irrelevant of the tire slip. Also works in air.
        /// </summary>
        [UnityEngine.Tooltip("Torque that will be applied to the Rigidbody to try and reach the steering angle,\r\nirrelevant of the tire slip. Also works in air.")]
        public float artificialSteerStrength = 1f;

        // Drift assist

        /// <summary>
        /// Strength of drift assist.
        /// </summary>
        [UnityEngine.Tooltip("Strength of drift assist.")]
        public float driftAssistStrength = 1f;

        /// <summary>
        /// angle that the vehicle will attempt to hold when drifting.
        /// Force is applied if the angle goes over this value. If the angle is below the drift angle, no force is applied.
        /// </summary>
        [UnityEngine.Tooltip("angle that the vehicle will attempt to hold when drifting.\r\nForce is applied if the angle goes over this value. If the angle is below the drift angle, no force is applied.")]
        public float targetDriftAngle = 45f;

        /// <summary>
        /// angle that will be added to targetDriftAngle based on the steering input.
        /// If the vehicle is drifting and there is steering input, drift angle will increase.
        /// </summary>
        [UnityEngine.Tooltip("angle that will be added to targetDriftAngle based on the steering input.\r\nIf the vehicle is drifting and there is steering input, drift angle will increase.")]
        public float steerAngleContribution = 10f;

        /// <summary>
        /// Maximum force that will be applied to the rear axle to keep the vehicle at or below the target drift angle.
        /// </summary>
        [UnityEngine.Tooltip("Maximum force that will be applied to the rear axle to keep the vehicle at or below the target drift angle.")]
        public float maxDriftAssistForce = 800f;

        private float _driftAngle;
        private float _prevDriftError;

        public float DriftAngle
        {
            get { return _driftAngle; }
        }


        public override void VC_FixedUpdate()
        {
            base.VC_FixedUpdate();
            if (!IsActive || !vehicleController.IsFullyGrounded() || vehicleController.SpeedSigned < 1f)
            {
                return;
            }

            // Steer assist
            if (artificialSteerStrength > 0f && vehicleController.Speed > 1f)
            {
                float steerTorque = vehicleController.input.Steering * artificialSteerStrength * vehicleController.vehicleRigidbody.mass
                                  * -Physics.gravity.y * 0.4f;
                steerTorque *= Mathf.Clamp01(vehicleController.Speed / 5f);
                vehicleController.vehicleRigidbody.AddTorque(new Vector3(0, steerTorque, 0));
            }


            // Drift assist
            if (driftAssistStrength > 0f && vehicleController.Speed > 1f)
            {
                if (vehicleController.powertrain.wheelGroups.Count != 2)
                {
                    return;
                }

                Vector3 normVel = vehicleController.vehicleRigidbody.linearVelocity.normalized;
                Vector3 vehicleDir = vehicleController.transform.forward;
                _driftAngle = Vector3.SignedAngle(normVel, vehicleDir, vehicleController.transform.up);
                _driftAngle = Mathf.Sign(_driftAngle)
                            * Mathf.Clamp(Mathf.Abs(Mathf.Clamp(_driftAngle, -90f, 90f)), 0f, Mathf.Infinity);

                WheelGroup a = vehicleController.powertrain.wheelGroups[1];
                if (a.Wheels.Count != 2)
                {
                    return;
                }
                WheelComponent leftWheel = a.LeftWheel;
                WheelComponent rightWheel = a.RightWheel;

                Vector3 center = (leftWheel.wheelUAPI.transform.position
                                + rightWheel.wheelUAPI.transform.position) * 0.5f;

                float driftAngleAimed = targetDriftAngle + vehicleController.input.Steering * steerAngleContribution;
                float absError = Mathf.Abs(_driftAngle) - Mathf.Abs(driftAngleAimed);
                float absErrorIntegral = (absError - _prevDriftError) / Time.fixedDeltaTime;
                Vector3 force = vehicleController.transform.right
                              * (Mathf.Clamp(absError + absErrorIntegral, 0, 90)
                               * Mathf.Sign(_driftAngle) * maxDriftAssistForce);
                force *= Mathf.Clamp01(vehicleController.Speed / 3f);
                vehicleController.vehicleRigidbody.AddForceAtPosition(force * driftAssistStrength, center);
                _prevDriftError = absError;
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.ArcadeModule
{
    [CustomPropertyDrawer(typeof(ArcadeModule))]
    public partial class ArcadeModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            ArcadeModule moduleTemplate = SerializedPropertyHelper.GetTargetObjectOfProperty(property) as ArcadeModule;
            if (moduleTemplate == null)
            {
                drawer.EndProperty();
                return false;
            }

            drawer.BeginSubsection("Artificial Steer");
            drawer.Field("artificialSteerStrength", true, "x100%");
            drawer.EndSubsection();

            drawer.BeginSubsection("Drift Assist");
            drawer.Field("driftAssistStrength", true, "x100%");
            drawer.Field("targetDriftAngle", true, "deg");
            drawer.Field("steerAngleContribution", true, "deg");
            drawer.Field("maxDriftAssistForce", true, "N");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
