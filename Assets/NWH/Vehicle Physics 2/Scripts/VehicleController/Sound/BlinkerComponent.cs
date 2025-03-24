using System;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Effects;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Click-clack of the working blinker.
    ///     Accepts two clips, first is for the blinker turning on and the second is for blinker turning off.
    /// </summary>
    [Serializable]
    public partial class BlinkerComponent : SoundComponent
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
            get { return 180; }
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                if (vehicleController.effectsManager.lightsManager.leftBlinkers.lightSources.Count > 0)
                {
                    LightSource ls = vehicleController.effectsManager.lightsManager.leftBlinkers.lightSources[0];
                    ls.onLightTurnedOn.AddListener(PlayBlinkerOn);
                    ls.onLightTurnedOff.AddListener(PlayBlinkerOff);
                }

                if (vehicleController.effectsManager.lightsManager.rightBlinkers.lightSources.Count > 0)
                {
                    LightSource ls = vehicleController.effectsManager.lightsManager.rightBlinkers.lightSources[0];
                    ls.onLightTurnedOn.AddListener(PlayBlinkerOn);
                    ls.onLightTurnedOff.AddListener(PlayBlinkerOff);
                }

                return true;
            }
            return false;
        }

        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                if (vehicleController.effectsManager.lightsManager.leftBlinkers.lightSources.Count > 0)
                {
                    LightSource ls = vehicleController.effectsManager.lightsManager.leftBlinkers.lightSources[0];
                    ls.onLightTurnedOn.RemoveListener(PlayBlinkerOn);
                    ls.onLightTurnedOff.RemoveListener(PlayBlinkerOff);
                }

                if (vehicleController.effectsManager.lightsManager.rightBlinkers.lightSources.Count > 0)
                {
                    LightSource ls = vehicleController.effectsManager.lightsManager.rightBlinkers.lightSources[0];
                    ls.onLightTurnedOn.RemoveListener(PlayBlinkerOn);
                    ls.onLightTurnedOff.RemoveListener(PlayBlinkerOff);
                }

                return true;
            }
            return false;
        }

        private void PlayBlinkerOn()
        {
            SetVolume(baseVolume);
            Play(0);
        }


        private void PlayBlinkerOff()
        {
            SetVolume(baseVolume);
            Play(1);
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.8f;
            if (Clip == null)
            {
                AddDefaultClip("BlinkerOn");
                AddDefaultClip("BlinkerOff");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(BlinkerComponent))]
    public partial class BlinkerComponentDrawer : SoundComponentDrawer
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
