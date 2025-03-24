using System;
using NWH.VehiclePhysics2.Modules.NOS;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules
{
    /// <summary>
    ///     NOS (Nitrous Oxide System) module.
    /// </summary>
    [Serializable]
    public partial class NOSModule : VehicleComponent
    {
        /// <summary>
        ///     Capacity of NOS bottle.
        /// </summary>
        [Tooltip("    Capacity of NOS bottle.")]
        public float capacity = 2f;

        /// <summary>
        ///     Current charge of NOS bottle.
        /// </summary>
        [Tooltip("    Current charge of NOS bottle.")]
        public float charge = 2f;

        /// <summary>
        ///     Can NOS be used while in reverse?
        /// </summary>
        [Tooltip("    Can NOS be used while in reverse?")]
        public bool disableInReverse = true;

        /// <summary>
        ///     Can NOS be used while there is no throttle input / engine is idling?
        /// </summary>
        [Tooltip("    Can NOS be used while there is no throttle input / engine is idling?")]
        public bool disableOffThrottle = true;

        /// <summary>
        ///     Makes engine sound louder while NOS is active.
        ///     Volume range of the engine running sound component will get multiplied by this value.
        /// </summary>
        [Range(1, 3)]
        [Tooltip(
            "Makes engine sound louder while NOS is active.\r\nVolume range of the engine running sound component will get multiplied by this value.")]
        public float engineVolumeCoefficient = 1.5f;

        /// <summary>
        ///     Value that will be used as base intensity of Exhaust Smoke effect while NOS is active.
        /// </summary>
        [Range(1, 3)]
        [Tooltip("    Value that will be used as base intensity of Exhaust Smoke effect while NOS is active.")]
        public float exhaustEmissionCoefficient = 2f;

        /// <summary>
        ///     Maximum flow of NOS in kg/s.
        /// </summary>
        [Tooltip("    Maximum flow of NOS in kg/s.")]
        public float flow = 0.1f;

        /// <summary>
        ///     Power of the engine will be multiplied by this value when NOS is active to get the final engine power.
        /// </summary>
        [Range(1, 5)]
        [Tooltip(
            "Power of the engine will be multiplied by this value when NOS is active to get the final engine power.")]
        public float powerCoefficient = 2f;

        [SerializeField]
        public NOSSoundComponent nosSoundComponent = new NOSSoundComponent();

        public bool IsUsingNOS
        {
            get
            {
                return state.isEnabled
                    && vehicleController.input.Boost
                    && charge > 0
                    && !(vehicleController.powertrain.transmission.Gear < 0 && disableInReverse)
                    && !(vehicleController.powertrain.engine.ThrottlePosition < 0.5f && disableOffThrottle);
            }
        }


        protected override void VC_Initialize()
        {
            nosSoundComponent.nosModule = this;
            vehicleController.soundManager.AddAndOnboardNewComponent(nosSoundComponent);

            base.VC_Initialize();
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.powertrain.engine.powerModifiers.Add(NOSPowerModifier);
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                vehicleController.powertrain.engine.powerModifiers.Remove(NOSPowerModifier);
                return true;
            }

            return false;
        }


        public float NOSPowerModifier()
        {
            if (!IsUsingNOS)
            {
                return 1f;
            }

            charge -= flow * vehicleController.fixedDeltaTime;
            charge = charge < 0 ? 0 : charge > capacity ? capacity : charge;

            if (charge <= 0)
            {
                return 1f;
            }

            if (vehicleController.effectsManager.exhaustFlash.IsActive)
            {
                vehicleController.effectsManager.exhaustFlash.Flash(false);
            }

            return powerCoefficient;
        }

        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            nosSoundComponent.VC_SetDefaults();
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.NOS
{
    [CustomPropertyDrawer(typeof(NOSModule))]
    public partial class NOSModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("capacity");
            drawer.Field("charge");
            drawer.Field("flow");
            drawer.Field("powerCoefficient");
            drawer.Field("exhaustEmissionCoefficient");
            drawer.Field("engineVolumeCoefficient");
            drawer.Field("disableOffThrottle");
            drawer.Field("disableInReverse");
            drawer.Property("nosSoundComponent");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
