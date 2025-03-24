using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Controls particle emitters that represent exhaust smoke based on engine state.
    /// </summary>
    [Serializable]
    public partial class ExhaustSmoke : Effect
    {
        /// <summary>
        ///     How much soot is emitted when throttle is pressed.
        /// </summary>
        [Range(0, 1)] public float sootIntensity = 0.4f;

        /// <summary>
        ///     Particle start speed is multiplied by this value based on engine RPM.
        /// </summary>
        [Range(1, 5)] public float maxSpeedMultiplier = 1.4f;

        /// <summary>
        ///     Particle start size is multiplied by this value based on engine RPM.
        /// </summary>
        [Range(1, 5)] public float maxSizeMultiplier = 1.2f;

        /// <summary>
        ///     Normal particle start color. Used when there is no throttle - engine is under no load.
        /// </summary>
        [Tooltip("    Normal particle start color. Used when there is no throttle - engine is under no load.")]
        public Color normalColor = new Color(0.6f, 0.6f, 0.6f, 0.3f);

        /// <summary>
        ///     Soot particle start color. Used under heavy throttle - engine is under load.
        /// </summary>
        [Tooltip("    Soot particle start color. Used under heavy throttle - engine is under load.")]
        public Color sootColor = new Color(0.1f, 0.1f, 0.8f);

        /// <summary>
        ///     List of exhaust particle systems.
        /// </summary>
        [Tooltip("    List of exhaust particle systems.")]
        public List<ParticleSystem> particleSystems = new List<ParticleSystem>();

        private float _initStartSpeedMin;
        private float _initStartSpeedMax;
        private float _initStartSizeMin;
        private float _initStartSizeMax;
        private float _sootAmount;
        private ParticleSystem.EmissionModule _emissionModule;
        private ParticleSystem.MainModule _mainModule;
        private ParticleSystem.MinMaxCurve _minMaxCurve;
        private float _vehicleSpeed;


        protected override void VC_Initialize()
        {
            foreach (ParticleSystem ps in particleSystems)
            {
                if (ps == null)
                {
                    Debug.LogError($"One or more of the exhaust ParticleSystems on the vehicle {vehicleController.name} is null.");
                }
            }

            if (particleSystems == null || particleSystems.Count == 0)
            {
                return;
            }

            _emissionModule = particleSystems[0].emission;
            _mainModule = particleSystems[0].main;

            _initStartSpeedMin = _mainModule.startSpeed.constantMin;
            _initStartSpeedMax = _mainModule.startSpeed.constantMax;
            _initStartSizeMin = _mainModule.startSize.constantMin;
            _initStartSizeMax = _mainModule.startSize.constantMax;

            maxSizeMultiplier = Mathf.Clamp(maxSizeMultiplier, 1f, Mathf.Infinity);
            maxSpeedMultiplier = Mathf.Clamp(maxSpeedMultiplier, 1f, Mathf.Infinity);

            base.VC_Initialize();
        }


        public override void VC_Update()
        {
            base.VC_Update();
            if (vehicleController.powertrain.IsActive && vehicleController.powertrain.engine.IsRunning)
            {
                _vehicleSpeed = vehicleController.Speed;

                foreach (ParticleSystem ps in particleSystems)
                {
                    if (!ps.isPlaying)
                    {
                        ps.Play();
                    }

                    _emissionModule = ps.emission;
                    _mainModule = ps.main;

                    float engineLoad = vehicleController.powertrain.engine.Load;
                    float rpmPercent = vehicleController.powertrain.engine.RPMPercent;

                    if (!_emissionModule.enabled)
                    {
                        _emissionModule.enabled = true;
                    }

                    // Color
                    _sootAmount = engineLoad * sootIntensity;
                    _mainModule.startColor = Color.Lerp(
                        _mainModule.startColor.color,
                        Color.Lerp(normalColor, sootColor, _sootAmount),
                        Time.deltaTime * 7f);

                    // Speed
                    float speedMultiplier = maxSpeedMultiplier - 1f;
                    _minMaxCurve = _mainModule.startSpeed;
                    _minMaxCurve.constantMin = _initStartSpeedMin + rpmPercent * speedMultiplier;
                    _minMaxCurve.constantMax = _initStartSpeedMax + rpmPercent * speedMultiplier;
                    _mainModule.startSpeed = _minMaxCurve;

                    // Size
                    float sizeMultiplier = maxSizeMultiplier - 1f;
                    _minMaxCurve = _mainModule.startSize;
                    _minMaxCurve.constantMin = _initStartSizeMin + rpmPercent * sizeMultiplier;
                    _minMaxCurve.constantMax = _initStartSizeMax + rpmPercent * sizeMultiplier;
                    _mainModule.startSize = _minMaxCurve;
                }
            }
            else
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    if (ps.isPlaying)
                    {
                        ps.Stop();
                    }

                    ParticleSystem.EmissionModule emission = ps.emission;
                    emission.enabled = false;
                }
            }
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    ps.Play();
                }

                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    ParticleSystem.EmissionModule emission = ps.emission;
                    ps.Stop();
                }

                return true;
            }

            return false;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Effects
{
    [CustomPropertyDrawer(typeof(ExhaustSmoke))]
    public partial class ExhaustSmokeDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Particle Settings");
            drawer.Field("normalColor");
            drawer.Field("sootColor");
            drawer.Field("sootIntensity");
            drawer.Field("maxSizeMultiplier");
            drawer.Field("maxSpeedMultiplier");
            drawer.ReorderableList("particleSystems");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
