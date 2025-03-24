using System;
using UnityEngine;
using UnityEngine.Audio;
using NWH.Common.Vehicles;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of vehicle transmission.
    ///     Most prominent on rally and racing cars with straight cut gears in the gearbox.
    /// </summary>
    [Serializable]
    public partial class TransmissionWhineComponent : SoundComponent
    {
        public override GameObject ContainerGO
        {
            get { return vehicleController.soundManager.transmissionSourceGO; }
        }

        public override AudioMixerGroup AudioMixerGroup
        {
            get { return vehicleController.soundManager.transmissionMixerGroup; }
        }

        public override bool InitPlayOnAwake
        {
            get { return false; }
        }

        public override int Priority
        {
            get { return 90; }
        }

        public override bool InitLoop
        {
            get { return true; }
        }

        /// <summary>
        ///     Maximum speed value [m/s] of the vehicle at which the pitch will be at the top end of the pitchRange.
        /// </summary>
        [Tooltip(
            "Maximum speed value [m/s] of the vehicle at which the pitch will be at the top end of the pitchRange.")]
        public float maxSpeed = 80f;

        /// <summary>
        ///     Volume range between transmission being under no load (baseVolume) or full load (baseVolume + volumeRange).
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Volume coefficient when transmission is not under load.")]
        public float volumeRange = 0.2f;

        /// <summary>
        ///     Starting pitch value.
        /// </summary>
        [UnityEngine.Tooltip("    Starting pitch value.")]
        public float basePitch = 0.2f;

        /// <summary>
        ///     Pitch range that will be added to the base pitch depending on transmission state.
        /// </summary>
        [Range(0f, 5f)]
        [Tooltip("    Pitch range that will be added to the base pitch depending on transmission state.")]
        public float pitchRange = 0.7f;


        public override void VC_Update()
        {
            base.VC_Update();
            float speed = vehicleController.Speed;

            float newPitch = basePitch;
            if (vehicleController.powertrain.transmission.Gear != 0)
            {
                newPitch += Mathf.Clamp01(speed / maxSpeed) * pitchRange;
            }
            SetPitch(newPitch);

            float speedCoeff = Mathf.Clamp01(Mathf.Abs(speed) * 0.8f);
            float newVolume = baseVolume + vehicleController.powertrain.engine.Load * volumeRange;
            newVolume *= speedCoeff;
            SetVolume(newVolume);

            if (newVolume > Vehicle.KINDA_SMALL_NUMBER)
            {
                Play();
            }
            else
            {
                Stop();
            }
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.005f;
            volumeRange = 0.005f;
            if (Clip == null)
            {
                AddDefaultClip("TransmissionWhine");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(TransmissionWhineComponent))]
    public partial class TransmissionWhineComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("volumeRange");
            drawer.Field("basePitch");
            drawer.Field("pitchRange");
            drawer.Field("maxSpeed");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
