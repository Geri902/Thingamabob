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
    ///     Sound produced by tire skidding over surface.
    /// </summary>
    [Serializable]
    public partial class WheelSkidComponent : SoundComponent
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
            get { return 60; }
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


        private float _prevVolume;


        public override void VC_Update()
        {
            base.VC_Update();

            if (vehicleController.groundDetection.state.isEnabled)
            {
                float newVolume = 0f;
                SurfacePreset dominantSurfacePreset = vehicleController.groundDetection.DominantSurfacePreset;

                if (dominantSurfacePreset != null && dominantSurfacePreset.skidSoundClip != null && dominantSurfacePreset.playSkidSounds)
                {
                    source.clip = dominantSurfacePreset.skidSoundClip;

                    for (int i = 0; i < vehicleController.powertrain.wheelCount; i++)
                    {
                        WheelComponent wheelComponent = vehicleController.powertrain.wheels[i];

                        bool isGrounded = wheelComponent.wheelUAPI.IsGrounded;
                        if (!isGrounded) continue;

                        bool isSkidding = wheelComponent.wheelUAPI.IsSkiddingLaterally ||
                                          wheelComponent.wheelUAPI.IsSkiddingLongitudinally;
                        if (!isSkidding) continue;

                        // Skidding and grounded, calculate new volume value
                        float slipPercent = Mathf.Clamp01(wheelComponent.wheelUAPI.NormalizedLateralSlip +
                                                          wheelComponent.wheelUAPI.NormalizedLongitudinalSlip);
                        float speedCoeff = Mathf.Min(1f, vehicleController.Speed * 0.33f + wheelComponent.wheelUAPI.AngularVelocity * 0.05f);
                        float wheelVolume = slipPercent * dominantSurfacePreset.skidSoundVolume * speedCoeff;
                        newVolume = newVolume > wheelVolume ? newVolume : wheelVolume;
                    }

                    newVolume = Mathf.Lerp(_prevVolume, newVolume, vehicleController.deltaTime * 10f);
                    SetVolume(newVolume);
                    _prevVolume = newVolume;

                    if (newVolume < Vehicle.KINDA_SMALL_NUMBER && source.isPlaying)
                    {
                        Stop();
                    }
                    else if (!source.isPlaying)
                    {
                        Play();
                    }
                }
                else
                {
                    Stop();
                }
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(WheelSkidComponent))]
    public partial class WheelSkidComponentDrawer : SoundComponentDrawer
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
