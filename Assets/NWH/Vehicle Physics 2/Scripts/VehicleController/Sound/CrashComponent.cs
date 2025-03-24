using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using NWH.VehiclePhysics2.Damage;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of vehicle crashing into an object.
    ///     Supports multiple audio clips of which one will be chosen at random each time this effect is played.
    /// </summary>
    [Serializable]
    public partial class CrashComponent : SoundComponent
    {
        public override GameObject ContainerGO
        {
            get { return vehicleController.soundManager.crashSourceGO; }
        }

        public override AudioMixerGroup AudioMixerGroup
        {
            get { return vehicleController.soundManager.otherMixerGroup; }
        }

        public override int Priority
        {
            get { return 80; }
        }


        /// <summary>
        ///     Different random pitch in range [basePitch + (1 +- pitchRandomness)] is set each time a collision happens.
        /// </summary>
        [Range(0, 0.5f)]
        [Tooltip(
            "Different random pitch in range [basePitch + (1 +- pitchRandomness)] is set each time a collision happens.")]
        public float pitchRandomness = 0.4f;

        /// <summary>
        ///     Higher values result in collisions getting louder for the given collision velocity magnitude.
        /// </summary>
        [Range(0, 5)]
        [Tooltip("    Higher values result in collisions getting louder for the given collision velocity magnitude.")]
        public float velocityMagnitudeEffect = 1f;


        protected override void VC_Initialize()
        {
            CreateAndRegisterAudioSource(vehicleController.soundManager.otherMixerGroup, vehicleController.soundManager.crashSourceGO);

            base.VC_Initialize();
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.onCollision.AddListener(PlayCollisionSound);
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                vehicleController.onCollision.RemoveListener(PlayCollisionSound);
                return true;
            }

            return false;
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.4f;
            if (Clip == null)
            {
                AddDefaultClip("Crash");
            }
        }


        public void PlayCollisionSound(Collision collision)
        {
            if (!IsActive)
            {
                return;
            }

            if (collision == null || collision.contacts.Length == 0)
            {
                return;
            }

            vehicleController.soundManager.crashSourceGO.transform.position = collision.contacts[0].point;

            float newVolume =
                Mathf.Clamp01(collision.relativeVelocity.magnitude * 0.2f * velocityMagnitudeEffect) *
                baseVolume;
            newVolume = Mathf.Clamp01(newVolume);
            float newPitch = Random.Range(1f - pitchRandomness, 1f + pitchRandomness);

            SetVolume(newVolume);
            SetPitch(newPitch);
            PlayRandomClip();
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(CrashComponent))]
    public partial class CrashComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("pitchRandomness");
            drawer.Field("velocityMagnitudeEffect");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif


