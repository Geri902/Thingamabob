using System;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of an engine starting / stopping.
    ///     Plays while start is active.
    ///     Clip at index 0 should be an engine starting sound, clip at 1 should be an engine stopping sound (optional).
    /// </summary>
    [Serializable]
    public partial class EngineStartStopComponent : SoundComponent
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
            get { return 50; }
        }


        [Range(0.1f, 2f)]
        public float pitch = 1f;


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.powertrain.engine.onStart.AddListener(PlayStarting);
                vehicleController.powertrain.engine.onStop.AddListener(PlayStopping);

                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                vehicleController.powertrain.engine.onStart.RemoveListener(PlayStarting);
                vehicleController.powertrain.engine.onStop.RemoveListener(PlayStopping);

                return true;
            }

            return false;
        }


        public override void VC_Update()
        {
            base.VC_Update();

            if (vehicleController.powertrain.engine.starterActive)
            {
                float newPitch = vehicleController.powertrain.engine.OutputRPM / vehicleController.powertrain.engine.idleRPM;
                newPitch = Mathf.Clamp01(newPitch) * pitch;
                SetPitch(newPitch);
            }
        }


        public virtual void PlayStarting()
        {
            SetVolume(baseVolume);
            SetPitch(pitch);
            vehicleController.StartCoroutine(PlayForDurationCoroutine(0, vehicleController.powertrain.engine.startDuration));
        }


        public virtual void PlayStopping()
        {
            if (!vehicleController.powertrain.engine.IsRunning)
            {
                return;
            }

            SetVolume(baseVolume);
            SetPitch(pitch);
            if (source.enabled) Play(1);
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.2f;
            if (Clip == null)
            {
                AddDefaultClip("EngineStart");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(EngineStartStopComponent))]
    public partial class EngineStartStopComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("pitch");


            drawer.EndProperty();
            return true;
        }
    }
}

#endif

