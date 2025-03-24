using System;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     EngineFanComponent is used to imitate engine fan running, the sound especially prominent in commercial vehicles and
    ///     off-road vehicles with clutch driven fan.
    ///     Can also be used to mimic induction noise.
    /// </summary>
    [Serializable]
    public partial class EngineFanComponent : SoundComponent
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
            get { return 100; }
        }

        /// <summary>
        /// Starting sound pitch at idle RPM.
        /// </summary>
        [UnityEngine.Tooltip("Starting sound pitch at idle RPM.")]
        public float basePitch = 1f;

        /// <summary>
        /// Pitch range, redline pitch equals basePitch + pitchRange.
        /// </summary>
        [Range(0, 4)]
        [UnityEngine.Tooltip("Pitch range, redline pitch equals basePitch + pitchRange.")]
        public float pitchRange = 0.5f;

        public override bool InitLoop
        {
            get { return true; }
        }


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
                else
                {
                    Stop();
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

                return true;
            }

            return false;
        }


        public override void VC_Update()
        {
            base.VC_Update();
            float rpmPercent = vehicleController.powertrain.engine.RPMPercent;
            SetVolume(rpmPercent * rpmPercent * baseVolume);
            SetPitch(basePitch + pitchRange * rpmPercent);
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.05f;
            if (Clip == null)
            {
                AddDefaultClip("EngineFan");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(EngineFanComponent))]
    public partial class EngineFanComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("basePitch");
            drawer.Field("pitchRange");

            drawer.EndProperty();
            return true;
        }
    }
}
#endif

