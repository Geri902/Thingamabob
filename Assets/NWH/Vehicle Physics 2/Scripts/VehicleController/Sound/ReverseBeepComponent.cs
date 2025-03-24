using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [Serializable]
    public partial class ReverseBeepComponent : SoundComponent
    {
        public override GameObject ContainerGO
        {
            get { return vehicleController.soundManager.otherSourceGO; }
        }

        public override AudioMixerGroup AudioMixerGroup
        {
            get { return vehicleController.soundManager.otherMixerGroup; }
        }

        public override bool InitLoop
        {
            get { return false; }
        }

        public override int Priority
        {
            get { return 160; }
        }


        /// <summary>
        /// Should beeping happen on negative velocity, ignoring the current gear?
        /// </summary>
        public bool beepOnNegativeVelocity = true;

        /// <summary>
        /// Should beeping happen when in reverse?
        /// </summary>
        public bool beepOnReverseGear = true;

        private Coroutine _beepCoroutine;


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                _beepCoroutine = vehicleController.StartCoroutine(BeepCoroutine());
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                if (_beepCoroutine != null) vehicleController.StopCoroutine(_beepCoroutine);
                return true;
            }

            return false;
        }

        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            if (Clip == null)
            {
                AddDefaultClip("ReverseBeep");
            }
        }

        IEnumerator BeepCoroutine()
        {
            while (true)
            {
                int gear = vehicleController.powertrain.transmission.Gear;
                bool beepReverse = beepOnReverseGear && gear < 0;
                bool beepNegativeVelocity = beepOnNegativeVelocity && vehicleController.LocalForwardVelocity < -0.2f;

                if (beepReverse || beepNegativeVelocity)
                {
                    SetVolume(baseVolume);
                    Play();
                }
                else
                {
                    Stop();
                }

                yield return new WaitForSeconds(1f);
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(ReverseBeepComponent))]
    public partial class ReverseBeepComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("beepOnReverseGear");
            drawer.Field("beepOnNegativeVelocity");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif

