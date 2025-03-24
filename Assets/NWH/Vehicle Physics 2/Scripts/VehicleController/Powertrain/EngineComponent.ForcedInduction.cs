using System;
using UnityEngine;
using UnityEngine.Events;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    public partial class EngineComponent
    {
        /// <summary>
        ///     Supercharger, turbocharger, etc.
        ///     Only an approximation. Engine capacity, air flow, turbo size, etc. are not taken into consideration.
        /// </summary>
        [Serializable]
        public partial class ForcedInduction
        {
            public enum ForcedInductionType
            {
                Turbocharger,
                Supercharger,
            }


            /// <summary>
            ///     Should forced induction be used?
            /// </summary>
            [ShowInTelemetry]
            [Tooltip("    Should forced induction be used?")]
            [ShowInSettings("Enabled")]
            public bool useForcedInduction = true;


            /// <summary>
            ///     Boost value as percentage in 0 to 1 range. Unitless.
            ///     Can be used for boost gauges.
            /// </summary>
            [Range(0, 1)]
            [ShowInTelemetry]
            [Tooltip("    Boost value as percentage in 0 to 1 range. Unitless.\r\n    Can be used for boost gauges.")]
            public float boost;


            /// <summary>
            ///     Type of forced induction.
            /// </summary>
            [Tooltip("    Type of forced induction.")]
            public ForcedInductionType forcedInductionType = ForcedInductionType.Turbocharger;


            /// <summary>
            ///     Imitates wastegate in a turbo setup.
            ///     Enable for turbo flutter sound effects and/or boost to drop off faster after closing throttle.
            ///     Not used with superchargers.
            /// </summary>
            [Tooltip(
                "Imitates wastegate in a turbo setup.\r\nEnable if you want turbo flutter sound effects and/or boost to drop off faster after closing throttle.\r\nNot used with superchargers.")]
            [ShowInSettings("Has Wastegate")]
            public bool hasWastegate = true;


            /// <summary>
            ///     Power coefficient that the maxPower of the engine will be multiplied by and represents power gained by
            ///     adding forced induction to the engine. E.g. 1.4 would mean that the engine will produce 140% of the maxPower.
            ///     Power gain is dependent on boost value.
            /// </summary>
            [Range(1, 3)]
            [Tooltip(
                "Additional power that will be added to the engine's power.\r\nThis is the maximum value possible and depends on spool percent.")]
            [ShowInSettings("Power Gain Mp.", 1f, 2f, 0.1f)]
            public float powerGainMultiplier = 1.4f;


            /// <summary>
            ///     Shortest time possible needed for turbo to spool up to its maximum RPM.
            ///     Use larger values for larger turbos and vice versa.
            ///     Forced to 0 for superchargers.
            /// </summary>
            [Range(0.1f, 4)]
            [Tooltip(
                "Shortest time possible needed for turbo to spool up to its maximum RPM.\r\nUse larger values for larger turbos and vice versa.\r\nForced to 0 for superchargers.")]
            [ShowInSettings("Spool Up Time", 0f, 2f, 0.05f)]
            public float spoolUpTime = 0.3f;



            /// <summary>
            ///     Flag for sound effects.
            ///     Paramter is the boost value at the time of release.
            /// </summary>
            [Tooltip("    Flag for sound effects.")]
            public UnityEvent<float> onWastegateRelease = new UnityEvent<float>();

            private float _boostVelocity;


            /// <summary>
            ///     Current power gained from forced induction.
            /// </summary>
            public float PowerGainMultiplier
            {
                get
                {
                    if (!useForcedInduction)
                    {
                        return 1f;
                    }

                    float multiplier = Mathf.Lerp(1f, powerGainMultiplier, boost);
                    return multiplier;
                }
            }


            public void Update(EngineComponent engine)
            {
                if (!useForcedInduction)
                {
                    return;
                }

                if (forcedInductionType == ForcedInductionType.Supercharger)
                {
                    boost = Mathf.Clamp01(engine.RPMPercent * 2f);
                }
                else
                {
                    float targetBoost;
                    if (engine.vehicleController.powertrain.transmission.isShifting)
                    {
                        targetBoost = 0f;
                    }
                    else
                    {
                        targetBoost = Mathf.Clamp01(engine.RPMPercent * 2f) * engine.ThrottlePosition;
                    }

                    boost = Mathf.SmoothDamp(boost, targetBoost, ref _boostVelocity, spoolUpTime);
                    boost = Mathf.Clamp01(boost);

                    if (hasWastegate)
                    {
                        if (targetBoost < 0.3f && boost > 0.7f)
                        {
                            onWastegateRelease.Invoke(boost);
                            boost = 0f;
                        }
                    }
                }
            }
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(EngineComponent.ForcedInduction))]
    public partial class ForcedInductionDrawer : NVP_NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            if (drawer.Field("useForcedInduction").boolValue)
            {
                drawer.Field("forcedInductionType");
                drawer.Field("powerGainMultiplier");
                drawer.Field("spoolUpTime", true, "s");
                drawer.Field("hasWastegate");
                drawer.Field("boost", false);
            }

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
