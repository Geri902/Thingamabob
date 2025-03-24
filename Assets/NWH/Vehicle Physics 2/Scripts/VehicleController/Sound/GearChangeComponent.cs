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
    ///     Shifter sound played when changing gears.
    ///     Supports multiple audio clips of which one is chosen at random each time this effect is played.
    /// </summary>
    [Serializable]
    public partial class GearChangeComponent : SoundComponent
    {
        public override GameObject ContainerGO
        {
            get { return vehicleController.soundManager.transmissionSourceGO; }
        }

        public override AudioMixerGroup AudioMixerGroup
        {
            get { return vehicleController.soundManager.transmissionMixerGroup; }
        }

        public override int Priority
        {
            get { return 160; }
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.powertrain.transmission.onShift.AddListener(PlayShiftSound);
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                vehicleController.powertrain.transmission.onShift.RemoveListener(PlayShiftSound);
                return true;
            }

            return false;
        }


        private void PlayShiftSound()
        {
            if (vehicleController.powertrain.transmission.Gear != 0)
            {
                SetVolume(baseVolume);
                PlayRandomClip();
            }
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.16f;
            if (Clip == null)
            {
                AddDefaultClip("GearChange");
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(GearChangeComponent))]
    public partial class GearChangeComponentDrawer : SoundComponentDrawer
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

