using System;
using System.Linq;
using NWH.Common.Utility;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.SpeedLimiter
{
    /// <summary>
    ///     Module that limits vehicle speed to the set speed limit.
    ///     Only limits throttle application, does not apply brakes.
    /// </summary>
    [Serializable]
    public partial class SpeedLimiterModule : VehicleComponent
    {
        public enum SpeedUnits
        {
            ms,
            kmh,
            mph,
        }

        public bool active;

        /// <summary>
        ///     Speed limit above which the throttle will be cut.
        /// </summary>
        [Tooltip("    Speed limit above which the throttle will be cut.")]
        public float speedLimit;

        /// <summary>
        ///     Units which will be used for speed limiter. Defaults to m/s.
        /// </summary>
        [Tooltip("    Units which will be used for speed limiter. Defaults to m/s.")]
        public SpeedUnits speedUnits;


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.powertrain.engine.powerModifiers.Add(SpeedPowerLimiter);
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                active = false;
                vehicleController.powertrain.engine.powerModifiers.Remove(SpeedPowerLimiter);
                return true;
            }

            return false; ;
        }


        public float SpeedPowerLimiter()
        {
            if (!IsActive || speedLimit == 0)
            {
                active = false;
                return 1f;
            }

            float msSpeedLimit = 0;
            if (speedUnits == SpeedUnits.ms)
            {
                msSpeedLimit = speedLimit;
            }
            else if (speedUnits == SpeedUnits.kmh)
            {
                msSpeedLimit = UnitConverter.Speed_kmhToMs(speedLimit);
            }
            else if (speedUnits == SpeedUnits.mph)
            {
                msSpeedLimit = UnitConverter.Speed_mphToMs(speedLimit);
            }

            if (vehicleController.Speed > msSpeedLimit)
            {
                active = true;
                return 0f;
            }

            active = false;
            return 1f;
        }
    }
}

#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Modules.SpeedLimiter
{
    [CustomPropertyDrawer(typeof(SpeedLimiterModule))]
    public partial class SpeedLimiterModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("speedLimit");
            drawer.Field("speedUnits");
            drawer.Field("active", false);

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
