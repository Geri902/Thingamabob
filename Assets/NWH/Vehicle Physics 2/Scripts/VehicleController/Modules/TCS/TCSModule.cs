using System;
using System.Linq;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.TCS
{
    /// <summary>
    ///     Traction Control System (TCS) module. Reduces engine throttle when excessive slip is present.
    /// </summary>
    [Serializable]
    public partial class TCSModule : VehicleComponent
    {
        /// <summary>
        /// Is TCS currently active?
        /// </summary>
        public bool active;

        /// <summary>
        ///     Speed under which TCS will not work.
        /// </summary>
        [Tooltip("    Speed under which TCS will not work.")]
        public float lowerSpeedThreshold = 2f;

        /// <summary>
        ///     Longitudinal slip threshold at which TCS will activate.
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("    Longitudinal slip threshold at which TCS will activate.")]
        public float slipThreshold = 0.1f;

        /// <summary>
        ///     Called each frame while TCS is active.
        /// </summary>
        [Tooltip("    Called each frame while TCS is active.")]
        public UnityEvent onTCSActive = new UnityEvent();


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.powertrain.engine.powerModifiers.Add(TCSPowerLimiter);
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                active = false;
                vehicleController.powertrain.engine.powerModifiers.Remove(TCSPowerLimiter);

                return true;
            }

            return false;
        }


        public float TCSPowerLimiter()
        {
            active = false;

            if (!IsActive)
            {
                return 1f;
            }

            foreach (WheelComponent wheelComponent in vehicleController.powertrain.wheels)
            {
                if (!wheelComponent.wheelUAPI.IsGrounded || vehicleController.powertrain.transmission.isShifting)
                {
                    continue;
                }

                float longSlip = wheelComponent.wheelUAPI.LongitudinalSlip;
                if (-longSlip * Mathf.Sign(vehicleController.LocalForwardVelocity) > slipThreshold)
                {
                    active = true;
                    onTCSActive.Invoke();
                    return 0.01f;
                }
            }

            return 1f;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.TCS
{
    [CustomPropertyDrawer(typeof(TCSModule))]
    public partial class TCSModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("slipThreshold");
            drawer.Field("lowerSpeedThreshold", true, "m/s");
            drawer.Field("active", false);

            drawer.EndProperty();
            return true;
        }
    }
}
#endif
