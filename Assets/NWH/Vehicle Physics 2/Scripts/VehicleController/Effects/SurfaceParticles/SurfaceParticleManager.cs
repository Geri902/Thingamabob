using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    [Serializable]
    public partial class SurfaceParticleManager : Effect
    {
        /// <summary>
        ///     How much will lateral slip contribute to the particle emission.
        ///     Ignored when particle type for the surface is set to other than Smoke.
        /// </summary>
        [Range(0, 5)]
        [Tooltip(
            "How much will lateral slip contribute to the particle emission.\r\nIgnored when particle type for the surface is set to other than Smoke.")]
        public float lateralSlipParticleCoeff = 1f;

        /// <summary>
        ///     How much will longitudinal slip contribute to the particle emission.
        ///     Ignored when particle type for the surface is set to other than Smoke.
        /// </summary>
        [Range(0, 5)]
        [Tooltip(
            "How much will longitudinal slip contribute to the particle emission.\r\nIgnored when particle type for the surface is set to other than Smoke.")]
        public float longitudinalSlipParticleCoeff = 1f;

        /// <summary>
        ///     Particle size multiplier specific to this vehicle.
        ///     Use to adjust particle size on per-vehicle basis.
        ///     For global particle size adjustment for individual surfaces check SurfacePresets.
        /// </summary>
        [Range(0, 2)]
        [Tooltip(
            "Particle size multiplier specific to this vehicle.\r\nUse to adjust particle size on per-vehicle basis.\r\nFor global particle size adjustment for individual surfaces check SurfacePresets.")]
        public float particleSizeCoeff = 1f;

        /// <summary>
        ///     Emission rate multiplier specific to this vehicle.
        ///     Use to adjust emission on per-vehicle basis.
        ///     For global emission adjustment for individual surfaces check SurfacePresets.
        /// </summary>
        [Range(0, 2)]
        [Tooltip(
            "Emission rate multiplier specific to this vehicle.\r\nUse to adjust emission on per-vehicle basis.\r\nFor global emission adjustment for individual surfaces check SurfacePresets.")]
        public float emissionRateCoeff = 1f;

        /// <summary>
        ///     When enabled the particle system will either emit or not emit, with no in-between. Also removes any smoothing.
        /// </summary>
        [Tooltip(
            "When enabled the particle system will either emit or not emit, with no in-between. Also removes any smoothing.")]
        public bool binaryEmission;

        [SerializeField]
        private List<SurfaceParticleSystem> particleSystems = new List<SurfaceParticleSystem>();


        protected override void VC_Initialize()
        {
            for (int i = 0; i < vehicleController.powertrain.wheels.Count; i++)
            {
                WheelComponent wheelWrapper = vehicleController.powertrain.wheels[i];
                SurfaceParticleSystem surfaceParticleSystem = new SurfaceParticleSystem();
                surfaceParticleSystem.Initialize(vehicleController, wheelWrapper);
                particleSystems.Add(surfaceParticleSystem);
            }

            base.VC_Initialize();
        }


        public override void VC_Update()
        {
            base.VC_Update();

            int psCount = particleSystems.Count;
            for (int i = 0; i < psCount; i++)
            {
                SurfaceParticleSystem sps = particleSystems[i];
                sps.longitudinalSlipCoeff = longitudinalSlipParticleCoeff;
                sps.lateralSlipCoeff = lateralSlipParticleCoeff;
                sps.particleSizeCoeff = particleSizeCoeff;
                sps.emissionRateCoeff = emissionRateCoeff;
                sps.binaryEmission = binaryEmission;
            }
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                int psCount = particleSystems.Count;
                for (int i = 0; i < psCount; i++)
                {
                    particleSystems[i].Enable();
                }

                return true;
            }

            return false; ;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                int psCount = particleSystems.Count;
                for (int i = 0; i < psCount; i++)
                {
                    particleSystems[i].Disable();
                }

                return true;
            }

            return false;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Effects
{
    [CustomPropertyDrawer(typeof(SurfaceParticleManager))]
    public partial class SurfaceParticleManagerDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("longitudinalSlipParticleCoeff");
            drawer.Field("lateralSlipParticleCoeff");
            drawer.Field("particleSizeCoeff");
            drawer.Field("emissionRateCoeff");
            drawer.Field("binaryEmission");
            drawer.Info("Surface specific particle settings are adjusted through SurfacePresets.");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
