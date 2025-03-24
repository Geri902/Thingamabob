using System;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Events;
using NWH.Common.Vehicles;


#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.ABS
{
    /// <summary>
    ///     Anti-lock Braking System module.
    ///     Prevents wheels from locking up by reducing brake torque when slip reaches too high value.
    /// </summary>
    [Serializable]
    public partial class ABSModule : VehicleComponent
    {
        /// <summary>
        ///     Called each frame while ABS is a active.
        /// </summary>
        [Tooltip("    Called each frame while ABS is a active.")]
        public UnityEvent absActivated = new UnityEvent();

        /// <summary>
        ///     Is ABS currently active?
        /// </summary>
        [Tooltip("    Is ABS currently active?")]
        public bool active;

        /// <summary>
        ///     ABS will not work below this speed.
        /// </summary>
        [Tooltip("    ABS will not work below this speed.")]
        public float lowerSpeedThreshold = 1f;

        /// <summary>
        ///     Longitudinal slip required for ABS to trigger. Larger value means less sensitive ABS.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Longitudinal slip required for ABS to trigger.")]
        public float slipThreshold = 0.16f;

        /// <summary>
        /// Range in which brake torque will be reduced. Larger value means less sensitive ABS.
        /// </summary>
        [Range(0,1)]
        [Tooltip("Range in which brake torque will be reduced. Larger value means less sensitive ABS.")]
        public float slipRange = 0.2f;

        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                bool all = true;
                foreach (Brakes.BrakeTorqueModifier x in vehicleController.brakes.brakeTorqueModifiers)
                {
                    if (x == BrakeTorqueModifier)
                    {
                        all = false;
                        break;
                    }
                }

                if (all)
                {
                    vehicleController.brakes.brakeTorqueModifiers.Add(BrakeTorqueModifier);
                }

                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                active = false;
                vehicleController.brakes.brakeTorqueModifiers.RemoveAll(p => p == BrakeTorqueModifier);
                return true;
            }

            return false;
        }


        public float BrakeTorqueModifier()
        {
            if (!IsActive)
            {
                return 1f;
            }

            active = false;

            // Prevent ABS from working at low speeds
            if (vehicleController.Speed < lowerSpeedThreshold)
            {
                return 1f;
            }

            if (vehicleController.brakes.IsActive && !vehicleController.powertrain.engine.revLimiterActive && vehicleController.input.Handbrake < 0.1f)
            {
                for (int index = 0; index < vehicleController.powertrain.wheelCount; index++)
                {
                    WheelComponent wheelComponent = vehicleController.powertrain.wheels[index];
                    if (!wheelComponent.wheelUAPI.IsGrounded)
                    {
                        continue;
                    }

                    if (wheelComponent.wheelUAPI.LongitudinalSlip * Mathf.Sign(vehicleController.LocalForwardVelocity) > slipThreshold)
                    {
                        active = true;
                        absActivated.Invoke();
                        slipRange = slipRange < Vehicle.SMALL_NUMBER ? Vehicle.SMALL_NUMBER : slipRange;
                        return Mathf.Clamp01(wheelComponent.wheelUAPI.LongitudinalSlip - slipThreshold) / slipRange;
                    }
                }
            }

            return 1f;
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Modules.ABS
{
    [CustomPropertyDrawer(typeof(ABSModule))]
    public partial class ABSModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("slipThreshold");
            drawer.Field("slipRange");
            drawer.Field("lowerSpeedThreshold", true, "m/s");
            drawer.Field("active", false);
            drawer.Field("active");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
