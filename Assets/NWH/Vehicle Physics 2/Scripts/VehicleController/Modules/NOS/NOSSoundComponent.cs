using System;
using NWH.VehiclePhysics2.Sound.SoundComponents;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.NOS
{
    /// <summary>
    ///     Sound component producing the distinct 'hiss' sound of active NOS.
    /// </summary>
    [Serializable]
    public partial class NOSSoundComponent : SoundComponent
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
            get { return 90; }
        }

        [NonSerialized]
        public NOSModule nosModule;

        public override bool InitLoop
        {
            get { return true; }
        }


        public override void VC_Update()
        {
            base.VC_Update();

            if (nosModule.IsUsingNOS)
            {
                SetVolume(baseVolume);
                if (!source.isPlaying)
                {
                    Play();
                }
            }
            else
            {
                Stop();
            }
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.2f;
            if (Clip == null)
            {
                Clip = Resources.Load(VehicleController.DEFAULT_RESOURCES_PATH + "Sound/NOS") as AudioClip;
                if (Clip == null)
                {
                    Debug.LogWarning(
                        $"Audio Clip for sound component {GetType().Name}  from resources. Source will not play.");
                }
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.NOS
{
    [CustomPropertyDrawer(typeof(NOSSoundComponent))]
    public partial class NOSSoundComponentDrawer : SoundComponentDrawer
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
