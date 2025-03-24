using System;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of turbocharger or supercharger.
    /// </summary>
    [Serializable]
    public partial class TurboWhistleComponent : SoundComponent
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
            get { return 40; }
        }

        /// <summary>
        ///     Pitch range that will be added to the base pitch depending on turbos's RPM.
        /// </summary>
        [Range(0, 5)]
        [Tooltip("    Pitch range that will be added to the base pitch depending on turbos's RPM.")]
        public float pitchRange = 0.9f;

        public override bool InitLoop
        {
            get { return true; }
        }


        public override void VC_Update()
        {
            base.VC_Update();
            if (vehicleController.powertrain.engine.IsRunning &&
                vehicleController.powertrain.engine.forcedInduction.useForcedInduction)
            {
                SetVolume(Mathf.Clamp01(baseVolume
                                        * vehicleController.powertrain.engine.forcedInduction.boost * vehicleController.powertrain.engine.forcedInduction.boost));
                SetPitch(pitchRange * vehicleController.powertrain.engine.forcedInduction.boost);
                Play();
            }
            else
            {
                if (source != null)
                {
                    SetVolume(0);
                    Stop();
                }
            }
        }

        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.004f;
            pitchRange = 1f;

            if (Clip == null)
            {
                AddDefaultClip("TurboWhistle");
            }
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(TurboWhistleComponent))]
    public partial class TurboWhistleComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("pitchRange");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
