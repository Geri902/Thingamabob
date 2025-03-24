using System;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Vehicle horn sound.
    /// </summary>
    [Serializable]
    public partial class HornComponent : SoundComponent
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
            get { return true; }
        }

        public override int Priority
        {
            get { return 200; }
        }


        public override void VC_Update()
        {
            base.VC_Update();
            if (vehicleController.input.Horn)
            {
                SetVolume(baseVolume);
                if (!source.isPlaying)
                {
                    Play();
                }
            }
            else if (source.isPlaying)
            {
                Stop();
            }
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            if (Clip == null)
            {
                AddDefaultClip("Horn");
            }
        }
    }
}



#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(HornComponent))]
    public partial class HornComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }


            drawer.EndProperty();
            return true;
        }
    }
}

#endif
