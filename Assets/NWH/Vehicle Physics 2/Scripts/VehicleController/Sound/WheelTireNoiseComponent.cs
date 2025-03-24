using System;
using NWH.VehiclePhysics2.GroundDetection;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using NWH.Common.Vehicles;


#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sounds produced by tire rolling over the surface.
    /// </summary>
    [Serializable]
    public partial class WheelTireNoiseComponent : SoundComponent
    {
        public override GameObject ContainerGO
        {
            get { return vehicleController.soundManager.otherSourceGO; }
        }

        public override AudioMixerGroup AudioMixerGroup
        {
            get { return vehicleController.soundManager.surfaceNoiseMixerGroup; }
        }

        public override int Priority
        {
            get { return 90; }
        }

        public override bool InitPlayOnAwake
        {
            get { return true; }
        }

        public override bool InitLoop
        {
            get { return true; }
        }

        public override bool InitializeWithNoClips
        {
            get { return true; }
        }


        private float _prevPitch;
        private float _prevVolume;


        public override void VC_Update()
        {
            base.VC_Update();
            if (!vehicleController.groundDetection.state.isEnabled)
            {
                return;
            }

            float newVolume = 0f;
            float newPitch = 0f;

            SurfacePreset dominantSurfacePreset = vehicleController.groundDetection.DominantSurfacePreset;
            if (dominantSurfacePreset == null || dominantSurfacePreset.surfaceSoundClip == null || !dominantSurfacePreset.playSurfaceSounds)
            {
                Stop();
                return;
            }

            source.clip = dominantSurfacePreset.surfaceSoundClip;

            for (int i = 0; i < vehicleController.powertrain.wheelCount; i++)
            {
                WheelComponent wheelComponent = vehicleController.powertrain.wheels[i];

                if (wheelComponent.wheelUAPI.IsGrounded)
                {
                    float surfaceModifier = 1f;
                    if (dominantSurfacePreset.slipSensitiveSurfaceSound)
                    {
                        surfaceModifier = wheelComponent.wheelUAPI.NormalizedLateralSlip / vehicleController.longitudinalSlipThreshold;
                        surfaceModifier = surfaceModifier < 0 ? 0 : surfaceModifier > 1 ? 1 : surfaceModifier;
                    }

                    float speedCoeff = vehicleController.Speed * 0.03f;
                    speedCoeff = speedCoeff < 0 ? 0 : speedCoeff > 1 ? 1 : speedCoeff;

                    // Change surface volume and pitch
                    float wheelVolume = dominantSurfacePreset.surfaceSoundVolume * surfaceModifier * speedCoeff;
                    wheelVolume = wheelVolume < 0 ? 0 : wheelVolume > 1 ? 1 : wheelVolume;
                    newVolume = Mathf.Max(newVolume, wheelVolume);

                    float wheelPitch = dominantSurfacePreset.surfaceSoundPitch * 0.5f + speedCoeff;
                    newPitch = Mathf.Max(newPitch, wheelPitch);
                }
            }

            newVolume = Mathf.Lerp(_prevVolume, newVolume, vehicleController.deltaTime * 20f);
            SetVolume(newVolume);
            _prevVolume = newVolume;

            newPitch = Mathf.Lerp(_prevPitch, newPitch, vehicleController.deltaTime * 20f);
            SetPitch(newPitch);
            _prevPitch = newPitch;

            if (newVolume < Vehicle.KINDA_SMALL_NUMBER && source.isPlaying)
            {
                Stop();
            }
            else if (!source.isPlaying)
            {
                Play();
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(WheelTireNoiseComponent))]
    public partial class WheelTireNoiseComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool initGUIState = GUI.enabled;
            GUI.enabled = false;
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Info("Check SurfaceMaps to change per-surface clips and settings.");


            GUI.enabled = initGUIState;


            drawer.EndProperty();
            return true;
        }
    }
}
#endif
