using System;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of an engine idling.
    /// </summary>
    [Serializable]
    public partial class EngineRunningComponent : SoundComponent
    {
        public override GameObject ContainerGO
        {
            get { return vehicleController.soundManager.engineSourceGO; }
        }

        public override AudioMixerGroup AudioMixerGroup
        {
            get { return vehicleController.soundManager.engineMixerGroup; }
        }

        public override int Priority
        {
            get { return 10; }
        }

        public override bool InitLoop
        {
            get { return true; }
        }


        /// <summary>
        ///     Distortion at maximum engine load.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Distortion at maximum engine load.")]
        public float maxDistortion = 0.4f;

        /// <summary>
        ///     Pitch added to the base engine pitch depending on engine RPM.
        /// </summary>
        [Range(0, 4)]
        [Tooltip("    Pitch added to the base engine pitch depending on engine RPM.")]
        public float pitchRange = 2f;

        /// <summary>
        /// The pitch that the component will have at 0RPM. Gets added to the pitch throughout the whole range.
        /// </summary>
        public float pitchOffset = 0.2f;

        /// <summary>
        ///     Smoothing of engine volume.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Smoothing of engine volume.")]
        public float smoothing = 0.05f;

        /// <summary>
        ///     Volume added to the base engine volume depending on engine state.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Volume added to the base engine volume depending on engine state.")]
        public float volumeRange = 0.1f;


        private float _volume;
        private float _volumeVelocity;
        private float _distortion;
        private float _distortionVelocity;


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.powertrain.engine.onStart.AddListener(Play);
                vehicleController.powertrain.engine.onStop.AddListener(Stop);

                if (vehicleController.powertrain.engine.IsRunning)
                {
                    Play();
                }

                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                vehicleController.powertrain.engine.onStart.RemoveListener(Play);
                vehicleController.powertrain.engine.onStop.RemoveListener(Stop);

                Stop();

                return true;
            }

            return false;
        }


        public override void VC_Update()
        {
            base.VC_Update();

            EngineComponent engine = vehicleController.powertrain.engine;
            float newPitch = (engine.RPMPercent * pitchRange) + pitchOffset;
            SetPitch(newPitch);

            float throttleInput = vehicleController.powertrain.engine.revLimiterActive ? 0.5f : engine.ThrottlePosition;
            float newDistortion = throttleInput * maxDistortion;
            _distortion = Mathf.SmoothDamp(_distortion, newDistortion, ref _distortionVelocity, smoothing);
            source.outputAudioMixerGroup.audioMixer.SetFloat("engineDistortion", _distortion);

            float newVolume = baseVolume;
            newVolume += engine.Load * volumeRange;
            newVolume -= _distortion;
            newVolume = Mathf.Clamp(newVolume, baseVolume, 2f);
            _volume = Mathf.SmoothDamp(_volume, newVolume, ref _volumeVelocity, smoothing);
            SetVolume(_volume);
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.5f;
            volumeRange = 0.4f;
            pitchRange = 1.8f;
            if (Clip == null)
            {
                AddDefaultClip("EngineRunning");
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(EngineRunningComponent))]
    public partial class EngineRunningComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("volumeRange");
            drawer.Field("pitchRange");
            drawer.Field("pitchOffset");
            drawer.Field("smoothing");
            drawer.Field("maxDistortion");

            drawer.EndProperty();
            return true;
        }
    }
}
#endif