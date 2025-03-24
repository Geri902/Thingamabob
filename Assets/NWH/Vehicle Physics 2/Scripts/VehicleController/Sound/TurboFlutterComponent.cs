using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of a wastegate releasing air on turbocharged vehicles.
    /// </summary>
    [Serializable]
    public partial class TurboFlutterComponent : SoundComponent
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
            get { return 120; }
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.powertrain.engine.forcedInduction.onWastegateRelease.AddListener(PlayFlutterSound);
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                vehicleController.powertrain.engine.forcedInduction.onWastegateRelease.RemoveListener(PlayFlutterSound);
                return true;
            }

            return false;
        }


        private void PlayFlutterSound(float wastegateBoost)
        {
            if (!source.isPlaying)
            {
                float newVolume = baseVolume * wastegateBoost * wastegateBoost * Random.Range(0.7f, 1.3f);
                SetVolume(Mathf.Clamp01(newVolume));
                PlayRandomClip();

            }
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.006f;
            if (Clip == null)
            {
                AddDefaultClip("TurboFlutter");
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(TurboFlutterComponent))]
    public partial class TurboFlutterComponentDrawer : SoundComponentDrawer
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
