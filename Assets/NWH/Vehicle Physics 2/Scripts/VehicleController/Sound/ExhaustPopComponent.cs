using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using NWH.Common.Vehicles;


#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of exhaust popping.
    ///     Requires exhaust flash to be enabled to work.
    /// </summary>
    [Serializable]
    public partial class ExhaustPopComponent : SoundComponent
    {
        public override GameObject ContainerGO
        {
            get { return vehicleController.soundManager.exhaustSourceGO; }
        }

        public override AudioMixerGroup AudioMixerGroup
        {
            get { return vehicleController.soundManager.engineMixerGroup; }
        }

        public override bool InitLoop
        {
            get { return false; }
        }

        public override int Priority
        {
            get { return 160; }
        }

        public enum PopSource
        {
            RevLimiter,
            ExhaustFlash,
        }

        /// <summary>
        /// The source for the pop trigger. 
        /// If ExhaustFlash is selected, ExhaustFlash effect needs to be set up for this to work.
        /// </summary>
        [UnityEngine.Tooltip("The source for the pop trigger. \r\nIf ExhaustFlash is selected, ExhaustFlash effect needs to be set up for this to work.")]
        public PopSource popSource = PopSource.ExhaustFlash;

        /// <summary>
        /// Each time there is an exhaust flash or rev limiter is hit, what is the chance of exhaust pop?
        /// </summary>
        [UnityEngine.Tooltip("Each time there is an exhaust flash or rev limiter is hit, what is the chance of exhaust pop?")]
        public float popChance = 0.1f;

        /// <summary>
        /// Should pops happen randomly when the vehicle is decelerating with throttle released.
        /// </summary>
        [UnityEngine.Tooltip("Should pops happen randomly when the vehicle is decelerating with throttle released.")]
        public bool popOnDeceleration = true;

        /// <summary>
        /// The amount of pops under deceleration.
        /// </summary>
        [UnityEngine.Tooltip("The amount of pops under deceleration.")]
        public float decelerationPopChanceCoeff = 1f;


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                if (popSource == PopSource.RevLimiter)
                {
                    vehicleController.powertrain.engine.onRevLimiter.AddListener(Pop);
                }
                else if (popSource == PopSource.ExhaustFlash)
                {
                    vehicleController.effectsManager.exhaustFlash.onFlash.AddListener(Pop);
                }

                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                vehicleController.powertrain.engine.onRevLimiter.RemoveListener(Pop);
                vehicleController.effectsManager.exhaustFlash.onFlash.RemoveListener(Pop);

                return true;
            }

            return false;
        }


        public override void VC_FixedUpdate()
        {
            base.VC_FixedUpdate();
            if (popOnDeceleration && vehicleController.powertrain.engine.ThrottlePosition < Vehicle.INPUT_DEADZONE && vehicleController.powertrain.engine.RPMPercent > 0.4f)
            {
                if (Random.Range(0f, 1f) < popChance * decelerationPopChanceCoeff * vehicleController.fixedDeltaTime)
                {
                    SetVolume(baseVolume * 0.5f + vehicleController.powertrain.engine.RPMPercent * 0.5f);
                    Pop();
                }
            }
        }


        public void Pop()
        {
            if (Random.Range(0f, 1f) > popChance)
            {
                return;
            }

            Stop();
            SetVolume(Random.Range(baseVolume * 0.5f, baseVolume * 1.5f));
            SetPitch(Random.Range(0.7f, 1.3f));
            PlayRandomClip();
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            baseVolume = 0.3f;

            if (Clip == null)
            {
                AddDefaultClip("ExhaustPop");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(ExhaustPopComponent))]
    public partial class ExhaustPopComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("popSource");
            drawer.Field("popChance");
            drawer.Field("popOnDeceleration");
            drawer.Field("decelerationPopChanceCoeff");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif


