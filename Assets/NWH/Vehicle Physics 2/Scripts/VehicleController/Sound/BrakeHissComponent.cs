using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Imitates brake hiss on vehicles with pneumatic brake systems such as trucks and buses.
    ///     Accepts multiple clips of which one will be chosen at random each time this effect is played.
    /// </summary>
    [Serializable]
    public partial class AirBrakeComponent : SoundComponent
    {
        public override GameObject ContainerGO
        {
            get { return vehicleController.soundManager.otherSourceGO; }
        }

        public override AudioMixerGroup AudioMixerGroup
        {
            get { return vehicleController.soundManager.otherMixerGroup; }
        }

        public override int Priority
        {
            get { return 150; }
        }

        /// <summary>
        ///     Minimum time between two plays.
        /// </summary>
        [Tooltip("    Minimum time between two plays.")]
        public float minInterval = 4f;

        private float _timer;


        public override void VC_Update()
        {
            base.VC_Update();
            _timer += Time.deltaTime;
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.brakes.onBrakesDeactivate.AddListener(PlayBrakeHiss);
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                vehicleController.brakes.onBrakesDeactivate.RemoveListener(PlayBrakeHiss);
                return true;
            }

            return false;
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.1f;
            if (Clip == null)
            {
                AddDefaultClip("AirBrakes");
            }
        }


        public void PlayBrakeHiss()
        {
            if (_timer < minInterval || !vehicleController.powertrain.engine.IsRunning)
            {
                return;
            }

            SetVolume(Random.Range(0.8f, 1.2f) * baseVolume);
            if (!source.isPlaying)
            {
                PlayRandomClip();
            }

            _timer = 0f;
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(AirBrakeComponent))]
    public partial class AirBrakeComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("minInterval", true, "s");

            drawer.EndProperty();
            return true;
        }
    }
}
#endif

